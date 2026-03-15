using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Models;
using TrinityCore.GameClient.Net.Navigation.Abstractions;

namespace TrinityCore.GameClient.Net.Lib;

internal sealed class GameClientSession(IGameClient gameClient, IPathfinder pathfinder) : IGameClientSession
{
    public CharacterInfo? CurrentCharacter { get; private set; }

    public RealmInfo? CurrentRealm { get; private set; }

    public async Task<bool> ConnectAsync(GameClientSessionOptions options, CancellationToken cancellationToken = default)
    {
        _ = pathfinder;

        CurrentCharacter = null;
        CurrentRealm = null;

        var authOk = await gameClient.LoginAsync(options.AuthServer, options.Credentials, cancellationToken);
        if (!authOk)
        {
            return false;
        }

        var realms = await gameClient.GetRealmsAsync(cancellationToken);
        var realm = realms.FirstOrDefault(r => !string.IsNullOrWhiteSpace(options.RealmName) &&
                                               r.Name.Equals(options.RealmName, StringComparison.OrdinalIgnoreCase))
                    ?? realms.FirstOrDefault();
        if (realm is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(options.WorldHostOverride) || options.WorldPortOverride.HasValue)
        {
            realm = new RealmInfo(
                realm.Id,
                realm.Name,
                options.WorldHostOverride ?? realm.Address,
                options.WorldPortOverride ?? realm.Port);
        }

        if (!await gameClient.ConnectRealmAsync(realm, cancellationToken))
        {
            return false;
        }

        var characters = await gameClient.GetCharactersAsync(cancellationToken);
        var character = characters.FirstOrDefault(c => !string.IsNullOrWhiteSpace(options.CharacterName) &&
                                                       c.Name.Equals(options.CharacterName, StringComparison.OrdinalIgnoreCase))
                        ?? characters.FirstOrDefault();
        if (character is null)
        {
            return false;
        }

        var worldOk = await gameClient.EnterWorldAsync(character, cancellationToken);
        if (!worldOk)
        {
            return false;
        }

        CurrentRealm = realm;
        CurrentCharacter = character;
        return true;
    }

    public async Task<bool> DisconnectAsync(int? logoutTimeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var ok = await gameClient.LogoutAsync(logoutTimeoutSeconds ?? 25, cancellationToken);
        if (ok)
        {
            CurrentCharacter = null;
            CurrentRealm = null;
        }

        return ok;
    }
}
