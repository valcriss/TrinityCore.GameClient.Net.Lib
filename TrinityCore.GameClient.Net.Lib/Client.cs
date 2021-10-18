using System.Collections.Generic;
using System.Linq;
using TrinityCore.GameClient.Net.Lib.Clients;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Player;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration;
using TrinityCore.GameClient.Net.Lib.Map;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;
using TrinityCore.Map.Net.IO;

namespace TrinityCore.GameClient.Net.Lib
{
    public class Client : ExtendableClient
    {
        internal WorldConfigurationComponent WorldConfiguration { get; set; }
        internal PlayerComponent Player { get; set; }
        internal EntitiesComponent Entities { get; set; }

        public Client(string host, int port, string login, string password, string dataDirectory) : base(host, port, login, password)
        {
            Entities = AddComponent(new EntitiesComponent());
            WorldConfiguration = AddComponent(new WorldConfigurationComponent());
            Player = AddComponent(new PlayerComponent());
            Waze.Initialize(dataDirectory);
        }

        public Path CalculatePath(Position target)
        {
            uint? mapId = GetMapId();
            Player player = GetPlayer();
            if (mapId == null || player == null) return null;
            return Waze.CalculatePath(player.GetPosition(), target, mapId.Value, player.Movement.MovementLiving.Speeds[UnitMoveType.MOVE_RUN]);
        }

        public Player GetPlayer()
        {
            return Entities?.Collection?.GetPlayer();
        }

        public float? GetRunSpeed()
        {
            Player player = GetPlayer();
            return player?.Movement.MovementLiving.Speeds[UnitMoveType.MOVE_RUN];
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



        public TravelState MoveTo(Position position)
        {
            float? speed = GetRunSpeed();
            if (speed == null) return TravelState.ERROR;
            return Player.MoveTo(position, speed.Value);
        }

        public TravelState MoveTo(Entity entity)
        {
            float? speed = GetRunSpeed();
            if (speed == null) return TravelState.ERROR;
            return Player.MoveTo(entity, speed.Value);
        }

        public bool Face(float orientation)
        {
            return Player.FaceOrientation(orientation);
        }
    }
}
