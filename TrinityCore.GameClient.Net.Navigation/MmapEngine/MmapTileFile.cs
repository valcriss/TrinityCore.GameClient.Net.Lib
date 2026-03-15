using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.Exceptions;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine
{
    public class MmapTileFile
    {
        #region Public Properties

        public string File { get; set; }

        public MmapTileHeader Header
        {
            get
            {
                if (!_loaded)
                    LoadOnDemand();
                return _mmapTileHeader;
            }
        }

        public string Key { get; set; }
        public int MapId { get; set; }

        public MmapMesh Mesh
        {
            get
            {
                if (!_loaded)
                    LoadOnDemand();
                return _mmapMesh;
            }
        }

        public int TileX { get; set; }
        public int TileY { get; set; }

        #endregion Public Properties

        #region Private Fields

        private bool _loaded;

        private MmapMesh _mmapMesh;

        private MmapTileHeader _mmapTileHeader;

        #endregion Private Fields

        #region Private Constructors

        private MmapTileFile(string file, int mapId, int tileX, int tileY)
        {
            File = file;
            Key = System.IO.Path.GetFileNameWithoutExtension(file);
            MapId = mapId;
            TileX = tileX;
            TileY = tileY;
        }

        #endregion Private Constructors

        #region Public Methods

        public static MmapTileFile Load(string file)
        {
            CheckFile(file);

            int mapId = GetMapIdFromFilename(file);
            int tileX = GetTileXFromFilename(file);
            int tileY = GetTileYFromFilename(file);

            return new MmapTileFile(file, mapId, tileX, tileY);
        }

        public Vector3 ClosestPointAtPosition(Vector3 position)
        {
            var worldPosition = position;
            var filePosition = position.ToFileFormat();
            MmapMeshPoly poly = GetNearestPoly(position);
            if (poly == null)
            {
                NavTrace.WriteLine($"NAV TILE PROJECT tile={Key} no poly world=({worldPosition.X:F3},{worldPosition.Y:F3},{worldPosition.Z:F3})");
                return worldPosition;
            }

            var closestPoint = FindClosestPointOnPoly(filePosition, poly);
            if (closestPoint == null)
            {
                NavTrace.WriteLine(
                    $"NAV TILE PROJECT tile={Key} poly={poly.Key} no triangle " +
                    $"world=({worldPosition.X:F3},{worldPosition.Y:F3},{worldPosition.Z:F3}) file=({filePosition.X:F3},{filePosition.Y:F3},{filePosition.Z:F3})");
                return worldPosition;
            }

            var projected = closestPoint.Value.ToWorldFormat();
            if (float.IsNaN(projected.X) || float.IsNaN(projected.Y) || float.IsNaN(projected.Z))
            {
                NavTrace.WriteLine(
                    $"NAV TILE PROJECT tile={Key} poly={poly.Key} triangle=NaN " +
                    $"world=({worldPosition.X:F3},{worldPosition.Y:F3},{worldPosition.Z:F3}) file=({filePosition.X:F3},{filePosition.Y:F3},{filePosition.Z:F3})");
                return worldPosition;
            }

            var projectedFile = projected.ToFileFormat();
            NavTrace.WriteLine(
                $"NAV TILE PROJECT tile={Key} poly={poly.Key} " +
                $"inW=({worldPosition.X:F3},{worldPosition.Y:F3},{worldPosition.Z:F3}) outW=({projected.X:F3},{projected.Y:F3},{projected.Z:F3}) " +
                $"inF=({filePosition.X:F3},{filePosition.Y:F3},{filePosition.Z:F3}) outF=({projectedFile.X:F3},{projectedFile.Y:F3},{projectedFile.Z:F3})");
            return projected;
        }

        public MmapMeshPoly GetNearestPoly(Vector3 position)
        {
            var worldPosition = position;
            var filePosition = position.ToFileFormat();

            // Closer to Trinity/Detour behavior:
            // - if point is over poly in XZ, favor vertical distance with walkable climb bias
            // - otherwise fallback to 3D distance to closest point on poly
            var walkableClimb = Mesh.Header.WalkableClimb;
            var bestDistance = float.MaxValue;
            var mode = "detour-like";
            MmapMeshPoly? poly = null;

            foreach (var candidate in Mesh.Polys)
            {
                var closest = FindClosestPointOnPoly(filePosition, candidate);
                if (closest == null)
                {
                    continue;
                }

                var overPoly = IsPointOverPoly(filePosition, candidate);
                var diff = filePosition - closest.Value;
                var distance = overPoly
                    ? MathF.Max(MathF.Abs(diff.Y) - walkableClimb, 0.0f)
                    : diff.Length();

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    poly = candidate;
                }
            }

            if (poly is null)
            {
                mode = "nearest-triangle-center-fallback";
                poly = Mesh.Polys.OrderBy(c => c.Triangles.Min(d => Vector3.Distance(filePosition, d.Center()))).FirstOrDefault();
            }

            NavTrace.WriteLine(
                $"NAV TILE POLY tile={Key} mode={mode} poly={poly?.Key ?? "null"} " +
                $"world=({worldPosition.X:F3},{worldPosition.Y:F3},{worldPosition.Z:F3}) file=({filePosition.X:F3},{filePosition.Y:F3},{filePosition.Z:F3})");
            return poly;
        }

        #endregion Public Methods

        #region Private Methods

        private static void CheckFile(string file)
        {
            if (!file.Exists())
            {
                throw new System.IO.FileNotFoundException();
            }
            else if (!file.CheckExtension(Constants.MMAP_TILE_FILE_EXTENSION))
            {
                throw new FileBadExtensionException(Constants.MMAP_TILE_FILE_EXTENSION);
            }
            else if (!file.CheckRegExp(Constants.MMAP_TILE_FILE_CHECK_REGEXP))
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_TILE_FILE_EXTENSION, Constants.MMAP_TILE_FILE_CHECK_REGEXP);
            }
        }

        private static Vector3 ClosestPointAtPosition(Vector3 position, Vector3 point1, Vector3 point2, Vector3 point3)
        {
            // Check if P in vertex region outside A
            Vector3 ab = Vector3.Subtract(point2, point1);
            Vector3 ac = Vector3.Subtract(point3, point1);
            Vector3 ap = Vector3.Subtract(position, point1);

            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f)
            {
                // barycentric coordinates (1,0,0)
                return point1.ToWorldFormat();
            }

            // Check if P in vertex region outside B
            Vector3 bp = Vector3.Subtract(position, point2);
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3)
            {
                // barycentric coordinates (0,1,0)
                return point2.ToWorldFormat();
            }

            // Check if P in edge region of AB, if so return projection of P onto AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                // barycentric coordinates (1-v,v,0)
                float v1 = d1 / (d1 - d3);

                return new Vector3(point1.X + v1 * ab.X, point1.Y + v1 * ab.Y, point1.Z + v1 * ab.Z).ToWorldFormat();
            }

            // Check if P in vertex region outside C
            Vector3 cp = Vector3.Subtract(position, point3);
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6)
            {
                // barycentric coordinates (0,0,1)
                return point3.ToWorldFormat();
            }

            // Check if P in edge region of AC, if so return projection of P onto AC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                // barycentric coordinates (1-w,0,w)
                float w1 = d2 / (d2 - d6);
                return new Vector3(point1.X + w1 * ac.X, point1.Y + w1 * ac.Y, point1.Z + w1 * ac.Z).ToWorldFormat();
            }

            // Check if P in edge region of BC, if so return projection of P onto BC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                // barycentric coordinates (0,1-w,w)
                float w2 = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return new Vector3(point2.X + w2 * (point3.X - point2.X), point2.Y + w2 * (point3.Y - point2.Y), point2.Z + w2 * (point3.Z - point2.Z)).ToWorldFormat();
            }

            // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
            float denom = 1.0f / (va + vb + vc);
            float v = vb * denom;
            float w = vc * denom;
            return new Vector3(point1.X + ab.X * v + ac.X * w, point1.Y + ab.Y * v + ac.Y * w, point1.Z + ab.Z * v + ac.Z * w).ToWorldFormat();
        }

        private Vector3? FindClosestPointOnPoly(Vector3 filePosition, MmapMeshPoly poly)
        {
            var bestDistance = float.MaxValue;
            Vector3? bestPoint = null;

            foreach (var triangle in EnumeratePolyTriangles(poly))
            {
                var candidate = ClosestPointOnTriangleFile(filePosition, triangle.Point1, triangle.Point2, triangle.Point3);
                var distance = Vector3.DistanceSquared(filePosition, candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = candidate;
                }
            }

            return bestPoint;
        }

        private bool IsPointOverPoly(Vector3 filePosition, MmapMeshPoly poly)
        {
            foreach (var triangle in EnumeratePolyTriangles(poly))
            {
                if (PointInTriangleXZ(filePosition, triangle.Point1, triangle.Point2, triangle.Point3))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<(Vector3 Point1, Vector3 Point2, Vector3 Point3)> EnumeratePolyTriangles(MmapMeshPoly poly)
        {
            var polyIndex = Mesh.Polys.IndexOf(poly);
            if (polyIndex >= 0 && polyIndex < Mesh.PolyDetails.Count)
            {
                var detail = Mesh.PolyDetails[polyIndex];
                if (detail.TriCount > 0)
                {
                    var emitted = 0;
                    for (var i = 0; i < detail.TriCount; i++)
                    {
                        var triIndex = (int)detail.TriBase + i;
                        if (triIndex < 0 || triIndex >= Mesh.TriDetails.Count)
                        {
                            continue;
                        }

                        var tri = Mesh.TriDetails[triIndex];
                        var p0 = ResolveDetailVertex(poly, detail, tri.Tri[0]);
                        var p1 = ResolveDetailVertex(poly, detail, tri.Tri[1]);
                        var p2 = ResolveDetailVertex(poly, detail, tri.Tri[2]);
                        if (p0 == null || p1 == null || p2 == null)
                        {
                            continue;
                        }

                        emitted++;
                        yield return (p0.Value, p1.Value, p2.Value);
                    }

                    if (emitted > 0)
                    {
                        yield break;
                    }
                }
            }

            // Fallback on simple fan-triangulation representation when detail mesh is unavailable.
            foreach (var triangle in poly.Triangles)
            {
                yield return (triangle.Points[0], triangle.Points[1], triangle.Points[2]);
            }
        }

        private Vector3? ResolveDetailVertex(MmapMeshPoly poly, MmapMeshPolyDetail detail, byte detailVertexIndex)
        {
            if (detailVertexIndex < poly.VertCount)
            {
                var vertexIndex = poly.Verts[detailVertexIndex];
                if (vertexIndex >= Mesh.Verts.Count)
                {
                    return null;
                }

                return Mesh.Verts[vertexIndex].Vector3;
            }

            var localDetailIndex = detailVertexIndex - poly.VertCount;
            var detailVertexArrayIndex = (int)detail.VertBase + localDetailIndex;
            if (detailVertexArrayIndex < 0 || detailVertexArrayIndex >= Mesh.VertDetails.Count)
            {
                return null;
            }

            return Mesh.VertDetails[detailVertexArrayIndex].Vector3;
        }

        private static Vector3 ClosestPointOnTriangleFile(Vector3 position, Vector3 point1, Vector3 point2, Vector3 point3)
        {
            var world = ClosestPointAtPosition(position, point1, point2, point3);
            return world.ToFileFormat();
        }

        private static bool PointInTriangleXZ(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            var b1 = SignXZ(point, a, b) < 0.0f;
            var b2 = SignXZ(point, b, c) < 0.0f;
            var b3 = SignXZ(point, c, a) < 0.0f;
            return b1 == b2 && b2 == b3;
        }

        private static float SignXZ(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.X - p3.X) * (p2.Z - p3.Z) - (p2.X - p3.X) * (p1.Z - p3.Z);
        }

        private static int GetMapIdFromFilename(string file)
        {
            try
            {
                return int.Parse(System.IO.Path.GetFileNameWithoutExtension(file)[..3]);
            }
            catch (Exception)
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_TILE_FILE_EXTENSION, Constants.MMAP_TILE_FILE_CHECK_REGEXP);
            }
        }

        private static int GetTileXFromFilename(string file)
        {
            try
            {
                return int.Parse(System.IO.Path.GetFileNameWithoutExtension(file).Substring(3, 2));
            }
            catch (Exception)
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_TILE_FILE_EXTENSION, Constants.MMAP_TILE_FILE_CHECK_REGEXP);
            }
        }

        private static int GetTileYFromFilename(string file)
        {
            try
            {
                return int.Parse(System.IO.Path.GetFileNameWithoutExtension(file).Substring(5, 2));
            }
            catch (Exception)
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_TILE_FILE_EXTENSION, Constants.MMAP_TILE_FILE_CHECK_REGEXP);
            }
        }

        private void LoadOnDemand()
        {
            DateTime start = DateTime.Now;

            MapBinaryReader reader = MapBinaryReader.FromFile(File);
            _mmapTileHeader = MmapTileHeader.FromMapBinaryReader(reader);
            _mmapMesh = MmapMesh.FromMapBinaryReader(this, reader);
            if (reader.BytesLeft > 0)
            {
                throw new FileBadLengthException(reader.BytesLeft, 0);
            }
            _loaded = true;
            System.Diagnostics.Debug.WriteLine("LoadOnDemand [" + System.IO.Path.GetFileName(File) + "] Duration :" + DateTime.Now.Subtract(start).TotalMilliseconds + " ms");
        }

        #endregion Private Methods
    }
}


