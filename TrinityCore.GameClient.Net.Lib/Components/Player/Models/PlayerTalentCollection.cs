using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class PlayerTalentCollection : TalentCollection
    {
        public List<Glyph> Glyphs { get; set; }

        public PlayerTalentCollection()
        {
            Glyphs = new List<Glyph>();
        }

        public PlayerTalentCollection(List<Talent> talents, uint unSpendPoints, List<Glyph> glyphs) : base(talents, unSpendPoints)
        {
            Glyphs = glyphs;
        }
    }
}
