using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class ClientCacheVersion : WorldReceivablePacket
    {
        public uint Version { get; set; }

        public ClientCacheVersion(ReceivablePacket receivablePacket, int readIndex = 0) : base(receivablePacket, readIndex)
        {
            Version = ReadUInt32();
        }
    }
}
