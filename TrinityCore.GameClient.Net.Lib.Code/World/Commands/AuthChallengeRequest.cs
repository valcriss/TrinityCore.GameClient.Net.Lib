using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class AuthChallengeRequest : WorldReceivablePacket
    {
        internal uint ServerSeed { get; set; }

        internal AuthChallengeRequest(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            uint one = ReadUInt32();
            ServerSeed = ReadUInt32();

            BigInteger seed1 = ReadBytes(16).ToBigInteger();
            BigInteger seed2 = ReadBytes(16).ToBigInteger();
        }
    }
}
