using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class Entity
    {
        private string _name = null;
        public Dictionary<UpdateFields, uint> Fields { get; set; }
        public ulong Guid { get; set; }
        public Movement Movement { get; set; }
        internal Dictionary<Powers, uint> Powers { get; set; }
        internal SplineMoveMode SplineMoveMode { get; set; }
        public TypeID Type { get; set; }
        public string Name { get => _name != null ? _name : Guid.ToString(); set => _name = value; }

        public Position GetPosition()
        {
            return Movement.Position;
        }
        internal void UpdatePosition(Position position)
        {
            Movement.Position = position;
        }

        internal Entity(UInt64 guid)
        {
            Guid = guid;
            SplineMoveMode = SplineMoveMode.RUN;
            Movement = new Movement();
            Fields = new Dictionary<UpdateFields, uint>();
            Powers = new Dictionary<Powers, uint>();
        }

        internal void UpdateFields(Dictionary<UpdateFields, uint> fields)
        {
            foreach (KeyValuePair<UpdateFields, uint> field in fields)
            {
                if (!Fields.ContainsKey(field.Key))
                {
                    Fields.Add(field.Key, field.Value);
                }

                Fields[field.Key] = field.Value;
            }
        }

        internal void UpdateMovement(MovementInfo movementInfo)
        {
            if (movementInfo.MovementLiving != null)
            {
                Movement.MovementLiving = movementInfo.MovementLiving;
                Movement.Position = null;
            }
            if (movementInfo.MovementPosition != null)
            {
                Movement.MovementPosition = movementInfo.MovementPosition;
                Movement.MovementLiving.TransportGuid = movementInfo.MovementPosition.TransportGuid;
                Movement.MovementLiving.Position = movementInfo.MovementPosition.Position;
                Movement.MovementLiving.TransportPosition = movementInfo.MovementPosition.TransportPosition;
                Movement.Position = null;
            }
            if (movementInfo.MovementStationary != null)
                Movement.MovementStationary = movementInfo.MovementStationary;
            if (movementInfo.MovementHasTarget != null)
                Movement.MovementHasTarget = movementInfo.MovementHasTarget;
            if (movementInfo.MovementRotation != null)
                Movement.MovementRotation = movementInfo.MovementRotation;
        }

        internal void UpdateMovement(MovementLiving living)
        {
            if (living != null)
            {
                Movement.MovementLiving.TransportGuid = living.TransportGuid;
                Movement.MovementLiving.Position = living.Position;
                Movement.MovementLiving.TransportPosition = living.TransportPosition;
                Movement.Position = null;
            }
        }

        internal void UpdatePower(Powers power, uint value)
        {
            if (!Powers.ContainsKey(power))
            {
                Powers.Add(power, value);
            }
            else
            {
                Powers[power] = value;
            }
        }
    }
}
