using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Outgoing
{
    internal class MoveStartForwardMovement : WorldMovementPacket
    {
        internal MoveStartForwardMovement(UInt64 guid, Position position) : base(WorldCommand.MSG_MOVE_START_FORWARD)
        {
            Guid = guid;
            Flags = MovementFlags.MOVEMENTFLAG_FORWARD;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
            ReadData();
        }
    }
}
