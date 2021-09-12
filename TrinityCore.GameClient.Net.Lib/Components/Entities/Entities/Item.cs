namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    internal class Item : Entity
    {
        internal Item(Entity entity) : base(entity.Guid)
        {
            Type = entity.Type;
            Powers = entity.Powers;
            Movement = entity.Movement;
            Fields = entity.Fields;
        }
    }
}
