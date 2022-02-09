using System;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Enums
{
    [Flags]
    public enum AccountDataTimesTypes
    {
        GLOBAL_CONFIG_CACHE = 0, // 0x01 g
        PER_CHARACTER_CONFIG_CACHE = 1, // 0x02 p
        GLOBAL_BINDINGS_CACHE = 2, // 0x04 g
        PER_CHARACTER_BINDINGS_CACHE = 3, // 0x08 p
        GLOBAL_MACROS_CACHE = 4, // 0x10 g
        PER_CHARACTER_MACROS_CACHE = 5, // 0x20 p
        PER_CHARACTER_LAYOUT_CACHE = 6, // 0x40 p
        PER_CHARACTER_CHAT_CACHE = 7 // 0x80 p
    }
}