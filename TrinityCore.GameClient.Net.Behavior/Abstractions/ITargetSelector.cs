using TrinityCore.GameClient.Net.GameState.Model;

namespace TrinityCore.GameClient.Net.Behavior.Abstractions;

public interface ITargetSelector
{
    NearbyUnitSnapshot? Select(WorldSnapshot snapshot, TargetSelectionOptions options);
}
