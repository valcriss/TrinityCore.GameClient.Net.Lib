using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    public class SpectreLoggerHandler : ILoggerHandler
    {
        private LogLevel MinLevel {get;set;}

        public SpectreLoggerHandler(LogLevel minLevel)
        {
            MinLevel = minLevel;
        }


        public void Append(LogCategory category, LogLevel level, string message)
        {
            if(MinLevel < level) return;

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
    }
}
