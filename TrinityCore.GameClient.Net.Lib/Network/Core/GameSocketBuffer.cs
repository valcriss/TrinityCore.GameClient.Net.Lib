using System;
using System.Net.Sockets;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal class GameSocketBuffer
    {
        #region Internal Properties

        internal int Index { get; set; }
        internal byte[] ReceiveData => _receiveData;
        internal int ReceiveDataLength { get; set; }
        internal int Remaining { get; set; }
        internal SocketAsyncEventArgs SocketArgs { get; set; }
        internal object SocketAsyncState { get; set; }
        internal EventHandler<SocketAsyncEventArgs> SocketCallback { get; set; }

        #endregion Internal Properties

        #region Private Fields

        private const int DEFAULT_BUFFER_SIZE = 128;
        private byte[] _receiveData;

        #endregion Private Fields

        #region Internal Constructors

        internal GameSocketBuffer()
        {
            _receiveData = new byte[DEFAULT_BUFFER_SIZE];
            SocketArgs = new SocketAsyncEventArgs();
            SocketArgs.Completed += CallSocketCallback;
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal void CallSocketCallback(object sender, SocketAsyncEventArgs e)
        {
            SocketCallback?.Invoke(sender, e);
        }

        internal void Check()
        {
            if (ReceiveData.Length >= Remaining - Index)
            {
                SocketCallback?.Invoke(this, SocketArgs);
            }
        }

        internal void ReserveData(int size, bool reset = false)
        {
            if (reset)
                _receiveData = new byte[DEFAULT_BUFFER_SIZE];
            if (_receiveData.Length < size)
                Array.Resize(ref _receiveData, size);
            ReceiveDataLength = size;
        }

        #endregion Internal Methods
    }
}