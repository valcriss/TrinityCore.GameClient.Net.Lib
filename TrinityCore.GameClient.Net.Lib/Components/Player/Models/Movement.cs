using System;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;
using TrinityCore.GameClient.Net.Lib.Map;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class Movement
    {
        #region Public Properties

        public bool CanMove { get; set; }
        public bool IsStanding { get; set; }

        #endregion Public Properties

        #region Private Properties

        private bool MovementInitialized { get; set; }
        private PlayerComponent Player { get; set; }
        private Travel Travel { get; set; }
        private WorldClient WorldClient { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public Movement(WorldClient worldClient, PlayerComponent player)
        {
            CanMove = true;
            IsStanding = true;
            MovementInitialized = false;

            Player = player;
            WorldClient = worldClient;

            Travel = new Travel();
            Travel.OnTravelMoveStop += TravelOnTravelMoveStop;
            Travel.OnTravelMoveForward += TravelOnTravelMoveForward;
            Travel.OnTravelMoveHeartBeat += TravelOnTravelMoveHeartBeat;
        }

        #endregion Public Constructors

        #region Public Methods

        public bool Face(Entities.Models.Entity target)
        {
            bool result = Face(target.GetPosition());
            if (result) Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Sending Facing entity : " + target.Guid);
            return result;
        }

        public bool Face(float angle)
        {
            if (!CanMove) return false;
            Position current = PlayerComponent.Position;
            if (Math.Abs(current.O - angle) > 0.01f)
            {
                Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Sending Facing angle : " + angle);
                current.O = angle;
                PlayerComponent.Position = current;
                SendActivlyMoving();
                return WorldClient.Send(new FacingMovement((ulong)Player.Guid, PlayerComponent.Position, false));
            }
            return false;
        }

        public bool Face(Position destination)
        {
            float angle = (destination - PlayerComponent.Position).Direction.O;
            bool result = Face(angle);
            if (result) Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Sending Facing position : " + destination);
            return result;
        }

        public TravelState MoveTo(Entities.Models.Entity target)
        {
            return MoveTo(target.GetPosition());
        }

        public TravelState MoveTo(Position position)
        {
            if (!CanMove) return TravelState.ERROR;
            return Travel.MoveTo(position);
        }

        public bool Stand(bool value)
        {
            if (!CanMove) return false;
            if (value == IsStanding)
                return false;

            return WorldClient.Send(new StandPositionRequest(value ? UnitStandStateType.UNIT_STAND_STATE_STAND : UnitStandStateType.UNIT_STAND_STATE_SIT));
        }

        public bool Stop()
        {
            if (!CanMove) return false;
            WorldClient.Send(new StopMovement((ulong)Player.Guid, PlayerComponent.Position));
            MovementInitialized = false;
            return true;
        }

        #endregion Public Methods

        #region Private Methods

        private void SendActivlyMoving()
        {
            if (!MovementInitialized)
            {
                Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Sending ActivlyMoving : " + Player.Guid);
                WorldClient.Send(new ActivlyMoving((ulong)Player.Guid));
                MovementInitialized = true;
            }
        }

        private void TravelOnTravelMoveForward(Position currentPosition)
        {
            SendActivlyMoving();
            PlayerComponent.Position = currentPosition;
            Face(currentPosition);
            WorldClient.Send(new MoveStartForwardMovement((ulong)Player.Guid, currentPosition));
        }

        private void TravelOnTravelMoveHeartBeat(Position currentPosition)
        {
            SendActivlyMoving();
            PlayerComponent.Position = currentPosition;
            WorldClient.Send(new HeartBeatMovement((ulong)Player.Guid, currentPosition, Network.World.Enums.MovementTypes.FORWARD));
        }

        private void TravelOnTravelMoveStop(Position currentPosition)
        {
            PlayerComponent.Position = currentPosition;
            Stop();
        }

        #endregion Private Methods
    }
}