using System;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;

namespace TrinityCore.GameClient.Net.Lib.Logging
{
    public interface ILoggerHandler
    {
        #region Public Methods

        void Append(LogCategory category, LogLevel level, string message);

        void Append(LogCategory category, Exception exception);

        #endregion Public Methods
    }
}