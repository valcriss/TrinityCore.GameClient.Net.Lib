namespace TrinityCore.GameClient.Net.Transport.Abstractions;

public interface IConnectionFactory
{
    IConnection Create();
}
