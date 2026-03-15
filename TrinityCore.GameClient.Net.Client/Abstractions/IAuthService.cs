using TrinityCore.GameClient.Net.Client.Models;

namespace TrinityCore.GameClient.Net.Client.Abstractions;

public interface IAuthService
{
    Task<bool> AuthenticateAsync(AuthServerInfo server, AuthServerCredentials credentials, CancellationToken cancellationToken = default);
}
