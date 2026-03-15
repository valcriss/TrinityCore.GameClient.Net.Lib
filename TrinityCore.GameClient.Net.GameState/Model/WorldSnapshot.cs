namespace TrinityCore.GameClient.Net.GameState.Model;

public sealed record WorldSnapshot(
    PlayerSnapshot? Player,
    TargetSnapshot? Target,
    IReadOnlyList<NearbyUnitSnapshot> NearbyUnits,
    IReadOnlyList<NearbyWorldObjectSnapshot> NearbyWorldObjects,
    IReadOnlyList<ActiveQuestSnapshot> ActiveQuests,
    DateTimeOffset UpdatedAtUtc);
