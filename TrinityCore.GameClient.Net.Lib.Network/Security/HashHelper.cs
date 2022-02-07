using System;
using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Network.Security
{
    using CryptoNS = System.Security.Cryptography;

    internal static class HashHelper
    {
        #region Private Delegates

        private delegate byte[] HashFunction(params byte[][] data);

        #endregion Private Delegates

        #region Private Fields

        private static readonly Dictionary<HashAlgorithm, HashFunction> HashFunctions;

        #endregion Private Fields

        #region Public Constructors

        static HashHelper()
        {
            HashFunctions = new Dictionary<HashAlgorithm, HashFunction> { [HashAlgorithm.SHA1] = Sha1 };
        }

        #endregion Public Constructors

        #region Internal Methods

        internal static byte[] Hash(this HashAlgorithm algorithm, params byte[][] data)
        {
            return HashFunctions[algorithm](data);
        }

        #endregion Internal Methods

        #region Private Methods

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

        #endregion Private Methods
    }

    internal enum HashAlgorithm
    {
        SHA1
    }
}