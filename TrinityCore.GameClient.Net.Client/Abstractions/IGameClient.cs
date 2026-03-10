using TrinityCore.GameClient.Net.Client.Models;
using TrinityCore.GameClient.Net.Navigation.Abstractions;

namespace TrinityCore.GameClient.Net.Client.Abstractions;

public interface IGameClient
{
    Task<bool> LoginAsync(AuthServerInfo server, AuthServerCredentials credentials, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RealmInfo>> GetRealmsAsync(CancellationToken cancellationToken = default);
    Task<bool> ConnectRealmAsync(RealmInfo realm, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CharacterInfo>> GetCharactersAsync(CancellationToken cancellationToken = default);
    Task<bool> EnterWorldAsync(CharacterInfo character, CancellationToken cancellationToken = default);
    Task<bool> MoveToAsync(
        NavigationPoint destination,
        float arrivalRadius = 2.5f,
        int repathIntervalMs = 750,
        int stuckTimeoutMs = 3000,
        CancellationToken cancellationToken = default);
    Task<bool> StopMovementAsync(CancellationToken cancellationToken = default);
    Task<bool> FaceTowardsAsync(NavigationPoint destination, CancellationToken cancellationToken = default);
    Task<bool> SelectTargetAsync(ulong targetGuid, CancellationToken cancellationToken = default);
    Task<bool> StartMeleeAttackAsync(ulong targetGuid, CancellationToken cancellationToken = default);
    Task<bool> StopMeleeAttackAsync(CancellationToken cancellationToken = default);
    Task<bool> RequestRepopAsync(CancellationToken cancellationToken = default);
    Task<bool> ReclaimCorpseAsync(ulong corpseGuid = 0, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(int timeoutSeconds = 25, CancellationToken cancellationToken = default);
}
