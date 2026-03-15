using TrinityCore.GameClient.Net.Client.Models;

namespace TrinityCore.GameClient.Net.Lib;

public interface IGameClientSession
{
    CharacterInfo? CurrentCharacter { get; }
    RealmInfo? CurrentRealm { get; }

    Task<bool> ConnectAsync(GameClientSessionOptions options, CancellationToken cancellationToken = default);
    Task<bool> DisconnectAsync(int? logoutTimeoutSeconds = null, CancellationToken cancellationToken = default);
}
