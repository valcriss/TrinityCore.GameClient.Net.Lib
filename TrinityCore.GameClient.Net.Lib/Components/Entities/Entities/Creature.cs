using System;
using TrinityCore.GameClient.Net.Lib.World.Entities;


namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    public class Creature : Entity
    {
        public UnitInfo Infos { get; set; }
        public int Level
        {
            get
            {
                if (Fields.ContainsKey(World.Enums.UpdateFields.UNIT_FIELD_LEVEL))
                {
                    return (int)Fields[World.Enums.UpdateFields.UNIT_FIELD_LEVEL];
                }

                return 0;
            }
        }

        public bool IsAlive => Health > 0;

        public uint Health
        {
            get
            {
                if (Fields.ContainsKey(World.Enums.UpdateFields.UNIT_FIELD_HEALTH))
                {
                    return Fields[World.Enums.UpdateFields.UNIT_FIELD_HEALTH];
                }

                return 0;
            }
        }
        public int PctHealth
        {
            get
            {
                if (Fields.ContainsKey(World.Enums.UpdateFields.UNIT_FIELD_HEALTH) && Fields.ContainsKey(World.Enums.UpdateFields.UNIT_FIELD_MAXHEALTH))
                {
                    return (int)Math.Floor((Fields[World.Enums.UpdateFields.UNIT_FIELD_HEALTH] / (double)Fields[World.Enums.UpdateFields.UNIT_FIELD_MAXHEALTH]) * 100);
                }

                return 0;
            }
        }

        public Creature(Entity entity, UnitInfo unitInfo) : base(entity.Guid)
        {
            Type = entity.Type;
            Powers = entity.Powers;
            Movement = entity.Movement;
            Fields = entity.Fields;
            Infos = unitInfo;
        }
    }
}
