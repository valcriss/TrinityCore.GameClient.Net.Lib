using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class NameQueryRequest : WorldSendablePacket
    {
        internal NameQueryRequest(WorldSocket worldSocket, ulong guid) : base(worldSocket, WorldCommand.CMSG_NAME_QUERY)
        {
            Append(guid);
        }
    }
}
