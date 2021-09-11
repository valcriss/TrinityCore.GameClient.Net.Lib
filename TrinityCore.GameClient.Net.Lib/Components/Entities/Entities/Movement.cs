using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    public class Movement
    {
        public Movement()
        {
            MovementLiving = new MovementLiving();
            MovementPosition = new MovementPosition();
            MovementStationary = new MovementStationary();
            MovementHasTarget = new MovementHasTarget();
            MovementRotation = new MovementRotation();
        }
        public MovementLiving MovementLiving { get; set; }
        public MovementPosition MovementPosition { get; set; }
        public MovementStationary MovementStationary { get; set; }
        public MovementHasTarget MovementHasTarget { get; set; }
        public MovementRotation MovementRotation { get; set; }

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
