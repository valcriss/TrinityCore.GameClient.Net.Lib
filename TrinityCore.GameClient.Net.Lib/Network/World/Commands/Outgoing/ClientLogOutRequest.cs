using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class ClientLogOutRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal ClientLogOutRequest() : base(WorldCommand.CMSG_LOGOUT_REQUEST)
        {
        }

        #endregion Internal Constructors
    }
}