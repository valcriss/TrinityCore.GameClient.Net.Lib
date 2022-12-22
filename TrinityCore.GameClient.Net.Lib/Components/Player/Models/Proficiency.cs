using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class Proficiency
    {
        #region Internal Properties

        internal ItemClass ItemClass { get; set; }

        #endregion Internal Properties

        #region Protected Properties

        protected uint SubItemClass { get; set; }

        #endregion Protected Properties

        #region Internal Constructors

        internal Proficiency(ItemClass itemClass, uint subItemClass)
        {
            ItemClass = itemClass;
            SubItemClass = subItemClass;
        }

        #endregion Internal Constructors
    }
}