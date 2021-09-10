using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    public class Proficiency
    {
        public ItemClass ItemClass { get; set; }
        protected uint SubItemClass { get; set; }

        public Proficiency(ItemClass itemClass, uint subItemClass)
        {
            ItemClass = itemClass;
            SubItemClass = subItemClass;
        }
    }
}
