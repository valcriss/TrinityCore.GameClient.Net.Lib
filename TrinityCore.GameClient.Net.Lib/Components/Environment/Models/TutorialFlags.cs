using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Models
{
    public class TutorialFlags
    {
        #region Public Properties

        public uint[] Values { get; set; }

        #endregion Public Properties

        #region Internal Fields

        internal const int MAX_ACCOUNT_TUTORIAL_VALUES = 8;

        #endregion Internal Fields

        #region Public Constructors

        public TutorialFlags()
        {
            Values = new uint[MAX_ACCOUNT_TUTORIAL_VALUES];
        }

        #endregion Public Constructors

        #region Public Methods

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < MAX_ACCOUNT_TUTORIAL_VALUES; i++)
            {
                result.Append(Values[i] + (i != MAX_ACCOUNT_TUTORIAL_VALUES - 1 ? "-" : null));
            }
            return result.ToString();
        }

        #endregion Public Methods
    }
}