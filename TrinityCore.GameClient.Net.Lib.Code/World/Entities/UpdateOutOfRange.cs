using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    internal class UpdateOutOfRange
    {
        internal List<ulong> GuidList { get; set; }

        internal UpdateOutOfRange()
        {
            GuidList = new List<ulong>();
        }
    }
}
