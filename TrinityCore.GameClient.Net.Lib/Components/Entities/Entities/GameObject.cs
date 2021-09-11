namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    public class GameObject : Entity
    {
        public GameObject(Entity entity) : base(entity.Guid)
        {
            Type = entity.Type;
            Powers = entity.Powers;
            Movement = entity.Movement;
            Fields = entity.Fields;
        }
    }
}
