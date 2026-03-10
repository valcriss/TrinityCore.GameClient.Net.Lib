namespace TrinityCore.GameClient.Net.Protocol.Auth;

public sealed record RealmEntry(byte Id, string Name, string Address, int Port);
