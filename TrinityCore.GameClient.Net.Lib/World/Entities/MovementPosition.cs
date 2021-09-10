using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    public class MovementPosition
    {
        public bool Transport { get; set; }
        public ulong? TransportGuid { get; set; }
        public Position Position { get; set; }
        public Position TransportPosition { get; set; }
    }
}
