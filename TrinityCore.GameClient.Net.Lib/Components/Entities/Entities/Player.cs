namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    public class Player : Entity
    {
        public Player(Entity entity) : base(entity.Guid)
        {
            Type = entity.Type;
            Powers = entity.Powers;
            Movement = entity.Movement;
            Fields = entity.Fields;
        }
    }
}
