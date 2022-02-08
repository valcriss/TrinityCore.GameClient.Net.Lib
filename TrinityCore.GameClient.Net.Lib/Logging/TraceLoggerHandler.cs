using System;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;

namespace TrinityCore.GameClient.Net.Lib.Logging
{
    public class TraceLoggerHandler : ILoggerHandler
    {
        #region Public Methods

        public void Append(LogCategory category, LogLevel level, string message)
        {
            System.Diagnostics.Trace.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][" + category.ToString().ToUpper() + "][" + level.ToString().ToUpper() + "] " + message);
        }

        public void Append(LogCategory category, Exception exception)
        {
            System.Diagnostics.Trace.WriteLine("[" + DateTime.Now.ToLongTimeString() + "][" + category.ToString().ToUpper() + "][EXCEPTION] " + exception.Message);
        }

        #endregion Public Methods
    }
}