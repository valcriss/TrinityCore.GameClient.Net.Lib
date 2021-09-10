using System.Collections.Generic;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Entities;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Entities;

namespace TrinityCore.GameClient.Net.Lib.Clients
{
    public class BaseClient
    {
        protected AuthClient AuthClient { get; set; }
        protected WorldClient WorldClient { get; set; }

        public BaseClient(string host, int port, string login, string password)
        {
            AuthClient = new AuthClient(host, port, login, password);
        }

        public virtual async Task<bool> Authenticate()
        {
            return await AuthClient.Authenticate();
        }

        public virtual async Task<List<WorldServerInfo>> GetWorlds()
        {
            return await AuthClient.GetWorlds();
        }

        public virtual async Task<bool> Connect(WorldServerInfo server)
        {
            WorldClient = new WorldClient(server, AuthClient.Username, AuthClient.SessionKey);
            return await Start();
        }

        protected virtual async Task<bool> Start()
        {
            return await WorldClient.Start();
        }

        public virtual async Task<List<Character>> GetCharacters()
        {
            return await WorldClient.GetCharacters();
        }

        public virtual async Task<bool> EnterWorld(Character selectedCharacter)
        {
            return await WorldClient.LoginCharacter(selectedCharacter);
        }
    }
}
