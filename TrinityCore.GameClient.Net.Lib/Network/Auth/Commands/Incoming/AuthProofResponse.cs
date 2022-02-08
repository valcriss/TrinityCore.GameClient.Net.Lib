using TrinityCore.GameClient.Net.Lib.Network.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Commands.Incoming
{
    internal class AuthProofResponse : ReceivablePacket<AuthCommand>
    {
        #region Internal Properties

        internal AuthResult Error { get; set; }
        internal byte[] M2 { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal override void LoadData()
        {
            Error = (AuthResult)ReadByte();
            if (Error != AuthResult.SUCCESS)
                return;

            M2 = ReadBytes(20);
            ReadUInt32();
            ReadUInt32();
            ReadUInt16();
            base.LoadData();
        }

        #endregion Internal Constructors
    }
}