using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using TrinityCore.GameClient.Net.Protocol.Common;

namespace TrinityCore.GameClient.Net.Protocol.Auth;

public static class AuthProofCalculator
{
    public static AuthProofData Compute(string usernameUpper, string password, AuthChallengeResponse challenge)
    {
        var passwordHash = SHA1.HashData(Encoding.ASCII.GetBytes($"{usernameUpper}:{password}".ToUpperInvariant()));

        var b = challenge.B.ToPositiveBigInteger();
        var g = challenge.G.ToPositiveBigInteger();
        var n = challenge.N.ToPositiveBigInteger();
        var salt = challenge.Salt;

        var x = SHA1.HashData(ByteExtensions.ConcatBytes(salt, passwordHash)).ToPositiveBigInteger();
        var k = new BigInteger(3);

        BigInteger a;
        BigInteger y;
        do
        {
            var random = RandomNumberGenerator.GetBytes(19);
            a = random.ToPositiveBigInteger();
            y = BigInteger.ModPow(g, a, n);
        } while (BigInteger.ModPow(y, BigInteger.One, n) == 0);

        var u = SHA1.HashData(ByteExtensions.ConcatBytes(y.ToCleanByteArray(), b.ToCleanByteArray())).ToPositiveBigInteger();
        var s = BigInteger.ModPow((b + k * (n - BigInteger.ModPow(g, x, n))) % n, a + u * x, n);

        var sData = s.ToCleanByteArray();
        if (sData.Length < 32)
        {
            var padded = new byte[32];
            Buffer.BlockCopy(sData, 0, padded, 32 - sData.Length, sData.Length);
            sData = padded;
        }

        var keyData = new byte[40];
        var temp = new byte[16];

        for (var i = 0; i < 16; i++)
        {
            temp[i] = sData[i * 2];
        }

        var keyHash = SHA1.HashData(temp);
        for (var i = 0; i < 20; i++)
        {
            keyData[i * 2] = keyHash[i];
        }

        for (var i = 0; i < 16; i++)
        {
            temp[i] = sData[(i * 2) + 1];
        }

        keyHash = SHA1.HashData(temp);
        for (var i = 0; i < 20; i++)
        {
            keyData[(i * 2) + 1] = keyHash[i];
        }

        var sessionKey = keyData.ToPositiveBigInteger();

        var nHash = SHA1.HashData(n.ToCleanByteArray());
        var gHash = SHA1.HashData(g.ToCleanByteArray());
        var gNHash = new byte[20];
        for (var i = 0; i < 20; i++)
        {
            gNHash[i] = (byte)(nHash[i] ^ gHash[i]);
        }

        var userHash = SHA1.HashData(Encoding.ASCII.GetBytes(usernameUpper));
        var m1 = SHA1.HashData(ByteExtensions.ConcatBytes(gNHash, userHash, salt, y.ToCleanByteArray(), b.ToCleanByteArray(), sessionKey.ToCleanByteArray()));
        var m2 = SHA1.HashData(ByteExtensions.ConcatBytes(y.ToCleanByteArray(), m1, keyData));

        return new AuthProofData(sessionKey, sessionKey.ToCleanByteArray(), y.ToCleanByteArray(), m1, m2);
    }

    public static bool ValidateServerProof(byte[] expectedM2, byte[] serverM2)
    {
        if (expectedM2.Length != serverM2.Length)
        {
            return false;
        }

        for (var i = 0; i < expectedM2.Length; i++)
        {
            if (expectedM2[i] != serverM2[i])
            {
                return false;
            }
        }

        return true;
    }
}
