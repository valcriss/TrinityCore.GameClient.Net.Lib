using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class ClientCacheVersion : WorldReceivablePacket
    {
        internal uint Version { get; set; }

        internal ClientCacheVersion(ReceivablePacket receivablePacket, int readIndex = 0) : base(receivablePacket, readIndex)
        {
            Version = ReadUInt32();
        }
    }
}
