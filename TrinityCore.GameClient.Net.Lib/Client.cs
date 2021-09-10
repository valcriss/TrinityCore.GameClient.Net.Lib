using TrinityCore.GameClient.Net.Lib.Clients;
using TrinityCore.GameClient.Net.Lib.Components.Player;
using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration;

namespace TrinityCore.GameClient.Net.Lib
{
    public class Client : ExtendableClient
    {
        public WorldConfigurationComponent WorldConfiguration { get; set; }
        public PlayerComponent Player { get; set; }
        public Client(string host, int port, string login, string password) : base(host, port, login, password)
        {
            WorldConfiguration = AddComponent(new WorldConfigurationComponent());
            Player = AddComponent(new PlayerComponent());
        }


    }
}
