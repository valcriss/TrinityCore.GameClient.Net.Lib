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
    internal class UpdateProficiencyInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        internal ItemClass ItemClass { get; set; }
        private uint ItemSubclass { get; set; }

        internal override void LoadData()
        {
            ItemClass = (ItemClass)ReadSByte();
            ItemSubclass = ReadUInt32();
        }

        internal ArmorProficiency GetArmorProficiency()
        {
            return new ArmorProficiency(ItemClass, ItemSubclass);
        }

        internal WeaponProficiency GetWeaponProficiency()
        {
            return new WeaponProficiency(ItemClass, ItemSubclass);
        }
    }
}
