using System;
using System.Linq;
using System.Numerics;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools
{
    public class MapBinaryReader
    {
        #region Public Properties

        public int BytesLeft => Buffer.Length - Index;
        public int Length => Buffer.Length;

        #endregion Public Properties

        #region Private Properties

        private byte[] Buffer { get; set; }

        private int Index { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public MapBinaryReader(byte[] buffer)
        {
            Buffer = buffer;
            Index = 0;
        }

        #endregion Public Constructors

        #region Public Methods

        public static MapBinaryReader FromFile(string file, int skip = 0, int take = -1)
        {
            try
            {
                byte[] buffer = System.IO.File.ReadAllBytes(file);
                if (take == -1 || buffer.Length - skip < take)
                {
                    return new MapBinaryReader(buffer.Skip(skip).ToArray());
                }
                return new MapBinaryReader(buffer.Skip(skip).Take(take).ToArray());
            }
            catch (Exception)
            {
                return null;
            }
        }

        public byte ReadByte()
        {
            byte value = Buffer[Index];
            MoveIndex(1);
            return value;
        }

        public byte[] ReadBytes(int len)
        {
            byte[] bytes = new byte[len];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = ReadByte();
            }
            return bytes;
        }

        public float ReadFloat()
        {
            float value = BitConverter.ToSingle(Buffer, Index);
            MoveIndex(4);
            return value;
        }

        public float[] ReadFloats(int len)
        {
            float[] floats = new float[len];
            for (int i = 0; i < floats.Length; i++)
            {
                floats[i] = ReadFloat();
            }
            return floats;
        }

        public int ReadInt()
        {
            int value = BitConverter.ToInt32(Buffer, Index);
            MoveIndex(4);
            return value;
        }

        public long ReadInt64()
        {
            long value = BitConverter.ToInt64(Buffer, Index);
            MoveIndex(8);
            return value;
        }

        public uint ReadUint()
        {
            uint value = BitConverter.ToUInt32(Buffer, Index);
            MoveIndex(4);
            return value;
        }

        public ushort ReadUShort()
        {
            ushort value = BitConverter.ToUInt16(Buffer, Index);
            MoveIndex(2);
            return value;
        }

        public ushort[] ReadUShorts(int len)
        {
            ushort[] ushorts = new ushort[len];
            for (int i = 0; i < ushorts.Length; i++)
            {
                ushorts[i] = ReadUShort();
            }
            return ushorts;
        }

        public Vector3 ReadVector3()
        {
            float x = BitConverter.ToSingle(Buffer, Index);
            MoveIndex(4);
            float y = BitConverter.ToSingle(Buffer, Index);
            MoveIndex(4);
            float z = BitConverter.ToSingle(Buffer, Index);
            MoveIndex(4);
            return new Vector3(x, y, z);
        }

        #endregion Public Methods

        #region Private Methods

        private void MoveIndex(int length)
        {
            Index += length;
        }

        #endregion Private Methods
    }
}

