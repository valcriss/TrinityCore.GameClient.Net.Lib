using System;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class Creature : Entity
    {
        #region Public Properties

        public uint Health
        {
            get
            {
                if (Fields.ContainsKey(Network.World.Enums.UpdateFields.UNIT_FIELD_HEALTH))
                {
                    return Fields[Network.World.Enums.UpdateFields.UNIT_FIELD_HEALTH];
                }

                return 0;
            }
        }

        public int Level
        {
            get
            {
                if (Fields.ContainsKey(Network.World.Enums.UpdateFields.UNIT_FIELD_LEVEL))
                {
                    return (int)Fields[Network.World.Enums.UpdateFields.UNIT_FIELD_LEVEL];
                }

                return 0;
            }
        }

        public int PctHealth
        {
            get
            {
                if (Fields.ContainsKey(Network.World.Enums.UpdateFields.UNIT_FIELD_HEALTH) && Fields.ContainsKey(Network.World.Enums.UpdateFields.UNIT_FIELD_MAXHEALTH))
                {
                    return (int)Math.Floor((Fields[Network.World.Enums.UpdateFields.UNIT_FIELD_HEALTH] / (double)Fields[Network.World.Enums.UpdateFields.UNIT_FIELD_MAXHEALTH]) * 100);
                }

                return 0;
            }
        }

        #endregion Public Properties

        #region Internal Properties

        internal UnitInfo Infos { get; set; }
        internal bool IsAlive => Health > 0;

        #endregion Internal Properties

        #region Internal Constructors

        internal Creature(Entity entity, UnitInfo unitInfo) : base(entity.Guid)
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