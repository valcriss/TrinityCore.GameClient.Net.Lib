using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class KeepAlive : WorldSendablePacket
    {
        public KeepAlive(WorldSocket worldSocket) : base(worldSocket, WorldCommand.CMSG_KEEP_ALIVE)
        {
        }
    }
}
