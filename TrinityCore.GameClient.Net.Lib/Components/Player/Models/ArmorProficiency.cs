using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class ArmorProficiency : Proficiency
    {
        #region Public Properties

        public ItemSubclassArmor Armor => (ItemSubclassArmor)SubItemClass;

        #endregion Public Properties

        #region Internal Constructors

        internal ArmorProficiency(ItemClass itemClass, uint subItemClass) : base(itemClass, subItemClass)
        {
            SubItemClass = subItemClass;
        }

        #endregion Internal Constructors

        #region Public Methods

        public override string ToString()
        {
            return "{" + ItemClass + "}{" + Armor.ToString() + "}";
        }

        #endregion Public Methods
    }
}