using System;
using System.IO;
using System.Net.Sockets;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth
{
    public class AuthSocket : GameSocket
    {
        #region Internal Properties

        internal PacketsHandler<AuthCommand> PacketsHandler { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal AuthSocket()
        {
            PacketsHandler = new PacketsHandler<AuthCommand>();
        }

        #endregion Internal Constructors

        #region Protected Methods

        protected override void ReceiveData()
        {
            Socket.Client.BeginReceive
            (
                SocketBuffer.ReceiveData, 0, 128,
                SocketFlags.None,
                PacketRead,
                null
            );
        }

        #endregion Protected Methods

        #region Private Methods

        private void PacketRead(IAsyncResult result)
        {
            try
            {
                int size = Socket.Client.EndReceive(result);

                if (size == 0)
                {
                    if (!Closing) RaiseSocketDisconnected();
                    return;
                }

                AuthCommand command = (AuthCommand)SocketBuffer.ReceiveData[0];
                byte[] content = SocketBuffer.ReceiveData.Split(1, SocketBuffer.ReceiveData.Length - 1);

                PacketsHandler.Handle(new ReceivablePacket<AuthCommand>(command, content));

                ReceiveData();
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
            }
            catch (SocketException ex)
            {
                Logger.Append(Logging.Enums.LogCategory.NETWORK, ex);
                RaiseSocketDisconnected();
            }
            catch (EndOfStreamException)
            {
                RaiseSocketDisconnected();
            }
        }

        #endregion Private Methods
    }
}