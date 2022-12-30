using System;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Zone;
using TrinityCore.GameClient.Net.Lib.Map.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    internal class Travel
    {
        #region Internal Delegates

        internal delegate void TravelMoveForwardEventHandler(Position currentPosition);

        internal delegate void TravelMoveHeartBeatEventHandler(Position currentPosition);

        internal delegate void TravelMoveStopEventHandler(Position currentPosition);

        #endregion Internal Delegates

        #region Internal Events

        internal event TravelMoveForwardEventHandler OnTravelMoveForward;

        internal event TravelMoveHeartBeatEventHandler OnTravelMoveHeartBeat;

        internal event TravelMoveStopEventHandler OnTravelMoveStop;

        #endregion Internal Events

        #region Private Properties

        private Position CurrentDestination { get; set; }
        private int CurrentMapId { get; set; }
        private Path CurrentPath { get; set; }
        private bool Moving { get; set; }
        private DateTime PreviousMovingTime { get; set; }
        private TravelState State { get; set; }

        #endregion Private Properties

        #region Private Fields

        private const float RECALCULTATE_DISTANCE = 5.0f;

        #endregion Private Fields

        #region Public Methods

        public TravelState MoveTo(Position destination, float distanceToPoint = 5.0f)
        {
            Entity player = GameClient.Get<EntitiesComponent>().Collection.GetPlayer();
            if (player == null) return TravelState.ERROR;
            int mapId = GameClient.Get<ZoneComponent>().WorldState.MapId;
            float speed = player.Movement.MovementLiving.Speeds[UnitMoveType.MOVE_RUN];
            Position currentPosition = player.GetPosition();
            float distance = (currentPosition - destination).Length;

            if (distance < distanceToPoint)
            {
                if (Moving)
                {
                    OnTravelMoveStop?.Invoke(currentPosition);
                }
                State = TravelState.DESTINATION_REACH;
                Reset();
                return State;
            }

            if (CurrentDestination == null || CurrentMapId != mapId || (CurrentDestination - destination).Length > RECALCULTATE_DISTANCE)
            {
                CurrentPath = null;
            }

            if (CurrentPath == null)
            {
                Path calculate = PathFinding.FindPath(mapId, currentPosition.ToVector3(), destination.ToVector3(), speed);
                if (calculate == null)
                {
                    OnTravelMoveStop?.Invoke(currentPosition);
                    State = TravelState.ERROR;
                    Reset();
                    return State;
                }
                CurrentPath = calculate;
                CurrentDestination = destination;
                CurrentMapId = mapId;
            }

            if (!Moving)
            {
                Position step = new Position(currentPosition.X, currentPosition.Y, currentPosition.Z, CurrentPath.CurrentOrientation);
                OnTravelMoveForward?.Invoke(step);
                PreviousMovingTime = DateTime.Now;
                State = TravelState.RUNNING;
                Moving = true;
                return State;
            }
            else
            {
                Point progressPosition = CurrentPath.MoveAlongPath((float)(DateTime.Now - PreviousMovingTime).TotalSeconds);
                progressPosition = PathFinding.Collection.ClosestPointAtPosition(CurrentMapId, progressPosition.ToVector3()).ToPoint();
                PreviousMovingTime = DateTime.Now;
                Position step = new Position(progressPosition.X, progressPosition.Y, progressPosition.Z, CurrentPath.CurrentOrientation);
                OnTravelMoveHeartBeat?.Invoke(step);
            }

            State = TravelState.RUNNING;
            return State;
        }

        #endregion Public Methods

        #region Private Methods

        private void Reset()
        {
            Moving = false;
            CurrentPath = null;
            CurrentDestination = null;
            PreviousMovingTime = DateTime.Now;
            CurrentMapId = int.MaxValue;
        }

        #endregion Private Methods
    }

    public enum TravelState
    {
        DESTINATION_REACH = 0,
        RUNNING = 1,
        ERROR = 2
    }
}