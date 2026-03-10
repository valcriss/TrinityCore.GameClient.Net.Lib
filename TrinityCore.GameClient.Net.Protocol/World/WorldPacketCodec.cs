using System.Buffers.Binary;
using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using TrinityCore.GameClient.Net.Protocol.Common;
using TrinityCore.GameClient.Net.Protocol.Crypto;

namespace TrinityCore.GameClient.Net.Protocol.World;

public static class WorldPacketCodec
{
    public static WorldPacketHeader ParseHeader(byte[] headerBuffer, int headerLength)
    {
        return headerLength switch
        {
            4 => new WorldPacketHeader(
                (ushort)(BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(0, 2)) - 2),
                (WorldOpcode)BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer.AsSpan(2, 2)),
                4),
            5 => new WorldPacketHeader(
                (ushort)((((headerBuffer[0] & 0x7F) << 16) | (headerBuffer[1] << 8) | headerBuffer[2]) - 2),
                (WorldOpcode)BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer.AsSpan(3, 2)),
                5),
            _ => throw new ArgumentOutOfRangeException(nameof(headerLength))
        };
    }

    public static byte[] BuildPacket(WorldOpcode opcode, byte[] payload, WorldCrypto crypto)
    {
        var data = new byte[6 + payload.Length];
        var header = new byte[6];

        // TrinityCore 3.3.5 client->server header:
        // - size: uint16 big-endian, includes cmd(4) + payload
        // - cmd: uint32 little-endian
        var packetSize = (ushort)(payload.Length + 4);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0, 2), packetSize);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(2, 4), (uint)opcode);
        crypto.Encrypt(header, 0, header.Length);

        header.CopyTo(data, 0);
        payload.CopyTo(data, 6);
        return data;
    }

    public static byte[] BuildAuthSessionPayload(
        string usernameUpper,
        uint realmId,
        uint serverSeed,
        BigInteger sessionKey,
        uint clientSeed)
    {
        var zero = 0u;
        var authDigest = SHA1.HashData(
            ByteExtensions.ConcatBytes(
                Encoding.ASCII.GetBytes(usernameUpper),
                BitConverter.GetBytes(zero),
                BitConverter.GetBytes(clientSeed),
                BitConverter.GetBytes(serverSeed),
                sessionKey.ToCleanByteArray()));

        var payload = new List<byte>();
        payload.AddRange(BitConverter.GetBytes(12340u));
        payload.AddRange(BitConverter.GetBytes(zero));
        payload.AddRange(usernameUpper.ToCString());
        payload.AddRange(BitConverter.GetBytes(zero));
        payload.AddRange(BitConverter.GetBytes(clientSeed));
        payload.AddRange(BitConverter.GetBytes(zero));
        payload.AddRange(BitConverter.GetBytes(zero));
        payload.AddRange(BitConverter.GetBytes(realmId));
        payload.AddRange(BitConverter.GetBytes(0ul));
        payload.AddRange(authDigest);
        payload.AddRange(BitConverter.GetBytes(zero));
        return payload.ToArray();
    }

    public static uint ParseServerAuthChallenge(byte[] payload)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(4, 4));
    }

    public static bool ParseServerAuthResponse(byte[] payload)
    {
        return payload.Length > 0 && payload[0] == (byte)WorldCommandDetail.AuthOk;
    }

    public static IReadOnlyList<WorldCharacterEntry> ParseCharacterList(byte[] payload)
    {
        var index = 0;
        var count = payload[index++];
        var list = new List<WorldCharacterEntry>(count);

        for (var i = 0; i < count; i++)
        {
            var guid = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(index, 8));
            index += 8;

            var name = ReadCString(payload, ref index);
            index += 3; // race, class, gender
            index += 5; // bytes skin/face/hair...
            var level = payload[index++];

            index += 4; // zone
            index += 4; // map
            index += 4 * 3; // xyz
            index += 4; // guild id
            index += 4; // flags
            index += 4; // customize
            index += 1; // first login
            index += 4; // pet info id
            index += 4; // pet level
            index += 4; // pet family

            index += 19 * 9; // equipped items
            index += 4 * 9; // bags

            list.Add(new WorldCharacterEntry(guid, name, level));
        }

        return list;
    }

    public static byte[] BuildCharacterEnumPayload() => [];

    public static byte[] BuildCharacterLoginPayload(ulong guid) => BitConverter.GetBytes(guid);
    public static byte[] BuildSetSelectionPayload(ulong guid) => BitConverter.GetBytes(guid);
    public static byte[] BuildAttackSwingPayload(ulong guid) => BitConverter.GetBytes(guid);
    public static byte[] BuildAttackStopPayload() => [];
    public static byte[] BuildRepopRequestPayload(bool checkInstance = false) => [checkInstance ? (byte)1 : (byte)0];
    public static byte[] BuildReclaimCorpsePayload(ulong corpseGuid = 0) => BitConverter.GetBytes(corpseGuid);
    public static byte[] BuildLogoutCancelPayload() => [];
    public static byte[] BuildMoveWorldportAckPayload() => [];
    public static byte[] BuildMoveTeleportAckPayload(ulong guid, uint sequenceIndex, uint movementTimeMs)
    {
        var payload = new List<byte>(20);
        AppendPackedGuid(payload, guid);
        payload.AddRange(BitConverter.GetBytes(sequenceIndex));
        payload.AddRange(BitConverter.GetBytes(movementTimeMs));
        return payload.ToArray();
    }

    public static byte[] BuildTimeSyncResponsePayload(uint counter, uint clientTimestamp)
    {
        var payload = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0, 4), counter);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4, 4), clientTimestamp);
        return payload;
    }

    public static uint ParseTimeSyncRequest(byte[] payload)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
    }

    public static byte[] DecompressUpdateObjectPayload(byte[] compressedPayload)
    {
        if (compressedPayload.Length < 5)
        {
            return [];
        }

        var uncompressedLength = BinaryPrimitives.ReadInt32LittleEndian(compressedPayload.AsSpan(0, 4));
        if (uncompressedLength <= 0)
        {
            return [];
        }

        using var source = new MemoryStream(compressedPayload, 4, compressedPayload.Length - 4);
        using var zlib = new ZLibStream(source, CompressionMode.Decompress);
        using var destination = new MemoryStream(uncompressedLength);
        zlib.CopyTo(destination);
        return destination.ToArray();
    }

    public static UpdateObjectBatch ParseUpdateObjectBatch(byte[] payload)
    {
        return UpdateObjectParser.Parse(payload);
    }

    public static LogoutResponseData ParseLogoutResponse(byte[] payload)
    {
        var resultCode = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
        var instant = payload.Length > 4 && payload[4] != 0;
        return new LogoutResponseData(resultCode, instant);
    }

    public static LoginVerifyWorldData ParseLoginVerifyWorld(byte[] payload)
    {
        var mapId = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(0, 4));
        var x = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(4, 4));
        var y = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(8, 4));
        var z = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(12, 4));
        var orientation = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(16, 4));
        return new LoginVerifyWorldData(mapId, x, y, z, orientation);
    }

    public static LoginVerifyWorldData ParseNewWorld(byte[] payload) => ParseLoginVerifyWorld(payload);

    public static int ParseTransferPendingMapId(byte[] payload)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(0, 4));
    }

    private static void AppendPackedGuid(List<byte> payload, ulong guid)
    {
        byte mask = 0;
        Span<byte> packed = stackalloc byte[8];
        var size = 0;
        for (var i = 0; i < 8; i++)
        {
            var b = (byte)((guid >> (i * 8)) & 0xFF);
            if (b == 0)
            {
                continue;
            }

            mask |= (byte)(1 << i);
            packed[size++] = b;
        }

        payload.Add(mask);
        for (var i = 0; i < size; i++)
        {
            payload.Add(packed[i]);
        }
    }

    private static string ReadCString(byte[] payload, ref int index)
    {
        var start = index;
        while (payload[index] != 0)
        {
            index++;
        }

        var value = Encoding.UTF8.GetString(payload, start, index - start);
        index++;
        return value;
    }
}
