using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class Talent
    {
        public int Group { get; set; }
        public uint TalentId { get; set; }
        public sbyte TalentRank { get; set; }
    }
}
