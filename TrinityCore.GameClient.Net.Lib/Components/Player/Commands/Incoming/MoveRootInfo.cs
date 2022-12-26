using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class MoveRootInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Public Properties

        public bool CanMove { get; set; }
        public ulong Guid { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Guid = ReadPackedGuid();
            uint zero = ReadUInt32();
            CanMove = (Command == Network.World.Enums.WorldCommand.SMSG_FORCE_MOVE_UNROOT);
        }

        #endregion Internal Methods
    }
}