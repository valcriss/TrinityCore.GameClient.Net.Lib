using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class ClientLogOutRequest : WorldSendablePacket
    {
        internal ClientLogOutRequest() : base(WorldCommand.CMSG_LOGOUT_REQUEST)
        {
        }
    }
}
