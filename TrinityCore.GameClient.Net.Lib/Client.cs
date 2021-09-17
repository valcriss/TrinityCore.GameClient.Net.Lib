using System.Collections.Generic;
using System.Linq;
using TrinityCore.GameClient.Net.Lib.Clients;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Player;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib
{
    public class Client : ExtendableClient
    {
        internal WorldConfigurationComponent WorldConfiguration { get; set; }
        internal PlayerComponent Player { get; set; }
        internal EntitiesComponent Entities { get; set; }

        public Client(string host, int port, string login, string password) : base(host, port, login, password)
        {
            Entities = AddComponent(new EntitiesComponent());
            WorldConfiguration = AddComponent(new WorldConfigurationComponent());
            Player = AddComponent(new PlayerComponent());
        }

        public Player GetPlayer()
        {
            return Entities?.Collection?.GetPlayer();
        }

        public uint? GetMapId()
        {
            return WorldClient?.Character?.MapId;
        }

        public List<Player> GetOtherPlayers()
        {
            Player player = GetPlayer();
            return (player != null) ? Entities.Collection.Players.Values.Where(c => c.Guid != player.Guid).ToList() : null;
        }

        public bool Face(float orientation)
        {
            return Player.FaceOrientation(orientation);
        }

        public bool Face(Position position)
        {
            return Player.FacePosition(position);
        }

        public bool MoveForward(Position position = null, PlayerMoveType moveType = PlayerMoveType.MOVE_RUN)
        {
            if(position != null)
            {
                Player.FacePosition(position);
            }

            return Player.MoveForward(moveType);
        }
    }
}
