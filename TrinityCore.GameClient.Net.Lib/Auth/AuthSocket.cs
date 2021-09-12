using System;
using System.IO;
using System.Net.Sockets;
using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.Auth
{
    public class AuthSocket : GameSocket
    {
        protected AuthPacketsHandler PacketsHandler { get; set; }

        internal AuthSocket(string host, int port) : base(host, port)
        {
            PacketsHandler = new AuthPacketsHandler();
        }

        protected override void ReceiveData()
        {
            TcpClient.Client.BeginReceive
            (
                SocketBuffer.ReceiveData, 0, 128,
                SocketFlags.None,
                BasicPacketRead,
                null
            );
        }

        private void BasicPacketRead(IAsyncResult result)
        {
            try
            {
                int size = TcpClient.Client.EndReceive(result);

                if (size == 0)
                {
                    RaiseSocketDisconnected();
                    return;
                }

                AuthCommand command = (AuthCommand)SocketBuffer.ReceiveData[0];
                byte[] content = SocketBuffer.ReceiveData.Split(1, SocketBuffer.ReceiveData.Length - 1);

                PacketsHandler.Handle(new AuthReceivablePacket(command, content));

                ReceiveData();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException ex)
            {
                Logger.LogException(ex);
                RaiseSocketDisconnected();
            }
            catch (EndOfStreamException)
            {
                RaiseSocketDisconnected();
            }
        }
    }
}
