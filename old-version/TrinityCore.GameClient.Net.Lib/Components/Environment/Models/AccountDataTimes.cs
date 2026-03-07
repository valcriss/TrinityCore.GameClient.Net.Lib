using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Environment.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Models
{
    public class AccountDataTimes
    {
        #region Public Properties

        public Dictionary<AccountDataTimesTypes, uint> Values { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public AccountDataTimes()
        {
            Values = new Dictionary<AccountDataTimesTypes, uint>();
        }

        #endregion Public Constructors

        #region Public Methods

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (KeyValuePair<AccountDataTimesTypes, uint> item in Values)
            {
                result.Append($"{item.Key} = {item.Value}, ");
            }
            return result.ToString().Trim();
        }

        #endregion Public Methods
    }
}