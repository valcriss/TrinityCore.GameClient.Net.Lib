using TrinityCore.GameClient.Net.GameState.Abstractions;
using TrinityCore.GameClient.Net.GameState.Model;

namespace TrinityCore.GameClient.Net.GameState;

public sealed class GameStateStore : IGameStateStore
{
    private WorldSnapshot _snapshot = new(
        null,
        null,
        Array.Empty<NearbyUnitSnapshot>(),
        Array.Empty<NearbyWorldObjectSnapshot>(),
        Array.Empty<ActiveQuestSnapshot>(),
        DateTimeOffset.UtcNow);

    public WorldSnapshot Snapshot => _snapshot;

    public void Update(WorldSnapshot snapshot)
    {
        _snapshot = snapshot;
    }
}
