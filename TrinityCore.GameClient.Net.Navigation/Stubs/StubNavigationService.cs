using TrinityCore.GameClient.Net.Navigation.Abstractions;

namespace TrinityCore.GameClient.Net.Navigation.Stubs;

public sealed class StubNavigationService : INavigationService
{
    public bool TryMoveTo(int mapId, NavigationPoint destination) => true;
}
