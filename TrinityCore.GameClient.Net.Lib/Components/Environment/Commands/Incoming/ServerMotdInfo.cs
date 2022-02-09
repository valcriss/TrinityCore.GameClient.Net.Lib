using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class ServerMotdInfo : ReceivablePacket<WorldCommand>
    {
        internal string Motd { get; set; }
        internal override void LoadData()
        {
            uint lines = ReadUInt32();
            Motd = string.Empty;
            for (int i = 0; i < lines; i++) Motd += ReadCString() + (i != lines - 1 ? "\n" : null);
        }
    }
}
