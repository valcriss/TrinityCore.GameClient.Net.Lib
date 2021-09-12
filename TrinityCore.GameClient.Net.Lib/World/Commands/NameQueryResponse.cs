using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class NameQueryResponse : WorldReceivablePacket
    {
        internal bool Found { get; set; }
        internal ulong Guid { get; set; }
        internal string Name { get; set; }

        internal NameQueryResponse(ReceivablePacket receivablePacket) : base(receivablePacket)
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
