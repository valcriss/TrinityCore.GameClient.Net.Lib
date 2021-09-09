using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World
{
    public class WorldReceivablePacket : ReceivablePacket
    {
        public WorldReceivablePacket(WorldCommand command, byte[] content, int readIndex = 0) : base((uint)command,
            content, readIndex)
        {
        }

        public WorldReceivablePacket(ReceivablePacket receivablePacket, int readIndex = 0) : base(receivablePacket,
            readIndex)
        {
        }

        public new WorldReceivablePacket Inflate()
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
