using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Outgoing
{
    internal class StandPositionRequest : WorldSendablePacket
    {
        internal StandPositionRequest(ulong guid, UnitStandStateType standType) : base(WorldCommand.CMSG_STANDSTATECHANGE)
        {
            Append((uint)standType);
        }
    }
}
