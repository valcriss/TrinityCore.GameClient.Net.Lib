using System;
using System.Runtime.Serialization;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.Exceptions
{
    [Serializable]
    public class FileBadFilenamePatternException : Exception
    {
        #region Public Constructors

        public FileBadFilenamePatternException(string patternType, string pattern) : base("Provided filename does not match expected " + patternType + " filename pattern (" + pattern + ")")
        {
        }

        #endregion Public Constructors

        #region Protected Constructors

        protected FileBadFilenamePatternException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion Protected Constructors
    }
}

