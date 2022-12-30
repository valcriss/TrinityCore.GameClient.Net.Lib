using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class AddonInfo : ReceivablePacket<WorldCommand>
    {
        #region Internal Methods

        internal override void LoadData()
        {
            // Nothing to load is revelant for the moment
        }

        #endregion Internal Methods
    }
}