using System;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands
{
    internal class SplineMoveSetMode : WorldReceivablePacket
    {
        internal UInt64 Guid { get; set; }

        internal SplineMoveSetMode(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Guid = ReadPackedGuid();
        }
    }
}
