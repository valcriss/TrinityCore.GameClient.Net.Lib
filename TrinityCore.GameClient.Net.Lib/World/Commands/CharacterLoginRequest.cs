using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class CharacterLoginRequest : WorldSendablePacket
    {
        public CharacterLoginRequest(WorldSocket worldSocket, Character character) : base(worldSocket,
            WorldCommand.CMSG_PLAYER_LOGIN)
        {
            Append(character.GUID);
        }
    }
}
