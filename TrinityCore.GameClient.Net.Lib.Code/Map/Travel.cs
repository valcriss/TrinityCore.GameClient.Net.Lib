using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Player.Commands;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;
using TrinityCore.Map.Net.IO;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class Travel
    {
        private const float RECALCULTATE_DISTANCE = 5.0f;
        private bool Moving { get; set; }
        private Path CurrentPath { get; set; }
        private Position CurrentDestination { get; set; }
        private uint CurrentMapId { get; set; }
        private DateTime PreviousMovingTime { get; set; }
        public TravelState State { get; set; }
        private WorldClient WorldClient { get; set; }
        private bool MovementsActivated { get; set; }


        public Travel(WorldClient worldClient)
        {
            MovementsActivated = false;
            WorldClient = worldClient;

            Reset();
        }

        public void Reset()
        {
            Moving = false;
            CurrentPath = null;
            CurrentDestination = null;
            PreviousMovingTime = DateTime.Now;
            CurrentMapId = uint.MaxValue;
        }

        private bool SendActivlyMoving()
        {
            if (!MovementsActivated)
            {
                Entity player = EntitiesComponent.Instance?.Collection.GetPlayer();
                if (player != null)
                {
                    WorldClient.Send(new ActivlyMoving(WorldClient, player.Guid));
                    MovementsActivated = true;
                    return true;
                }
            }
            return false;
        }

        public TravelState MoveTo(uint mapId, Position destination, float speed, float distanceToPoint = 5.0f)
        {
            SendActivlyMoving();
            Entity player = EntitiesComponent.Instance?.Collection.GetPlayer();
            if (player == null) return TravelState.ERROR;
            Position currentPosition = player.GetPosition();
            float distance = (currentPosition - destination).Length;

            if (distance < distanceToPoint)
            {
                MoveStop(currentPosition);
                State = TravelState.DESTINATION_REACH;
                return State;
            }

            if (CurrentDestination == null || CurrentMapId != mapId || (CurrentDestination - destination).Length > RECALCULTATE_DISTANCE)
            {
                CurrentPath = null;
            }

            if (CurrentPath == null)
            {
                Path calculate = Waze.CalculatePath(currentPosition, destination, mapId, speed);
                if (calculate == null)
                {
                    MoveStop(currentPosition);
                    State = TravelState.ERROR;
                    return State;
                }
                CurrentPath = calculate;
                CurrentDestination = destination;
                CurrentMapId = mapId;
            }

            if (!Moving)
            {
                Position step = new Position(currentPosition.X, currentPosition.Y, currentPosition.Z, CurrentPath.CurrentOrientation);
                MoveForwardTo(step);
                PreviousMovingTime = DateTime.Now;
                State = TravelState.RUNNING;
                return State;
            }
            else
            {
                Point progressPosition = CurrentPath.MoveAlongPath((float)(DateTime.Now - PreviousMovingTime).TotalSeconds);
                PreviousMovingTime = DateTime.Now;
                Position step = new Position(progressPosition.X, progressPosition.Y, progressPosition.Z, CurrentPath.CurrentOrientation);
                MoveHeartBeat(step);
            }

            State = TravelState.RUNNING;
            return State;
        }

        public bool MoveStop(Position position)
        {
            Entity player = EntitiesComponent.Instance?.Collection.GetPlayer();
            if (player != null)
            {
                if (Moving)
                {
                    player.UpdatePosition(position);
                    WorldClient.Send(new StopMovement(WorldClient, player.Guid, player.GetPosition()));
                }
                Moving = false;
                return true;
            }
            return false;
        }

        public bool MoveForwardTo(Position position = null)
        {

            Entity player = EntitiesComponent.Instance?.Collection.GetPlayer();
            if (player != null)
            {
                Moving = true;
                if (position == null) position = player.GetPosition();
                position = Waze.GetSticked((int)CurrentMapId, position);
                player.UpdatePosition(position);
                WorldClient?.Send(new FacingMovement(WorldClient, player.Guid, position, true));
                WorldClient?.Send(new MoveStartForwardMovement(WorldClient, player.Guid, position));
                return true;
            }
            return false;
        }

        public void MoveHeartBeat(Position position = null)
        {
            Moving = true;
            Entity player = EntitiesComponent.Instance?.Collection.GetPlayer();
            if (player != null)
            {
                if (position == null) position = player.GetPosition();
                position = Waze.GetSticked((int)CurrentMapId, position);
                player.UpdatePosition(position);
                WorldClient?.Send(new HeartBeatMovement(WorldClient, player.Guid, position, MovementFlags.MOVEMENTFLAG_FORWARD));
            }
        }
    }

    public enum TravelState
    {
        DESTINATION_REACH = 0,
        RUNNING = 1,
        ERROR = 2
    }
}
