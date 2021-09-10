using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class ClientLogOutRequest : WorldSendablePacket
    {
        public ClientLogOutRequest(WorldSocket worldSocket) : base(worldSocket, WorldCommand.CMSG_LOGOUT_REQUEST)
        {
        }
    }
}
