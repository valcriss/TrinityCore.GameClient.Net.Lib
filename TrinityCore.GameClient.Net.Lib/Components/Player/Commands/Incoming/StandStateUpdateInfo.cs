using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class StandStateUpdateInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        public UnitStandStateType StandType { get; set; }

        internal override void LoadData()
        {
            StandType = (UnitStandStateType)ReadSByte();
        }
    }
}
