using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    internal class MovementPosition
    {
        internal bool Transport { get; set; }
        internal ulong? TransportGuid { get; set; }
        internal Position Position { get; set; }
        internal Position TransportPosition { get; set; }
    }
}
