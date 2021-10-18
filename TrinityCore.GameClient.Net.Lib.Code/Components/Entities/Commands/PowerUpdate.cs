using System;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands
{
    internal class PowerUpdate : WorldReceivablePacket
    {
        internal UInt64 Guid { get; set; }
        internal Powers Power { get; set; }
        internal UInt32 Value { get; set; }

        internal PowerUpdate(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Guid = ReadPackedGuid();
            Power = (Powers)ReadSByte();
            Value = ReadUInt32();
        }
    }
}
