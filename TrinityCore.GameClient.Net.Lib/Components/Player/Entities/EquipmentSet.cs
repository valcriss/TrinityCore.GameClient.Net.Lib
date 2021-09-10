using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    public class EquipmentSet
    {
        public ulong Guid { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public uint SetId { get; set; }
        public Dictionary<EquipmentSlots, ulong> SetSlotItem { get; set; }

        public EquipmentSet()
        {
            SetSlotItem = new Dictionary<EquipmentSlots, ulong>();
        }
    }
}
