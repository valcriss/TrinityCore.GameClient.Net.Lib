namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class Npc : Entity
    {
        #region Internal Properties

        internal UnitInfo Infos { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal Npc(Entity entity, UnitInfo unitInfo) : base(entity.Guid)
        {
            Type = entity.Type;
            Powers = entity.Powers;
            Movement = entity.Movement;
            Fields = entity.Fields;
            Infos = unitInfo;
        }

        #endregion Internal Constructors
    }
}