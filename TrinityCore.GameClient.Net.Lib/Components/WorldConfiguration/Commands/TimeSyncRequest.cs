using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class TimeSyncRequest : WorldReceivablePacket
    {
        internal uint SyncNextCounter { get; set; }

        internal TimeSyncRequest(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            SyncNextCounter = ReadUInt32();
        }
    }
}
