using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class CreatureQueryRequest : WorldSendablePacket
    {
        internal CreatureQueryRequest(uint creatureId, ulong guid) : base(WorldCommand.CMSG_CREATURE_QUERY)
        {
            Append(creatureId);
            Append(guid);
        }
    }
}
