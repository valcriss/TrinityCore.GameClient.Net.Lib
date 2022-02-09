using System;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public class Packet
    {
        #region Internal Properties

        internal int ReadIndex { get; set; }

        #endregion Internal Properties

        #region Protected Properties

        protected byte[] Buffer { get; set; }

        #endregion Protected Properties

        #region Protected Constructors

        protected Packet(byte[] content = null, int readIndex = 0)
        {
            ReadIndex = readIndex;
            Buffer = content;
        }

        #endregion Protected Constructors

        #region Internal Methods

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

        #endregion Internal Methods

        #region Protected Methods

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

        protected DateTime ReadPackedDate()
        {
            uint packed = ReadUInt32();
            
            int min = (int)(packed & 0x3F);
            int hour = (int)((packed >> 6) & 0x1F);
            int day = (int)(((packed >> 14) & 0x3F) + 1);
            int mon = (int)(((packed >> 20) & 0xF) + 1);
            int year = (int)(((packed >> 24) & 0x1F) + 2000);

            return new DateTime(year, mon, day, hour, min, 0);
        }

        protected float ReadSingle()
        {
            if (Buffer.Length - 4 > ReadIndex)
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

        #endregion Protected Methods
    }
}