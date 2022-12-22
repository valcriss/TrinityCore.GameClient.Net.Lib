using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class PlayerTalentCollection : TalentCollection
    {
        #region Public Properties

        public List<Glyph> Glyphs { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public PlayerTalentCollection()
        {
            Glyphs = new List<Glyph>();
        }

        public PlayerTalentCollection(List<Talent> talents, uint unSpendPoints, List<Glyph> glyphs) : base(talents, unSpendPoints)
        {
            Glyphs = glyphs;
        }

        #endregion Public Constructors
    }
}