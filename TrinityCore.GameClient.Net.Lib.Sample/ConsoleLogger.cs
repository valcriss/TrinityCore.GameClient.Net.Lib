using Spectre.Console;
using System;
using System.Reflection;
using TrinityCore.GameClient.Net.Lib.Log;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    class ConsoleLogger : ILogger
    {
        public void Log(string message, LogLevel level = LogLevel.INFO)
        {
            string color = null;
            if (message.EndsWith(" YES"))
            {
                message = message.Substring(0, message.Length - 3) + "[chartreuse2]YES[/]";
            }
            else if (message.EndsWith(" NO"))
            {
                message = message.Substring(0, message.Length - 2) + "[indianred1]NO[/]";
            }
            switch (level)
            {
                case LogLevel.VERBOSE:
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
                case LogLevel.DETAIL:
                    color = "white";
                    break;
            }
            AnsiConsole.MarkupLine("[bold " + color + "]" + level.ToString().EnsureLength(7) + " : " + message + "[/]");
            WriteToFile(level.ToString().EnsureLength(7) + " : " + message);
        }

        public void LogException(Exception e)
        {
            AnsiConsole.WriteException(e);
            WriteToFile(e.ToString());
        }

        private void WriteToFile(string content)
        {
            string filename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "app-log.txt");
            System.IO.File.AppendAllText(filename, content + Environment.NewLine);
        }
    }
}
