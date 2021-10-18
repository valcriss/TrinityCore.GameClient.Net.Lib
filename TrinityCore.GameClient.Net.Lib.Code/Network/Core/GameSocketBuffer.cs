using System;
using System.Net.Sockets;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public class GameSocketBuffer
    {
        private const int DEFAULT_BUFFER_SIZE = 128;
        private byte[] _receiveData;
        internal int Index { get; set; }
        internal byte[] ReceiveData => _receiveData;
        internal int ReceiveDataLength { get; set; }
        internal int Remaining { get; set; }
        internal SocketAsyncEventArgs SocketArgs { get; set; }
        internal object SocketAsyncState { get; set; }
        internal EventHandler<SocketAsyncEventArgs> SocketCallback { get; set; }

        internal GameSocketBuffer()
        {
            _receiveData = new byte[DEFAULT_BUFFER_SIZE];
            SocketArgs = new SocketAsyncEventArgs();
            SocketArgs.Completed += CallSocketCallback;
        }

        internal void CallSocketCallback(object sender, SocketAsyncEventArgs e)
        {
            SocketCallback?.Invoke(sender, e);
        }

        internal void ReserveData(int size, bool reset = false)
        {
            if (reset)
                _receiveData = new byte[DEFAULT_BUFFER_SIZE];
            if (_receiveData.Length < size)
                Array.Resize(ref _receiveData, size);
            ReceiveDataLength = size;
        }

        internal void Check()
        {
            if(ReceiveData.Length>= Remaining - Index)
            {
                SocketCallback?.Invoke(this, SocketArgs);
            }
        }
    }
}
