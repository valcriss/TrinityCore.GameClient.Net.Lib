using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class NameQueryRequest : WorldSendablePacket
    {
        public NameQueryRequest(WorldSocket worldSocket, ulong guid) : base(worldSocket, WorldCommand.CMSG_NAME_QUERY)
        {
            Append(guid);
        }
    }
}
