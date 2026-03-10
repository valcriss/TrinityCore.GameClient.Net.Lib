namespace TrinityCore.GameClient.Net.Protocol.World;

public sealed record WorldCharacterEntry(ulong Guid, string Name, byte Level);
