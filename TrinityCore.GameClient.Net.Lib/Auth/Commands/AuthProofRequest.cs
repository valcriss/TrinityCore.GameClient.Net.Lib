using TrinityCore.GameClient.Net.Lib.Auth.Enums;

namespace TrinityCore.GameClient.Net.Lib.Auth.Commands
{
    public class AuthProofRequest : AuthSendablePacket
    {
        public byte[] M2 { get; set; }

        public AuthProofRequest(byte[] a, byte[] m1, byte[] crc, byte[] m2) : base(AuthCommand.LOGON_PROOF)
        {
            M2 = m2;
            Append(a);
            Append(m1);
            Append(crc);
            Append(0);
            Append(0);
        }
    }
}