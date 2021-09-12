using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class DungeonDifficulty : WorldReceivablePacket
    {
        internal Difficulty Difficulty { get; set; }
        internal bool IsInGroup { get; set; }

        internal DungeonDifficulty(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Difficulty = (Difficulty)ReadUInt32();
            ReadUInt32();
            IsInGroup = ReadUInt32() != 0;
        }
    }
}
