using System;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Outgoing
{
    internal class StopMovement : WorldMovementPacket
    {
        #region Internal Constructors

        internal StopMovement(UInt64 guid, Position position) : base(WorldCommand.MSG_MOVE_STOP)
        {
            Guid = guid;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
            ReadData();
        }

        #endregion Internal Constructors
    }
}