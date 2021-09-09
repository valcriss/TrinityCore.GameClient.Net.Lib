using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Auth
{
    public class AuthSendablePacket : SendablePacket
    {
        private AuthCommand Command { get; }

        public AuthSendablePacket(AuthCommand command) : base(new byte[0])
        {
            Command = command;
            Reset();
        }

        protected void Reset()
        {
            Buffer = null;
            Append((byte)Command);
        }
    }
}
