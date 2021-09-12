using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    internal class Travel
    {
        internal PlayerMoveType CurrentMoveType { get; set; }
        private DateTime LastMovementAction { get; set; }
        private WorldCommand CurrentMoveAction { get; set; }
        private Position LastPosition { get; set; }
        private bool Moving { get; set; }

        public Travel()
        {
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

        public void Stop()
        {

            Moving = false;
        }

        public void StartMovement(WorldCommand action, Position position)
        {
            CurrentMoveAction = action;
            LastPosition = position;
        }


    }
}