using System;
using System.Runtime.Serialization;

namespace TrinityCore.GameClient.Net.Lib.Map.Exceptions
{
    [Serializable]
    public class FileBadExtensionException : Exception
    {
        #region Public Constructors

        public FileBadExtensionException(string expectedExtension)
            : base("Provided file extension does not match expected extension (" + expectedExtension + ")")
        {
        }

        #endregion Public Constructors

        #region Protected Constructors

        protected FileBadExtensionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion Protected Constructors
    }
}