using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class KeepAlive : WorldSendablePacket
    {
        internal KeepAlive(WorldSocket worldSocket) : base(worldSocket, WorldCommand.CMSG_KEEP_ALIVE)
        {
        }
    }
}
