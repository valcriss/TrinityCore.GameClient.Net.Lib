using System;
using System.Numerics;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Network.Tools
{
    internal static class BytesExtensions
    {
        #region Internal Methods

        internal static byte[] Append(this byte[] array1, byte value)
        {
            return array1.Append(new[] { value });
        }

        internal static byte[] Append(this byte[] array1, byte[] array2, int length = -1)
        {
            if (array1 == null) array1 = new byte[0];
            if (array2 == null) return array1;
            if (length == -1) length = array2.Length;

            byte[] buffer = new byte[array1.Length + length];

            Array.ConstrainedCopy(array1, 0, buffer, 0, array1.Length);
            Array.ConstrainedCopy(array2, 0, buffer, array1.Length, length);

            return buffer;
        }

        internal static string BytesToString(this byte[] value)
        {
            return BitConverter.ToString(value).Replace("-", "");
        }

        internal static BigInteger ModPow(this BigInteger value, BigInteger pow, BigInteger mod)
        {
            return BigInteger.ModPow(value, pow, mod);
        }

        internal static string ReadCString(this byte[] data, int start, out int length)
        {
            StringBuilder builder = new StringBuilder();
            length = 0;
            while (true)
            {
                byte letter = data[start];
                start++;
                length++;
                if (letter == 0)
                    break;

                builder.Append((char)letter);
            }

            return builder.ToString();
        }

        internal static byte[] Split(this byte[] data, int startIndex, int length)
        {
            byte[] buffer = new byte[length];
            Array.ConstrainedCopy(data, startIndex, buffer, 0, length);
            return buffer;
        }

        internal static byte[] SubArray(this byte[] array, int start, int count)
        {
            byte[] subArray = new byte[count];
            Array.Copy(array, start, subArray, 0, count);
            return subArray;
        }

        internal static BigInteger ToBigInteger(this byte[] array)
        {
            byte[] temp;
            if ((array[array.Length - 1] & 0x80) == 0x80)
            {
                temp = new byte[array.Length + 1];
                temp[array.Length] = 0;
            }
            else
            {
                temp = new byte[array.Length];
            }

            Array.Copy(array, temp, array.Length);
            return new BigInteger(temp);
        }

        internal static byte[] ToCleanByteArray(this BigInteger b)
        {
            byte[] array = b.ToByteArray();
            if (array[array.Length - 1] != 0)
                return array;

            byte[] temp = new byte[array.Length - 1];
            Array.Copy(array, temp, temp.Length);
            return temp;
        }

        internal static byte[] ToCString(this string str)
        {
            byte[] utf8StringBytes = Encoding.UTF8.GetBytes(str);
            byte[] data = new byte[utf8StringBytes.Length + 1];
            Array.Copy(utf8StringBytes, data, utf8StringBytes.Length);
            data[data.Length - 1] = 0;
            return data;
        }

        internal static string ToHexString(this byte[] array)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = array.Length - 1; i >= 0; --i)
                builder.Append(array[i].ToString("X2"));

            return builder.ToString();
        }

        #endregion Internal Methods
    }
}