using System;

namespace TrinityCore.GameClient.Net.Lib.Map.Exceptions
{
    public class DirectoryInvalidException : Exception
    {
        #region Public Constructors

        public DirectoryInvalidException()
            : base("Provided directory is not valid (not exists or no map data found)")
        {
        }

        #endregion Public Constructors
    }
}