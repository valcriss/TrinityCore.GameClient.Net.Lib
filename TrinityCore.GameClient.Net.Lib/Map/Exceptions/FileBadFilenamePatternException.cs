using System;

namespace TrinityCore.GameClient.Net.Lib.Map.Exceptions
{
    public class FileBadFilenamePatternException : Exception
    {
        #region Public Constructors

        public FileBadFilenamePatternException(string patternType, string pattern) : base("Provided filename does not match expected " + patternType + " filename pattern (" + pattern + ")")
        {
        }

        #endregion Public Constructors
    }
}