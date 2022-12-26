using System;
using System.Collections.Generic;
using System.Linq;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Player;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities
{
    public class EntitiesComponent : Component
    {
        #region Public Properties

        public EntitiesCollection Collection { get; set; }

        #endregion Public Properties

        #region Private Properties

        private PlayerComponent Player { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public EntitiesComponent(WorldClient worldClient) : base(worldClient)
        {
            Collection = new EntitiesCollection(worldClient);
            WorldClient.PacketsHandler.RegisterHandler<PowerUpdateInfo>(Network.World.Enums.WorldCommand.SMSG_POWER_UPDATE, PowerUpdateInfo);
            WorldClient.PacketsHandler.RegisterHandler<UpdateObjectInfo>(Network.World.Enums.WorldCommand.SMSG_UPDATE_OBJECT, UpdateObjectInfo);

            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_FORWARD, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_BACKWARD, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_STOP, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_STRAFE_LEFT, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_STRAFE_RIGHT, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_STOP_STRAFE, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_JUMP, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_TURN_LEFT, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_TURN_RIGHT, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_STOP_TURN, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_PITCH_UP, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_PITCH_DOWN, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_STOP_PITCH, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_SET_RUN_MODE, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_SET_WALK_MODE, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_FALL_LAND, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_SWIM, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_STOP_SWIM, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_SET_FACING, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_SET_PITCH, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_HEARTBEAT, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_ASCEND, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_STOP_ASCEND, HandleMovement);
            WorldClient.PacketsHandler.RegisterHandler<HandleMovement>(WorldCommand.MSG_MOVE_START_DESCEND, HandleMovement);

            WorldClient.PacketsHandler.RegisterHandler<HandleNameQueryReponse>(WorldCommand.SMSG_NAME_QUERY_RESPONSE, HandleNameQueryReponse);
            WorldClient.PacketsHandler.RegisterHandler<MonsterMove>(WorldCommand.SMSG_MONSTER_MOVE, MonsterMove);
        }

        #endregion Public Constructors

        #region Public Methods

        public override void Close()
        {
            Collection.Close();
            base.Close();
        }

        #endregion Public Methods

        #region Private Methods

        private void CreateObjects(List<UpdateCreateObject> values)
        {
            foreach (UpdateCreateObject updateCreateObject in values)
            {
                Entity entity = Collection.GetUnit(updateCreateObject.Guid);
                entity.UpdateFields(updateCreateObject.Fields);
                entity.UpdateMovement(updateCreateObject.Movement);
                Collection.Categorize(entity, updateCreateObject.ObjectType);
            }
        }

        private bool HandleMovement(HandleMovement handleMovement)
        {
            Entity entity = Collection.GetUnit(handleMovement.Guid);
            entity.UpdateMovement(handleMovement.MovementLiving);
            return true;
        }

        private bool HandleNameQueryReponse(HandleNameQueryReponse nameQueryResponse)
        {
            if (nameQueryResponse.Found)
            {
                Entity entity = Collection.GetUnit(nameQueryResponse.Guid);
                entity.Name = nameQueryResponse.Name;
            }
            return true;
        }

        private bool MonsterMove(MonsterMove monsterMove)
        {
            Entity entity = Collection.GetUnit(monsterMove.MonsterGuid);
            entity.UpdatePosition(monsterMove.Position);
            return true;
        }

        private bool PowerUpdateInfo(PowerUpdateInfo powerUpdateInfo)
        {
            Entity entity = Collection.GetUnit(powerUpdateInfo.Guid);
            entity.UpdatePower(powerUpdateInfo.Power, powerUpdateInfo.Value);
            Logger.Append(Logging.Enums.LogCategory.PLAYER, Logging.Enums.LogLevel.DEBUG, $"Update entity {powerUpdateInfo.Guid}  Power : {powerUpdateInfo.Power} = {powerUpdateInfo.Value}");
            return true;
        }

        private void UpdateFields(List<UpdateValues> values)
        {
            foreach (UpdateValues updateValues in values)
            {
                Entity entity = Collection.GetUnit(updateValues.Guid);
                entity.UpdateFields(updateValues.Fields);
            }
        }

        private void UpdateMovements(List<UpdateMovement> values)
        {
            foreach (UpdateMovement updateMovement in values)
            {
                Entity entity = Collection.GetUnit(updateMovement.Guid);
                entity.UpdateMovement(updateMovement.Movement);
            }
        }

        private bool UpdateObjectInfo(UpdateObjectInfo updateObject)
        {
            if (updateObject.UpdateValues.Count > 0)
            {
                UpdateFields(updateObject.UpdateValues);
            }

            if (updateObject.UpdateCreateObjects.Count > 0)
            {
                CreateObjects(updateObject.UpdateCreateObjects);
            }

            if (updateObject.Movements.Count > 0)
            {
                UpdateMovements(updateObject.Movements);
            }

            if (updateObject.UpdateOutOfRanges.Count > 0)
            {
                // TODO: do something
            }

            return true;
        }

        public Models.Player FindPlayerByName(string name)
        {
            return Collection.Players.Values.Where(c => c.Name == name).FirstOrDefault();
        }

        #endregion Private Methods
    }
}