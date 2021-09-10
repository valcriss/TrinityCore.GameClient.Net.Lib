using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    public class Spell
    {
        public uint SpellId { get; set; }

        public Spell(uint spellId)
        {
            SpellId = spellId;
        }
    }
}
