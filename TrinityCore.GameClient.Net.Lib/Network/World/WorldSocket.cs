using System;
using System.Net.Sockets;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Security;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Network.World
{
    public class WorldSocket : GameSocket
    {
        #region Internal Properties

        internal PacketsHandler<WorldCommand> PacketsHandler { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal WorldSocket()
        {
            PacketsHandler = new PacketsHandler<WorldCommand>();
        }

        #endregion Internal Constructors

        #region Protected Methods

        protected override void ReceiveData()
        {
            SocketBuffer.ReserveData(4, true);
            SocketBuffer.Index = 0;
            SocketBuffer.Remaining = 1;
            WorldPacketRead(ReadSizeCallback);
        }

        #endregion Protected Methods

        #region Private Methods

        private void ReadHeaderCallback(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                int bytesRead = e.BytesTransferred;
                if (bytesRead == 0)
                {
                    if (!Closing)
                    {
                        Logger.Append(Logging.Enums.LogCategory.NETWORK, Logging.Enums.LogLevel.DEBUG, "Server has closed the connection");
                        RaiseSocketDisconnected();
                    }
                    return;
                }

                if (bytesRead == SocketBuffer.Remaining)
                {
                    AuthenticationCrypto.Instance.Decrypt(SocketBuffer.ReceiveData, 1, SocketBuffer.ReceiveDataLength - 1);
                    WorldPacketHeader header = new WorldPacketHeader(SocketBuffer.ReceiveData, SocketBuffer.ReceiveDataLength);

                    if (header.InputDataLength > 5 || header.InputDataLength < 4)
                        Logger.Append(Logging.Enums.LogCategory.NETWORK, Logging.Enums.LogLevel.DEBUG, $"Header.InputDataLength invalid: {header.InputDataLength}");

                    if (header.Size > 0)
                    {
                        SocketBuffer.Index = 0;
                        SocketBuffer.Remaining = header.Size;
                        SocketBuffer.ReserveData(header.Size);
                        WorldPacketRead(ReadPayloadCallback, header);
                    }
                    else
                    {
                        PacketsHandler.Handle(new ReceivablePacket<WorldCommand>(header.Command, new byte[0]));
                        ReceiveData();
                    }
                }
                else
                {
                    SocketBuffer.Index += bytesRead;
                    SocketBuffer.Remaining -= bytesRead;
                    WorldPacketRead(ReadHeaderCallback);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
            catch (NullReferenceException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
            catch (SocketException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
        }

        private void ReadPayloadCallback(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                int bytesRead = e.BytesTransferred;
                if (bytesRead == 0)
                {
                    if (!Closing)
                    {
                        Logger.Append(Logging.Enums.LogCategory.NETWORK, Logging.Enums.LogLevel.DEBUG, "Server has closed the connection");
                        RaiseSocketDisconnected();
                    }
                    return;
                }

                if (bytesRead == SocketBuffer.Remaining)
                {
                    WorldPacketHeader header = (WorldPacketHeader)SocketBuffer.SocketAsyncState;
                    PacketsHandler.Handle(new ReceivablePacket<WorldCommand>(header.Command, SocketBuffer.ReceiveData.Split(0, bytesRead)));
                    ReceiveData();
                }
                else
                {
                    SocketBuffer.Index += bytesRead;
                    SocketBuffer.Remaining -= bytesRead;
                    WorldPacketRead(ReadPayloadCallback, SocketBuffer.SocketAsyncState);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
            catch (NullReferenceException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
            catch (SocketException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
                RaiseSocketDisconnected();
            }
        }

        private void ReadSizeCallback(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                int bytesRead = e.BytesTransferred;
                if (bytesRead == 0)
                {
                    if (!Closing)
                    {
                        Logger.Append(Logging.Enums.LogCategory.NETWORK, Logging.Enums.LogLevel.DEBUG, "Server has closed the connection");
                        RaiseSocketDisconnected();
                    }
                    return;
                }

                AuthenticationCrypto.Instance.Decrypt(SocketBuffer.ReceiveData, 0, 1);
                if ((SocketBuffer.ReceiveData[0] & 0x80) != 0)
                {
                    byte temp = SocketBuffer.ReceiveData[0];
                    SocketBuffer.ReserveData(5);
                    SocketBuffer.ReceiveData[0] = (byte)(0x7f & temp);

                    SocketBuffer.Remaining = 4;
                }
                else
                {
                    SocketBuffer.Remaining = 3;
                }

                SocketBuffer.Index = 1;
                WorldPacketRead(ReadHeaderCallback);
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
            catch (NullReferenceException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
            catch (SocketException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
                RaiseSocketDisconnected();
            }
        }

        private void WorldPacketRead(EventHandler<SocketAsyncEventArgs> callback, object state = null)
        {
            try
            {
                SocketBuffer.SocketAsyncState = state;
                SocketBuffer.SocketArgs.SetBuffer(SocketBuffer.ReceiveData, SocketBuffer.Index, SocketBuffer.Remaining);
                SocketBuffer.SocketCallback = callback;
                bool isPending = Socket.Client.ReceiveAsync(SocketBuffer.SocketArgs);
                if (!isPending)
                {
                    callback(this, SocketBuffer.SocketArgs);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
        }

        #endregion Private Methods
    }
}