using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class UpdateWeatherState : WorldReceivablePacket
    {
        internal WeatherState State { get; set; }
        internal float Intensity { get; set; }
        internal byte Abrupt { get; set; }
        internal UpdateWeatherState(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            State = (WeatherState)ReadUInt32();
            Intensity = ReadSingle();
            Abrupt = ReadByte();
        }
    }
}
