using System;
using System.Net.Sockets;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public class GameSocketBuffer
    {
        private const int DEFAULT_BUFFER_SIZE = 128;
        private byte[] _receiveData;
        public int Index { get; set; }
        public byte[] ReceiveData => _receiveData;
        public int ReceiveDataLength { get; set; }
        public int Remaining { get; set; }
        public SocketAsyncEventArgs SocketArgs { get; set; }
        public object SocketAsyncState { get; set; }
        public EventHandler<SocketAsyncEventArgs> SocketCallback { get; set; }

        public GameSocketBuffer()
        {
            _receiveData = new byte[DEFAULT_BUFFER_SIZE];
            SocketArgs = new SocketAsyncEventArgs();
            SocketArgs.Completed += CallSocketCallback;
        }

        public void CallSocketCallback(object sender, SocketAsyncEventArgs e)
        {
            SocketCallback?.Invoke(sender, e);
        }

        public void ReserveData(int size, bool reset = false)
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
