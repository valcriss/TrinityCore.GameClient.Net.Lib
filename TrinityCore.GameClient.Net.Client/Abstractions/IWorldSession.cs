using TrinityCore.GameClient.Net.Client.Models;

namespace TrinityCore.GameClient.Net.Client.Abstractions;

public interface IWorldSession
{
    Task<bool> ConnectRealmAsync(RealmInfo realm, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CharacterInfo>> GetCharactersAsync(CancellationToken cancellationToken = default);
    Task<bool> EnterWorldAsync(CharacterInfo character, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(CancellationToken cancellationToken = default);
}
