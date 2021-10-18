

using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class CharactersListRequest : WorldSendablePacket
    {
        internal CharactersListRequest(WorldSocket worldSocket) : base(worldSocket, WorldCommand.CMSG_CHAR_ENUM)
        {
        }
    }
}