using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Environment.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Models
{
    public class AccountDataTimes
    {
        public Dictionary<AccountDataTimesTypes, uint> Values { get; set; }

        public AccountDataTimes()
        {
            Values = new Dictionary<AccountDataTimesTypes, uint>();
        }

        public override string ToString()
        {
            string result = string.Empty;
            foreach (KeyValuePair<AccountDataTimesTypes, uint> item in Values)
            {
                result += $"{item.Key} = {item.Value}, ";
            }
            return result.Trim();
        }
    }
}
