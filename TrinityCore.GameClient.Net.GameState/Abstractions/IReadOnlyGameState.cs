using TrinityCore.GameClient.Net.GameState.Model;

namespace TrinityCore.GameClient.Net.GameState.Abstractions;

public interface IReadOnlyGameState
{
    WorldSnapshot Snapshot { get; }
}
