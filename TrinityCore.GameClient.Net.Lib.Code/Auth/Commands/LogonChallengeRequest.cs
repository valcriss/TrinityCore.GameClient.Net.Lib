using System;
using System.Net;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.Auth.Commands
{
    internal class LogonChallengeRequest : AuthSendablePacket
    {
        internal LogonChallengeRequest(string username, IPAddress address) : base(AuthCommand.LOGON_CHALLENGE)
        {
            Append(6);
            Append((ushort)(username.Length + 30));
            Append("WoW".ToCString());
            Append(new byte[] { 3, 3, 5 });
            Append(12340);
            Append("68x".ToCString());
            Append("toB".ToCString());
            Append(Encoding.ASCII.GetBytes("SUne"));
            Append((uint)0x3c);
            Append(BitConverter.ToUInt32(address.GetAddressBytes(), 0));
            Append((byte)username.Length);
            Append(Encoding.ASCII.GetBytes(username));
        }
    }
}