using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class UpdateProficiencyInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Properties

        internal ItemClass ItemClass { get; set; }

        #endregion Internal Properties

        #region Private Properties

        private uint ItemSubclass { get; set; }

        #endregion Private Properties

        #region Internal Methods

        internal ArmorProficiency GetArmorProficiency()
        {
            return new ArmorProficiency(ItemClass, ItemSubclass);
        }

        internal WeaponProficiency GetWeaponProficiency()
        {
            return new WeaponProficiency(ItemClass, ItemSubclass);
        }

        internal override void LoadData()
        {
            ItemClass = (ItemClass)ReadSByte();
            ItemSubclass = ReadUInt32();
        }

        #endregion Internal Methods
    }
}