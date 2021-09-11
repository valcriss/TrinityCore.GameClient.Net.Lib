using System;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands
{
    public class PowerUpdate : WorldReceivablePacket
    {
        public UInt64 Guid { get; set; }
        public Powers Power { get; set; }
        public UInt32 Value { get; set; }

        public PowerUpdate(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Guid = ReadPackedGuid();
            Power = (Powers)ReadSByte();
            Value = ReadUInt32();
        }
    }
}
