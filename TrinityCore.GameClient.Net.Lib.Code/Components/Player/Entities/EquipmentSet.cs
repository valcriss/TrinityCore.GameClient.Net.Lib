using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    internal class EquipmentSet
    {
        internal ulong Guid { get; set; }
        internal string Icon { get; set; }
        internal string Name { get; set; }
        internal uint SetId { get; set; }
        internal Dictionary<EquipmentSlots, ulong> SetSlotItem { get; set; }

        internal EquipmentSet()
        {
            SetSlotItem = new Dictionary<EquipmentSlots, ulong>();
        }
    }
}
