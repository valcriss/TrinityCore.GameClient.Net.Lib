using TrinityCore.GameClient.Net.Lib.Network.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Commands.Incoming
{
    internal class AuthProofResponse : ReceivablePacket<AuthCommand>
    {
        #region Internal Properties

        internal AuthResult Error { get; set; }
        internal byte[] M2 { get; }

        #endregion Internal Properties

        #region Internal Constructors

        internal AuthProofResponse(ReceivablePacket<AuthCommand> receivable) : base(receivable)
        {
            Error = (AuthResult)ReadByte();
            if (Error != AuthResult.SUCCESS)
                return;

            M2 = ReadBytes(20);
            uint unk1 = ReadUInt32();
            uint unk2 = ReadUInt32();
            uint unk3 = ReadUInt16();
        }

        #endregion Internal Constructors
    }
}