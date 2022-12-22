using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
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
