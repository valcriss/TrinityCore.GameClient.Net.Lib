using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class NameQueryResponse : WorldReceivablePacket
    {
        public ulong Guid { get; set; }
        public string Name { get; set; }

        public NameQueryResponse(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Name = string.Empty;
            Guid = ReadPackedGuid();
            var end = ReadBoolean();
            if (end) //! True if not found, false if found
                return;
            Name = ReadCString();
        }
    }
}
