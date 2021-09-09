using Spectre.Console;
using System;
using TrinityCore.GameClient.Net.Lib.Log;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    class ConsoleLogger : ILogger
    {
        public void Log(string message, LogLevel level = LogLevel.INFO)
        {
            string color = null;
            switch (level)
            {
                case LogLevel.DETAIL:
                    color = "grey46";
                    break;
                case LogLevel.INFO:
                    color = "white";
                    break;
                case LogLevel.WARNING:
                    color = "orange1";
                    break;
                case LogLevel.ERROR:
                    color = "indianred1";
                    break;
                case LogLevel.SUCCESS:
                    color = "chartreuse2";
                    break;
            }
            AnsiConsole.MarkupLine("[bold " + color + "]" + level + " : " + message + "[/]");
        }

        public void LogException(Exception e)
        {
            AnsiConsole.MarkupLine("[bold indianred1]EXCEPTION : " + e.Message + "[/]");
            AnsiConsole.MarkupLine("[bold indianred1]EXCEPTION : " + e.StackTrace + "[/]");
        }
    }
}
