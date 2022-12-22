using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class EquipmentSet
    {
        public ulong Guid { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public uint SetId { get; set; }
        internal Dictionary<EquipmentSlots, ulong> SetSlotItem { get; set; }

        internal EquipmentSet()
        {
            SetSlotItem = new Dictionary<EquipmentSlots, ulong>();
        }
    }
}
