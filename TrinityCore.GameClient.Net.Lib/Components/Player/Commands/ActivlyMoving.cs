using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class ActivlyMoving : WorldSendablePacket
    {
        internal ActivlyMoving(WorldSocket worldSocket, ulong guid) : base(worldSocket, WorldCommand.CMSG_SET_ACTIVE_MOVER)
        {
            Append(guid);
        }
    }
}
