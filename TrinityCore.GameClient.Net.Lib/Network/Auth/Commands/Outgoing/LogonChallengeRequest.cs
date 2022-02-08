using System;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Commands.Outgoing
{
    internal class LogonChallengeRequest : AuthSendablePacket
    {
        #region Internal Constructors

        internal LogonChallengeRequest(AuthCredentials credentials) : base(Enums.AuthCommand.LOGON_CHALLENGE)
        {
            Append(6);
            Append((ushort)(credentials.Username.Length + 30));
            Append("WoW".ToCString());
            Append(new byte[] { 3, 3, 5 });
            Append(12340);
            Append("68x".ToCString());
            Append("toB".ToCString());
            Append(Encoding.ASCII.GetBytes("SUne"));
            Append((uint)0x3c);
            Append(BitConverter.ToUInt32(credentials.IPAddress.GetAddressBytes(), 0));
            Append((byte)credentials.Username.Length);
            Append(Encoding.ASCII.GetBytes(credentials.Username));
        }

        #endregion Internal Constructors
    }
}