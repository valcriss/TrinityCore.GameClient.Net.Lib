using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Models
{
    public class Reputation
    {
        #region Public Properties

        public FactionOptions Flags { get; set; }
        public uint Id { get; set; }
        public uint Standing { get; set; }

        #endregion Public Properties
    }
}