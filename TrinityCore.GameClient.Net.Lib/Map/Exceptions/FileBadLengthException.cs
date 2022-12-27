using System;

namespace TrinityCore.GameClient.Net.Lib.Map.Exceptions
{
    public class FileBadLengthException : Exception
    {
        #region Public Constructors

        public FileBadLengthException(int found, int expected) : base("Provided file length does not match expected file length (found:" + found + ", expected:" + expected + ")")
        {
        }

        #endregion Public Constructors
    }
}