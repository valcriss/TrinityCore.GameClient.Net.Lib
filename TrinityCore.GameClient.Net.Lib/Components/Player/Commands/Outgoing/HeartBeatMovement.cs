using System;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Outgoing
{
    internal class HeartBeatMovement : WorldMovementPacket
    {
        #region Internal Constructors

        internal HeartBeatMovement(UInt64 guid, Position position, MovementTypes flag) : base(WorldCommand.MSG_MOVE_HEARTBEAT)
        {
            Guid = guid;
            Flags = flag;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
            ReadData();
        }

        #endregion Internal Constructors
    }
}