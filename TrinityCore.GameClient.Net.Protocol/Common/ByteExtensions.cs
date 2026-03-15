using System.Numerics;
using System.Text;

namespace TrinityCore.GameClient.Net.Protocol.Common;

internal static class ByteExtensions
{
    public static byte[] ToCString(this string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var result = new byte[bytes.Length + 1];
        bytes.CopyTo(result, 0);
        return result;
    }

    public static BigInteger ToPositiveBigInteger(this byte[] value)
    {
        if (value.Length == 0)
        {
            return BigInteger.Zero;
        }

        if ((value[^1] & 0x80) == 0)
        {
            return new BigInteger(value);
        }

        var extended = new byte[value.Length + 1];
        Array.Copy(value, extended, value.Length);
        return new BigInteger(extended);
    }

    public static byte[] ToCleanByteArray(this BigInteger value)
    {
        var bytes = value.ToByteArray();
        return bytes.Length > 1 && bytes[^1] == 0
            ? bytes[..^1]
            : bytes;
    }

    public static byte[] ConcatBytes(params byte[][] buffers)
    {
        var total = 0;
        foreach (var buffer in buffers)
        {
            total += buffer.Length;
        }

        var result = new byte[total];
        var offset = 0;
        foreach (var buffer in buffers)
        {
            Buffer.BlockCopy(buffer, 0, result, offset, buffer.Length);
            offset += buffer.Length;
        }

        return result;
    }
}
