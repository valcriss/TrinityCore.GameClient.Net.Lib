using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class ClientLogOutResponse : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal bool LogOut { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            bool logoutOk = ReadUInt32() == 0;
            bool instant = ReadByte() != 0;
            LogOut = instant || !logoutOk;
            base.LoadData();
        }

        #endregion Internal Methods
    }
}