using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands
{
    public class DestroyObject : WorldReceivablePacket
    {
        public UInt64 DestroyedGuid { get; set; }
        public bool OnDeath { get; set; }

        public DestroyObject(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            DestroyedGuid = ReadUInt64();
            OnDeath = ReadSByte() != 0;
        }
    }
}
