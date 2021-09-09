using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Log
{
    public static class Logger
    {
        public static ILogger LogObject { get; set; }
        public static LogLevel Level { get; set; }

        static Logger()
        {
            Level = LogLevel.WARNING;
        }

        public static void LogException(Exception e)
        {
            LogObject?.LogException(e);
        }

        public static void Log(string message, LogLevel level = LogLevel.INFO)
        {
            if ((int)level <= (int)Level)
            {
                LogObject?.Log(message, level);
            }
        }
    }

    public interface ILogger
    {
        void LogException(Exception e);
        void Log(string message, LogLevel level = LogLevel.INFO);
    }

    public enum LogLevel
    {
        DETAIL = 4,
        INFO = 3,
        WARNING = 2,
        ERROR = 0,
        SUCCESS = 1,
    }
}
