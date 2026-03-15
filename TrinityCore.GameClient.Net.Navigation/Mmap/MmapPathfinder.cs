using System.Diagnostics;
using System.Numerics;
using TrinityCore.GameClient.Net.Navigation;
using TrinityCore.GameClient.Net.Navigation.Abstractions;
using TrinityCore.GameClient.Net.Navigation.MmapEngine;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;
using TrinityCore.GameClient.Net.Navigation.Vmap;

namespace TrinityCore.GameClient.Net.Navigation.Mmap;

public sealed class MmapPathfinder : IPathfinder
{
    private const float LosSampleStepMeters = 0.35f;
    private const float LosMaxHorizontalSnapMeters = 0.55f;
    private const float LosMaxVerticalStepMeters = 1.60f;
    private const float LosMaxTerrainOverhangMeters = 0.65f;
    private const float CloseRangeDirectShortcutMaxDistanceMeters = 12.0f;
    private const float PathStraightRatioThreshold = 1.03f;
    private const float DirectShortcutMaxDistanceMeters = 14.0f;
    private const float DirectShortcutMaxDeviationMeters = 1.35f;
    private const float SmoothShortcutMaxDistanceMeters = 10.0f;
    private const float SmoothShortcutMaxDeviationMeters = 1.05f;
    private const float GroundPolylineSampleStepMeters = 1.40f;
    private const float VmapProjectionSearchDistanceMeters = 12.0f;
    private const float VmapProjectionMaxDeltaFromInputMeters = 6.0f;
    private const float VmapProjectionMinFloorGainMeters = 0.35f;
    private const float VmapDetourMarginMinMeters = 1.5f;
    private const float VmapDetourMarginMaxMeters = 6.0f;
    private const float VmapDetourMarginFactor = 0.18f;
    private readonly string _dataRoot;
    private readonly Lazy<MmapFilesCollection> _collection;
    private readonly VmapManager _vmapManager;
    private readonly bool _useVmapLineOfSight;
    private readonly bool _useVmapHeightProjection;
    private readonly bool _useVmapDetours;

    public MmapPathfinder(TrinityCoreDataOptions dataOptions)
    {
        ArgumentNullException.ThrowIfNull(dataOptions);

        _dataRoot = ResolveRequiredDataRoot(dataOptions);
        _collection = new Lazy<MmapFilesCollection>(LoadCollection);
        _vmapManager = new VmapManager(_dataRoot);
        _useVmapLineOfSight = dataOptions.UseVmapLineOfSight;
        _useVmapHeightProjection = dataOptions.UseVmapHeightProjection;
        _useVmapDetours = dataOptions.UseVmapDetours;
        NavTrace.WriteLine(
            $"NAV VMAP CONFIG los={_useVmapLineOfSight} height={_useVmapHeightProjection} detours={_useVmapDetours}");
    }

    public IReadOnlyList<NavigationPoint> FindPath(int mapId, NavigationPoint start, NavigationPoint end)
    {
        var collection = _collection.Value;

        var startFile = new Vector3(start.X, start.Y, start.Z).ToFileFormat();
        var endFile = new Vector3(end.X, end.Y, end.Z).ToFileFormat();
        NavTrace.WriteLine(
            $"NAV PATH REQUEST map={mapId} " +
            $"startW=({start.X:F3},{start.Y:F3},{start.Z:F3}) endW=({end.X:F3},{end.Y:F3},{end.Z:F3}) " +
            $"startF=({startFile.X:F3},{startFile.Y:F3},{startFile.Z:F3}) endF=({endFile.X:F3},{endFile.Y:F3},{endFile.Z:F3})");

        var directLength = Distance(start, end);
        if (directLength > 0.001f &&
            directLength <= CloseRangeDirectShortcutMaxDistanceMeters &&
            HasLineOfSightCore(mapId, start, end, "close-direct"))
        {
            NavTrace.WriteLine(
                $"NAV PATH CLOSE_DIRECT_OK map={mapId} directLength={directLength:F3} " +
                $"start=({start.X:F3},{start.Y:F3},{start.Z:F3}) end=({end.X:F3},{end.Y:F3},{end.Z:F3})");
            return [start, end];
        }

        var result = PathFinding.FindPath(
            mapId,
            new Vector3(start.X, start.Y, start.Z),
            new Vector3(end.X, end.Y, end.Z),
            7.0f);
        if (result is null || result.Points.Length < 2)
        {
            NavTrace.WriteLine($"NAV PATH RESULT map={mapId} failed, fallback direct");
            return [start, end];
        }

        var rawPath = result.Points
            .Select(p => new NavigationPoint(p.X, p.Y, p.Z))
            .ToArray();
        var densifiedSurfacePath = rawPath.Length <= 2
            ? BuildProjectedSurfacePath(mapId, start, end)
            : rawPath;
        var effectivePath = _useVmapDetours
            ? ApplyVmapDetours(mapId, densifiedSurfacePath)
            : densifiedSurfacePath;
        var first = effectivePath[0];
        var last = effectivePath[^1];
        var rawLength = ComputePathLength(effectivePath);
        NavTrace.WriteLine(
            $"NAV PATH RESULT map={mapId} points={rawPath.Length} effectivePoints={effectivePath.Count} " +
            $"first=({first.X:F3},{first.Y:F3},{first.Z:F3}) last=({last.X:F3},{last.Y:F3},{last.Z:F3})");
        if (rawPath.Length > 2 &&
            directLength > 0.001f &&
            (rawLength / directLength) <= PathStraightRatioThreshold &&
            CanShortcutSegment(
                mapId,
                start,
                end,
                effectivePath,
                0,
                effectivePath.Count - 1,
                DirectShortcutMaxDistanceMeters,
                DirectShortcutMaxDeviationMeters,
                "direct"))
        {
            NavTrace.WriteLine(
                $"NAV PATH DIRECT_OK map={mapId} ratio={(rawLength / directLength):F3} " +
                $"rawLength={rawLength:F3} directLength={directLength:F3}");
            return [start, end];
        }

        if (rawPath.Length <= 2)
        {
            NavTrace.WriteLine(
                $"NAV PATH SURFACE_FOLLOW map={mapId} directPoints={rawPath.Length} followPoints={effectivePath.Count}");
            return effectivePath.ToArray();
        }

        var smoothed = SmoothPath(mapId, effectivePath);
        if (_useVmapDetours)
        {
            smoothed = ApplyVmapDetours(mapId, smoothed);
        }

        NavTrace.WriteLine(
            $"NAV PATH SMOOTH map={mapId} rawPoints={effectivePath.Count} smoothPoints={smoothed.Count}");
        return smoothed;
    }

    public NavigationPoint ProjectToSurface(int mapId, NavigationPoint point)
    {
        var collection = _collection.Value;

        var inputW = new Vector3(point.X, point.Y, point.Z);
        var inputF = inputW.ToFileFormat();
        var projected = collection.ClosestPointAtPosition(mapId, new Vector3(point.X, point.Y, point.Z));
        var projectedF = projected.ToFileFormat();
        NavTrace.WriteLine(
            $"NAV PROJ map={mapId} " +
            $"inW=({inputW.X:F3},{inputW.Y:F3},{inputW.Z:F3}) outW=({projected.X:F3},{projected.Y:F3},{projected.Z:F3}) " +
            $"inF=({inputF.X:F3},{inputF.Y:F3},{inputF.Z:F3}) outF=({projectedF.X:F3},{projectedF.Y:F3},{projectedF.Z:F3})");
        var mmapPoint = new NavigationPoint(projected.X, projected.Y, projected.Z);
        if (_useVmapHeightProjection &&
            _vmapManager.TryGetHeight(mapId, point, VmapProjectionSearchDistanceMeters, out var vmapHeight) &&
            ShouldUseVmapProjection(point, mmapPoint, vmapHeight))
        {
            NavTrace.WriteLine(
                $"NAV PROJ VMAP map={mapId} in=({point.X:F3},{point.Y:F3},{point.Z:F3}) " +
                $"mmapZ={mmapPoint.Z:F3} vmapZ={vmapHeight:F3}");
            return new NavigationPoint(point.X, point.Y, vmapHeight);
        }

        return mmapPoint;
    }

    public bool HasLineOfSight(int mapId, NavigationPoint start, NavigationPoint end)
    {
        return HasLineOfSightCore(mapId, start, end, "public");
    }

    private bool HasLineOfSightCore(int mapId, NavigationPoint start, NavigationPoint end, string context)
    {
        var collection = _collection.Value;

        var map = collection.GetMap(mapId);
        if (map is null)
        {
            return false;
        }

        var startTile = map.GetMmapTileFileFromVector3(start.X, start.Y, start.Z);
        var endTile = map.GetMmapTileFileFromVector3(end.X, end.Y, end.Z);
        if (startTile is null || endTile is null)
        {
            NavTrace.WriteLine(
                $"NAV LOS BLOCKED map={mapId} context={context} reason=missing_endpoint_tile " +
                $"start=({start.X:F3},{start.Y:F3},{start.Z:F3}) end=({end.X:F3},{end.Y:F3},{end.Z:F3})");
            return false;
        }

        var distance = Distance(start, end);
        var segments = Math.Max(2, (int)MathF.Ceiling(distance / LosSampleStepMeters));
        var prevProjected = new NavigationPoint(start.X, start.Y, start.Z);

        for (var i = 1; i <= segments; i++)
        {
            var t = i / (float)segments;
            var sample = Lerp(start, end, prevProjected.Z, t);
            var sampleTile = map.GetMmapTileFileFromVector3(sample.X, sample.Y, sample.Z);
            if (sampleTile is null)
            {
                NavTrace.WriteLine(
                    $"NAV LOS BLOCKED map={mapId} context={context} reason=no_tile t={t:F3} sample=({sample.X:F3},{sample.Y:F3},{sample.Z:F3})");
                return false;
            }

            var projected = ProjectToSurface(mapId, sample);
            var snap2d = Distance2D(sample, projected);
            if (snap2d > LosMaxHorizontalSnapMeters)
            {
                NavTrace.WriteLine(
                    $"NAV LOS BLOCKED map={mapId} context={context} reason=horizontal_snap t={t:F3} snap2d={snap2d:F3} " +
                    $"sample=({sample.X:F3},{sample.Y:F3},{sample.Z:F3}) projected=({projected.X:F3},{projected.Y:F3},{projected.Z:F3})");
                return false;
            }

            var deltaZ = MathF.Abs(projected.Z - prevProjected.Z);
            if (deltaZ > LosMaxVerticalStepMeters)
            {
                NavTrace.WriteLine(
                    $"NAV LOS BLOCKED map={mapId} context={context} reason=vertical_step t={t:F3} deltaZ={deltaZ:F3} " +
                    $"sample=({sample.X:F3},{sample.Y:F3},{sample.Z:F3}) projected=({projected.X:F3},{projected.Y:F3},{projected.Z:F3})");
                return false;
            }

            var terrainOverhang = projected.Z - sample.Z;
            if (terrainOverhang > LosMaxTerrainOverhangMeters)
            {
                NavTrace.WriteLine(
                    $"NAV LOS BLOCKED map={mapId} context={context} reason=terrain_overhang t={t:F3} overhangZ={terrainOverhang:F3} " +
                    $"sample=({sample.X:F3},{sample.Y:F3},{sample.Z:F3}) projected=({projected.X:F3},{projected.Y:F3},{projected.Z:F3})");
                return false;
            }

            prevProjected = projected;
        }

        if (_useVmapLineOfSight &&
            _vmapManager.TryGetFirstCollision(mapId, start, end, out var hit))
        {
            NavTrace.WriteLine(
                $"NAV LOS BLOCKED map={mapId} context={context} reason=vmap_collision " +
                $"model=\"{hit.ModelName}\" m2={hit.IsM2} dist={hit.Distance:F3} bounds={hit.WorldBounds} " +
                $"start=({start.X:F3},{start.Y:F3},{start.Z:F3}) end=({end.X:F3},{end.Y:F3},{end.Z:F3})");
            return false;
        }

        NavTrace.WriteLine(
            $"NAV LOS OK map={mapId} context={context} " +
            $"start=({start.X:F3},{start.Y:F3},{start.Z:F3}) end=({end.X:F3},{end.Y:F3},{end.Z:F3})");
        return true;
    }

    private MmapFilesCollection LoadCollection()
    {
        var mmapsPath = System.IO.Path.Combine(_dataRoot, "mmaps");
        try
        {
            var loaded = MmapFilesCollection.Load(mmapsPath);
            NavTrace.WriteLine($"NAV MMAPS LOAD OK dataRoot={_dataRoot} root={mmapsPath} maps={loaded.MmapFiles.Count}");
            return loaded;
        }
        catch (Exception ex)
        {
            NavTrace.WriteLine($"NAV MMAPS LOAD FAIL dataRoot={_dataRoot} root={mmapsPath} reason={ex.Message}");
            throw new InvalidOperationException(
                $"Unable to load TrinityCore mmaps from '{mmapsPath}'. Verify that the extracted navigation data is present and readable.",
                ex);
        }
    }

    private static string ResolveRequiredDataRoot(TrinityCoreDataOptions dataOptions)
    {
        if (!string.IsNullOrWhiteSpace(dataOptions.DataDirectory))
        {
            var configuredPath = System.IO.Path.GetFullPath(dataOptions.DataDirectory);
            ValidateRequiredDataRoot(configuredPath, "configured DataDirectory");
            NavTrace.WriteLine($"NAV DATA ROOT source=configured path={configuredPath}");
            return configuredPath;
        }

        var env = Environment.GetEnvironmentVariable("TRINITY_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(env))
        {
            var environmentPath = System.IO.Path.GetFullPath(env);
            ValidateRequiredDataRoot(environmentPath, "TRINITY_DATA_DIR");
            NavTrace.WriteLine($"NAV DATA ROOT source=env path={environmentPath}");
            return environmentPath;
        }

        var cursor = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 8 && cursor is not null; i++)
        {
            var candidate = System.IO.Path.Combine(cursor.FullName, "data");
            if (Directory.Exists(candidate))
            {
                var projectPath = System.IO.Path.GetFullPath(candidate);
                ValidateRequiredDataRoot(projectPath, "project data directory");
                NavTrace.WriteLine($"NAV DATA ROOT source=project path={projectPath}");
                return projectPath;
            }

            cursor = cursor.Parent;
        }

        throw new InvalidOperationException(
            "Unable to locate TrinityCore data directory. Configure it with " +
            "AddTrinityCoreGameClientCore(options => options.DataDirectory = \"...\") " +
            "or set TRINITY_DATA_DIR. Required content: dbc, maps, mmaps, vmaps.");
    }

    private static void ValidateRequiredDataRoot(string dataRoot, string sourceLabel)
    {
        if (!Directory.Exists(dataRoot))
        {
            throw new InvalidOperationException(
                $"TrinityCore data directory from {sourceLabel} was not found: '{dataRoot}'.");
        }

        var missing = new List<string>();
        ValidateSubdirectory(dataRoot, "dbc", "*.dbc", missing);
        ValidateSubdirectory(dataRoot, "maps", "*.map", missing);
        ValidateSubdirectory(dataRoot, "mmaps", "*.mmap", missing);
        ValidateSubdirectory(dataRoot, "mmaps", "*.mmtile", missing);
        ValidateSubdirectory(dataRoot, "vmaps", "*.vmtree", missing);
        ValidateSubdirectory(dataRoot, "vmaps", "*.vmtile", missing);
        ValidateSubdirectory(dataRoot, "vmaps", "*.vmo", missing);

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"TrinityCore data directory from {sourceLabel} is incomplete: '{dataRoot}'. " +
                $"Missing required extracted data: {string.Join(", ", missing)}. " +
                "Expected content: dbc/*.dbc, maps/*.map, mmaps/*.mmap, mmaps/*.mmtile, vmaps/*.vmtree, vmaps/*.vmtile, vmaps/*.vmo.");
        }
    }

    private static void ValidateSubdirectory(string dataRoot, string subdirectory, string searchPattern, ICollection<string> missing)
    {
        var directory = System.IO.Path.Combine(dataRoot, subdirectory);
        if (!Directory.Exists(directory))
        {
            missing.Add($"{subdirectory}/ ({searchPattern})");
            return;
        }

        if (!Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly).Any())
        {
            missing.Add($"{subdirectory}/{searchPattern}");
        }
    }

    private IReadOnlyList<NavigationPoint> BuildProjectedSurfacePath(int mapId, NavigationPoint start, NavigationPoint end)
    {
        var projectedStart = ProjectToSurface(mapId, start);
        var projectedEnd = ProjectToSurface(mapId, end);
        var planarDistance = Distance2D(projectedStart, projectedEnd);
        var segments = Math.Max(1, (int)MathF.Ceiling(planarDistance / GroundPolylineSampleStepMeters));
        if (segments <= 1)
        {
            return [projectedStart, projectedEnd];
        }

        var path = new List<NavigationPoint>(segments + 1) { projectedStart };
        var previous = projectedStart;
        for (var i = 1; i < segments; i++)
        {
            var t = i / (float)segments;
            var sample = new NavigationPoint(
                projectedStart.X + ((projectedEnd.X - projectedStart.X) * t),
                projectedStart.Y + ((projectedEnd.Y - projectedStart.Y) * t),
                previous.Z);
            var projected = ProjectToSurface(mapId, sample);
            if (Distance(projected, previous) <= 0.05f)
            {
                continue;
            }

            path.Add(projected);
            previous = projected;
        }

        if (Distance(path[^1], projectedEnd) > 0.05f)
        {
            path.Add(projectedEnd);
        }

        return path;
    }

    private IReadOnlyList<NavigationPoint> ApplyVmapDetours(int mapId, IReadOnlyList<NavigationPoint> path)
    {
        if (path.Count < 2)
        {
            return path;
        }

        var repaired = new List<NavigationPoint>(path.Count) { path[0] };
        var changed = false;
        for (var i = 1; i < path.Count; i++)
        {
            var segmentStart = repaired[^1];
            var segmentEnd = path[i];
            if (_vmapManager.TryGetFirstCollision(mapId, segmentStart, segmentEnd, out var hit) &&
                TryBuildVmapDetour(mapId, segmentStart, segmentEnd, hit, out var detour))
            {
                NavTrace.WriteLine(
                    $"NAV VMAP DETOUR OK map={mapId} model=\"{hit.ModelName}\" " +
                    $"from=({segmentStart.X:F3},{segmentStart.Y:F3},{segmentStart.Z:F3}) " +
                    $"to=({segmentEnd.X:F3},{segmentEnd.Y:F3},{segmentEnd.Z:F3}) " +
                    $"waypoints={detour.Count}");
                for (var detourIndex = 1; detourIndex < detour.Count; detourIndex++)
                {
                    repaired.Add(detour[detourIndex]);
                }

                changed = true;
                continue;
            }

            repaired.Add(segmentEnd);
        }

        return changed
            ? CollapseDuplicatePoints(repaired)
            : path;
    }

    private bool TryBuildVmapDetour(
        int mapId,
        NavigationPoint start,
        NavigationPoint end,
        VmapManager.VmapCollisionHit hit,
        out IReadOnlyList<NavigationPoint> detour)
    {
        detour = Array.Empty<NavigationPoint>();
        var bounds = hit.WorldBounds;
        var margin = Math.Clamp(
            MathF.Max(bounds.HorizontalExtent * VmapDetourMarginFactor, VmapDetourMarginMinMeters),
            VmapDetourMarginMinMeters,
            VmapDetourMarginMaxMeters);
        var expanded = bounds.Expand(margin);
        var candidates = BuildDetourCandidates(start, end, expanded);
        List<NavigationPoint>? best = null;
        var bestLength = float.MaxValue;

        foreach (var candidate in candidates)
        {
            var projected = candidate
                .Select(point => ProjectToSurface(mapId, point))
                .ToList();
            var candidatePath = new List<NavigationPoint>(projected.Count + 2) { start };
            candidatePath.AddRange(projected);
            candidatePath.Add(end);
            var collapsed = CollapseDuplicatePoints(candidatePath);
            if (!AreSegmentsTraversable(mapId, collapsed))
            {
                continue;
            }

            var length = ComputePathLength(collapsed);
            if (length < bestLength)
            {
                best = collapsed;
                bestLength = length;
            }
        }

        if (best is null)
        {
            NavTrace.WriteLine(
                $"NAV VMAP DETOUR FAIL map={mapId} model=\"{hit.ModelName}\" " +
                $"from=({start.X:F3},{start.Y:F3},{start.Z:F3}) to=({end.X:F3},{end.Y:F3},{end.Z:F3})");
            return false;
        }

        detour = best;
        return true;
    }

    private static IEnumerable<List<NavigationPoint>> BuildDetourCandidates(
        NavigationPoint start,
        NavigationPoint end,
        VmapManager.VmapAabb bounds)
    {
        var startY = Math.Clamp(start.Y, bounds.Low.Y, bounds.High.Y);
        var endY = Math.Clamp(end.Y, bounds.Low.Y, bounds.High.Y);
        yield return
        [
            new NavigationPoint(bounds.Low.X, startY, start.Z),
            new NavigationPoint(bounds.Low.X, endY, end.Z)
        ];
        yield return
        [
            new NavigationPoint(bounds.High.X, startY, start.Z),
            new NavigationPoint(bounds.High.X, endY, end.Z)
        ];

        var startX = Math.Clamp(start.X, bounds.Low.X, bounds.High.X);
        var endX = Math.Clamp(end.X, bounds.Low.X, bounds.High.X);
        yield return
        [
            new NavigationPoint(startX, bounds.Low.Y, start.Z),
            new NavigationPoint(endX, bounds.Low.Y, end.Z)
        ];
        yield return
        [
            new NavigationPoint(startX, bounds.High.Y, start.Z),
            new NavigationPoint(endX, bounds.High.Y, end.Z)
        ];
    }

    private bool AreSegmentsTraversable(int mapId, IReadOnlyList<NavigationPoint> path)
    {
        for (var i = 1; i < path.Count; i++)
        {
            if (!HasLineOfSightCore(mapId, path[i - 1], path[i], "vmap-detour"))
            {
                return false;
            }
        }

        return true;
    }

    private static List<NavigationPoint> CollapseDuplicatePoints(IReadOnlyList<NavigationPoint> path)
    {
        var collapsed = new List<NavigationPoint>(path.Count);
        foreach (var point in path)
        {
            if (collapsed.Count > 0 && Distance(collapsed[^1], point) <= 0.15f)
            {
                continue;
            }

            collapsed.Add(point);
        }

        return collapsed;
    }

    private IReadOnlyList<NavigationPoint> SmoothPath(int mapId, IReadOnlyList<NavigationPoint> rawPath)
    {
        if (rawPath.Count <= 2)
        {
            return rawPath.ToArray();
        }

        var smoothed = new List<NavigationPoint>(rawPath.Count) { rawPath[0] };
        var currentIndex = 0;
        while (currentIndex < rawPath.Count - 1)
        {
            var bestIndex = currentIndex + 1;
            for (var candidate = rawPath.Count - 1; candidate > currentIndex + 1; candidate--)
            {
                if (!CanShortcutSegment(
                        mapId,
                        rawPath[currentIndex],
                        rawPath[candidate],
                        rawPath,
                        currentIndex,
                        candidate,
                        SmoothShortcutMaxDistanceMeters,
                        SmoothShortcutMaxDeviationMeters,
                        "smooth"))
                {
                    continue;
                }

                bestIndex = candidate;
                break;
            }

            smoothed.Add(rawPath[bestIndex]);
            currentIndex = bestIndex;
        }

        return smoothed;
    }

    private bool CanShortcutSegment(
        int mapId,
        NavigationPoint start,
        NavigationPoint end,
        IReadOnlyList<NavigationPoint> sourcePath,
        int startIndex,
        int endIndex,
        float maxSegmentDistance,
        float maxDeviation,
        string context)
    {
        var segmentLength2D = Distance2D(start, end);
        if (segmentLength2D > maxSegmentDistance)
        {
            NavTrace.WriteLine(
                $"NAV SHORTCUT BLOCKED map={mapId} context={context} reason=segment_length " +
                $"length2d={segmentLength2D:F3} max={maxSegmentDistance:F3} startIndex={startIndex} endIndex={endIndex}");
            return false;
        }

        var deviation2D = ComputeMaxDeviationFromSegment2D(sourcePath, startIndex, endIndex, start, end);
        if (deviation2D > maxDeviation)
        {
            NavTrace.WriteLine(
                $"NAV SHORTCUT BLOCKED map={mapId} context={context} reason=path_deviation " +
                $"deviation2d={deviation2D:F3} max={maxDeviation:F3} startIndex={startIndex} endIndex={endIndex}");
            return false;
        }

        if (!HasLineOfSightCore(mapId, start, end, context))
        {
            NavTrace.WriteLine(
                $"NAV SHORTCUT BLOCKED map={mapId} context={context} reason=los " +
                $"startIndex={startIndex} endIndex={endIndex}");
            return false;
        }

        NavTrace.WriteLine(
            $"NAV SHORTCUT OK map={mapId} context={context} length2d={segmentLength2D:F3} " +
            $"deviation2d={deviation2D:F3} startIndex={startIndex} endIndex={endIndex}");
        return true;
    }

    private static float ComputeMaxDeviationFromSegment2D(
        IReadOnlyList<NavigationPoint> path,
        int startIndex,
        int endIndex,
        NavigationPoint start,
        NavigationPoint end)
    {
        if (endIndex <= startIndex + 1)
        {
            return 0.0f;
        }

        var maxDeviation = 0.0f;
        for (var i = startIndex + 1; i < endIndex; i++)
        {
            var deviation = DistancePointToSegment2D(path[i], start, end);
            if (deviation > maxDeviation)
            {
                maxDeviation = deviation;
            }
        }

        return maxDeviation;
    }

    private static float DistancePointToSegment2D(NavigationPoint point, NavigationPoint segmentStart, NavigationPoint segmentEnd)
    {
        var dx = segmentEnd.X - segmentStart.X;
        var dy = segmentEnd.Y - segmentStart.Y;
        var lengthSq = (dx * dx) + (dy * dy);
        if (lengthSq <= 0.0001f)
        {
            return MathF.Sqrt(DistanceSquared(point, segmentStart));
        }

        var t = (((point.X - segmentStart.X) * dx) + ((point.Y - segmentStart.Y) * dy)) / lengthSq;
        t = Math.Clamp(t, 0.0f, 1.0f);
        var projection = new NavigationPoint(
            segmentStart.X + (dx * t),
            segmentStart.Y + (dy * t),
            point.Z);
        return Distance2D(point, projection);
    }

    private static float DistanceSquared(NavigationPoint left, NavigationPoint right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        var dz = left.Z - right.Z;
        return (dx * dx) + (dy * dy) + (dz * dz);
    }

    private static NavigationPoint Lerp(
        NavigationPoint startAnchor,
        NavigationPoint endAnchor,
        float probeZ,
        float t)
    {
        var x = startAnchor.X + ((endAnchor.X - startAnchor.X) * t);
        var y = startAnchor.Y + ((endAnchor.Y - startAnchor.Y) * t);
        var z = probeZ;
        return new NavigationPoint(x, y, z);
    }

    private static float ComputePathLength(IReadOnlyList<NavigationPoint> points)
    {
        if (points.Count < 2)
        {
            return 0f;
        }

        var total = 0f;
        for (var i = 1; i < points.Count; i++)
        {
            total += Distance(points[i - 1], points[i]);
        }

        return total;
    }

    private static bool ShouldUseVmapProjection(
        NavigationPoint inputPoint,
        NavigationPoint mmapProjection,
        float vmapHeight)
    {
        var vmapDelta = MathF.Abs(vmapHeight - inputPoint.Z);
        if (vmapDelta > VmapProjectionMaxDeltaFromInputMeters)
        {
            return false;
        }

        if (vmapHeight > mmapProjection.Z + VmapProjectionMinFloorGainMeters)
        {
            return inputPoint.Z > mmapProjection.Z + 0.75f || vmapDelta <= 2.5f;
        }

        return vmapDelta + 0.25f < MathF.Abs(mmapProjection.Z - inputPoint.Z);
    }

    private static float Distance(NavigationPoint a, NavigationPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return MathF.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }

    private static float Distance2D(NavigationPoint a, NavigationPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }
}

