namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class EntityTypeCollection<T> : ValueCollection<T> where T : Entity
    {
        #region Public Constructors

        public EntityTypeCollection() : base()
        {
        }

        #endregion Public Constructors

        #region Public Methods

        public void Add(T entity)
        {
            Add(entity.Guid, entity);
        }

        #endregion Public Methods
    }
}