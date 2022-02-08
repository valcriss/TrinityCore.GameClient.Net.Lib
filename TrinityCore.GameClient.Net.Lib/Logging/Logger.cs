using System;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;

namespace TrinityCore.GameClient.Net.Lib.Logging
{
    public static class Logger
    {
        #region Private Properties

        private static Dictionary<string, ILoggerHandler> Handlers { get; set; }

        #endregion Private Properties

        #region Public Constructors

        static Logger()
        {
            Handlers = new Dictionary<string, ILoggerHandler>();
        }

        #endregion Public Constructors

        #region Public Methods

        public static void Append(LogCategory category, LogLevel level, string message)
        {
            foreach (ILoggerHandler handler in Handlers.Values)
            {
                handler.Append(category, level, message);
            }
        }

        public static void Append(LogCategory category, Exception exception)
        {
            foreach (ILoggerHandler handler in Handlers.Values)
            {
                handler.Append(category, exception);
            }
        }

        public static void RegisterHandler(string name, ILoggerHandler handler)
        {
            lock (Handlers)
            {
                if (!Handlers.ContainsKey(name))
                {
                    Handlers.Add(name, handler);
                }              
            }
        }

        public static void UnregisterHandler(string name)
        {
            lock (Handlers)
            {
                if (!Handlers.ContainsKey(name))
                {
                    throw new InvalidOperationException("Name not found in registered handlers");
                }
                Handlers.Remove(name);
            }
        }

        #endregion Public Methods
    }
}