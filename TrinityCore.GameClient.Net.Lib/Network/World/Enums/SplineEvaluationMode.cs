using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Enums
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
