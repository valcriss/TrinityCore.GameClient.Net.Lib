using System;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands
{
    internal class DestroyObject : WorldReceivablePacket
    {
        internal UInt64 DestroyedGuid { get; set; }
        internal bool OnDeath { get; set; }

        internal DestroyObject(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            DestroyedGuid = ReadUInt64();
            OnDeath = ReadSByte() != 0;
        }
    }
}
