using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Crypto;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using HashAlgorithm = TrinityCore.GameClient.Net.Lib.Network.Crypto.HashAlgorithm;

namespace TrinityCore.GameClient.Net.Lib.Auth.Commands
{
    internal class LogonChallengeResponse : AuthReceivablePacket
    {
        internal AuthProofRequest AuthProofRequest { get; set; }
        internal AuthResult Error { get; set; }
        internal BigInteger Key { get; set; }
        internal byte[] Proof { get; set; }
        private byte[] B { get; }
        private byte[] G { get; }
        private byte GLen { get; }
        private byte[] N { get; }
        private byte NLen { get; }
        private byte[] Salt { get; }
        private byte SecurityFlags { get; }
        private byte Unk2 { get; }
        private byte[] Unk3 { get; }

        internal LogonChallengeResponse(ReceivablePacket receivable, string username, string password) : base(receivable)
        {
            Unk2 = ReadByte();
            Error = (AuthResult)ReadByte();
            if (Error != AuthResult.SUCCESS)
                return;

            B = ReadBytes(32);
            GLen = ReadByte();
            G = ReadBytes(1);
            NLen = ReadByte();
            N = ReadBytes(32);
            Salt = ReadBytes(32);
            Unk3 = ReadBytes(16);
            SecurityFlags = ReadByte();
            AuthProofRequest = GetClientAuthProof(username, password);
            Proof = AuthProofRequest.M2;
        }

        private AuthProofRequest GetClientAuthProof(string username, string password)
        {
            string authString = $"{username}:{password}";
            byte[] passwordHash = HashAlgorithm.SHA1.Hash(Encoding.ASCII.GetBytes(authString.ToUpper()));

            BigInteger y, a;
            BigInteger k = new BigInteger(3);

            #region Receive and initialize

            BigInteger b = B.ToBigInteger();
            BigInteger g = G.ToBigInteger();
            BigInteger n = N.ToBigInteger();
            Salt.ToBigInteger();
            Unk3.ToBigInteger();

            #endregion Receive and initialize

            #region Hash password

            BigInteger x = HashAlgorithm.SHA1.Hash(Salt, passwordHash).ToBigInteger();

            Logger.Log("---====== shared password hash ======---", LogLevel.VERBOSE);
            Logger.Log($"g={g.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);
            Logger.Log($"x={x.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);
            Logger.Log($"N={n.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);

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

            Logger.Log("---====== Send data to server: ======---", LogLevel.VERBOSE);
            Logger.Log($"A={y.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);

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

            Key = keyData.ToBigInteger();

            Logger.Log("---====== Compute session key ======---", LogLevel.VERBOSE);
            Logger.Log($"u={u.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);
            Logger.Log($"S={s.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);
            Logger.Log($"K={Key.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);

            #endregion Compute session key

            #region Generate crypto proof

            // XOR the hashes of N and g together
            byte[] gNHash = new byte[20];

            byte[] nHash = HashAlgorithm.SHA1.Hash(n.ToCleanByteArray());
            for (int i = 0; i < 20; ++i)
                gNHash[i] = nHash[i];
            Logger.Log($"nHash={nHash.ToHexString()}", LogLevel.VERBOSE);

            byte[] gHash = HashAlgorithm.SHA1.Hash(g.ToCleanByteArray());
            for (int i = 0; i < 20; ++i)
                gNHash[i] ^= gHash[i];
            Logger.Log($"gHash={gHash.ToHexString()}", LogLevel.VERBOSE);

            // hash username
            byte[] userHash = HashAlgorithm.SHA1.Hash(Encoding.ASCII.GetBytes(username));

            // our proof
            byte[] m1Hash = HashAlgorithm.SHA1.Hash
            (
                gNHash,
                userHash,
                Salt,
                y.ToCleanByteArray(),
                b.ToCleanByteArray(),
                Key.ToCleanByteArray()
            );

            Logger.Log("---====== Client proof: ======---", LogLevel.VERBOSE);
            Logger.Log($"gNHash={gNHash.ToHexString()}", LogLevel.VERBOSE);
            Logger.Log($"userHash={userHash.ToHexString()}", LogLevel.VERBOSE);
            Logger.Log($"salt={Salt.ToHexString()}", LogLevel.VERBOSE);
            Logger.Log($"A={y.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);
            Logger.Log($"B={b.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);
            Logger.Log($"key={Key.ToCleanByteArray().ToHexString()}", LogLevel.VERBOSE);

            Logger.Log("---====== Send proof to server: ======---", LogLevel.VERBOSE);
            Logger.Log($"M={m1Hash.ToHexString()}", LogLevel.VERBOSE);

            // expected proof for server
            byte[] m2 = HashAlgorithm.SHA1.Hash(y.ToCleanByteArray(), m1Hash, keyData);

            #endregion Generate crypto proof

            return new AuthProofRequest(y.ToCleanByteArray(), m1Hash, new byte[20], m2);
        }
    }
}