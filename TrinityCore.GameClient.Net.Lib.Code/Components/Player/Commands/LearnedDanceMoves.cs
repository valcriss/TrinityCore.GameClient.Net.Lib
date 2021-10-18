using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class LearnedDanceMoves : WorldReceivablePacket
    {
        internal LearnedDanceMoves(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            uint v1 = ReadUInt32();
            uint v2 = ReadUInt32();

            if (v1 + v2 != 0)
                Logger.Log("LearnedDanceMoves sum should be 0 according to trinity code ???", LogLevel.ERROR);
        }
    }
}
