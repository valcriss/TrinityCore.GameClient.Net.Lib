using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class StopMovement : WorldMovementPacket
    {
        internal StopMovement(WorldSocket worldSocket, UInt64 guid, Position position) : base(worldSocket, WorldCommand.MSG_MOVE_STOP)
        {
            Guid = guid;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
        }
    }
}
