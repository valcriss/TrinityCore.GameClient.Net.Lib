using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Auth.Commands
{
    internal class AuthProofResponse : AuthReceivablePacket
    {
        internal AuthResult Error { get; set; }
        internal bool IsValid { get; set; }
        private byte[] M2 { get; }
        private uint Unk1 { get; }
        private uint Unk2 { get; }
        private ushort Unk3 { get; }

        internal AuthProofResponse(ReceivablePacket receivable, byte[] m2) : base(receivable)
        {
            Error = (AuthResult)ReadByte();
            if (Error != AuthResult.SUCCESS)
                return;

            M2 = ReadBytes(20);
            Unk1 = ReadUInt32();
            Unk2 = ReadUInt32();
            Unk3 = ReadUInt16();

            IsValid = CheckServerAuthProof(m2);
        }

        private bool CheckServerAuthProof(byte[] m2)
        {
            if (m2 == null) return false;
            bool equal = m2.Length == 20;
            for (int i = 0; i < m2.Length && equal; ++i)
                if (!(equal = m2[i] == M2[i]))
                    break;
            return equal;
        }
    }
}