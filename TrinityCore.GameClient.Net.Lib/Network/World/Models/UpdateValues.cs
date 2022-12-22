using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
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
