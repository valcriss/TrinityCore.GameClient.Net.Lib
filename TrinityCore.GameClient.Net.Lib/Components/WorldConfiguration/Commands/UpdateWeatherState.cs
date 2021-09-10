using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class UpdateWeatherState : WorldReceivablePacket
    {
        public WeatherState State { get; set; }
        public float Intensity { get; set; }
        public byte Abrupt { get; set; }
        public UpdateWeatherState(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            State = (WeatherState)ReadUInt32();
            Intensity = ReadSingle();
            Abrupt = ReadByte();
        }
    }
}
