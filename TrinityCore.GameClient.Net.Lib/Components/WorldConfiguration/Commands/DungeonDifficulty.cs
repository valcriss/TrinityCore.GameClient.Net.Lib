using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class DungeonDifficulty : WorldReceivablePacket
    {
        public Difficulty Difficulty { get; set; }
        public bool IsInGroup { get; set; }

        public DungeonDifficulty(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Difficulty = (Difficulty)ReadUInt32();
            ReadUInt32();
            IsInGroup = ReadUInt32() != 0;
        }
    }
}
