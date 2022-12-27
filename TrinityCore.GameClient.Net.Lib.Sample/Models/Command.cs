using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Sample.Models
{
    internal abstract class Command
    {
        public abstract bool Handle(string command);
    }
}
