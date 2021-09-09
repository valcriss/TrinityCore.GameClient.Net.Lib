

using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class CharactersListRequest : WorldSendablePacket
    {
        public CharactersListRequest(WorldSocket worldSocket) : base(worldSocket, WorldCommand.CMSG_CHAR_ENUM)
        {
        }
    }
}