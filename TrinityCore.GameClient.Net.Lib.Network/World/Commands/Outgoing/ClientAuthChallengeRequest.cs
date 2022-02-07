using System;
using System.Numerics;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;
using TrinityCore.GameClient.Net.Lib.Network.Security;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class ClientAuthChallengeRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal ClientAuthChallengeRequest(AuthCredentials credentials,
            WorldServerInfo serverInfo, uint serverSeed) : base(WorldCommand.CLIENT_AUTH_SESSION)
        {
            var rand = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            rand.GetBytes(bytes);
            BigInteger ourSeed = bytes.ToBigInteger();

            uint zero = 0;

            byte[] authResponse = HashAlgorithm.SHA1.Hash
            (
                Encoding.ASCII.GetBytes(credentials.Username),
                BitConverter.GetBytes(zero),
                BitConverter.GetBytes((uint)ourSeed),
                BitConverter.GetBytes(serverSeed),
                credentials.SessionKey.ToCleanByteArray()
            );

            Append((uint)12340); // client build
            Append(zero);
            Append(credentials.Username.ToCString());
            Append(zero);
            Append((uint)ourSeed);
            Append(zero);
            Append(zero);
            Append(serverInfo.Id);
            Append((ulong)zero);
            Append(authResponse);
            Append(zero);
        }

        #endregion Internal Constructors
    }
}