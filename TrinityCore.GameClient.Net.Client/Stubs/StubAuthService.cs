using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Models;

namespace TrinityCore.GameClient.Net.Client.Stubs;

public sealed class StubAuthService : IAuthService
{
    public Task<bool> AuthenticateAsync(AuthServerInfo server, AuthServerCredentials credentials, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
