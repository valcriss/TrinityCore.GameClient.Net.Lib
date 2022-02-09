using System;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class LoginSetTimeSpeedInfo : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal DateTime GameTime { get; set; }
        internal int TimeHolidayOffset { get; set; }
        internal float TimeSpeed { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            GameTime = ReadPackedDate();
            TimeSpeed = ReadSingle();
            TimeHolidayOffset = ReadInt32();
        }

        #endregion Internal Methods
    }
}