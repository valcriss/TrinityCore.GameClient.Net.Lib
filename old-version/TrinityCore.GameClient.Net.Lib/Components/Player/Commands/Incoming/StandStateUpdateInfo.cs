using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class StandStateUpdateInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Public Properties

        public UnitStandStateType StandType { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            StandType = (UnitStandStateType)ReadSByte();
        }

        #endregion Internal Methods
    }
}