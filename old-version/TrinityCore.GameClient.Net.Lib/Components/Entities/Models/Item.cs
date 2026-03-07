namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class Item : Entity
    {
        #region Internal Constructors

        internal Item(Entity entity) : base(entity.Guid)
        {
            Type = entity.Type;
            Powers = entity.Powers;
            Movement = entity.Movement;
            Fields = entity.Fields;
        }

        #endregion Internal Constructors
    }
}