using System.Buffers.Binary;
using System.Net;
using System.Text;
using TrinityCore.GameClient.Net.Protocol.Common;

namespace TrinityCore.GameClient.Net.Protocol.Auth;

public static class AuthPacketCodec
{
    public static byte[] BuildLogonChallengePacket(string usernameUpper, IPAddress localAddress)
    {
        var payload = new List<byte>();
        payload.Add(6);
        payload.AddRange(BitConverter.GetBytes((ushort)(usernameUpper.Length + 30)));
        payload.AddRange("WoW".ToCString());
        payload.AddRange([3, 3, 5]);
        payload.AddRange(BitConverter.GetBytes((ushort)12340));
        payload.AddRange("68x".ToCString());
        payload.AddRange("toB".ToCString());
        payload.AddRange(Encoding.ASCII.GetBytes("SUne"));
        payload.AddRange(BitConverter.GetBytes(0x3cu));
        payload.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReadUInt32LittleEndian(localAddress.GetAddressBytes())));
        payload.Add((byte)usernameUpper.Length);
        payload.AddRange(Encoding.ASCII.GetBytes(usernameUpper));

        var data = new byte[payload.Count + 1];
        data[0] = (byte)AuthCommand.LogonChallenge;
        payload.CopyTo(data, 1);
        return data;
    }

    public static AuthChallengeResponse ParseLogonChallengePayload(byte[] payload)
    {
        var index = 0;
        _ = payload[index++];
        var result = (AuthResult)payload[index++];
        if (result != AuthResult.Success)
        {
            return new AuthChallengeResponse(result, [], [], [], [], []);
        }

        var b = payload[index..(index + 32)];
        index += 32;
        _ = payload[index++];
        var g = payload[index..(index + 1)];
        index += 1;
        _ = payload[index++];
        var n = payload[index..(index + 32)];
        index += 32;
        var salt = payload[index..(index + 32)];
        index += 32;
        var unk3 = payload[index..(index + 16)];
        return new AuthChallengeResponse(result, b, g, n, salt, unk3);
    }

    public static byte[] BuildLogonProofPacket(byte[] clientPublicEphemeral, byte[] clientM1, byte[] crc)
    {
        var data = new byte[1 + clientPublicEphemeral.Length + clientM1.Length + crc.Length + 2];
        data[0] = (byte)AuthCommand.LogonProof;
        var offset = 1;
        Buffer.BlockCopy(clientPublicEphemeral, 0, data, offset, clientPublicEphemeral.Length);
        offset += clientPublicEphemeral.Length;
        Buffer.BlockCopy(clientM1, 0, data, offset, clientM1.Length);
        offset += clientM1.Length;
        Buffer.BlockCopy(crc, 0, data, offset, crc.Length);
        return data;
    }

    public static (AuthResult Result, byte[] M2) ParseLogonProofPayload(byte[] payload)
    {
        var result = (AuthResult)payload[0];
        if (result != AuthResult.Success)
        {
            return (result, []);
        }

        var m2 = payload[1..21];
        return (result, m2);
    }

    public static byte[] BuildRealmListPacket()
    {
        return [(byte)AuthCommand.RealmList, 0, 0, 0, 0];
    }

    public static IReadOnlyList<RealmEntry> ParseRealmListPayload(byte[] payload)
    {
        var realms = new List<RealmEntry>();
        var index = 0;
        _ = BinaryPrimitives.ReadUInt16LittleEndian(payload[index..]);
        index += 2;
        _ = BinaryPrimitives.ReadUInt32LittleEndian(payload[index..]);
        index += 4;
        var count = BinaryPrimitives.ReadUInt16LittleEndian(payload[index..]);
        index += 2;
        for (var i = 0; i < count; i++)
        {
            _ = payload[index++];
            _ = payload[index++];
            var flags = payload[index++];

            var name = ReadCString(payload, ref index);
            var fullAddress = ReadCString(payload, ref index);
            var split = fullAddress.Split(':');
            var address = split[0];
            var port = split.Length > 1 && int.TryParse(split[1], out var p) ? p : 8085;

            _ = BinaryPrimitives.ReadSingleLittleEndian(payload[index..]);
            index += 4;
            _ = payload[index++];
            _ = payload[index++];
            var realmId = payload[index++];

            if ((flags & 4) != 0)
            {
                index += 5;
            }

            realms.Add(new RealmEntry(realmId, name, address, port));
        }

        return realms;
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
