using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    public class AchievementCriteria
    {
        public ulong Counter { get; set; }
        public uint CriteriaId { get; set; }
        public DateTime Date { get; set; }
    }
}
