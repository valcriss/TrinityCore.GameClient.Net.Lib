using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class UpdateMovement
    {
        internal ulong Guid { get; set; }
        internal MovementInfo Movement { get; set; }
    }
}
