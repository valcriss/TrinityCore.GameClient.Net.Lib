using TrinityCore.GameClient.Net.Client.Models;

namespace TrinityCore.GameClient.Net.Client.Abstractions;

public interface IRealmService
{
    Task<IReadOnlyList<RealmInfo>> GetRealmsAsync(CancellationToken cancellationToken = default);
}
