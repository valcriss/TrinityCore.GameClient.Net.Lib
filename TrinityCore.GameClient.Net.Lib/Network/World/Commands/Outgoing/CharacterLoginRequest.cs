using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class CharacterLoginRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal CharacterLoginRequest(Character character) : base(WorldCommand.CMSG_PLAYER_LOGIN)
        {
            Append(character.GUID);
        }

        #endregion Internal Constructors
    }
}