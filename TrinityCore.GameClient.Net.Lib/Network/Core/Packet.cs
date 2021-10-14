using System;
using System.Linq;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public abstract class Packet
    {
        internal int ReadIndex { get; set; }
        protected byte[] Buffer { get; set; }

        protected Packet(byte[] content = null, int readIndex = 0)
        {
            ReadIndex = readIndex;
            Buffer = content;
        }

        internal void AppendPacketGuid(UInt64 guid)
        {
            byte[] packGuid = new byte[8 + 1];
            packGuid[0] = 0;
            var size = 1;
            for (byte i = 0; guid != 0; ++i)
            {
                if ((guid & 0xFF) != 0)
                {
                    packGuid[0] |= (byte)(1 << i);
                    packGuid[size] = (byte)(guid & 0xFF);
                    ++size;
                }

                guid >>= 8;
            }
            Append(packGuid.Take(size).ToArray());
        }

        internal void Append(byte value)
        {
            Buffer = Buffer.Append(value);
        }

        internal void Append(ushort value)
        {
            Buffer = Buffer.Append(BitConverter.GetBytes(value));
        }

        internal void Append(float value)
        {
            Buffer = Buffer.Append(BitConverter.GetBytes(value));
        }

        internal void Append(uint value)
        {
            Buffer = Buffer.Append(BitConverter.GetBytes(value));
        }

        internal void Append(ulong value)
        {
            Buffer = Buffer.Append(BitConverter.GetBytes(value));
        }

        internal void Append(byte[] value)
        {
            Buffer = Buffer.Append(value);
        }

        internal virtual byte[] GetBuffer()
        {
            return Buffer;
        }

        internal DateTime ReadPackedTime()
        {
            var packedDate = ReadInt32();
            var minute = packedDate & 0x3F;
            var hour = (packedDate >> 6) & 0x1F;
            // var weekDay = (packedDate >> 11) & 7;
            var day = (packedDate >> 14) & 0x3F;
            var month = (packedDate >> 20) & 0xF;
            var year = (packedDate >> 24) & 0x1F;
            // var something2 = (packedDate >> 29) & 3; always 0

            return new DateTime(2000, 1, 1).AddYears(year).AddMonths(month).AddDays(day).AddHours(hour)
                .AddMinutes(minute);
        }

        internal Vector3 ReadVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }

        protected byte PeekByte()
        {
            return Buffer[ReadIndex];
        }

        protected bool ReadBoolean()
        {
            byte b = ReadByte();
            return b > 0;
        }

        protected byte ReadByte()
        {
            byte value = Buffer[ReadIndex];
            ReadIndex++;
            return value;
        }

        protected byte[] ReadBytes(int length)
        {
            byte[] buffer = new byte[length];
            Array.ConstrainedCopy(Buffer, ReadIndex, buffer, 0, length);
            ReadIndex += length;
            return buffer;
        }

        protected string ReadCString()
        {
            string value = Buffer.ReadCString(ReadIndex, out int length);
            ReadIndex += length;
            return value;
        }

        protected int ReadInt32()
        {
            int value = BitConverter.ToInt32(Buffer, ReadIndex);
            ReadIndex += 4;
            return value;
        }

        protected long ReadInt64()
        {
            long value = BitConverter.ToInt64(Buffer, ReadIndex);
            ReadIndex += 8;
            return value;
        }

        protected ulong ReadPackedGuid()
        {
            var mask = ReadByte();

            if (mask == 0)
                return 0;

            ulong res = 0;

            var i = 0;
            while (i < 8)
            {
                if ((mask & (1 << i)) != 0)
                    res += (ulong)ReadByte() << (i * 8);

                i++;
            }

            return res;
        }

        protected sbyte ReadSByte()
        {
            sbyte value = (sbyte)Buffer[ReadIndex];
            ReadIndex++;
            return value;
        }

        protected float ReadSingle()
        {
            if(Buffer.Length - 4 > ReadIndex)
            {
                float value = BitConverter.ToSingle(Buffer, ReadIndex);
                ReadIndex += 4;
                return value;
            }
            ReadIndex = Buffer.Length - 1;
            return 0;
        }

        protected ushort ReadUInt16()
        {
            ushort value = BitConverter.ToUInt16(Buffer, ReadIndex);
            ReadIndex += 2;
            return value;
        }

        protected uint ReadUInt32()
        {
            uint value = BitConverter.ToUInt32(Buffer, ReadIndex);
            ReadIndex += 4;
            return value;
        }

        protected ulong ReadUInt64()
        {
            ulong value = BitConverter.ToUInt64(Buffer, ReadIndex);
            ReadIndex += 8;
            return value;
        }
    }
}
