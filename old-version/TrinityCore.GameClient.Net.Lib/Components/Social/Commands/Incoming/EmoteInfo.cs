using TrinityCore.GameClient.Net.Lib.Components.Social.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Incoming
{
    internal class EmoteInfo : ReceivablePacket<WorldCommand>
    {
        #region Public Properties

        public Emote Emote { get; set; }
        public ulong Guid { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Emote = (Emote)ReadUInt32();
            Guid = ReadUInt64();
        }

        #endregion Internal Methods
    }
}