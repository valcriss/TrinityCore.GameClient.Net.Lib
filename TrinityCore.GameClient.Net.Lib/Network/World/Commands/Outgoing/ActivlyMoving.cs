using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class ActivlyMoving : WorldSendablePacket
    {
        internal ActivlyMoving(ulong guid) : base(WorldCommand.CMSG_SET_ACTIVE_MOVER)
        {
            Append(guid);
        }
    }
}
