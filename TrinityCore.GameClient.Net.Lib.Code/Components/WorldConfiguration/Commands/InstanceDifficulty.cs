using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class InstanceDifficulty : WorldReceivablePacket
    {
        internal Difficulty Difficulty { get; set; }
        internal Difficulty RaidDynamicDifficulty { get; set; }

        internal InstanceDifficulty(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Difficulty = (Difficulty)ReadUInt32();
            RaidDynamicDifficulty = (Difficulty)ReadUInt32();
        }
    }
}
