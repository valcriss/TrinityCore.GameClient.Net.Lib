using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Logging.Tools
{
    internal static class LoggingExtensions
    {
        #region Internal Methods

        internal static string ListToString<T>(this List<T> list, string separator = ", ")
        {
            StringBuilder result = new StringBuilder();
            result.Append("count : " + list.Count + (list.Count > 0 ? separator : null));
            for (int i = 0; i < list.Count; i++)
            {
                T item = list[i];
                result.Append(item.ToString() + (i < list.Count - 1 ? separator : null));
            }
            return result.ToString();
        }

        #endregion Internal Methods
    }
}