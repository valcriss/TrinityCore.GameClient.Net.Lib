using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;


namespace Testing
{
    public class NUnitLoggerHandler : TrinityCore.GameClient.Net.Lib.Logging.ILoggerHandler
    {
        public void Append(TrinityCore.GameClient.Net.Lib.Logging.Enums.LogCategory category, TrinityCore.GameClient.Net.Lib.Logging.Enums.LogLevel level, string message)
        {
            TestContext.WriteLine("[" + DateTime.Now.ToShortTimeString() + "][" + category.ToString().ToUpper() + "][" + level.ToString().ToUpper() + "] " + message);
        }

        public void Append(TrinityCore.GameClient.Net.Lib.Logging.Enums.LogCategory category, Exception exception)
        {
            TestContext.WriteLine("[" + DateTime.Now.ToShortTimeString() + "][" + category.ToString().ToUpper() + "][EXCEPTION] " + exception.Message);
        }
    }
}
