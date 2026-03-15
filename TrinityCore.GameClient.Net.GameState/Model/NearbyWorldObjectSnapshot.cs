namespace TrinityCore.GameClient.Net.GameState.Model;

public sealed record NearbyWorldObjectSnapshot(
    ulong Guid,
    byte TypeId,
    uint? EntryId,
    ulong OwnerGuid,
    float? X,
    float? Y,
    float? Z,
    float? Orientation)
{
    public bool IsGameObject => TypeId == 5;

    public bool IsDynamicObject => TypeId == 6;
}
