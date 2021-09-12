namespace TrinityCore.GameClient.Net.Lib.World.Enums
{
    internal enum SplineEvaluationMode : sbyte
    {
        ModeLinear = 0,
        ModeCatmullrom = 1,
        ModeBezier3_Unused = 2,
        UninitializedMode = 3,
        ModesEnd = 4
    }
}
