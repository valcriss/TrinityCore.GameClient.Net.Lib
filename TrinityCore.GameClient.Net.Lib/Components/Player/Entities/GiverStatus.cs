using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    public class GiverStatus
    {
        public ulong GiverGuid { get; set; }
        public QuestGiverStatus Status { get; set; }
    }
}
