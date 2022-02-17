using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Models
{
    public class Reputation
    {
        #region Public Properties

        public FactionFlags Flags { get; set; }
        public uint Id { get; set; }
        public uint Standing { get; set; }

        #endregion Public Properties
    }
}