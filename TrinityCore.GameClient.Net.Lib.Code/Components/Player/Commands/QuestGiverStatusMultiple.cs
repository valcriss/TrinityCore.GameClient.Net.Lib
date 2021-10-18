using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class QuestGiverStatusMultiple : WorldReceivablePacket
    {
        internal List<GiverStatus> GiverStatuses { get; set; }

        internal QuestGiverStatusMultiple(ReceivablePacket receivablePacket) : base(receivablePacket)
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
