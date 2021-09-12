using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class LogOutResponse : ReceivablePacket
    {
        internal bool LogOut { get; set; }

        internal LogOutResponse(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            bool logoutOk = ReadUInt32() == 0;
            bool instant = ReadByte() != 0;
            LogOut = instant || !logoutOk;
        }
    }
}
