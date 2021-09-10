using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class ServerMotd : WorldReceivablePacket
    {
        public string Motd { get; set; }

        public ServerMotd(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            uint lines = ReadUInt32();
            Motd = string.Empty;
            for (int i = 0; i < lines; i++) Motd += ReadCString() + (i != lines - 1 ? "\n" : null);
        }
    }
}
