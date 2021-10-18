using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    internal class UpdateCreateObject
    {
        internal Dictionary<UpdateFields, uint> Fields { get; set; }
        internal ulong Guid { get; set; }
        internal MovementInfo Movement { get; set; }
        internal TypeID ObjectType { get; set; }

        internal UpdateCreateObject()
        {
            Fields = new Dictionary<UpdateFields, uint>();
        }
    }
}
