using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Auth
{
    internal class AuthSendablePacket : SendablePacket
    {
        private AuthCommand Command { get; }

        internal AuthSendablePacket(AuthCommand command) : base(new byte[0])
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
