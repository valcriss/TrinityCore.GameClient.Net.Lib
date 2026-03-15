namespace TrinityCore.GameClient.Net.Client.Models;

public readonly record struct MeleeAttackState(
    bool IsActive,
    ulong TargetGuid);
