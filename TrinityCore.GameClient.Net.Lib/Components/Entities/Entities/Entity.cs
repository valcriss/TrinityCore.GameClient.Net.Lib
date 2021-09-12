using System;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Entities
{
    public class Entity
    {
        private string _name = null;
        public Dictionary<UpdateFields, uint> Fields { get; set; }
        public ulong Guid { get; set; }
        public Movement Movement { get; set; }
        internal Dictionary<Powers, uint> Powers { get; set; }
        internal SplineMoveMode SplineMoveMode { get; set; }
        internal TypeID Type { get; set; }
        public string Name { get => _name != null ? _name : Guid.ToString(); set => _name = value; }

        public Position GetPosition()
        {
            return Movement.Position;
        }
        internal void UpdatePosition(Position position)
        {
            Movement.Position = position;
            Logger.Log("Entity " + Name + " Position update : " + position, LogLevel.VERBOSE);
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
                Logger.Log("Update " + field.Key + " = " + field.Value + " for Guid " + Guid, LogLevel.VERBOSE);
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
