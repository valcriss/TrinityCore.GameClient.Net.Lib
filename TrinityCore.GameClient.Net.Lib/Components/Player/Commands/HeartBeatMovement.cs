using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class HeartBeatMovement : WorldMovementPacket
    {
        internal HeartBeatMovement(WorldSocket worldSocket, UInt64 guid, Position position, MovementFlags flag) : base(worldSocket, WorldCommand.MSG_MOVE_HEARTBEAT)
        {
            Guid = guid;
            Flags = flag;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
        }
    }
}
