using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    internal class WeaponProficiency : Proficiency
    {
        internal ItemSubclassWeapon Weapon => (ItemSubclassWeapon)SubItemClass;

        internal WeaponProficiency(ItemClass itemClass, uint subItemClass) : base(itemClass, subItemClass)
        {
        }

        public override string ToString()
        {
            return "{" + ItemClass + "}{" + Weapon + "}";
        }
    }
}
