using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    internal class UpdateValues
    {
        internal Dictionary<UpdateFields, uint> Fields { get; set; }
        internal ulong Guid { get; set; }

        internal UpdateValues()
        {
            Fields = new Dictionary<UpdateFields, uint>();
        }
    }
}
