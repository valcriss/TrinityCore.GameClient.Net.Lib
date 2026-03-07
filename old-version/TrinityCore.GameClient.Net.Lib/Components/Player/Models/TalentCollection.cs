using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class TalentCollection
    {
        #region Public Properties

        public List<Talent> Talents { get; set; }
        public uint UnSpendPoints { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public TalentCollection()
        {
            Talents = new List<Talent>();
            UnSpendPoints = 0;
        }

        public TalentCollection(List<Talent> talents, uint unSpendPoints)
        {
            Talents = talents;
            UnSpendPoints = unSpendPoints;
        }

        #endregion Public Constructors
    }
}