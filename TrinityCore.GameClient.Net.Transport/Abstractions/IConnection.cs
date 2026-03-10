using System.Net;

namespace TrinityCore.GameClient.Net.Transport.Abstractions;

public interface IConnection : IAsyncDisposable
{
    bool IsConnected { get; }
    IPAddress? LocalAddress { get; }
    int Available { get; }
    Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
    Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);
    Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
