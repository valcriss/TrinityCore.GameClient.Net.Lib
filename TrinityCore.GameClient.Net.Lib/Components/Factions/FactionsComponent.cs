using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Factions.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Models;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Factions
{
    public class FactionsComponent : Component
    {
        #region Public Properties

        public List<Reputation> ForcedReactions { get; set; }
        public List<Reputation> Reputations { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public FactionsComponent(WorldClient worldClient) : base(worldClient)
        {
            Reputations = new List<Reputation>();
            ForcedReactions = new List<Reputation>();

            WorldClient.PacketsHandler.RegisterHandler<InitializeFactionsInfo>(WorldCommand.SMSG_INITIALIZE_FACTIONS, InitializeFactionsInfo);
            WorldClient.PacketsHandler.RegisterHandler<ForcedReactionsInfo>(WorldCommand.SMSG_SET_FORCED_REACTIONS, ForcedReactionsInfo);
        }

        #endregion Public Constructors

        #region Private Methods

        private bool ForcedReactionsInfo(ForcedReactionsInfo forcedReactions)
        {
            ForcedReactions = forcedReactions.ForcedReactions;
            return true;
        }

        private bool InitializeFactionsInfo(InitializeFactionsInfo initializeFactions)
        {
            Reputations = initializeFactions.Reputations;
            return true;
        }

        #endregion Private Methods
    }
}