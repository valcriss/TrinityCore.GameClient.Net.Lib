using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class LoginSetTimeSpeedInfo : ReceivablePacket<WorldCommand>
    {
        internal float TimeSpeed { get; set; }
        internal DateTime GameTime { get; set; }
        internal int TimeHolidayOffset { get; set; }
        internal override void LoadData()
        {
            GameTime = ReadPackedDate();
            TimeSpeed = ReadSingle();
            TimeHolidayOffset = ReadInt32();
        }
    }
}
