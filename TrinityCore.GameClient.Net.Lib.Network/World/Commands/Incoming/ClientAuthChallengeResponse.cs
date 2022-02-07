using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class ClientAuthChallengeResponse : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal bool Success { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal ClientAuthChallengeResponse(ReceivablePacket<WorldCommand> receivablePacket) : base(receivablePacket)
        {
            CommandDetail detail = (CommandDetail)ReadByte();

            ReadUInt32();
            ReadByte();
            ReadUInt32();
            ReadByte();

            Success = detail == CommandDetail.AUTH_OK;
        }

        #endregion Internal Constructors
    }
}