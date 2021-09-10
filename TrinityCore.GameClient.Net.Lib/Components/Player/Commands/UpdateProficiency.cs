using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    public class UpdateProficiency : WorldReceivablePacket
    {
        public ItemClass ItemClass { get; set; }
        private uint ItemSubclass { get; }

        public UpdateProficiency(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            ItemClass = (ItemClass)ReadSByte();
            ItemSubclass = ReadUInt32();
        }

        public ArmorProficiency GetArmorProficiency()
        {
            return new ArmorProficiency(ItemClass, ItemSubclass);
        }

        public WeaponProficiency GetWeaponProficiency()
        {
            return new WeaponProficiency(ItemClass, ItemSubclass);
        }
    }
}
