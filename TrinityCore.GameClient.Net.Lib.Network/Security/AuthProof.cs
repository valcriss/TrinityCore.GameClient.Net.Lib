using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.Network.Security
{
    internal class AuthProof
    {
        #region Internal Properties

        internal BigInteger Key { get; set; }
        internal byte[] M1Hash { get; set; }
        internal byte[] Proof { get; set; }
        internal byte[] Y { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal static AuthProof ComputeAuthProof(AuthCredentials credentials, LogonChallengeResponse challenge)
        {
            string authString = $"{credentials.Username}:{credentials.Password}";
            byte[] passwordHash = HashAlgorithm.SHA1.Hash(Encoding.ASCII.GetBytes(authString.ToUpper()));

            BigInteger y, a;
            BigInteger k = new BigInteger(3);

            #region Receive and initialize

            BigInteger b = challenge.B.ToBigInteger();
            BigInteger g = challenge.G.ToBigInteger();
            BigInteger n = challenge.N.ToBigInteger();
            challenge.Salt.ToBigInteger();
            challenge.Unk3.ToBigInteger();

            #endregion Receive and initialize

            #region Hash password

            BigInteger x = HashAlgorithm.SHA1.Hash(challenge.Salt, passwordHash).ToBigInteger();

            #endregion Hash password

            #region Create random key pair

            RandomNumberGenerator rand = RandomNumberGenerator.Create();

            do
            {
                byte[] randBytes = new byte[19];
                rand.GetBytes(randBytes);
                a = randBytes.ToBigInteger();

                y = g.ModPow(a, n);
            } while (y.ModPow(1, n) == 0);

            #endregion Create random key pair

            #region Compute session key

            BigInteger u = HashAlgorithm.SHA1.Hash(y.ToCleanByteArray(), b.ToCleanByteArray()).ToBigInteger();

            // compute session key
            BigInteger s = ((b + k * (n - g.ModPow(x, n))) % n).ModPow(a + u * x, n);
            byte[] keyHash;
            byte[] sData = s.ToCleanByteArray();
            if (sData.Length < 32)
            {
                byte[] tmpBuffer = new byte[32];
                System.Buffer.BlockCopy(sData, 0, tmpBuffer, 32 - sData.Length, sData.Length);
                sData = tmpBuffer;
            }

            byte[] keyData = new byte[40];
            byte[] temp = new byte[16];

            // take every even indices byte, hash, store in even indices
            for (int i = 0; i < 16; ++i)
                temp[i] = sData[i * 2];
            keyHash = HashAlgorithm.SHA1.Hash(temp);
            for (int i = 0; i < 20; ++i)
                keyData[i * 2] = keyHash[i];

            // do the same for odd indices
            for (int i = 0; i < 16; ++i)
                temp[i] = sData[i * 2 + 1];
            keyHash = HashAlgorithm.SHA1.Hash(temp);
            for (int i = 0; i < 20; ++i)
                keyData[i * 2 + 1] = keyHash[i];

            BigInteger currentKey = keyData.ToBigInteger();

            #endregion Compute session key

            #region Generate crypto proof

            // XOR the hashes of N and g together
            byte[] gNHash = new byte[20];

            byte[] nHash = HashAlgorithm.SHA1.Hash(n.ToCleanByteArray());
            for (int i = 0; i < 20; ++i)
                gNHash[i] = nHash[i];

            byte[] gHash = HashAlgorithm.SHA1.Hash(g.ToCleanByteArray());
            for (int i = 0; i < 20; ++i)
                gNHash[i] ^= gHash[i];

            // hash username
            byte[] userHash = HashAlgorithm.SHA1.Hash(Encoding.ASCII.GetBytes(credentials.Username));

            // our proof
            byte[] m1Hash = HashAlgorithm.SHA1.Hash
            (
                gNHash,
                userHash,
                challenge.Salt,
                y.ToCleanByteArray(),
                b.ToCleanByteArray(),
                currentKey.ToCleanByteArray()
            );

            // expected proof for server
            byte[] m2 = HashAlgorithm.SHA1.Hash(y.ToCleanByteArray(), m1Hash, keyData);

            #endregion Generate crypto proof

            return new AuthProof() { Y = y.ToCleanByteArray(), M1Hash = m1Hash, Proof = m2, Key = currentKey };
        }

        internal static bool IsProofValid(byte[] receivedProof, byte[] computedProof)
        {
            if (receivedProof == null) return false;
            bool equal = receivedProof.Length == 20;
            for (int i = 0; i < receivedProof.Length && equal; ++i)
                if (!(equal = receivedProof[i] == computedProof[i]))
                    break;
            return equal;
        }

        #endregion Internal Methods
    }
}