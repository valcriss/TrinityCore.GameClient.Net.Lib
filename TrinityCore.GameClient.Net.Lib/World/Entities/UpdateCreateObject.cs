using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    public class UpdateCreateObject
    {
        public Dictionary<UpdateFields, uint> Fields { get; set; }
        public ulong Guid { get; set; }
        public MovementInfo Movement { get; set; }
        public TypeID ObjectType { get; set; }

        public UpdateCreateObject()
        {
            Fields = new Dictionary<UpdateFields, uint>();
        }
    }
}
