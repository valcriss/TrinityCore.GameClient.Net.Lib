using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class UpdateProficiency : WorldReceivablePacket
    {
        internal ItemClass ItemClass { get; set; }
        private uint ItemSubclass { get; }

        internal UpdateProficiency(ReceivablePacket receivablePacket) : base(receivablePacket)
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
