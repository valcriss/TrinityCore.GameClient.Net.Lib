namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class Player : Entity
    {
        #region Public Constructors

        public Player(Entity entity) : base(entity.Guid)
        {
            Type = entity.Type;
            Powers = entity.Powers;
            Movement = entity.Movement;
            Fields = entity.Fields;
        }

        #endregion Public Constructors
    }
}