using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class WeaponProficiency : Proficiency
    {
        public ItemSubclassWeapon Weapon => (ItemSubclassWeapon)SubItemClass;

        internal WeaponProficiency(ItemClass itemClass, uint subItemClass) : base(itemClass, subItemClass)
        {
        }

        public override string ToString()
        {
            return "{" + ItemClass + "}{" + Weapon + "}";
        }
    }
}
