using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class WeaponProficiency : Proficiency
    {
        #region Public Properties

        public ItemSubclassWeapon Weapon => (ItemSubclassWeapon)SubItemClass;

        #endregion Public Properties

        #region Internal Constructors

        internal WeaponProficiency(ItemClass itemClass, uint subItemClass) : base(itemClass, subItemClass)
        {
        }

        #endregion Internal Constructors

        #region Public Methods

        public override string ToString()
        {
            return "{" + ItemClass + "}{" + Weapon + "}";
        }

        #endregion Public Methods
    }
}