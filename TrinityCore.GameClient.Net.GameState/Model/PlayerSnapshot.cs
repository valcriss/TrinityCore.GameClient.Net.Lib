namespace TrinityCore.GameClient.Net.GameState.Model;

public sealed record PlayerSnapshot(
    ulong Guid,
    string Name,
    int Level,
    int? FactionTemplateId,
    float X,
    float Y,
    float Z,
    float Orientation,
    int HealthPercent,
    bool IsGhost,
    bool IsDeadHint);
