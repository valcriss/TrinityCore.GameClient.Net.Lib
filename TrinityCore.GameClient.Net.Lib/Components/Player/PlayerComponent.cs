using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Logging.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player
{
    public class PlayerComponent : Component
    {
        #region Public Properties

        public WorldPoint BindPoint { get; set; }
        public List<Spell> Spells { get; set; }
        public List<Spell> UnlearnedSpells { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public PlayerComponent(WorldClient worldClient) : base(worldClient)
        {
            Spells = new List<Spell>();
            UnlearnedSpells = new List<Spell>();

            WorldClient.PacketsHandler.RegisterHandler<BindPointUpdate>(WorldCommand.SMSG_BINDPOINTUPDATE, BindPointUpdate);
            WorldClient.PacketsHandler.RegisterHandler<InitialSpellsInfo>(WorldCommand.SMSG_INITIAL_SPELLS, InitialSpellsInfo);
            WorldClient.PacketsHandler.RegisterHandler<UnlearnedSpellsInfo>(WorldCommand.SMSG_SEND_UNLEARN_SPELLS, UnlearnedSpellsInfo);
        }

        #endregion Public Constructors

        #region Private Methods

        private bool BindPointUpdate(BindPointUpdate bindPointUpdate)
        {
            BindPoint = bindPointUpdate.BindPoint;
            Logger.Append(Logging.Enums.LogCategory.PLAYER, Logging.Enums.LogLevel.DEBUG, "Bind Point : " + BindPoint.ToString());
            return true;
        }

        private bool InitialSpellsInfo(InitialSpellsInfo initialSpells)
        {
            Spells = initialSpells.Spells;
            Logger.Append(Logging.Enums.LogCategory.PLAYER, Logging.Enums.LogLevel.DEBUG, "Initial Spells : " + Spells.ListToString());
            return true;
        }

        private bool UnlearnedSpellsInfo(UnlearnedSpellsInfo unlearnedSpells)
        {
            UnlearnedSpells = unlearnedSpells.UnlearnedSpells;
            Logger.Append(Logging.Enums.LogCategory.PLAYER, Logging.Enums.LogLevel.DEBUG, "Unlearned Spells : " + UnlearnedSpells.ListToString());
            return true;
        }

        #endregion Private Methods
    }
}