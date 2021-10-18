using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class EquipmentSetList : WorldReceivablePacket
    {
        internal List<EquipmentSet> EquipmentSets { get; set; }

        internal EquipmentSetList(ReceivablePacket receivablePacket) : base(receivablePacket)
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
