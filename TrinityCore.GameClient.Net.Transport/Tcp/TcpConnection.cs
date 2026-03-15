using System.Net;
using System.Net.Sockets;
using TrinityCore.GameClient.Net.Transport.Abstractions;

namespace TrinityCore.GameClient.Net.Transport.Tcp;

public sealed class TcpConnection : IConnection
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private TcpClient? _client;
    private NetworkStream? _stream;

    public bool IsConnected { get; private set; }
    public IPAddress? LocalAddress => (_client?.Client.LocalEndPoint as IPEndPoint)?.Address;
    public int Available => _client?.Available ?? 0;

    public Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        return ConnectInternalAsync(host, port, cancellationToken);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream is null)
        {
            throw new InvalidOperationException("Connection is not established.");
        }

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _stream.WriteAsync(payload, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream is null)
        {
            throw new InvalidOperationException("Connection is not established.");
        }

        return await _stream.ReadAsync(buffer, cancellationToken);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _stream?.Dispose();
        _stream = null;
        _client?.Dispose();
        _client = null;
        IsConnected = false;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _sendLock.Dispose();
    }

    private async Task ConnectInternalAsync(string host, int port, CancellationToken cancellationToken)
    {
        await DisconnectAsync(cancellationToken);

        _client = new TcpClient();
        await _client.ConnectAsync(host, port, cancellationToken);
        _stream = _client.GetStream();
        IsConnected = true;
    }
}
