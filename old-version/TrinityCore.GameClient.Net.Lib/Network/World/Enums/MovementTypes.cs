using System;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Enums
{
    [Flags]
    internal enum MovementOptions : ushort
    {
        None = 0x00000000,
        NO_STRAFE = 0x00000001,
        NO_JUMPING = 0x00000002,
        UNK3 = 0x00000004,        // Overrides various clientside checks
        FULL_SPEED_TURNING = 0x00000008,
        FULL_SPEED_PITCHING = 0x00000010,
        ALWAYS_ALLOW_PITCHING = 0x00000020,
        UNK7 = 0x00000040,
        UNK8 = 0x00000080,
        UNK9 = 0x00000100,
        UNK10 = 0x00000200,
        INTERPOLATED_MOVEMENT = 0x00000400,
        INTERPOLATED_TURNING = 0x00000800,
        INTERPOLATED_PITCHING = 0x00001000,
        UNK14 = 0x00002000,
        CAN_TRANSITION_BETWEEN_SWIM_AND_FLY = 0x00004000,
        UNK16 = 0x00008000
    }

    [Flags]
    internal enum MovementTypes : uint
    {
        None = 0x00000000,
        FORWARD = 0x00000001,
        BACKWARD = 0x00000002,
        STRAFE_LEFT = 0x00000004,
        STRAFE_RIGHT = 0x00000008,
        LEFT = 0x00000010,
        RIGHT = 0x00000020,
        PITCH_UP = 0x00000040,
        PITCH_DOWN = 0x00000080,
        WALKING = 0x00000100,               // Walking
        ONTRANSPORT = 0x00000200,               // Used for flying on some creatures
        DISABLE_GRAVITY = 0x00000400,               // Former MOVEMENTFLAG_LEVITATING. This is used when walking is not possible.
        ROOT = 0x00000800,               // Must not be set along with MOVEMENTFLAG_MASK_MOVING
        FALLING = 0x00001000,               // damage dealt on that type of falling
        FALLING_FAR = 0x00002000,
        PENDING_STOP = 0x00004000,
        PENDING_STRAFE_STOP = 0x00008000,
        PENDING_FORWARD = 0x00010000,
        PENDING_BACKWARD = 0x00020000,
        PENDING_STRAFE_LEFT = 0x00040000,
        PENDING_STRAFE_RIGHT = 0x00080000,
        PENDING_ROOT = 0x00100000,
        SWIMMING = 0x00200000,               // appears with fly flag also
        ASCENDING = 0x00400000,               // press "space" when flying or swimming
        DESCENDING = 0x00800000,
        CAN_FLY = 0x01000000,               // Appears when unit can fly AND also walk
        FLYING = 0x02000000,               // unit is actually flying. pretty sure this is only used for players. creatures use disable_gravity
        SPLINE_ELEVATION = 0x04000000,               // used for flight paths
        SPLINE_ENABLED = 0x08000000,               // used for flight paths
        WATERWALKING = 0x10000000,               // prevent unit from falling through water
        FALLING_SLOW = 0x20000000,               // active rogue safe fall spell (passive)
        HOVER = 0x40000000,               // hover, cannot jump

        MOVEMENTFLAG_MASK_MOVING =
            FORWARD | BACKWARD | STRAFE_LEFT | STRAFE_RIGHT |
            FALLING | FALLING_FAR | ASCENDING | DESCENDING |
            SPLINE_ELEVATION,

        MOVEMENTFLAG_MASK_TURNING =
            LEFT | RIGHT | PITCH_UP | PITCH_DOWN,

        MOVEMENTFLAG_MASK_MOVING_FLY =
            FLYING | ASCENDING | DESCENDING,

        MOVEMENTFLAG_MASK_PLAYER_ONLY =
            FLYING,

        MOVEMENTFLAG_MASK_HAS_PLAYER_STATUS_OPCODE = DISABLE_GRAVITY | ROOT |
            CAN_FLY | WATERWALKING | FALLING_SLOW | HOVER
    }
}