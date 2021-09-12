using TrinityCore.GameClient.Net.Lib.World.Entities;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    internal class Npc : Entity
    {
        internal UnitInfo Infos { get; set; }

        internal Npc(Entity entity, UnitInfo unitInfo) : base(entity.Guid)
        {
            Type = entity.Type;
            Powers = entity.Powers;
            Movement = entity.Movement;
            Fields = entity.Fields;
            Infos = unitInfo;
        }
    }
}
