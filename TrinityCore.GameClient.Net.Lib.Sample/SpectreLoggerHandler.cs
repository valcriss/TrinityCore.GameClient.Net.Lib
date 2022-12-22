using Spectre.Console;
using System;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    public class SpectreLoggerHandler : ILoggerHandler
    {
        #region Private Properties

        private LogLevel MinLevel { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public SpectreLoggerHandler(LogLevel minLevel)
        {
            MinLevel = minLevel;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Append(LogCategory category, LogLevel level, string message)
        {
            if (MinLevel < level) return;

            string color = "grey";
            switch (level)
            {
                case LogLevel.DEBUG:
                    color = "grey";
                    break;

                case LogLevel.VERBOSE:
                    color = "grey";
                    break;

                case LogLevel.INFORMATION:
                    color = "white";
                    break;

                case LogLevel.WARNING:
                    color = "orange1";
                    break;

                case LogLevel.ERROR:
                    color = "red";
                    break;
            }

            AnsiConsole.MarkupLine($"[{color}]{DateTime.Now.ToLongTimeString()} - {category.ToString().ToUpper()} - {level.ToString().ToUpper()} : {message}[/]");
        }

        public void Append(LogCategory category, Exception exception)
        {
            AnsiConsole.MarkupLine($"[red]{DateTime.Now.ToLongTimeString()} - {category.ToString().ToUpper()} : {exception.Message}[/]");
        }

        #endregion Public Methods
    }
}