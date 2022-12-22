using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class TalentCollection
    {
        public List<Talent> Talents { get; set; }
        public uint UnSpendPoints { get; set; }

        public TalentCollection()
        {
            Talents = new List<Talent>();
            UnSpendPoints= 0;
        }

        public TalentCollection(List<Talent> talents, uint unSpendPoints)
        {
            Talents = talents;
            UnSpendPoints = unSpendPoints;
        }
    }
}
