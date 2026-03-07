using TrinityCore.GameClient.Net.Lib.Network.Auth.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Commands.Outgoing
{
    internal class AuthProofRequest : AuthSendablePacket
    {
        #region Internal Constructors

        internal AuthProofRequest(byte[] a, byte[] m1, byte[] crc) : base(AuthCommand.LOGON_PROOF)
        {
            Append(a);
            Append(m1);
            Append(crc);
            Append(0);
            Append(0);
        }

        #endregion Internal Constructors
    }
}