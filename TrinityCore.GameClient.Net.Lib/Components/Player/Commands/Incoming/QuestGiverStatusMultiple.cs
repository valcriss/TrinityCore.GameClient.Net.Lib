using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class QuestGiverStatusMultiple : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        internal List<GiverStatus> GiverStatuses { get; set; }

        internal override void LoadData()
        {
            GiverStatuses = new List<GiverStatus>();
            uint count = ReadUInt32();
            for (int i = 0; i < count; i++)
                GiverStatuses.Add(new GiverStatus
                {
                    GiverGuid = ReadUInt64(),
                    Status = (QuestGiverStatus)ReadSByte()
                });
        }
    }
}
