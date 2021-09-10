using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    public class Faction
    {
        public sbyte Flags { get; set; }
        public int Id { get; set; }
        public uint Standing { get; set; }
    }
}
