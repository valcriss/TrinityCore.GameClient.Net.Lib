using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
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
