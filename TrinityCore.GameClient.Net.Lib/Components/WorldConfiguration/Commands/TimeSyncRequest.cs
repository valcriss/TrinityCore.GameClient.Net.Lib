using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class TimeSyncRequest : WorldReceivablePacket
    {
        public uint SyncNextCounter { get; set; }

        public TimeSyncRequest(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            SyncNextCounter = ReadUInt32();
        }
    }
}
