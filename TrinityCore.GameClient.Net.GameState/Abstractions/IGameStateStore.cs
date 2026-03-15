using TrinityCore.GameClient.Net.GameState.Model;

namespace TrinityCore.GameClient.Net.GameState.Abstractions;

public interface IGameStateStore : IReadOnlyGameState
{
    void Update(WorldSnapshot snapshot);
}
