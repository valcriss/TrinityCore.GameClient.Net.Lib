using TrinityCore.GameClient.Net.Navigation.Abstractions;

namespace TrinityCore.GameClient.Net.Navigation.Stubs;

public sealed class StubPathfinder : IPathfinder
{
    public IReadOnlyList<NavigationPoint> FindPath(int mapId, NavigationPoint start, NavigationPoint end)
    {
        return [start, end];
    }

    public NavigationPoint ProjectToSurface(int mapId, NavigationPoint point)
    {
        return point;
    }

    public bool HasLineOfSight(int mapId, NavigationPoint start, NavigationPoint end)
    {
        return true;
    }
}
