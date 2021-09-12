using System.IO;
using System.IO.Compression;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World
{
    internal class WorldReceivablePacket : ReceivablePacket
    {
        internal WorldReceivablePacket(WorldCommand command, byte[] content, int readIndex = 0) : base((uint)command,
            content, readIndex)
        {
        }

        internal WorldReceivablePacket(ReceivablePacket receivablePacket, int readIndex = 0) : base(receivablePacket,
            readIndex)
        {
        }

        internal new WorldReceivablePacket Inflate()
        {
            uint uncompressedSize = ReadUInt32();
            //Skip first 2 bytes used by zlib only
            ReadBytes(2);

            using (MemoryStream baseStream = new MemoryStream(ReadBytes(Buffer.Length - ReadIndex)))
            {
                using (DeflateStream decompressedStream = new DeflateStream(baseStream, CompressionMode.Decompress))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        decompressedStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        return new WorldReceivablePacket((WorldCommand)Command, memoryStream.GetBuffer());
                    }
                }
            }
        }

        public override string ToString()
        {
            return "" + (WorldCommand)Command + " (" + Content.Length + ")";
        }
    }
}
