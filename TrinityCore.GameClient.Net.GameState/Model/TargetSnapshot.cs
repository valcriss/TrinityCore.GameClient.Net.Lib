namespace TrinityCore.GameClient.Net.GameState.Model;

public sealed record TargetSnapshot(ulong Guid, string? Name, int? Level, float? X, float? Y, float? Z, int? HealthPercent);
