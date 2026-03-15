namespace TrinityCore.GameClient.Net.Navigation.Abstractions;

public interface INavigationService
{
    bool TryMoveTo(int mapId, NavigationPoint destination);
}
