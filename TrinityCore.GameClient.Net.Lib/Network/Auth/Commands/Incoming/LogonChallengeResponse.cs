using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Commands.Incoming
{
    internal class LogonChallengeResponse : ReceivablePacket<AuthCommand>
    {
        #region Internal Properties

        internal byte[] B { get; set; }
        internal AuthResult Error { get; set; }
        internal byte[] G { get; set; }
        internal BigInteger Key { get; set; }
        internal byte[] N { get; set; }
        internal byte[] Proof { get; set; }
        internal byte[] Salt { get; set; }
        internal byte[] Unk3 { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            ReadByte();
            Error = (AuthResult)ReadByte();
            if (Error != AuthResult.SUCCESS)
                return;

            B = ReadBytes(32);
            ReadByte();
            G = ReadBytes(1);
            ReadByte();
            N = ReadBytes(32);
            Salt = ReadBytes(32);
            Unk3 = ReadBytes(16);
            ReadByte();

            base.LoadData();
        }

        #endregion Internal Methods
    }
}