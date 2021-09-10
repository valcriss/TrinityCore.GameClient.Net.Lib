using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    public class Entity
    {
        public Dictionary<UpdateFields, uint> Fields { get; set; }
        public ulong Guid { get; set; }
        public Movement Movement { get; set; }
        public Dictionary<Powers, uint> Powers { get; set; }
        public SplineMoveMode SplineMoveMode { get; set; }
        public TypeID Type { get; set; }

        public Position GetPosition()
        {
            return Movement.Position;
        }
        public void UpdatePosition(Position position)
        {
            Movement.Position = position;
            Logger.Log("Entity [" + Guid + "] Position update : " + position, LogLevel.DETAIL);
        }

        public Entity(UInt64 guid)
        {
            Guid = guid;
            SplineMoveMode = SplineMoveMode.RUN;
            Movement = new Movement();
            Fields = new Dictionary<UpdateFields, uint>();
            Powers = new Dictionary<Powers, uint>();
        }

        public void UpdateFields(Dictionary<UpdateFields, uint> fields)
        {
            foreach (KeyValuePair<UpdateFields, uint> field in fields)
            {
                if (!Fields.ContainsKey(field.Key))
                {
                    Fields.Add(field.Key, field.Value);
                }

                Fields[field.Key] = field.Value;
                Logger.Log("Update " + field.Key + " = " + field.Value + " for Guid " + Guid, LogLevel.DETAIL);
            }
        }

        public void UpdateMovement(MovementInfo movementInfo)
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

        public void UpdateMovement(MovementLiving living)
        {
            if (living != null)
            {
                Movement.MovementLiving.TransportGuid = living.TransportGuid;
                Movement.MovementLiving.Position = living.Position;
                Movement.MovementLiving.TransportPosition = living.TransportPosition;
                Movement.Position = null;
            }
        }

        public void UpdatePower(Powers power, uint value)
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
