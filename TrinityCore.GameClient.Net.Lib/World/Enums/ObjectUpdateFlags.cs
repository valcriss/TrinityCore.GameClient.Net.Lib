using System;

namespace TrinityCore.GameClient.Net.Lib.World.Enums
{
    [Flags]
    public enum ObjectUpdateFlags : ushort
    {
        UPDATEFLAG_NONE = 0x0000,
        UPDATEFLAG_SELF = 0x0001,
        UPDATEFLAG_TRANSPORT = 0x0002,
        UPDATEFLAG_HAS_TARGET = 0x0004,
        UPDATEFLAG_UNKNOWN = 0x0008,
        UPDATEFLAG_LOWGUID = 0x0010,
        UPDATEFLAG_LIVING = 0x0020,
        UPDATEFLAG_STATIONARY_POSITION = 0x0040,
        UPDATEFLAG_VEHICLE = 0x0080,
        UPDATEFLAG_POSITION = 0x0100,
        UPDATEFLAG_ROTATION = 0x0200
    }

    [Flags]
    public enum ObjectUpdateType
    {
        UPDATETYPE_VALUES = 0,
        UPDATETYPE_MOVEMENT = 1,
        UPDATETYPE_CREATE_OBJECT = 2,
        UPDATETYPE_CREATE_OBJECT2 = 3,
        UPDATETYPE_OUT_OF_RANGE_OBJECTS = 4,
        UPDATETYPE_NEAR_OBJECTS = 5
    }
}
