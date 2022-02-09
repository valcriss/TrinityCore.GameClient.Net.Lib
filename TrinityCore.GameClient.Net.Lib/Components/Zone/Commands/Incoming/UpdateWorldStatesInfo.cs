using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Zone.Commands.Incoming
{
    internal class UpdateWorldStatesInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        internal uint Id { get; set; }
        internal uint Value { get; set; }
        internal override void LoadData()
        {
            Id = ReadUInt32();
            Value = ReadUInt32();
        }
    }
}
