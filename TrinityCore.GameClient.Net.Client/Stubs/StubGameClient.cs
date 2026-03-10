using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Models;
using TrinityCore.GameClient.Net.Navigation.Abstractions;

namespace TrinityCore.GameClient.Net.Client.Stubs;

public sealed class StubGameClient(IAuthService authService, IRealmService realmService, IWorldSession worldSession) : IGameClient
{
    public Task<bool> LoginAsync(AuthServerInfo server, AuthServerCredentials credentials, CancellationToken cancellationToken = default)
    {
        return authService.AuthenticateAsync(server, credentials, cancellationToken);
    }

    public Task<IReadOnlyList<RealmInfo>> GetRealmsAsync(CancellationToken cancellationToken = default)
    {
        return realmService.GetRealmsAsync(cancellationToken);
    }

    public Task<bool> ConnectRealmAsync(RealmInfo realm, CancellationToken cancellationToken = default)
    {
        return worldSession.ConnectRealmAsync(realm, cancellationToken);
    }

    public Task<IReadOnlyList<CharacterInfo>> GetCharactersAsync(CancellationToken cancellationToken = default)
    {
        return worldSession.GetCharactersAsync(cancellationToken);
    }

    public Task<bool> EnterWorldAsync(CharacterInfo character, CancellationToken cancellationToken = default)
    {
        return worldSession.EnterWorldAsync(character, cancellationToken);
    }

    public Task<bool> MoveToAsync(
        NavigationPoint destination,
        float arrivalRadius = 2.5f,
        int repathIntervalMs = 750,
        int stuckTimeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StopMovementAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> FaceTowardsAsync(NavigationPoint destination, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SelectTargetAsync(ulong targetGuid, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StartMeleeAttackAsync(ulong targetGuid, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StopMeleeAttackAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> RequestRepopAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ReclaimCorpseAsync(ulong corpseGuid = 0, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> LogoutAsync(int timeoutSeconds = 25, CancellationToken cancellationToken = default)
    {
        return worldSession.LogoutAsync(cancellationToken);
    }
}
