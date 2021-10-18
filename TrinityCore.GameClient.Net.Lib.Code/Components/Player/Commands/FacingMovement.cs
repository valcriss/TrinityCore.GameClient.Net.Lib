using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class FacingMovement : WorldMovementPacket
    {
        internal FacingMovement(WorldSocket worldSocket, ulong guid, Position position, bool moving) : base(worldSocket, WorldCommand.MSG_MOVE_SET_FACING)
        {
            Guid = guid;
            Flags = moving ? MovementFlags.MOVEMENTFLAG_FORWARD: MovementFlags.MOVEMENTFLAG_NONE;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
        }
    }
}
