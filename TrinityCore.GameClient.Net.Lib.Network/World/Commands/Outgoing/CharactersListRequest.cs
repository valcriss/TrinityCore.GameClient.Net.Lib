using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class CharactersListRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal CharactersListRequest() : base(WorldCommand.CMSG_CHAR_ENUM)
        {
        }

        #endregion Internal Constructors
    }
}