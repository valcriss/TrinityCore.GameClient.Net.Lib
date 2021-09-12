using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class LoginSetTimeSpeed : WorldReceivablePacket
    {
        internal float NewTime { get; set; }
        internal uint GameTime { get; set; }
        internal int TimeHolidayOffset { get; set; }

        internal LoginSetTimeSpeed(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            NewTime = ReadSingle();
            GameTime = ReadUInt32();
            TimeHolidayOffset = ReadInt32();
        }
    }
}
