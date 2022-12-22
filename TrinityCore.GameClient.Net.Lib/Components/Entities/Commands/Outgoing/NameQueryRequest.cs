using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Outgoing
{
    internal class NameQueryRequest : WorldSendablePacket
    {
        internal NameQueryRequest(ulong guid) : base(WorldCommand.CMSG_NAME_QUERY)
        {
            Append(guid);
        }
    }
}
