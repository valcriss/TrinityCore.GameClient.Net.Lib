using TrinityCore.GameClient.Net.Lib.Components.Social.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Incoming
{
    public class TextEmoteInfo : ReceivablePacket<WorldCommand>
    {
        #region Public Properties

        public ulong Guid { get; set; }
        public string Name { get; set; }
        public TextEmotes TextEmote { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Guid = ReadUInt64();
            TextEmote = (TextEmotes)ReadUInt32();
            ReadUInt32();
            Name = ReadUInt32String();
        }

        #endregion Internal Methods
    }
}