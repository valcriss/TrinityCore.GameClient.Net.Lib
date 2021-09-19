using System;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    internal class Travel
    {
        internal PlayerMoveType CurrentMoveType { get; set; }
        private DateTime LastMovementAction { get; set; }
        private WorldCommand CurrentMoveAction { get; set; }
        private Position Position { get; set; }
        private bool Moving { get; set; }
        private Dictionary<PlayerMoveType, float> Speeds { get; set; }

        public Travel(Dictionary<UnitMoveType, float> speeds)
        {
            Speeds = new Dictionary<PlayerMoveType, float>() {
                {PlayerMoveType.MOVE_RUN,speeds[UnitMoveType.MOVE_RUN] },
                {PlayerMoveType.MOVE_WALK,speeds[UnitMoveType.MOVE_WALK] },
            };
            CurrentMoveType = PlayerMoveType.NONE;
            LastMovementAction = DateTime.Now;
            CurrentMoveAction = WorldCommand.MSG_NULL_ACTION;
            Moving = false;
        }

        public bool NeedMoveTypeChange(PlayerMoveType moveType)
        {
            bool value = (moveType != CurrentMoveType);
            CurrentMoveType = moveType;
            return value;
        }

        public bool TravelInProgress()
        {
            return Moving;
        }

        public Position Stop()
        {
            InterpolatePosition();
            Moving = false;
            return Position;
        }

        public bool StartMovement(WorldCommand action, Position position, PlayerMoveType type)
        {
            if(!Speeds.ContainsKey(type))
            {
                return false;
            }
            CurrentMoveType = type;
            CurrentMoveAction = action;
            Position = position;
            Moving = true;
            LastMovementAction = DateTime.Now;
            return true;
        }

        public Position ChangeDirection(float orientation)
        {
            InterpolatePosition();
            Position.O = orientation;
            return Position;
        }

        public Position HeartBeat()
        {
            if (!Moving) return null;
            if (DateTime.Now.Subtract(LastMovementAction).TotalMilliseconds < 1000) return null;
            InterpolatePosition();
            return Position;
        }

        private void InterpolatePosition()
        {
            if (!Moving) return;
            float elapsedTime = (float)DateTime.Now.Subtract(LastMovementAction).TotalSeconds;
            float speed = Speeds[CurrentMoveType];
            float distance = elapsedTime * speed;
            Logger.Log("Previous position " + Position);
            switch(CurrentMoveAction)
            {
                case WorldCommand.MSG_MOVE_START_FORWARD:
                    float orientation = Position.O;
                    Position = Position + (Position.Direction * distance);
                    Position.O = orientation;
                    break;
            }
            Logger.Log("New position " + Position);
            LastMovementAction = DateTime.Now;
        }

        internal MovementFlags GetMovementFlags()
        {
            if (!Moving) return MovementFlags.MOVEMENTFLAG_NONE;
            switch (CurrentMoveAction)
            {
                case WorldCommand.MSG_MOVE_START_FORWARD:
                    return MovementFlags.MOVEMENTFLAG_FORWARD;
            }
            return MovementFlags.MOVEMENTFLAG_NONE;
        }
    }
}