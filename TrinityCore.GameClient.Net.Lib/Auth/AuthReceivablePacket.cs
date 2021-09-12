using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Auth
{
    internal class AuthReceivablePacket : ReceivablePacket
    {
        internal new AuthCommand Command => (AuthCommand)base.Command;

        internal AuthReceivablePacket(AuthCommand command, byte[] content) : base((byte)command, content)
        {
        }

        internal AuthReceivablePacket(ReceivablePacket receivable, int readIndex = 0) : base(receivable, readIndex)
        {
        }
    }
}
