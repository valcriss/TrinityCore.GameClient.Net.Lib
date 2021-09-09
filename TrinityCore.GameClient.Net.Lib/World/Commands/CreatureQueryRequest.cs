using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class CreatureQueryRequest : WorldSendablePacket
    {
        public CreatureQueryRequest(WorldSocket worldSocket, uint creatureId, ulong guid) : base(worldSocket,
            WorldCommand.CMSG_CREATURE_QUERY)
        {
            Append(creatureId);
            Append(guid);
        }
    }
}
