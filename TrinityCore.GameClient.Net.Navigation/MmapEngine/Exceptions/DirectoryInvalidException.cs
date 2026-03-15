using System;
using System.Runtime.Serialization;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.Exceptions
{
    [Serializable]
    public class DirectoryInvalidException : Exception
    {
        #region Public Constructors

        public DirectoryInvalidException()
            : base("Provided directory is not valid (not exists or no map data found)")
        {
        }

        #endregion Public Constructors

        #region Protected Constructors

        protected DirectoryInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion Protected Constructors
    }
}

