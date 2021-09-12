using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class CharacterLoginRequest : WorldSendablePacket
    {
        internal CharacterLoginRequest(WorldSocket worldSocket, Character character) : base(worldSocket,
            WorldCommand.CMSG_PLAYER_LOGIN)
        {
            Append(character.GUID);
        }
    }
}
