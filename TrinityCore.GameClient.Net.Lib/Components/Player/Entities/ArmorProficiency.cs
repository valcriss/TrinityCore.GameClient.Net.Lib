using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    public class ArmorProficiency : Proficiency
    {
        public ItemSubclassArmor Armor => (ItemSubclassArmor)SubItemClass;

        public ArmorProficiency(ItemClass itemClass, uint subItemClass) : base(itemClass, subItemClass)
        {
            SubItemClass = subItemClass;
        }

        public override string ToString()
        {
            return "{" + ItemClass + "}{" + Armor + "}";
        }
    }
}
