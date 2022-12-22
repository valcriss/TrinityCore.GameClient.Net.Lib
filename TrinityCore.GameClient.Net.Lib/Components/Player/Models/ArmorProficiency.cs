using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class ArmorProficiency : Proficiency
    {
        public ItemSubclassArmor Armor => (ItemSubclassArmor)SubItemClass;

        internal ArmorProficiency(ItemClass itemClass, uint subItemClass) : base(itemClass, subItemClass)
        {
            SubItemClass = subItemClass;
        }

        public override string ToString()
        {
            return "{" + ItemClass + "}{" + Armor.ToString() + "}";
        }
    }
}
