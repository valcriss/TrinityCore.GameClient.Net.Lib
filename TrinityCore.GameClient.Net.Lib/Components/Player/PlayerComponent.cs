using System;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;
using TrinityCore.GameClient.Net.Lib.Logging.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Player
{
    public class PlayerComponent : Component
    {
        #region Public Properties

        public WorldPoint BindPoint { get; set; }
        public List<EquipmentSet> EquipmentSets { get; set; }

        public UInt64? Guid
        { get { if (WorldClient.GetCharacter() != null) { return WorldClient.GetCharacter().GUID; } return null; } }

        public bool IsStanding { get; set; }
        public TalentCollection PetTalents { get; set; }
        public PlayerTalentCollection PlayerTalents { get; set; }

        public Position Position
        { 
            get 
            { 
                return GameClient.Get<EntitiesComponent>().Collection.GetPlayer().GetPosition(); 
            } 
            set 
            {
                GameClient.Get<EntitiesComponent>().Collection.GetPlayer().UpdatePosition(value); 
            } 
        }

        public List<Spell> Spells { get; set; }
        public List<Spell> UnlearnedSpells { get; set; }

        #endregion Public Properties

        #region Internal Properties

        internal List<GiverStatus> QuestsGiverStatuses { get; set; }

        #endregion Internal Properties

        #region Private Properties

        private bool CanMove { get; set; }
        private bool IsInCombat { get; set; }
        internal bool MovementInitialized { get; set; }
        private Dictionary<Powers, UInt32> Powers { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public PlayerComponent(WorldClient worldClient) : base(worldClient)
        {
            IsInCombat = false;
            IsStanding = true;
            CanMove = true;
            MovementInitialized = false;
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
            WorldClient.PacketsHandler.RegisterHandler<StandStateUpdateInfo>(WorldCommand.SMSG_STANDSTATE_UPDATE, StandStateUpdateInfo);
            WorldClient.PacketsHandler.RegisterHandler<MoveRootInfo>(WorldCommand.SMSG_FORCE_MOVE_ROOT, MoveRootInfo);
            WorldClient.PacketsHandler.RegisterHandler<MoveRootInfo>(WorldCommand.SMSG_FORCE_MOVE_UNROOT, MoveRootInfo);
            WorldClient.PacketsHandler.RegisterHandler<CancelCombatRequest>(WorldCommand.SMSG_CANCEL_COMBAT, CancelCombatRequest);
        }

        #endregion Public Constructors

        #region Public Methods

        public bool Face(Entities.Models.Entity target)
        {
            bool result = Face(target.GetPosition());
            if (result) Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Sending Facing entity : " + target.Guid);
            return result;
        }

        public bool Face(float angle)
        {
            if (!CanMove) return false;
            Position current = Position;
            if (Math.Abs(current.O - angle) > 0.01f)
            {
                Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Sending Facing angle : " + angle);
                current.O = angle;
                Position = current;
                SendActivlyMoving();
                return WorldClient.Send(new FacingMovement((ulong)Guid, Position, false));
            }
            return false;
        }

        public bool Face(Position destination)
        {
            float angle = (destination - Position).Direction.O;
            bool result = Face(angle);
            if (result) Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Sending Facing position : " + destination);
            return result;
        }

        public bool Stand(bool value)
        {
            if (!CanMove) return false;
            if (value == IsStanding)
                return false;

            return WorldClient.Send(new StandPositionRequest((ulong)Guid, value ? UnitStandStateType.UNIT_STAND_STATE_STAND : UnitStandStateType.UNIT_STAND_STATE_SIT));
        }

        #endregion Public Methods

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

        internal void SendActivlyMoving()
        {
            if (!MovementInitialized)
            {
                Logger.Append(LogCategory.PLAYER, LogLevel.DEBUG, "Sending ActivlyMoving : " + Guid);
                WorldClient.Send(new ActivlyMoving((ulong)Guid));
                MovementInitialized = true;
            }
        }

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

        private bool CancelCombatRequest(CancelCombatRequest cancelCombatRequest)
        {
            IsInCombat = false;
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

        private bool MoveRootInfo(MoveRootInfo moveRootInfo)
        {
            CanMove = moveRootInfo.CanMove;
            return true;
        }

        private bool QuestGiverStatusMultiple(QuestGiverStatusMultiple questGiverStatusMultiple)
        {
            QuestsGiverStatuses = questGiverStatusMultiple.GiverStatuses;
            return true;
        }

        private bool StandStateUpdateInfo(StandStateUpdateInfo stateUpdateInfo)
        {
            IsStanding = stateUpdateInfo.StandType == UnitStandStateType.UNIT_STAND_STATE_STAND;
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