using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Clients;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Commands;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities
{
    public class EntitiesComponent : GameComponent
    {
        private EntitiesCollection Collection { get; set; }

        public EntitiesComponent()
        {
            
        }

        public override void RegisterHandlers()
        {
            Collection = new EntitiesCollection(WorldClient);

            RegisterHandler(WorldCommand.SMSG_POWER_UPDATE, PowerUpdate);
            RegisterHandler(WorldCommand.SMSG_DESTROY_OBJECT, DestroyObject);
            RegisterHandler(WorldCommand.SMSG_MONSTER_MOVE, MonsterMove);
            RegisterHandler(WorldCommand.SMSG_SPLINE_MOVE_SET_WALK_MODE, SplineMoveSetWalkMode);
            RegisterHandler(WorldCommand.SMSG_SPLINE_MOVE_SET_RUN_MODE, SplineMoveSetRunMode);
            RegisterHandler(WorldCommand.MSG_MOVE_START_FORWARD, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_BACKWARD, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_STOP, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_STRAFE_LEFT, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_STRAFE_RIGHT, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_STOP_STRAFE, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_JUMP, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_TURN_LEFT, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_TURN_RIGHT, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_STOP_TURN, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_PITCH_UP, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_PITCH_DOWN, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_STOP_PITCH, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_SET_RUN_MODE, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_SET_WALK_MODE, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_FALL_LAND, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_SWIM, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_STOP_SWIM, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_SET_FACING, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_SET_PITCH, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_HEARTBEAT, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_ASCEND, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_STOP_ASCEND, HandleMovement);
            RegisterHandler(WorldCommand.MSG_MOVE_START_DESCEND, HandleMovement);

            RegisterInternalHandler(Internals.UPDATE_FIELDS, UpdateFields);
            RegisterInternalHandler(Internals.CREATE_OBJECTS, CreateObjects);
            RegisterInternalHandler(Internals.MOVEMENTS, UpdateMovements);
        }

        private void PowerUpdate(ReceivablePacket content)
        {
            PowerUpdate powerUpdate = new PowerUpdate(content);
 
            Logger.Log("PowerUpdate [" + powerUpdate.Guid + "] Power [" + powerUpdate.Power + "] : " + powerUpdate.Value + " for Guid : " + powerUpdate.Guid, LogLevel.DETAIL);

            Entity entity = Collection.GetUnit(powerUpdate.Guid);
            entity.UpdatePower(powerUpdate.Power, powerUpdate.Value);
        }

        private void DestroyObject(ReceivablePacket content)
        {
            DestroyObject destroyObject = new DestroyObject(content);
            Collection.DestroyEntity(destroyObject.DestroyedGuid);
        }

        private void MonsterMove(ReceivablePacket content)
        {
            MonsterMove monsterMove = new MonsterMove(content);
            Collection.GetUnit(monsterMove.MonsterGuid).UpdatePosition(monsterMove.Position);
        }

        private void HandleMovement(ReceivablePacket content)
        {
            HandleMovement handleMovement = new HandleMovement(content);
            Collection.GetUnit(handleMovement.Guid).UpdateMovement(handleMovement.MovementLiving);
        }

        private void SplineMoveSetWalkMode(ReceivablePacket content)
        {
            SplineMoveSetMode splineMoveSetMode = new SplineMoveSetMode(content);
            Collection.GetUnit(splineMoveSetMode.Guid).SplineMoveMode = SplineMoveMode.WALK;
        }

        private void SplineMoveSetRunMode(ReceivablePacket content)
        {
            SplineMoveSetMode splineMoveSetMode = new SplineMoveSetMode(content);
            Collection.GetUnit(splineMoveSetMode.Guid).SplineMoveMode = SplineMoveMode.RUN;
        }

        private void UpdateFields(object content)
        {
            List<UpdateValues> values = (List<UpdateValues>)content;
            foreach (UpdateValues updateValues in values)
            {                
                Entity entity = Collection.GetUnit(updateValues.Guid);
                entity.UpdateFields(updateValues.Fields);
            }
        }

        private void CreateObjects(object content)
        {
            List<UpdateCreateObject> values = (List<UpdateCreateObject>)content;
            foreach (UpdateCreateObject updateCreateObject in values)
            {
                Entity entity = Collection.GetUnit(updateCreateObject.Guid);
                entity.UpdateFields(updateCreateObject.Fields);
                entity.UpdateMovement(updateCreateObject.Movement);
                Collection.Categorize(entity, updateCreateObject.ObjectType);
            }
        }

        private void UpdateMovements(object content)
        {
            List<UpdateMovement> values = (List<UpdateMovement>)content;
            foreach (UpdateMovement updateMovement in values)
            {
                Entity entity = Collection.GetUnit(updateMovement.Guid);
                entity.UpdateMovement(updateMovement.Movement);
                Logger.Log("Updating Movement for Guid " + updateMovement.Guid, LogLevel.INFO);
            }
        }
    }
}
