using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class ClientLogOutResponse : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal bool LogOut { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal ClientLogOutResponse(ReceivablePacket<WorldCommand> receivablePacket) : base(receivablePacket)
        {
            bool logoutOk = ReadUInt32() == 0;
            bool instant = ReadByte() != 0;
            LogOut = instant || !logoutOk;
        }

        #endregion Internal Constructors
    }
}