using System.Diagnostics;
using System.Numerics;
using TrinityCore.GameClient.Net.Navigation.Abstractions;
using TrinityCore.GameClient.Net.Navigation.MmapEngine;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;

namespace TrinityCore.GameClient.Net.Navigation.Mmap;

public sealed class MmapPathfinder : IPathfinder
{
    private const float LosSampleStepMeters = 1.0f;
    private const float LosMaxHorizontalSnapMeters = 1.25f;
    private const float LosMaxVerticalStepMeters = 3.0f;
    private const float PathStraightRatioThreshold = 1.08f;
    private readonly Lazy<MmapFilesCollection?> _collection = new(LoadCollection);

    public IReadOnlyList<NavigationPoint> FindPath(int mapId, NavigationPoint start, NavigationPoint end)
    {
        var collection = _collection.Value;
        if (collection is null)
        {
            NavTrace.WriteLine($"NAV PATH FALLBACK map={mapId} (mmaps not loaded) start=({start.X:F3},{start.Y:F3},{start.Z:F3}) end=({end.X:F3},{end.Y:F3},{end.Z:F3})");
            return [start, end];
        }

        var startFile = new Vector3(start.X, start.Y, start.Z).ToFileFormat();
        var endFile = new Vector3(end.X, end.Y, end.Z).ToFileFormat();
        NavTrace.WriteLine(
            $"NAV PATH REQUEST map={mapId} " +
            $"startW=({start.X:F3},{start.Y:F3},{start.Z:F3}) endW=({end.X:F3},{end.Y:F3},{end.Z:F3}) " +
            $"startF=({startFile.X:F3},{startFile.Y:F3},{startFile.Z:F3}) endF=({endFile.X:F3},{endFile.Y:F3},{endFile.Z:F3})");

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
        var first = rawPath[0];
        var last = rawPath[^1];
        var rawLength = ComputePathLength(rawPath);
        var directLength = Distance(start, end);
        NavTrace.WriteLine(
            $"NAV PATH RESULT map={mapId} points={rawPath.Length} " +
            $"first=({first.X:F3},{first.Y:F3},{first.Z:F3}) last=({last.X:F3},{last.Y:F3},{last.Z:F3})");
        if (directLength > 0.001f && (rawLength / directLength) <= PathStraightRatioThreshold && HasLineOfSight(mapId, start, end))
        {
            NavTrace.WriteLine(
                $"NAV PATH DIRECT_OK map={mapId} ratio={(rawLength / directLength):F3} " +
                $"rawLength={rawLength:F3} directLength={directLength:F3}");
            return [start, end];
        }

        var smoothed = SmoothPath(mapId, rawPath);
        NavTrace.WriteLine(
            $"NAV PATH SMOOTH map={mapId} rawPoints={rawPath.Length} smoothPoints={smoothed.Count}");
        return smoothed;
    }

    public NavigationPoint ProjectToSurface(int mapId, NavigationPoint point)
    {
        var collection = _collection.Value;
        if (collection is null)
        {
            NavTrace.WriteLine($"NAV PROJ FALLBACK map={mapId} point=({point.X:F3},{point.Y:F3},{point.Z:F3})");
            return point;
        }

        var inputW = new Vector3(point.X, point.Y, point.Z);
        var inputF = inputW.ToFileFormat();
        var projected = collection.ClosestPointAtPosition(mapId, new Vector3(point.X, point.Y, point.Z));
        var projectedF = projected.ToFileFormat();
        NavTrace.WriteLine(
            $"NAV PROJ map={mapId} " +
            $"inW=({inputW.X:F3},{inputW.Y:F3},{inputW.Z:F3}) outW=({projected.X:F3},{projected.Y:F3},{projected.Z:F3}) " +
            $"inF=({inputF.X:F3},{inputF.Y:F3},{inputF.Z:F3}) outF=({projectedF.X:F3},{projectedF.Y:F3},{projectedF.Z:F3})");
        return new NavigationPoint(projected.X, projected.Y, projected.Z);
    }

    public bool HasLineOfSight(int mapId, NavigationPoint start, NavigationPoint end)
    {
        var collection = _collection.Value;
        if (collection is null)
        {
            return true;
        }

        var map = collection.GetMap(mapId);
        if (map is null)
        {
            return false;
        }

        var startTile = map.GetMmapTileFileFromVector3(start.X, start.Y, start.Z);
        var endTile = map.GetMmapTileFileFromVector3(end.X, end.Y, end.Z);
        if (startTile is null || endTile is null)
        {
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
                    $"NAV LOS BLOCKED map={mapId} reason=no_tile t={t:F3} sample=({sample.X:F3},{sample.Y:F3},{sample.Z:F3})");
                return false;
            }

            var projected = ProjectToSurface(mapId, sample);
            var snap2d = Distance2D(sample, projected);
            if (snap2d > LosMaxHorizontalSnapMeters)
            {
                NavTrace.WriteLine(
                    $"NAV LOS BLOCKED map={mapId} reason=horizontal_snap t={t:F3} snap2d={snap2d:F3} " +
                    $"sample=({sample.X:F3},{sample.Y:F3},{sample.Z:F3}) projected=({projected.X:F3},{projected.Y:F3},{projected.Z:F3})");
                return false;
            }

            var deltaZ = MathF.Abs(projected.Z - prevProjected.Z);
            if (deltaZ > LosMaxVerticalStepMeters)
            {
                NavTrace.WriteLine(
                    $"NAV LOS BLOCKED map={mapId} reason=vertical_step t={t:F3} deltaZ={deltaZ:F3} " +
                    $"sample=({sample.X:F3},{sample.Y:F3},{sample.Z:F3}) projected=({projected.X:F3},{projected.Y:F3},{projected.Z:F3})");
                return false;
            }

            prevProjected = projected;
        }

        return true;
    }

    private static MmapFilesCollection? LoadCollection()
    {
        var dataRoot = ResolveDataRoot();
        if (dataRoot is null)
        {
            return null;
        }

        var mmapsPath = System.IO.Path.Combine(dataRoot, "mmaps");
        try
        {
            var loaded = MmapFilesCollection.Load(mmapsPath);
            NavTrace.WriteLine($"NAV MMAPS LOAD OK root={mmapsPath} maps={loaded.MmapFiles.Count}");
            return loaded;
        }
        catch (Exception ex)
        {
            NavTrace.WriteLine($"NAV MMAPS LOAD FAIL root={mmapsPath} reason={ex.Message}");
            return null;
        }
    }

    private static string? ResolveDataRoot()
    {
        var env = Environment.GetEnvironmentVariable("TRINITY_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
        {
            return env;
        }

        var cursor = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 8 && cursor is not null; i++)
        {
            var candidate = System.IO.Path.Combine(cursor.FullName, "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            cursor = cursor.Parent;
        }

        return null;
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
                if (!HasLineOfSight(mapId, rawPath[currentIndex], rawPath[candidate]))
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

