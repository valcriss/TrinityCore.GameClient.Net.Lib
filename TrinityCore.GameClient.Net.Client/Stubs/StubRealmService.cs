using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Models;

namespace TrinityCore.GameClient.Net.Client.Stubs;

public sealed class StubRealmService : IRealmService
{
    public Task<IReadOnlyList<RealmInfo>> GetRealmsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<RealmInfo>>([new RealmInfo(1, "Local Trinity 3.3.5", "127.0.0.1", 8085)]);
    }
}
