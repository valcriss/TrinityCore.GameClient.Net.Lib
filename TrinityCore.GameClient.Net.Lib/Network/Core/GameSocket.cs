using System;
using System.Net;
using System.Net.Sockets;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    /// <summary>
    /// Abstract class handling the core socket connection
    /// </summary>
    public abstract class GameSocket : IDisposable
    {
        #region Internal Events

        /// <summary>
        /// Event raised when socket is connected
        /// </summary>
        internal event GameSocketConnectedEventHandler GameSocketConnected;

        /// <summary>
        /// Event raised when socket is disconnected
        /// </summary>
        internal event GameSocketDisconnectedEventHandler GameSocketDisconnected;

        #endregion Internal Events

        #region Internal Properties

        internal GameSocketBuffer SocketBuffer { get; set; }

        #endregion Internal Properties

        #region Protected Properties

        protected bool Closing { get; set; }

        /// <summary>
        /// TcpClient object handling the remote connection
        /// </summary>
        protected TcpClient Socket { get; set; }

        #endregion Protected Properties

        #region Private Properties

        private string Host { get; set; }
        private int Port { get; set; }

        #endregion Private Properties

        #region Protected Fields

        protected bool _disposed;

        #endregion Protected Fields

        #region Private Fields

        private const int CONNECTION_TIMEOUT = 5000;

        #endregion Private Fields

        #region Protected Constructors

        /// <summary>
        /// GameSocket constructor
        /// </summary>
        /// <param name="host">Remote hostname</param>
        /// <param name="port">Remote port</param>
        protected GameSocket()
        {
            SocketBuffer = new GameSocketBuffer();
        }

        #endregion Protected Constructors

        #region Public Methods

        /// <summary>
        /// Close the current connection
        /// </summary>
        /// <returns>returns true if successed</returns>
        public bool Close()
        {
            try
            {
                Closing = true;
                if (Socket == null) return false;
                if (!Socket.Connected) return false;
                Socket.Close();
                RaiseSocketDisconnected();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Append(LogCategory.NETWORK, ex);
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Starts the connection to the remote peer
        /// </summary>
        /// <returns>returns true if successed</returns>
        internal bool Connect(string host, int port)
        {
            try
            {
                Close();
                Host = host;
                Port = port;
                Closing = false;
                Logger.Append(LogCategory.NETWORK, LogLevel.DEBUG, $"Socket Connecting to {Host}:{Port}");
                Socket = new TcpClient();
                IAsyncResult result = Socket.BeginConnect(Host, Port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(CONNECTION_TIMEOUT));

                if (!success)
                {
                    throw new TimeoutException();
                }

                Socket.EndConnect(result);
                RaiseSocketConnected();
                ReceiveData();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Append(LogCategory.NETWORK, ex);
                return false;
            }
        }

        internal bool Send(SendablePacket packet)
        {
            return Send(packet.GetData());
        }

        internal bool Send(byte[] data)
        {
            try
            {
                if (Socket == null) return false;
                if (!Socket.Connected) return false;
                if (data.Length == 0) return false;
                Socket.GetStream().Write(data, 0, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Append(LogCategory.NETWORK, ex);
                return false;
            }
        }

        #endregion Internal Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Close();
                }
                _disposed = true;
            }
        }

        protected IPAddress GetIpAddress()
        {
            return ((IPEndPoint)Socket?.Client.LocalEndPoint)?.Address;
        }

        /// <summary>
        /// Raises the SocketConnected Event
        /// </summary>
        protected void RaiseSocketConnected()
        {
            Logger.Append(LogCategory.NETWORK, LogLevel.DEBUG, "Socket Connected");
            GameSocketConnected?.Invoke();
        }

        /// <summary>
        /// Raises the SocketDisconnected Event
        /// </summary>
        protected void RaiseSocketDisconnected()
        {
            Logger.Append(LogCategory.NETWORK, LogLevel.DEBUG, "Socket Disconnected");
            GameSocketDisconnected?.Invoke();
        }

        /// <summary>
        /// Abstract method handing the data reception
        /// </summary>
        protected abstract void ReceiveData();

        #endregion Protected Methods
    }
}