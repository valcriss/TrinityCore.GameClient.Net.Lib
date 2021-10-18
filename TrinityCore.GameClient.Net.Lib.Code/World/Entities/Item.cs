using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    public class Item : Packet
    {
        public uint DisplayId { get; set; }
        public byte InventoryType { get; set; }

        internal Item(byte[] data, int readIndex = 0) : base(data, readIndex)
        {
            DisplayId = ReadUInt32();
            InventoryType = ReadByte();
            ReadUInt32();
        }
    }
}
