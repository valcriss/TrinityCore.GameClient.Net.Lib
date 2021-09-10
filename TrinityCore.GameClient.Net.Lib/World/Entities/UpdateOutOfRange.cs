using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    public class UpdateOutOfRange
    {
        public List<ulong> GuidList { get; set; }

        public UpdateOutOfRange()
        {
            GuidList = new List<ulong>();
        }
    }
}
