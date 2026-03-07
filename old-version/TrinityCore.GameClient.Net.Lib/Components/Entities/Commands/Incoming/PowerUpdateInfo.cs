using System;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Incoming
{
    internal class PowerUpdateInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Public Properties

        public UInt64 Guid { get; set; }
        public Powers Power { get; set; }
        public UInt32 Value { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Guid = ReadPackedGuid();
            Power = (Powers)ReadSByte();
            Value = ReadUInt32();
        }

        #endregion Internal Methods
    }
}