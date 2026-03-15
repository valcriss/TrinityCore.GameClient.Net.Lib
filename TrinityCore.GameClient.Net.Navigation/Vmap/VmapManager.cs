using System.Collections.Concurrent;
using System.Numerics;
using TrinityCore.GameClient.Net.Navigation.Abstractions;
using TrinityCore.GameClient.Net.Navigation.MmapEngine;

namespace TrinityCore.GameClient.Net.Navigation.Vmap;

internal sealed class VmapManager
{
    private const float TileSize = 533.33333333f;
    private const float CoordinateMid = 0.5f * 64.0f * TileSize;
    private const float MinBlockingM2HorizontalExtent = 2.5f;
    private const float MinBlockingM2VerticalExtent = 3.0f;
    private readonly string _vmapsRoot;
    private readonly ConcurrentDictionary<int, Lazy<VmapMapMetadata?>> _mapMetadataCache = new();
    private readonly ConcurrentDictionary<string, Lazy<VmapTile?>> _tileCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Lazy<VmapWorldModel>> _modelCache = new(StringComparer.OrdinalIgnoreCase);

    public VmapManager(string dataRoot)
    {
        ArgumentNullException.ThrowIfNull(dataRoot);
        _vmapsRoot = System.IO.Path.Combine(dataRoot, "vmaps");
    }

    public bool TryGetFirstCollision(
        int mapId,
        NavigationPoint start,
        NavigationPoint end,
        out VmapCollisionHit hit)
    {
        var startInternal = ToInternal(start);
        var endInternal = ToInternal(end);
        var delta = endInternal - startInternal;
        var maxDistance = delta.Length();
        if (maxDistance <= 0.001f)
        {
            hit = default;
            return false;
        }

        var direction = Vector3.Normalize(delta);
        return TryRaycastInternal(
            mapId,
            startInternal,
            direction,
            maxDistance,
            ignoreM2: false,
            out hit);
    }

    public bool TryGetHeight(
        int mapId,
        NavigationPoint point,
        float maxSearchDistance,
        out float worldHeight)
    {
        var originInternal = ToInternal(point);
        var direction = new Vector3(0.0f, 0.0f, -1.0f);
        if (TryRaycastInternal(
                mapId,
                originInternal,
                direction,
                maxSearchDistance,
                ignoreM2: true,
                out var hit))
        {
            worldHeight = point.Z - hit.Distance;
            NavTrace.WriteLine(
                $"NAV VMAP HEIGHT HIT map={mapId} model=\"{hit.ModelName}\" " +
                $"point=({point.X:F3},{point.Y:F3},{point.Z:F3}) z={worldHeight:F3} dist={hit.Distance:F3}");
            return true;
        }

        worldHeight = default;
        return false;
    }

    private bool TryRaycastInternal(
        int mapId,
        Vector3 originInternal,
        Vector3 directionInternal,
        float maxDistance,
        bool ignoreM2,
        out VmapCollisionHit hit)
    {
        hit = default;
        var metadata = GetMapMetadata(mapId);
        if (metadata is null)
        {
            return false;
        }

        if (!metadata.IsTiled)
        {
            NavTrace.WriteLine($"NAV VMAP MAP_UNSUPPORTED map={mapId} reason=non_tiled");
            return false;
        }

        var endInternal = originInternal + (directionInternal * maxDistance);
        var seenSpawnIds = new HashSet<uint>();
        var nearestDistance = maxDistance;
        var nearestHit = default(VmapCollisionHit);

        foreach (var tile in EnumerateTiles(mapId, originInternal, endInternal))
        {
            foreach (var spawn in tile.Spawns)
            {
                if (!seenSpawnIds.Add(spawn.Id))
                {
                    continue;
                }

                if (ignoreM2 && spawn.IsM2)
                {
                    continue;
                }

                if (!ignoreM2 && !spawn.BlocksLineOfSight)
                {
                    continue;
                }

                if (!spawn.BoundsInternal.TryIntersectRay(originInternal, directionInternal, nearestDistance, out _, out _))
                {
                    continue;
                }

                var model = GetWorldModel(spawn.Name);
                var instance = spawn.CreateInstance(model);
                if (!instance.TryRaycast(originInternal, directionInternal, nearestDistance, out var collisionDistance))
                {
                    continue;
                }

                nearestDistance = collisionDistance;
                nearestHit = new VmapCollisionHit(
                    spawn.Id,
                    spawn.Name,
                    spawn.IsM2,
                    spawn.WorldBounds,
                    collisionDistance);
            }
        }

        if (nearestDistance < maxDistance)
        {
            hit = nearestHit;
            NavTrace.WriteLine(
                $"NAV VMAP HIT map={mapId} model=\"{hit.ModelName}\" m2={hit.IsM2} " +
                $"dist={hit.Distance:F3} bounds={hit.WorldBounds}");
            return true;
        }

        return false;
    }

    private IEnumerable<VmapTile> EnumerateTiles(int mapId, Vector3 startInternal, Vector3 endInternal)
    {
        var minTileX = ClampTileIndex(Math.Min(GetTileX(startInternal), GetTileX(endInternal)) - 1);
        var maxTileX = ClampTileIndex(Math.Max(GetTileX(startInternal), GetTileX(endInternal)) + 1);
        var minTileY = ClampTileIndex(Math.Min(GetTileY(startInternal), GetTileY(endInternal)) - 1);
        var maxTileY = ClampTileIndex(Math.Max(GetTileY(startInternal), GetTileY(endInternal)) + 1);

        for (var tileX = minTileX; tileX <= maxTileX; tileX++)
        {
            for (var tileY = minTileY; tileY <= maxTileY; tileY++)
            {
                var tile = GetTile(mapId, tileX, tileY);
                if (tile is not null)
                {
                    yield return tile;
                }
            }
        }
    }

    private VmapMapMetadata? GetMapMetadata(int mapId)
    {
        var lazy = _mapMetadataCache.GetOrAdd(
            mapId,
            id => new Lazy<VmapMapMetadata?>(() => LoadMapMetadata(id), LazyThreadSafetyMode.ExecutionAndPublication));
        return lazy.Value;
    }

    private VmapTile? GetTile(int mapId, int tileX, int tileY)
    {
        var key = GetTileKey(mapId, tileX, tileY);
        var lazy = _tileCache.GetOrAdd(
            key,
            _ => new Lazy<VmapTile?>(() => LoadTile(mapId, tileX, tileY), LazyThreadSafetyMode.ExecutionAndPublication));
        return lazy.Value;
    }

    private VmapWorldModel GetWorldModel(string modelName)
    {
        var lazy = _modelCache.GetOrAdd(
            modelName,
            name => new Lazy<VmapWorldModel>(() => LoadWorldModel(name), LazyThreadSafetyMode.ExecutionAndPublication));
        return lazy.Value;
    }

    private VmapMapMetadata? LoadMapMetadata(int mapId)
    {
        var file = System.IO.Path.Combine(_vmapsRoot, $"{mapId:000}.vmtree");
        if (!File.Exists(file))
        {
            NavTrace.WriteLine($"NAV VMAP MAP_MISSING map={mapId} file={file}");
            return null;
        }

        using var stream = File.OpenRead(file);
        using var reader = new BinaryReader(stream);
        ExpectChunk(reader, "VMAP_4.8", file);
        var tiled = reader.ReadByte() != 0;
        NavTrace.WriteLine($"NAV VMAP MAP_LOAD map={mapId} file={file} tiled={tiled}");
        return new VmapMapMetadata(tiled);
    }

    private VmapTile? LoadTile(int mapId, int tileX, int tileY)
    {
        var file = System.IO.Path.Combine(_vmapsRoot, GetTileKey(mapId, tileX, tileY) + ".vmtile");
        if (!File.Exists(file))
        {
            return null;
        }

        using var stream = File.OpenRead(file);
        using var reader = new BinaryReader(stream);
        ExpectChunk(reader, "VMAP_4.8", file);
        var count = reader.ReadUInt32();
        var spawns = new List<VmapModelSpawn>((int)count);
        for (var i = 0; i < count; i++)
        {
            var flags = reader.ReadUInt32();
            var adtId = reader.ReadUInt16();
            var id = reader.ReadUInt32();
            var position = ReadVector3(reader);
            var rotation = ReadVector3(reader);
            var scale = reader.ReadSingle();
            var bounds = (flags & VmapModelSpawn.FlagHasBounds) != 0
                ? new VmapAabb(ReadVector3(reader), ReadVector3(reader))
                : new VmapAabb(position, position);
            var nameLength = reader.ReadUInt32();
            var name = ReadAscii(reader, checked((int)nameLength), file);
            _ = reader.ReadUInt32(); // referenced node index
            spawns.Add(new VmapModelSpawn(id, flags, adtId, position, rotation, scale, bounds, name));
        }

        NavTrace.WriteLine($"NAV VMAP TILE_LOAD map={mapId} tileX={tileX} tileY={tileY} spawns={spawns.Count}");
        return new VmapTile(spawns);
    }

    private VmapWorldModel LoadWorldModel(string modelName)
    {
        var file = System.IO.Path.Combine(_vmapsRoot, modelName + ".vmo");
        if (!File.Exists(file))
        {
            throw new InvalidOperationException(
                $"TrinityCore vmap model file was not found: '{file}'. " +
                "The extracted vmaps appear incomplete for this world object.");
        }

        using var stream = File.OpenRead(file);
        using var reader = new BinaryReader(stream);
        ExpectChunk(reader, "VMAP_4.8", file);
        ExpectChunk(reader, "WMOD", file);
        _ = reader.ReadUInt32(); // chunk size
        _ = reader.ReadUInt32(); // root WMO id
        var groups = new List<VmapGroupModel>();

        if (stream.Position < stream.Length)
        {
            var nextChunk = ReadAscii(reader, 4, file);
            if (string.Equals(nextChunk, "GMOD", StringComparison.Ordinal))
            {
                var groupCount = reader.ReadUInt32();
                groups.Capacity = checked((int)groupCount);
                for (var i = 0; i < groupCount; i++)
                {
                    groups.Add(ReadGroupModel(reader, file));
                }

                ExpectChunk(reader, "GBIH", file);
                SkipBih(reader, file);
            }
            else
            {
                throw new InvalidDataException(
                    $"Unexpected vmap chunk '{nextChunk}' in '{file}'. Expected 'GMOD'.");
            }
        }

        NavTrace.WriteLine($"NAV VMAP MODEL_LOAD model=\"{modelName}\" groups={groups.Count}");
        return new VmapWorldModel(groups);
    }

    private static VmapGroupModel ReadGroupModel(BinaryReader reader, string file)
    {
        var bounds = new VmapAabb(ReadVector3(reader), ReadVector3(reader));
        _ = reader.ReadUInt32(); // mogp flags
        _ = reader.ReadUInt32(); // group WMO id

        ExpectChunk(reader, "VERT", file);
        _ = reader.ReadUInt32(); // chunk size
        var vertexCount = reader.ReadUInt32();
        if (vertexCount == 0)
        {
            return new VmapGroupModel(bounds, [], []);
        }

        var vertices = new Vector3[vertexCount];
        for (var i = 0; i < vertexCount; i++)
        {
            vertices[i] = ReadVector3(reader);
        }

        ExpectChunk(reader, "TRIM", file);
        _ = reader.ReadUInt32(); // chunk size
        var triangleCount = reader.ReadUInt32();
        var triangles = new VmapTriangle[triangleCount];
        for (var i = 0; i < triangleCount; i++)
        {
            triangles[i] = new VmapTriangle(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32());
        }

        ExpectChunk(reader, "MBIH", file);
        SkipBih(reader, file);

        ExpectChunk(reader, "LIQU", file);
        var liquidChunkSize = reader.ReadUInt32();
        if (liquidChunkSize > 0)
        {
            reader.BaseStream.Seek(liquidChunkSize, SeekOrigin.Current);
        }

        return new VmapGroupModel(bounds, vertices, triangles);
    }

    private static void SkipBih(BinaryReader reader, string file)
    {
        _ = ReadVector3(reader);
        _ = ReadVector3(reader);
        var treeSize = reader.ReadUInt32();
        reader.BaseStream.Seek(checked((long)treeSize * sizeof(uint)), SeekOrigin.Current);
        var objectCount = reader.ReadUInt32();
        reader.BaseStream.Seek(checked((long)objectCount * sizeof(uint)), SeekOrigin.Current);
        if (reader.BaseStream.Position > reader.BaseStream.Length)
        {
            throw new InvalidDataException($"Unexpected end of BIH chunk while reading '{file}'.");
        }
    }

    private static void ExpectChunk(BinaryReader reader, string expected, string file)
    {
        var value = ReadAscii(reader, expected.Length, file);
        if (!string.Equals(value, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Unexpected chunk '{value}' in '{file}'. Expected '{expected}'.");
        }
    }

    private static string ReadAscii(BinaryReader reader, int length, string file)
    {
        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new InvalidDataException($"Unexpected end of file while reading '{file}'.");
        }

        return System.Text.Encoding.ASCII.GetString(bytes);
    }

    private static Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private static string GetTileKey(int mapId, int tileX, int tileY)
    {
        return $"{mapId:000}_{tileY:00}_{tileX:00}";
    }

    private static int ClampTileIndex(int value)
    {
        return Math.Clamp(value, 0, 63);
    }

    private static int GetTileX(Vector3 internalPosition)
    {
        return ClampTileIndex((int)(internalPosition.X / TileSize));
    }

    private static int GetTileY(Vector3 internalPosition)
    {
        return ClampTileIndex((int)(internalPosition.Y / TileSize));
    }

    private static Vector3 ToInternal(NavigationPoint point)
    {
        return new Vector3(CoordinateMid - point.X, CoordinateMid - point.Y, point.Z);
    }

    private static Vector3 ToWorld(Vector3 point)
    {
        return new Vector3(CoordinateMid - point.X, CoordinateMid - point.Y, point.Z);
    }

    private sealed record VmapMapMetadata(bool IsTiled);

    private sealed class VmapTile(IReadOnlyList<VmapModelSpawn> spawns)
    {
        public IReadOnlyList<VmapModelSpawn> Spawns { get; } = spawns;
    }

    private sealed class VmapModelSpawn(
        uint id,
        uint flags,
        ushort adtId,
        Vector3 positionInternal,
        Vector3 rotationDegrees,
        float scale,
        VmapAabb boundsInternal,
        string name)
    {
        public const uint FlagM2 = 1u << 0;
        public const uint FlagHasBounds = 1u << 2;
        private readonly VmapAabb _worldBounds = ToWorldBounds(boundsInternal);

        public uint Id { get; } = id;
        public uint Flags { get; } = flags;
        public ushort AdtId { get; } = adtId;
        public Vector3 PositionInternal { get; } = positionInternal;
        public Vector3 RotationDegrees { get; } = rotationDegrees;
        public float Scale { get; } = scale;
        public VmapAabb BoundsInternal { get; } = boundsInternal;
        public string Name { get; } = name;
        public bool IsM2 => (Flags & FlagM2) != 0;
        public VmapAabb WorldBounds => _worldBounds;

        public bool BlocksLineOfSight =>
            !IsM2 ||
            _worldBounds.HorizontalExtent >= MinBlockingM2HorizontalExtent ||
            _worldBounds.VerticalExtent >= MinBlockingM2VerticalExtent;

        public VmapModelInstance CreateInstance(VmapWorldModel model)
        {
            return new VmapModelInstance(this, model);
        }

        private static VmapAabb ToWorldBounds(VmapAabb internalBounds)
        {
            var worldCorner1 = ToWorld(internalBounds.Low);
            var worldCorner2 = ToWorld(internalBounds.High);
            return VmapAabb.FromExtrema(worldCorner1, worldCorner2);
        }
    }

    private sealed class VmapModelInstance
    {
        private readonly VmapModelSpawn _spawn;
        private readonly VmapWorldModel _model;
        private readonly Matrix3x3 _inverseRotation;
        private readonly float _inverseScale;

        public VmapModelInstance(VmapModelSpawn spawn, VmapWorldModel model)
        {
            _spawn = spawn;
            _model = model;
            _inverseRotation = Matrix3x3.CreateInverseFromVmapRotation(spawn.RotationDegrees);
            _inverseScale = 1.0f / MathF.Max(spawn.Scale, 0.0001f);
        }

        public bool TryRaycast(
            Vector3 originInternal,
            Vector3 directionInternal,
            float maxDistance,
            out float worldDistance)
        {
            var localOrigin = _inverseRotation.Transform((originInternal - _spawn.PositionInternal) * _inverseScale);
            var localDirection = _inverseRotation.Transform(directionInternal);
            var localDistance = maxDistance * _inverseScale;
            if (_model.TryRaycast(localOrigin, localDirection, ref localDistance))
            {
                worldDistance = localDistance * _spawn.Scale;
                return true;
            }

            worldDistance = default;
            return false;
        }
    }

    private sealed class VmapWorldModel(IReadOnlyList<VmapGroupModel> groups)
    {
        private readonly IReadOnlyList<VmapGroupModel> _groups = groups;

        public bool TryRaycast(Vector3 originModel, Vector3 directionModel, ref float maxDistance)
        {
            var hit = false;
            foreach (var group in _groups)
            {
                if (!group.Bounds.TryIntersectRay(originModel, directionModel, maxDistance, out _, out _))
                {
                    continue;
                }

                if (group.TryRaycast(originModel, directionModel, ref maxDistance))
                {
                    hit = true;
                }
            }

            return hit;
        }
    }

    private sealed class VmapGroupModel(VmapAabb bounds, Vector3[] vertices, VmapTriangle[] triangles)
    {
        public VmapAabb Bounds { get; } = bounds;
        private Vector3[] Vertices { get; } = vertices;
        private VmapTriangle[] Triangles { get; } = triangles;

        public bool TryRaycast(Vector3 origin, Vector3 direction, ref float maxDistance)
        {
            if (Triangles.Length == 0)
            {
                return false;
            }

            var hit = false;
            for (var i = 0; i < Triangles.Length; i++)
            {
                var triangle = Triangles[i];
                var point1 = Vertices[triangle.Index1];
                var point2 = Vertices[triangle.Index2];
                var point3 = Vertices[triangle.Index3];
                if (TryIntersectTriangle(origin, direction, point1, point2, point3, ref maxDistance))
                {
                    hit = true;
                }
            }

            return hit;
        }
    }

    internal readonly record struct VmapCollisionHit(
        uint SpawnId,
        string ModelName,
        bool IsM2,
        VmapAabb WorldBounds,
        float Distance);

    internal readonly record struct VmapAabb(Vector3 Low, Vector3 High)
    {
        public float HorizontalExtent => MathF.Max(High.X - Low.X, High.Y - Low.Y);
        public float VerticalExtent => High.Z - Low.Z;

        public VmapAabb Expand(float amount)
        {
            var delta = new Vector3(amount, amount, amount);
            return new VmapAabb(Low - delta, High + delta);
        }

        public bool TryIntersectRay(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out float nearDistance,
            out float farDistance)
        {
            nearDistance = 0.0f;
            farDistance = maxDistance;

            if (!TryIntersectAxis(origin.X, direction.X, Low.X, High.X, ref nearDistance, ref farDistance) ||
                !TryIntersectAxis(origin.Y, direction.Y, Low.Y, High.Y, ref nearDistance, ref farDistance) ||
                !TryIntersectAxis(origin.Z, direction.Z, Low.Z, High.Z, ref nearDistance, ref farDistance))
            {
                return false;
            }

            return farDistance >= 0.0f && nearDistance <= maxDistance;
        }

        public static VmapAabb FromExtrema(Vector3 point1, Vector3 point2)
        {
            return new VmapAabb(
                new Vector3(
                    MathF.Min(point1.X, point2.X),
                    MathF.Min(point1.Y, point2.Y),
                    MathF.Min(point1.Z, point2.Z)),
                new Vector3(
                    MathF.Max(point1.X, point2.X),
                    MathF.Max(point1.Y, point2.Y),
                    MathF.Max(point1.Z, point2.Z)));
        }

        private static bool TryIntersectAxis(
            float origin,
            float direction,
            float min,
            float max,
            ref float nearDistance,
            ref float farDistance)
        {
            if (MathF.Abs(direction) < 0.00001f)
            {
                return origin >= min && origin <= max;
            }

            var inverse = 1.0f / direction;
            var axisNear = (min - origin) * inverse;
            var axisFar = (max - origin) * inverse;
            if (axisNear > axisFar)
            {
                (axisNear, axisFar) = (axisFar, axisNear);
            }

            nearDistance = MathF.Max(nearDistance, axisNear);
            farDistance = MathF.Min(farDistance, axisFar);
            return nearDistance <= farDistance;
        }

        public override string ToString()
        {
            return $"low=({Low.X:F3},{Low.Y:F3},{Low.Z:F3}) high=({High.X:F3},{High.Y:F3},{High.Z:F3})";
        }
    }

    private readonly record struct VmapTriangle(uint Index1, uint Index2, uint Index3);

    private readonly record struct Matrix3x3(
        float M11,
        float M12,
        float M13,
        float M21,
        float M22,
        float M23,
        float M31,
        float M32,
        float M33)
    {
        public Vector3 Transform(Vector3 vector)
        {
            return new Vector3(
                (M11 * vector.X) + (M12 * vector.Y) + (M13 * vector.Z),
                (M21 * vector.X) + (M22 * vector.Y) + (M23 * vector.Z),
                (M31 * vector.X) + (M32 * vector.Y) + (M33 * vector.Z));
        }

        public Matrix3x3 Transpose()
        {
            return new Matrix3x3(
                M11, M21, M31,
                M12, M22, M32,
                M13, M23, M33);
        }

        public static Matrix3x3 CreateInverseFromVmapRotation(Vector3 rotationDegrees)
        {
            var zRotation = CreateRotationZ(DegreesToRadians(rotationDegrees.Y));
            var yRotation = CreateRotationY(DegreesToRadians(rotationDegrees.X));
            var xRotation = CreateRotationX(DegreesToRadians(rotationDegrees.Z));
            var rotation = Multiply(Multiply(zRotation, yRotation), xRotation);
            return rotation.Transpose();
        }

        private static float DegreesToRadians(float degrees)
        {
            return degrees * (MathF.PI / 180.0f);
        }

        private static Matrix3x3 CreateRotationX(float radians)
        {
            var cos = MathF.Cos(radians);
            var sin = MathF.Sin(radians);
            return new Matrix3x3(
                1.0f, 0.0f, 0.0f,
                0.0f, cos, -sin,
                0.0f, sin, cos);
        }

        private static Matrix3x3 CreateRotationY(float radians)
        {
            var cos = MathF.Cos(radians);
            var sin = MathF.Sin(radians);
            return new Matrix3x3(
                cos, 0.0f, sin,
                0.0f, 1.0f, 0.0f,
                -sin, 0.0f, cos);
        }

        private static Matrix3x3 CreateRotationZ(float radians)
        {
            var cos = MathF.Cos(radians);
            var sin = MathF.Sin(radians);
            return new Matrix3x3(
                cos, -sin, 0.0f,
                sin, cos, 0.0f,
                0.0f, 0.0f, 1.0f);
        }

        private static Matrix3x3 Multiply(Matrix3x3 left, Matrix3x3 right)
        {
            return new Matrix3x3(
                (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31),
                (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32),
                (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33),
                (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31),
                (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32),
                (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33),
                (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31),
                (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32),
                (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33));
        }
    }

    private static bool TryIntersectTriangle(
        Vector3 origin,
        Vector3 direction,
        Vector3 point1,
        Vector3 point2,
        Vector3 point3,
        ref float maxDistance)
    {
        const float epsilon = 0.00001f;
        var edge1 = point2 - point1;
        var edge2 = point3 - point1;
        var p = Vector3.Cross(direction, edge2);
        var determinant = Vector3.Dot(edge1, p);
        if (MathF.Abs(determinant) < epsilon)
        {
            return false;
        }

        var inverseDeterminant = 1.0f / determinant;
        var s = origin - point1;
        var u = inverseDeterminant * Vector3.Dot(s, p);
        if (u < 0.0f || u > 1.0f)
        {
            return false;
        }

        var q = Vector3.Cross(s, edge1);
        var v = inverseDeterminant * Vector3.Dot(direction, q);
        if (v < 0.0f || (u + v) > 1.0f)
        {
            return false;
        }

        var distance = inverseDeterminant * Vector3.Dot(edge2, q);
        if (distance > 0.0f && distance < maxDistance)
        {
            maxDistance = distance;
            return true;
        }

        return false;
    }
}
