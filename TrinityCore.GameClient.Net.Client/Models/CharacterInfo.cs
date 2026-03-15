namespace TrinityCore.GameClient.Net.Client.Models;

public sealed record CharacterInfo(
    ulong Guid,
    string Name,
    int Level,
    byte? RaceId = null,
    byte? ClassId = null,
    byte? GenderId = null);
