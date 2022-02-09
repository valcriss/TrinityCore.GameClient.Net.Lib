using TrinityCore.GameClient.Net.Lib.Components.Zone.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Components.Zone.Models;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Zone
{
    public class ZoneComponent : Component
    {
        #region Public Properties

        public Models.WorldState WorldState { get; set; }

        #endregion Public Properties

        #region Internal Constructors

        internal ZoneComponent(WorldClient worldClient) : base(worldClient)
        {
            WorldClient.PacketsHandler.RegisterHandler<InitWorldStatesInfo>(WorldCommand.SMSG_INIT_WORLD_STATES, InitWorldStatesInfo);
            WorldClient.PacketsHandler.RegisterHandler<UpdateWorldStatesInfo>(WorldCommand.SMSG_UPDATE_WORLD_STATE, UpdateWorldStatesInfo);
        }

        #endregion Internal Constructors

        #region Private Methods

        private bool InitWorldStatesInfo(InitWorldStatesInfo initWorldStates)
        {
            WorldState = initWorldStates.WorldState;
            Logger.Append(Logging.Enums.LogCategory.ZONE, Logging.Enums.LogLevel.DEBUG, $"WorldState : {WorldState}");
            return true;
        }

        private bool UpdateWorldStatesInfo(UpdateWorldStatesInfo updateWorldStates)
        {
            WorldState.UpdateVariable(updateWorldStates.Id, updateWorldStates.Value);
            Logger.Append(Logging.Enums.LogCategory.ZONE, Logging.Enums.LogLevel.DEBUG, $"Updating WorldState Variable : {updateWorldStates.Id} -> {updateWorldStates.Value}");

            return true;
        }

        #endregion Private Methods
    }
}