using System;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class AchievementCriteria
    {
        #region Public Properties

        public ulong Counter { get; set; }
        public uint CriteriaId { get; set; }
        public DateTime Date { get; set; }

        #endregion Public Properties
    }
}