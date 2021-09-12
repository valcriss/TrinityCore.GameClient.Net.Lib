using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Log;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public abstract class GameSocket
    {
        internal event OnGameSocketConnectedEventHandler OnGameSocketConnected;

        internal event OnGameSocketDisconnectedEventHandler OnGameSocketDisconnected;

        protected GameSocketBuffer SocketBuffer { get; set; }
        protected TcpClient TcpClient { get; set; }

        internal bool IsConnected => (TcpClient != null && TcpClient.Connected);
        private string Host { get; }
        private int Port { get; }

        protected GameSocket(string host, int port)
        {
            SocketBuffer = new GameSocketBuffer();
            Host = host;
            Port = port;
        }

        protected virtual void RaiseSocketConnected()
        {
            Logger.Log("SocketConnected", LogLevel.VERBOSE);
            OnGameSocketConnected?.Invoke(this);
        }

        protected virtual void RaiseSocketDisconnected()
        {
            Logger.Log("SocketDisconnected", LogLevel.VERBOSE);
            OnGameSocketDisconnected?.Invoke(this);
        }

        protected abstract void ReceiveData();

        #region Close & Connect

        protected bool Close()
        {
            try
            {
                if (TcpClient == null) return false;
                if (!TcpClient.Connected) return false;
                OnGameSocketDisconnected?.Invoke(this);
                TcpClient.Close();
                return true;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return false;
            }
        }

        protected async Task<bool> Connect()
        {
            return await Task.Run(() =>
            {
                try
                {
                    TcpClient?.Close();
                    Logger.Log("Connecting to " + Host + ":" + Port, LogLevel.VERBOSE);
                    TcpClient = new TcpClient(Host, Port);
                    OnGameSocketConnected?.Invoke(this);
                    ReceiveData();
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogException(e);
                    return false;
                }
            });
        }

        protected IPAddress GetIpAddress()
        {
            return ((IPEndPoint)TcpClient?.Client.LocalEndPoint)?.Address;
        }

        #endregion Close & Connect

        #region Sending

        internal bool Send(SendablePacket sendablePacket)
        {
            return Send(sendablePacket.GetBuffer());
        }

        protected bool Send(byte[] data)
        {
            try
            {
                if (TcpClient == null) return false;
                if (!TcpClient.Connected) return false;
                if (data.Length == 0) return false;
                TcpClient.GetStream().Write(data, 0, data.Length);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return false;
            }
        }

        #endregion Sending

        #region Disposing

        ~GameSocket()
        {
            Dispose();
        }

        internal void Dispose()
        {
            Close();
        }

        #endregion Disposing
    }
}
