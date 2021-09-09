using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public abstract class SendablePacket : Packet
    {
        protected SendablePacket(byte[] content = null, int readIndex = 0) : base(content, readIndex)
        {
        }
    }
}
