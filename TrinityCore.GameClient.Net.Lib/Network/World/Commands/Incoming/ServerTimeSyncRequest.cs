using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class ServerTimeSyncRequest : ReceivablePacket<WorldCommand>
    {
        internal uint SyncNextCounter { get; set; }

        internal ServerTimeSyncRequest(ReceivablePacket<WorldCommand> receivablePacket) : base(receivablePacket)
        {
            SyncNextCounter = ReadUInt32();
        }
    }
}
