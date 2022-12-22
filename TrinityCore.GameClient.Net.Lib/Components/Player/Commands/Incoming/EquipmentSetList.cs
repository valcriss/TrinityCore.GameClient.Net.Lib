using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class EquipmentSetList : ReceivablePacket<Network.World.Enums.WorldCommand>
    {    
        public List<EquipmentSet> EquipmentSets { get; set; }

        internal override void LoadData()
        {
            EquipmentSets = new List<EquipmentSet>();
            uint count = ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                EquipmentSet equipmentSet = new EquipmentSet();
                equipmentSet.Guid = ReadPackedGuid();
                equipmentSet.SetId = ReadUInt32();
                equipmentSet.Name = ReadCString();
                equipmentSet.Icon = ReadCString();
                for (int slot = (int)EquipmentSlots.EQUIPMENT_SLOT_HEAD;
                    slot < (int)EquipmentSlots.EQUIPMENT_SLOT_END;
                    slot++)
                {
                    ulong equipmentSlot = ReadPackedGuid();
                    if (equipmentSlot != 1)
                        equipmentSet.SetSlotItem.Add((EquipmentSlots)slot, equipmentSlot);
                }

                EquipmentSets.Add(equipmentSet);
            }
        }
    }
}
