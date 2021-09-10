using System;
using System.Numerics;
using HashAlgorithm = TrinityCore.GameClient.Net.Lib.Network.Crypto.HashAlgorithm;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Entities;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Crypto;
using System.Security.Cryptography;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class ClientAuthChallenge : WorldSendablePacket
    {
        public ClientAuthChallenge(WorldSocket worldSocket, string username, uint serverSeed, BigInteger key,
            WorldServerInfo serverInfo) : base(worldSocket, WorldCommand.CLIENT_AUTH_SESSION)
        {
            var rand = RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            rand.GetBytes(bytes);
            BigInteger ourSeed = bytes.ToBigInteger();

            uint zero = 0;

            byte[] authResponse = HashAlgorithm.SHA1.Hash
            (
                Encoding.ASCII.GetBytes(username.ToUpper()),
                BitConverter.GetBytes(zero),
                BitConverter.GetBytes((uint)ourSeed),
                BitConverter.GetBytes(serverSeed),
                key.ToCleanByteArray()
            );

            Append((uint)12340); // client build
            Append(zero);
            Append(username.ToUpper().ToCString());
            Append(zero);
            Append((uint)ourSeed);
            Append(zero);
            Append(zero);
            Append(serverInfo.Id);
            Append((ulong)zero);
            Append(authResponse);
            Append(zero);
        }
    }
}
