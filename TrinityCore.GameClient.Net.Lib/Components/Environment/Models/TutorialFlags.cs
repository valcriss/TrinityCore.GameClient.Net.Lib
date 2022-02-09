using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Models
{
    public class TutorialFlags
    {
        internal const int MAX_ACCOUNT_TUTORIAL_VALUES = 8;
        public uint[] Values { get; set; }

        public TutorialFlags()
        {
            Values = new uint[MAX_ACCOUNT_TUTORIAL_VALUES];
        }

        public override string ToString()
        {
            string result = string.Empty;
            for (int i = 0; i < MAX_ACCOUNT_TUTORIAL_VALUES; i++)
            {
                result += Values[i] + (i != MAX_ACCOUNT_TUTORIAL_VALUES - 1 ? "-" : null);
            }
            return result;
        }
    }
}
