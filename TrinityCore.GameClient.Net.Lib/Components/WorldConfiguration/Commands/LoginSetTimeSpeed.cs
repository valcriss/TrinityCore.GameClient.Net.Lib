using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class LoginSetTimeSpeed : WorldReceivablePacket
    {
        public float NewTime { get; set; }
        public uint GameTime { get; set; }
        public int TimeHolidayOffset { get; set; }

        public LoginSetTimeSpeed(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            NewTime = ReadSingle();
            GameTime = ReadUInt32();
            TimeHolidayOffset = ReadInt32();
        }
    }
}
