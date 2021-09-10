using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    public class WeaponProficiency : Proficiency
    {
        public ItemSubclassWeapon Weapon => (ItemSubclassWeapon)SubItemClass;

        public WeaponProficiency(ItemClass itemClass, uint subItemClass) : base(itemClass, subItemClass)
        {
        }

        public override string ToString()
        {
            return "{" + ItemClass + "}{" + Weapon + "}";
        }
    }
}
