namespace TrinityCore.GameClient.Net.Lib.World.Enums
{
    internal enum SplineFlags : uint
    {
        None = 0x00000000,
        // x00-xFF(first byte) used as animation Ids storage in pair with Animation flag
        Done = 0x00000100,
        Falling = 0x00000200,           // Affects elevation computation, can't be combined with Parabolic flag
        No_Spline = 0x00000400,
        Parabolic = 0x00000800,           // Affects elevation computation, can't be combined with Falling flag
        CanSwim = 0x00001000,
        Flying = 0x00002000,           // Smooth movement(Catmullrom interpolation mode), flying animation
        OrientationFixed = 0x00004000,           // Model orientation fixed
        Final_Point = 0x00008000,
        Final_Target = 0x00010000,
        Final_Angle = 0x00020000,
        Catmullrom = 0x00040000,           // Used Catmullrom interpolation mode
        Cyclic = 0x00080000,           // Movement by cycled spline
        Enter_Cycle = 0x00100000,           // Everytimes appears with cyclic flag in monster move packet, erases first spline vertex after first cycle done
        Animation = 0x00200000,           // Plays animation after some time passed
        Frozen = 0x00400000,           // Will never arrive
        TransportEnter = 0x00800000,
        TransportExit = 0x01000000,
        Unknown7 = 0x02000000,
        Unknown8 = 0x04000000,
        Backward = 0x08000000,
        Unknown10 = 0x10000000,
        Unknown11 = 0x20000000,
        Unknown12 = 0x40000000,
        Unknown13 = 0x80000000,

        // Masks
        Mask_Final_Facing = Final_Point | Final_Target | Final_Angle,
        // animation ids stored here, see AnimationTier enum, used with Animation flag
        Mask_Animations = 0xFF,
        // flags that shouldn't be appended into SMSG_MONSTER_MOVE\SMSG_MONSTER_MOVE_TRANSPORT packet, should be more probably
        Mask_No_Monster_Move = Mask_Final_Facing | Mask_Animations | Done,
        // CatmullRom interpolation mode used
        Mask_CatmullRom = Flying | Catmullrom,
        // Unused, not suported flags
        Mask_Unused = No_Spline | Enter_Cycle | Frozen | Unknown7 | Unknown8 | Unknown10 | Unknown11 | Unknown12 | Unknown13
    }
}
