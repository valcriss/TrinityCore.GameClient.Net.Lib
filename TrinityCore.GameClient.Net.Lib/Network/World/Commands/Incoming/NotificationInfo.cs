using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class NotificationInfo : ReceivablePacket<WorldCommand>
    {
        #region Public Properties

        public string Message { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Message = ReadCString();
        }

        #endregion Internal Methods
    }
}