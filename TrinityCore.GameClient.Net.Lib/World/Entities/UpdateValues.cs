using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    public class UpdateValues
    {
        public Dictionary<UpdateFields, uint> Fields { get; set; }
        public ulong Guid { get; set; }

        public UpdateValues()
        {
            Fields = new Dictionary<UpdateFields, uint>();
        }
    }
}
