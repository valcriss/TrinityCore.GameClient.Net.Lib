using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class AuthChallengeResponse : WorldReceivablePacket
    {
        public bool Success { get; set; }

        public AuthChallengeResponse(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            CommandDetail detail = (CommandDetail)ReadByte();

            uint billingTimeRemaining = ReadUInt32();
            byte billingFlags = ReadByte();
            uint billingTimeRested = ReadUInt32();
            byte expansion = ReadByte();

            Success = detail == CommandDetail.AUTH_OK;
        }
    }
}
