using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Outgoing
{
    internal class FacingMovement : WorldMovementPacket
    {
        #region Internal Constructors

        internal FacingMovement(ulong guid, Position position, bool moving) : base(WorldCommand.MSG_MOVE_SET_FACING)
        {
            Guid = guid;
            Flags = moving ? MovementTypes.FORWARD : MovementTypes.None;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
            ReadData();
        }

        #endregion Internal Constructors
    }
}