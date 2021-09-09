using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public class ReceivablePacket : Packet
    {
        public uint Command { get; set; }
        public byte[] Content => Buffer;

        protected ReceivablePacket(uint command, byte[] content, int readIndex = 0)
        {
            Command = command;
            Buffer = content;
            ReadIndex = readIndex;
        }

        protected ReceivablePacket(ReceivablePacket receivablePacket, int readIndex = 0)
        {
            Command = receivablePacket.Command;
            Buffer = receivablePacket.Content;
            ReadIndex = readIndex;
        }

        public virtual ReceivablePacket Inflate()
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
                        return new ReceivablePacket(Command, memoryStream.GetBuffer());
                    }
                }
            }
        }
    }
}
