using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class FeatureSystemStatusInfo : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal bool VoiceChatEnabled { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            ReadSByte();
            VoiceChatEnabled = ReadSByte() != 0;
        }

        #endregion Internal Methods
    }
}