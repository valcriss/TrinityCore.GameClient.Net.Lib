﻿using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Clients;
using TrinityCore.GameClient.Net.Lib.Components.Player.Commands;
using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;
using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player
{
    public class PlayerComponent : GameComponent
    {
        public BindPosition BindPosition { get; set; }
        public uint? MapId => WorldClient?.Character?.MapId;
        public ulong? Guid => WorldClient?.Character?.GUID;
        public List<Faction> Factions { get; set; }
        public List<Faction> ForcedFactions { get; set; }
        public List<Spell> Spells { get; set; }
        public List<TalentInfo> PetTalents { get; set; }
        public List<TalentInfo> Talents { get; set; }
        public List<GlyphInfo> Glyphs { get; set; }
        public Dictionary<ulong, GiverStatus> QuestsGiversStatus { get; set; }

        private List<CompletedAchievement> CompletedAchievements { get; set; }
        private List<AchievementCriteria> AchievementCriteriaList { get; set; }
        public PlayerComponent()
        {
            Factions = new List<Faction>();
            ForcedFactions = new List<Faction>();
            QuestsGiversStatus = new Dictionary<ulong, GiverStatus>();
            Spells = new List<Spell>();
            PetTalents = new List<TalentInfo>();
            Talents = new List<TalentInfo>();
            Glyphs = new List<GlyphInfo>();
            CompletedAchievements = new List<CompletedAchievement>();
            AchievementCriteriaList = new List<AchievementCriteria>();
        }

        public override void RegisterHandlers()
        {
            RegisterHandler(WorldCommand.SMSG_LEARNED_DANCE_MOVES, LearnedDanceMoves);
            RegisterHandler(WorldCommand.SMSG_BINDPOINTUPDATE, BindPointUpdate);
            RegisterHandler(WorldCommand.SMSG_INITIALIZE_FACTIONS, InitializeFactions);
            RegisterHandler(WorldCommand.SMSG_QUESTGIVER_STATUS_MULTIPLE, QuestGiverStatusMultiple);
            RegisterHandler(WorldCommand.SMSG_INITIAL_SPELLS, InitialSpells);
            RegisterHandler(WorldCommand.SMSG_TALENTS_INFO, TalentsInformations);
            RegisterHandler(WorldCommand.SMSG_SET_FORCED_REACTIONS, SetForcedReactions);
            RegisterHandler(WorldCommand.SMSG_ALL_ACHIEVEMENT_DATA, AllAchievementData);
            RegisterHandler(WorldCommand.SMSG_SET_PROFICIENCY, UpdateProficiency);
            RegisterHandler(WorldCommand.SMSG_EQUIPMENT_SET_LIST, EquipmentSetList);
        }

        private void LearnedDanceMoves(ReceivablePacket content)
        {
            LearnedDanceMoves learnedDanceMoves = new LearnedDanceMoves(content);
        }

        private void BindPointUpdate(ReceivablePacket content)
        {
            BindPointUpdate bindPointUpdate = new BindPointUpdate(content);
            BindPosition = bindPointUpdate.BindPosition;
            Logger.Log("Bind Point {" + BindPosition + "}", LogLevel.DETAIL);
        }

        private void InitializeFactions(ReceivablePacket content)
        {
            InitializeFactions initializeFactions = new InitializeFactions(content);
            Factions = initializeFactions.Factions;
            Logger.Log("Received " + Factions.Count + " Factions descriptions", LogLevel.DETAIL);
            foreach (Faction faction in Factions)
            {
                Logger.Log("Faction " + faction.Id.ToString("000") + " {Flags:" + faction.Flags.ToString("00") + ", Standing:" + faction.Standing + "}", LogLevel.VERBOSE);
            }
        }

        private void QuestGiverStatusMultiple(ReceivablePacket content)
        {
            QuestGiverStatusMultiple questGiverStatusMultiple = new QuestGiverStatusMultiple(content);
            lock (QuestsGiversStatus)
            {
                foreach (var status in questGiverStatusMultiple.GiverStatuses)
                {
                    Logger.Log("Quest Giver Status {" + status.GiverGuid + "} : " + status.Status, LogLevel.DETAIL);
                    if (QuestsGiversStatus.ContainsKey(status.GiverGuid))
                    {
                        QuestsGiversStatus[status.GiverGuid].Status = status.Status;
                    }
                    else
                    {
                        QuestsGiversStatus.Add(status.GiverGuid, status);
                    }
                }
            }
        }

        private void InitialSpells(ReceivablePacket content)
        {
            InitialSpells initialSpells = new InitialSpells(content);
            Spells = initialSpells.Spells;
            Logger.Log("Received " + Spells.Count + " Initial Spells", LogLevel.DETAIL);
        }

        private void TalentsInformations(ReceivablePacket content)
        {
            TalentsInformations talentsInformations = new TalentsInformations(content);
            Logger.Log("Received " + (talentsInformations.IsPet ? "Pet" : "Player") + " Talents informations",
                LogLevel.DETAIL);
            if (talentsInformations.IsPet)
            {
                PetTalents = talentsInformations.TalentInfos;
                foreach (var petTalent in PetTalents)
                {
                    Logger.Log("Pet Talent {" + petTalent.TalentId + "} : " + petTalent.TalentRank, LogLevel.VERBOSE);
                }
            }
            else
            {
                Talents = talentsInformations.TalentInfos;
                foreach (var talent in Talents)
                {
                    Logger.Log("Talent {" + talent.Group + "} {" + talent.TalentId + "} : " + talent.TalentRank, LogLevel.VERBOSE);
                }
                Glyphs = talentsInformations.GlyphInfos;
                foreach (var glyph in Glyphs)
                {
                    Logger.Log("Glyph  {" + glyph.Group + "} {" + glyph.GlyphId + "}", LogLevel.VERBOSE);
                }
            }
        }

        private void SetForcedReactions(ReceivablePacket content)
        {
            ForcedReactions forcedReactions = new ForcedReactions(content);
            ForcedFactions = forcedReactions.ForcedFactions;
            Logger.Log("Received " + ForcedFactions.Count + " Forced Factions", LogLevel.DETAIL);
        }

        private void AllAchievementData(ReceivablePacket content)
        {
            AllAchievementData allAchievementData = new AllAchievementData(content);
            CompletedAchievements = allAchievementData.CompletedAchievements;
            Logger.Log("Received " + CompletedAchievements.Count + " Completed Achievements", LogLevel.DETAIL);
            AchievementCriteriaList = allAchievementData.AchievementCriteriaList;
            Logger.Log("Received " + AchievementCriteriaList.Count + " Achievement Criterias", LogLevel.DETAIL);
        }

        private void UpdateProficiency(ReceivablePacket content)
        {
            UpdateProficiency updateProficiency = new UpdateProficiency(content);
            switch (updateProficiency.ItemClass)
            {
                case ItemClass.ITEM_CLASS_ARMOR:
                    Logger.Log(updateProficiency.GetArmorProficiency().ToString(), LogLevel.DETAIL);
                    break;

                case ItemClass.ITEM_CLASS_WEAPON:
                    Logger.Log(updateProficiency.GetWeaponProficiency().ToString(), LogLevel.DETAIL);
                    break;

                default:
                    Logger.Log("Unhandled Item Class : " + updateProficiency.ItemClass, LogLevel.ERROR);
                    break;
            }
        }

        private void EquipmentSetList(ReceivablePacket content)
        {
            EquipmentSetList equipmentSetList = new EquipmentSetList(content);
            Logger.Log("Received " + equipmentSetList.EquipmentSets.Count + " Equipment Set Details", LogLevel.DETAIL);
            foreach (EquipmentSet set in equipmentSetList.EquipmentSets)
            {
                Logger.Log("EquipmentSet " + set.Name + " " + set.Icon, LogLevel.INFO);
            }

        }
    }
}
