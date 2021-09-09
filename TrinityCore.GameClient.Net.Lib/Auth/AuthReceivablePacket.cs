using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Auth
{
    public class AuthReceivablePacket : ReceivablePacket
    {
        public new AuthCommand Command => (AuthCommand)base.Command;

        public AuthReceivablePacket(AuthCommand command, byte[] content) : base((byte)command, content)
        {
        }

        public AuthReceivablePacket(ReceivablePacket receivable, int readIndex = 0) : base(receivable, readIndex)
        {
        }
    }
}
