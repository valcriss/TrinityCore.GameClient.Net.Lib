using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Models;

namespace TrinityCore.GameClient.Net.Client.Stubs;

public sealed class StubWorldSession : IWorldSession
{
    public Task<bool> ConnectRealmAsync(RealmInfo realm, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<CharacterInfo>> GetCharactersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<CharacterInfo>>([new CharacterInfo(1, "SampleBot", 1)]);
    }

    public Task<bool> EnterWorldAsync(CharacterInfo character, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
