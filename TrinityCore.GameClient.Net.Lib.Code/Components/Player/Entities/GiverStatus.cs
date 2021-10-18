using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    internal class GiverStatus
    {
        internal ulong GiverGuid { get; set; }
        internal QuestGiverStatus Status { get; set; }
    }
}
