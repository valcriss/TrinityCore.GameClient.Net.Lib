using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class AuthChallengeRequest : WorldReceivablePacket
    {
        public uint ServerSeed { get; set; }

        public AuthChallengeRequest(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            uint one = ReadUInt32();
            ServerSeed = ReadUInt32();

            BigInteger seed1 = ReadBytes(16).ToBigInteger();
            BigInteger seed2 = ReadBytes(16).ToBigInteger();
        }
    }
}
