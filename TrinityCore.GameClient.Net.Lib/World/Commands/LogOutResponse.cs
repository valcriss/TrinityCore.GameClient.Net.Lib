using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class LogOutResponse : ReceivablePacket
    {
        public bool LogOut { get; set; }

        public LogOutResponse(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            bool logoutOk = ReadUInt32() == 0;
            bool instant = ReadByte() != 0;
            LogOut = instant || !logoutOk;
        }
    }
}
