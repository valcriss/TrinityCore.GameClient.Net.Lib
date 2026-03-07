using System;
using System.Runtime.Serialization;

namespace TrinityCore.GameClient.Net.Lib.Map.Exceptions
{
    [Serializable]
    public class FileBadLengthException : Exception
    {
        #region Public Constructors

        public FileBadLengthException(int found, int expected) : base("Provided file length does not match expected file length (found:" + found + ", expected:" + expected + ")")
        {
        }

        #endregion Public Constructors

        #region Protected Constructors

        protected FileBadLengthException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion Protected Constructors
    }
}