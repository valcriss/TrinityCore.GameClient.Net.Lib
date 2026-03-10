using System.Security.Cryptography;

namespace TrinityCore.GameClient.Net.Protocol.Crypto;

public sealed class WorldCrypto
{
    private static readonly byte[] DecryptionKey =
    [
        0xCC, 0x98, 0xAE, 0x04, 0xE8, 0x97, 0xEA, 0xCA,
        0x12, 0xDD, 0xC0, 0x93, 0x42, 0x91, 0x53, 0x57
    ];

    private static readonly byte[] EncryptionKey =
    [
        0xC2, 0xB3, 0x72, 0x3C, 0xC6, 0xAE, 0xD9, 0xB5,
        0x34, 0x3C, 0x53, 0xEE, 0x2F, 0x43, 0x67, 0xCE
    ];

    private Arc4? _decryptor;
    private Arc4? _encryptor;

    public bool IsInitialized { get; private set; }

    public void Initialize(byte[] sessionKey)
    {
        using var outHmac = new HMACSHA1(EncryptionKey);
        _encryptor = Arc4.Create(outHmac.ComputeHash(sessionKey));
        _encryptor.Process(new byte[1024], 0, 1024);

        using var inHmac = new HMACSHA1(DecryptionKey);
        _decryptor = Arc4.Create(inHmac.ComputeHash(sessionKey));
        _decryptor.Process(new byte[1024], 0, 1024);

        IsInitialized = true;
    }

    public void Encrypt(byte[] data, int offset, int count)
    {
        if (!IsInitialized || _encryptor is null)
        {
            return;
        }

        _encryptor.Process(data, offset, count);
    }

    public void Decrypt(byte[] data, int offset, int count)
    {
        if (!IsInitialized || _decryptor is null)
        {
            return;
        }

        _decryptor.Process(data, offset, count);
    }
}
