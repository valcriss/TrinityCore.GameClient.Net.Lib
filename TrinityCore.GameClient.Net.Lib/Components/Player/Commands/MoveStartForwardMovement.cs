using System;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    public class MoveStartForwardMovement : WorldMovementPacket
    {
        public MoveStartForwardMovement(WorldSocket worldSocket, UInt64 guid, Position position) : base(worldSocket, WorldCommand.MSG_MOVE_START_FORWARD)
        {
            Guid = guid;
            Flags = MovementFlags.MOVEMENTFLAG_FORWARD;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
        }
    }
}
