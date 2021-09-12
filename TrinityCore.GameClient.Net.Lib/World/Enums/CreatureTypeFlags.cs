using System;

namespace TrinityCore.GameClient.Net.Lib.World.Enums
{
    [Flags]
    internal enum CreatureTypeFlags : uint
    {
        CREATURE_TYPE_FLAG_TAMEABLE_PET =
            0x00000001, // Makes the mob tameable (must also be a beast and have family set)

        CREATURE_TYPE_FLAG_GHOST_VISIBLE =
            0x00000002, // Creature are also visible for not alive player. Allow gossip interaction if npcflag allow?

        CREATURE_TYPE_FLAG_BOSS_MOB =
            0x00000004, // Changes creature's visible level to "??" in the creature's portrait - Immune Knockback.

        CREATURE_TYPE_FLAG_DO_NOT_PLAY_WOUND_PARRY_ANIMATION = 0x00000008,
        CREATURE_TYPE_FLAG_HIDE_FACTION_TOOLTIP = 0x00000010,
        CREATURE_TYPE_FLAG_UNK5 = 0x00000020, // Sound related
        CREATURE_TYPE_FLAG_SPELL_ATTACKABLE = 0x00000040,

        CREATURE_TYPE_FLAG_CAN_INTERACT_WHILE_DEAD =
            0x00000080, // Player can interact with the creature if its dead (not player dead)

        CREATURE_TYPE_FLAG_HERB_SKINNING_SKILL = 0x00000100, // Can be looted by herbalist
        CREATURE_TYPE_FLAG_MINING_SKINNING_SKILL = 0x00000200, // Can be looted by miner
        CREATURE_TYPE_FLAG_DO_NOT_LOG_DEATH = 0x00000400, // Death event will not show up in combat log
        CREATURE_TYPE_FLAG_MOUNTED_COMBAT_ALLOWED = 0x00000800, // Creature can remain mounted when entering combat
        CREATURE_TYPE_FLAG_CAN_ASSIST = 0x00001000, // ? Can aid any player in combat if in range?
        CREATURE_TYPE_FLAG_IS_PET_BAR_USED = 0x00002000,
        CREATURE_TYPE_FLAG_MASK_UID = 0x00004000,
        CREATURE_TYPE_FLAG_ENGINEERING_SKINNING_SKILL = 0x00008000, // Can be looted by engineer
        CREATURE_TYPE_FLAG_EXOTIC_PET = 0x00010000, // Can be tamed by hunter as exotic pet

        CREATURE_TYPE_FLAG_USE_DEFAULT_COLLISION_BOX =
            0x00020000, // Collision related. (always using default collision box?)

        CREATURE_TYPE_FLAG_IS_SIEGE_WEAPON = 0x00040000,

        CREATURE_TYPE_FLAG_CAN_COLLIDE_WITH_MISSILES =
            0x00080000, // Projectiles can collide with this creature - interacts with TARGET_DEST_TRAJ

        CREATURE_TYPE_FLAG_HIDE_NAME_PLATE = 0x00100000,
        CREATURE_TYPE_FLAG_DO_NOT_PLAY_MOUNTED_ANIMATIONS = 0x00200000,
        CREATURE_TYPE_FLAG_IS_LINK_ALL = 0x00400000,
        CREATURE_TYPE_FLAG_INTERACT_ONLY_WITH_CREATOR = 0x00800000,
        CREATURE_TYPE_FLAG_DO_NOT_PLAY_UNIT_EVENT_SOUNDS = 0x01000000,
        CREATURE_TYPE_FLAG_HAS_NO_SHADOW_BLOB = 0x02000000,

        CREATURE_TYPE_FLAG_TREAT_AS_RAID_UNIT =
            0x04000000, // ! Creature can be targeted by spells that require target to be in caster's party/raid

        CREATURE_TYPE_FLAG_FORCE_GOSSIP = 0x08000000, // Allows the creature to display a single gossip option.
        CREATURE_TYPE_FLAG_DO_NOT_SHEATHE = 0x10000000,
        CREATURE_TYPE_FLAG_DO_NOT_TARGET_ON_INTERACTION = 0x20000000,
        CREATURE_TYPE_FLAG_DO_NOT_RENDER_OBJECT_NAME = 0x40000000,
        CREATURE_TYPE_FLAG_UNIT_IS_QUEST_BOSS = 0x80000000 // Not verified
    }
}
