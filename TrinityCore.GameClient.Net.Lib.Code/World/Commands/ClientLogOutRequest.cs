using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class ClientLogOutRequest : WorldSendablePacket
    {
        internal ClientLogOutRequest(WorldSocket worldSocket) : base(worldSocket, WorldCommand.CMSG_LOGOUT_REQUEST)
        {
        }
    }
}
