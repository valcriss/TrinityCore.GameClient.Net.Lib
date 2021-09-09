using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.World.Enums
{
    public enum SplineEvaluationMode : sbyte
    {
        ModeLinear = 0,
        ModeCatmullrom = 1,
        ModeBezier3_Unused = 2,
        UninitializedMode = 3,
        ModesEnd = 4
    }
}
