using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class InstanceDifficulty : WorldReceivablePacket
    {
        public Difficulty Difficulty { get; set; }
        public Difficulty RaidDynamicDifficulty { get; set; }

        public InstanceDifficulty(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Difficulty = (Difficulty)ReadUInt32();
            RaidDynamicDifficulty = (Difficulty)ReadUInt32();
        }
    }
}
