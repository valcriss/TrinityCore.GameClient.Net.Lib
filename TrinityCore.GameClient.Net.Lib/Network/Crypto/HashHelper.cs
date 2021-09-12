using System;
using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Network.Crypto
{
    using CryptoNS = System.Security.Cryptography;

    internal enum HashAlgorithm
    {
        SHA1
    }

    internal static class HashHelper
    {
        private delegate byte[] HashFunction(params byte[][] data);

        private static readonly Dictionary<HashAlgorithm, HashFunction> HashFunctions;

        static HashHelper()
        {
            HashFunctions = new Dictionary<HashAlgorithm, HashFunction> { [HashAlgorithm.SHA1] = Sha1 };
        }

        internal static byte[] Hash(this HashAlgorithm algorithm, params byte[][] data)
        {
            return HashFunctions[algorithm](data);
        }

        private static byte[] Combine(byte[][] buffers)
        {
            int length = 0;
            foreach (var buffer in buffers)
                length += buffer.Length;

            byte[] result = new byte[length];

            int position = 0;

            foreach (var buffer in buffers)
            {
                Buffer.BlockCopy(buffer, 0, result, position, buffer.Length);
                position += buffer.Length;
            }

            return result;
        }

        private static byte[] Sha1(params byte[][] data)
        {
            using (CryptoNS.SHA1 alg = CryptoNS.SHA1.Create())
            {
                return alg.ComputeHash(Combine(data));
            }
        }
    }
}