using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class MovementPosition
    {
        internal bool Transport { get; set; }
        internal ulong? TransportGuid { get; set; }
        internal Position Position { get; set; }
        internal Position TransportPosition { get; set; }
    }
}
