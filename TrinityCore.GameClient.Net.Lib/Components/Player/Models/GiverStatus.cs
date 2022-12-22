using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class GiverStatus
    {
        public ulong GiverGuid { get; set; }
        public QuestGiverStatus Status { get; set; }
    }
}
