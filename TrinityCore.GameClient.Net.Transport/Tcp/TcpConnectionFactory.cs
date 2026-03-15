using TrinityCore.GameClient.Net.Transport.Abstractions;

namespace TrinityCore.GameClient.Net.Transport.Tcp;

public sealed class TcpConnectionFactory : IConnectionFactory
{
    public IConnection Create() => new TcpConnection();
}
