using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    internal class Proficiency
    {
        internal ItemClass ItemClass { get; set; }
        protected uint SubItemClass { get; set; }

        internal Proficiency(ItemClass itemClass, uint subItemClass)
        {
            ItemClass = itemClass;
            SubItemClass = subItemClass;
        }
    }
}
