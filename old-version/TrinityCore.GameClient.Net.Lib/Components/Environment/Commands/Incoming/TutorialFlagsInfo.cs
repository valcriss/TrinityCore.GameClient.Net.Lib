using TrinityCore.GameClient.Net.Lib.Components.Environment.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class TutorialFlagsInfo : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal TutorialFlags TutorialFlags { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            TutorialFlags = new TutorialFlags();
            for (int i = 0; i < TutorialFlags.MAX_ACCOUNT_TUTORIAL_VALUES; i++) TutorialFlags.Values[i] = ReadUInt32();
        }

        #endregion Internal Methods
    }
}