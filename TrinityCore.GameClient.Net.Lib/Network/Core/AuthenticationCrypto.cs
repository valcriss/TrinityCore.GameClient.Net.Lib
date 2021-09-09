using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Crypto;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public enum CryptoAuthStatus
    {
        UNINITIALIZED,
        PENDING,
        READY
    }

    public class AuthenticationCrypto
    {
        private static readonly byte[] DecryptionKey =
        {
            0xCC, 0x98, 0xAE, 0x04, 0xE8, 0x97, 0xEA, 0xCA,
            0x12, 0xDD, 0xC0, 0x93, 0x42, 0x91, 0x53, 0x57
        };

        private static readonly byte[] EncryptionKey =
        {
            0xC2, 0xB3, 0x72, 0x3C, 0xC6, 0xAE, 0xD9, 0xB5,
            0x34, 0x3C, 0x53, 0xEE, 0x2F, 0x43, 0x67, 0xCE
        };

        private Arc4 _decryptionStream;
        private Arc4 _encryptionStream;
        public CryptoAuthStatus Status { get; private set; }

        public AuthenticationCrypto()
        {
            Status = CryptoAuthStatus.UNINITIALIZED;
        }

        public void Decrypt(byte[] data, int start, int count)
        {
            if (Status == CryptoAuthStatus.READY)
                _decryptionStream.Process(data, start, count);
        }

        public void Encrypt(byte[] data, int start, int count)
        {
            if (Status == CryptoAuthStatus.READY)
                _encryptionStream.Process(data, start, count);
        }

        public void Initialize(byte[] sessionKey)
        {
            // create RC4-drop[1024] stream
            using (HMACSHA1 outputHmac = new HMACSHA1(EncryptionKey))
            {
                _encryptionStream = new Arc4(outputHmac.ComputeHash(sessionKey));
            }

            _encryptionStream.Process(new byte[1024], 0, 1024);

            // create RC4-drop[1024] stream
            using (HMACSHA1 inputHmac = new HMACSHA1(DecryptionKey))
            {
                _decryptionStream = new Arc4(inputHmac.ComputeHash(sessionKey));
            }

            _decryptionStream.Process(new byte[1024], 0, 1024);

            Status = CryptoAuthStatus.READY;
        }

        [Obsolete("NYI", true)]
        public void Pending()
        {
            Status = CryptoAuthStatus.PENDING;
        }
    }
}
