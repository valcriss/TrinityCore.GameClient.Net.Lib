using TrinityCore.GameClient.Net.Client.Models;

namespace TrinityCore.GameClient.Net.Lib;

public sealed record GameClientSessionOptions(
    AuthServerInfo AuthServer,
    AuthServerCredentials Credentials,
    string? RealmName = null,
    string? WorldHostOverride = null,
    int? WorldPortOverride = null,
    string? CharacterName = null,
    int LogoutTimeoutSeconds = 25);
