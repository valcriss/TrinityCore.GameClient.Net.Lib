using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class GiverStatus
    {
        #region Public Properties

        public ulong GiverGuid { get; set; }
        public QuestGiverStatus Status { get; set; }

        #endregion Public Properties
    }
}