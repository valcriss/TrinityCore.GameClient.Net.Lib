namespace TrinityCore.GameClient.Net.Navigation.Abstractions;

public interface IPathfinder
{
    IReadOnlyList<NavigationPoint> FindPath(int mapId, NavigationPoint start, NavigationPoint end);
    NavigationPoint ProjectToSurface(int mapId, NavigationPoint point);
    bool HasLineOfSight(int mapId, NavigationPoint start, NavigationPoint end);
}
