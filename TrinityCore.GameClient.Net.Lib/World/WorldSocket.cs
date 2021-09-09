using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Crypto;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World
{
    public class WorldSocket : Network.Core.GameSocket
    {
        public AuthenticationCrypto AuthenticationCrypto { get; set; }
        protected WorldPacketsHandler PacketsHandler { get; set; }

        public WorldSocket(string host, int port) : base(host, port)
        {
            PacketsHandler = new WorldPacketsHandler();
            AuthenticationCrypto = new AuthenticationCrypto();
        }

        public void AppendHandler(WorldCommand command, Network.Core.PacketHandler handler)
        {
            lock (PacketsHandler)
            {
                PacketsHandler.RegisterHandler(command, handler);
            }
        }

        protected override void ReceiveData()
        {

            SocketBuffer.ReserveData(4, true);
            SocketBuffer.Index = 0;
            SocketBuffer.Remaining = 1;
            WorldPacketRead(ReadSizeCallback);
        }

        private void ReadHeaderCallback(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                int bytesRead = e.BytesTransferred;
                if (bytesRead == 0)
                {
                    Logger.Log("Server has closed the connection", LogLevel.ERROR);
                    RaiseSocketDisconnected();
                    return;
                }

                if (bytesRead == SocketBuffer.Remaining)
                {
                    AuthenticationCrypto.Decrypt(SocketBuffer.ReceiveData, 1, SocketBuffer.ReceiveDataLength - 1);
                    WorldPacketHeader header =
                        new WorldPacketHeader(SocketBuffer.ReceiveData, SocketBuffer.ReceiveDataLength);

                    if (header.InputDataLength > 5 || header.InputDataLength < 4)
                        Logger.Log($"Header.InputDataLength invalid: {header.InputDataLength}", LogLevel.ERROR);

                    if (header.Size > 0)
                    {
                        SocketBuffer.Index = 0;
                        SocketBuffer.Remaining = header.Size;
                        SocketBuffer.ReserveData(header.Size);
                        WorldPacketRead(ReadPayloadCallback, header);
                    }
                    else
                    {
                        PacketsHandler.Handle(new WorldReceivablePacket(header.Command, new byte[0]));
                        ReceiveData();
                    }
                }
                else
                {
                    // more header to read
                    SocketBuffer.Index += bytesRead;
                    SocketBuffer.Remaining -= bytesRead;
                    WorldPacketRead(ReadHeaderCallback);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException ex)
            {
                Logger.LogException(ex);
            }
            catch (SocketException ex)
            {
                Logger.LogException(ex);
            }
        }

        private void ReadPayloadCallback(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                int bytesRead = e.BytesTransferred;
                if (bytesRead == 0)
                {
                    Logger.Log("Server has closed the connection", LogLevel.ERROR);
                    RaiseSocketDisconnected();
                    return;
                }

                if (bytesRead == SocketBuffer.Remaining)
                {
                    WorldPacketHeader header = (WorldPacketHeader)SocketBuffer.SocketAsyncState;
                    PacketsHandler.Handle(new WorldReceivablePacket(header.Command,
                        SocketBuffer.ReceiveData.Split(0, bytesRead)));
                    ReceiveData();
                }
                else
                {
                    SocketBuffer.Index += bytesRead;
                    SocketBuffer.Remaining -= bytesRead;
                    WorldPacketRead(ReadPayloadCallback, SocketBuffer.SocketAsyncState);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException ex)
            {
                Logger.LogException(ex);
            }
            catch (SocketException ex)
            {
                Logger.LogException(ex);
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
                    Logger.Log("Server has closed the connection", LogLevel.ERROR);
                    RaiseSocketDisconnected();
                    return;
                }

                AuthenticationCrypto.Decrypt(SocketBuffer.ReceiveData, 0, 1);
                if ((SocketBuffer.ReceiveData[0] & 0x80) != 0)
                {
                    // need to resize the buffer
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
                Logger.LogException(ex);
            }
            catch (NullReferenceException ex)
            {
                Logger.LogException(ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.LogException(ex);
            }
            catch (SocketException ex)
            {
                Logger.LogException(ex);
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
                bool isPending = TcpClient.Client.ReceiveAsync(SocketBuffer.SocketArgs);
                if(!isPending)
                {
                    callback(this, SocketBuffer.SocketArgs);
                }
            }
            catch (ObjectDisposedException)
            {

            }
        }
    }
}
