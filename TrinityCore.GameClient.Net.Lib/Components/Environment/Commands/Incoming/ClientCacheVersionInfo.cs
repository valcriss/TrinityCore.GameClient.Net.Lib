using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class ClientCacheVersionInfo : ReceivablePacket<WorldCommand>
    {
        internal uint Version { get; set; }
        internal override void LoadData()
        {            
            Version = ReadUInt32();
        }
    }
}
