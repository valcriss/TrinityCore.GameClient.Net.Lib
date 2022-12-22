using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class Movement
    {
        internal Movement()
        {
            MovementLiving = new MovementLiving();
            MovementPosition = new MovementPosition();
            MovementStationary = new MovementStationary();
            MovementHasTarget = new MovementHasTarget();
            MovementRotation = new MovementRotation();
        }
        internal MovementLiving MovementLiving { get; set; }
        internal MovementPosition MovementPosition { get; set; }
        internal MovementStationary MovementStationary { get; set; }
        internal MovementHasTarget MovementHasTarget { get; set; }
        internal MovementRotation MovementRotation { get; set; }

        private Position _position;
        public Position Position
        {
            get => CalculatePosition();
            set => _position = value;
        }

        private Position CalculatePosition()
        {
            if (_position != null) return _position;

            return MovementLiving != null ? MovementLiving.Position : new Position();
        }
    }
}
