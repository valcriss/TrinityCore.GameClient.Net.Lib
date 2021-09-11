using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class NameQueryResponse : WorldReceivablePacket
    {
        public bool Found { get; set; }
        public ulong Guid { get; set; }
        public string Name { get; set; }

        public NameQueryResponse(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Name = string.Empty;
            Guid = ReadPackedGuid();
            Found = !ReadBoolean();
            if (!Found) //! True if not found, false if found
                return;
            Name = ReadCString();
        }
    }
}
