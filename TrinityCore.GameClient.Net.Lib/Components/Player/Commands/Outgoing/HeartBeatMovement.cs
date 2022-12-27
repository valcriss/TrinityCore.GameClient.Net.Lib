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
    internal class HeartBeatMovement : WorldMovementPacket
    {
        internal HeartBeatMovement(UInt64 guid, Position position, MovementFlags flag) : base(WorldCommand.MSG_MOVE_HEARTBEAT)
        {
            Guid = guid;
            Flags = flag;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
            ReadData();
        }
    }
}
