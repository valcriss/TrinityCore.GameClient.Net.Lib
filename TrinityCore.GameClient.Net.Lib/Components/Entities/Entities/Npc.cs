using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World.Entities;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    public class Npc : Entity
    {
        public UnitInfo Infos { get; set; }

        public Npc(Entity entity, UnitInfo unitInfo) : base(entity.Guid)
        {
            Type = entity.Type;
            Powers = entity.Powers;
            Movement = entity.Movement;
            Fields = entity.Fields;
            Infos = unitInfo;
        }
    }
}
