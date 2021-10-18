using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    internal class ArmorProficiency : Proficiency
    {
        internal ItemSubclassArmor Armor => (ItemSubclassArmor)SubItemClass;

        internal ArmorProficiency(ItemClass itemClass, uint subItemClass) : base(itemClass, subItemClass)
        {
            SubItemClass = subItemClass;
        }

        public override string ToString()
        {
            return "{" + ItemClass + "}{" + Armor + "}";
        }
    }
}
