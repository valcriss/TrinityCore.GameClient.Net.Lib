using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Network.World
{
    public delegate void CharacterLoggedInEventHandler(Character character);

    public delegate void CharacterLoggedOutEventHandler();
}