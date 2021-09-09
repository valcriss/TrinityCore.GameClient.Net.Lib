using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class LoginVerifyResponse : WorldReceivablePacket
    {
        public uint MapId { get; set; }
        public float O { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public LoginVerifyResponse(ReceivablePacket receivablePacket, int readIndex = 0) : base(receivablePacket,
            readIndex)
        {
            MapId = ReadUInt32();
            X = ReadSingle();
            Y = ReadSingle();
            Z = ReadSingle();
            O = ReadSingle();
        }
    }
}
