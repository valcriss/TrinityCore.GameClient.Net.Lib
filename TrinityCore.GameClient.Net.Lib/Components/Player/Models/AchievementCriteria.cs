using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class AchievementCriteria
    {
        public ulong Counter { get; set; }
        public uint CriteriaId { get; set; }
        public DateTime Date { get; set; }
    }
}
