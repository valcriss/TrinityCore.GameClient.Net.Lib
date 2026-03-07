using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class EquipmentSet
    {
        #region Public Properties

        public ulong Guid { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public uint SetId { get; set; }

        #endregion Public Properties

        #region Internal Properties

        internal Dictionary<EquipmentSlots, ulong> SetSlotItem { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal EquipmentSet()
        {
            SetSlotItem = new Dictionary<EquipmentSlots, ulong>();
        }

        #endregion Internal Constructors
    }
}