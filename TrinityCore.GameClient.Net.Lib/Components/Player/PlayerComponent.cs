using System;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;
using TrinityCore.GameClient.Net.Lib.Logging.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player
{
    public class PlayerComponent : Component
    {
        #region Public Properties

        public WorldPoint BindPoint { get; set; }
        public List<EquipmentSet> EquipmentSets { get; set; }
        public UInt64? Guid
        { get { if (WorldClient.GetCharacter() != null) { return WorldClient.GetCharacter().GUID; } return null; } }
        public TalentCollection PetTalents { get; set; }
        public PlayerTalentCollection PlayerTalents { get; set; }
        public List<Spell> Spells { get; set; }
        public List<Spell> UnlearnedSpells { get; set; }

        #endregion Public Properties

        #region Internal Properties

        internal List<GiverStatus> QuestsGiverStatuses { get; set; }

        #endregion Internal Properties

        #region Private Properties

        private Dictionary<Powers, UInt32> Powers { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public PlayerComponent(WorldClient worldClient) : base(worldClient)
        {
            Spells = new List<Spell>();
            UnlearnedSpells = new List<Spell>();
            Powers = new Dictionary<Powers, uint>();
            PlayerTalents = new PlayerTalentCollection();
            PetTalents = new TalentCollection();
            QuestsGiverStatuses = new List<GiverStatus>();

            WorldClient.PacketsHandler.RegisterHandler<BindPointUpdate>(WorldCommand.SMSG_BINDPOINTUPDATE, BindPointUpdate);
            WorldClient.PacketsHandler.RegisterHandler<InitialSpellsInfo>(WorldCommand.SMSG_INITIAL_SPELLS, InitialSpellsInfo);
            WorldClient.PacketsHandler.RegisterHandler<UnlearnedSpellsInfo>(WorldCommand.SMSG_SEND_UNLEARN_SPELLS, UnlearnedSpellsInfo);
            WorldClient.PacketsHandler.RegisterHandler<UpdateProficiencyInfo>(WorldCommand.SMSG_SET_PROFICIENCY, UpdateProficiencyInfo);
            WorldClient.PacketsHandler.RegisterHandler<TalentsInfo>(WorldCommand.SMSG_TALENTS_INFO, TalentsInfo);
            WorldClient.PacketsHandler.RegisterHandler<AllAchievementDataInfo>(WorldCommand.SMSG_ALL_ACHIEVEMENT_DATA, AllAchievementDataInfo);
            WorldClient.PacketsHandler.RegisterHandler<EquipmentSetList>(WorldCommand.SMSG_EQUIPMENT_SET_LIST, EquipmentSetList);
            WorldClient.PacketsHandler.RegisterHandler<QuestGiverStatusMultiple>(WorldCommand.SMSG_QUESTGIVER_STATUS_MULTIPLE, QuestGiverStatusMultiple);
        }

        #endregion Public Constructors

        #region Internal Methods

        internal void UpdatePower(Powers power, uint value)
        {
            Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, $"Update power : {power} = {value}");
            if (!Powers.ContainsKey(power))
            {
                Powers.Add(power, value);
                return;
            }
            Powers[power] = value;
        }

        #endregion Internal Methods

        #region Private Methods

        private bool AllAchievementDataInfo(AllAchievementDataInfo allAchievementDataInfo)
        {
            // TODO : do something with that
            return true;
        }

        private bool BindPointUpdate(BindPointUpdate bindPointUpdate)
        {
            BindPoint = bindPointUpdate.BindPoint;
            Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Bind Point : " + BindPoint.ToString());
            return true;
        }

        private bool EquipmentSetList(EquipmentSetList equipmentSetList)
        {
            EquipmentSets = equipmentSetList.EquipmentSets;
            return true;
        }

        private bool InitialSpellsInfo(InitialSpellsInfo initialSpells)
        {
            Spells = initialSpells.Spells;
            Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Initial Spells : " + Spells.ListToString());
            return true;
        }

        private bool QuestGiverStatusMultiple(QuestGiverStatusMultiple questGiverStatusMultiple)
        {
            QuestsGiverStatuses = questGiverStatusMultiple.GiverStatuses;
            return true;
        }

        private bool TalentsInfo(TalentsInfo talentsInfo)
        {
            // TODO : Add log info
            if (talentsInfo.IsPet)
            {
                PetTalents = new TalentCollection(talentsInfo.Talents, talentsInfo.UnSpendPoints);
            }
            else
            {
                PlayerTalents = new PlayerTalentCollection(talentsInfo.Talents, talentsInfo.UnSpendPoints, talentsInfo.Glyphs);
            }
            return true;
        }

        private bool UnlearnedSpellsInfo(UnlearnedSpellsInfo unlearnedSpells)
        {
            UnlearnedSpells = unlearnedSpells.UnlearnedSpells;
            Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Unlearned Spells : " + UnlearnedSpells.ListToString());
            return true;
        }

        private bool UpdateProficiencyInfo(UpdateProficiencyInfo updateProficiencyInfo)
        {
            // TODO: Bug
            switch (updateProficiencyInfo.ItemClass)
            {
                case ItemClass.ITEM_CLASS_ARMOR:
                    Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, updateProficiencyInfo.GetArmorProficiency().ToString());
                    break;

                case ItemClass.ITEM_CLASS_WEAPON:
                    Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, updateProficiencyInfo.GetWeaponProficiency().ToString());
                    break;

                default:
                    Logger.Append(LogCategory.PLAYER, LogLevel.ERROR, "Unhandled Item Class : " + updateProficiencyInfo.ItemClass.ToString());
                    break;
            }
            return true;
        }

        #endregion Private Methods
    }
}