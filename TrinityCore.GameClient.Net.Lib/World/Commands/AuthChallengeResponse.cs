using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class AuthChallengeResponse : WorldReceivablePacket
    {
        internal bool Success { get; set; }

        internal AuthChallengeResponse(ReceivablePacket receivablePacket) : base(receivablePacket)
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
