using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands
{
    public class SplineMoveSetMode : WorldReceivablePacket
    {
        public UInt64 Guid { get; set; }

        public SplineMoveSetMode(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Guid = ReadPackedGuid();
        }
    }
}
