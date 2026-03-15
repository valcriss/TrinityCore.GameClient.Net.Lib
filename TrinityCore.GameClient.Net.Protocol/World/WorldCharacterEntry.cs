namespace TrinityCore.GameClient.Net.Protocol.World;

public sealed record WorldCharacterEntry(
    ulong Guid,
    string Name,
    byte Level,
    byte? RaceId = null,
    byte? ClassId = null,
    byte? GenderId = null);
