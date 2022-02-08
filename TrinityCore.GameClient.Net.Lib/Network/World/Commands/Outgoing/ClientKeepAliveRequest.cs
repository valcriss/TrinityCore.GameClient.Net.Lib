using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class ClientKeepAliveRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal ClientKeepAliveRequest() : base(WorldCommand.CMSG_KEEP_ALIVE)
        {
        }

        #endregion Internal Constructors
    }
}