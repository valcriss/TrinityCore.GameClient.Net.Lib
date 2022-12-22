using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class Movement
    {
        #region Public Properties

        public Position Position
        {
            get => CalculatePosition();
            set => _position = value;
        }

        #endregion Public Properties

        #region Internal Properties

        internal MovementHasTarget MovementHasTarget { get; set; }

        internal MovementLiving MovementLiving { get; set; }

        internal MovementPosition MovementPosition { get; set; }

        internal MovementRotation MovementRotation { get; set; }

        internal MovementStationary MovementStationary { get; set; }

        #endregion Internal Properties

        #region Private Fields

        private Position _position;

        #endregion Private Fields

        #region Internal Constructors

        internal Movement()
        {
            MovementLiving = new MovementLiving();
            MovementPosition = new MovementPosition();
            MovementStationary = new MovementStationary();
            MovementHasTarget = new MovementHasTarget();
            MovementRotation = new MovementRotation();
        }

        #endregion Internal Constructors

        #region Private Methods

        private Position CalculatePosition()
        {
            if (_position != null) return _position;

            return MovementLiving != null ? MovementLiving.Position : new Position();
        }

        #endregion Private Methods
    }
}