using Microsoft.VisualBasic;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    internal static class Program
    {
        #region Private Properties

        private static GameClient GameClient { get; set; }
        private static ManualResetEvent Running { get; set; }

        #endregion Private Properties

        #region Private Methods

        private static void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Running.Set();
        }

        private static bool LoginCharacter(Configuration configuration)
        {
            GameClient = new GameClient();
            AuthServerInfo authServer = new AuthServerInfo(configuration.Host, configuration.Port);
            AuthServerCredentials credentials = new AuthServerCredentials(configuration.Login, configuration.Password);
            bool authAuthenticate = GameClient.Authenticate(authServer, credentials).Result;

            if (!authAuthenticate)
            {
                AnsiConsole.MarkupLine("AuthClient authentification  [red]FAILED[/]");
                return false;
            }
            else
            {
                AnsiConsole.MarkupLine("AuthClient authentification  [green]OK[/]");
            }

            List<WorldServerInfo> realms = GameClient.GetRealms().Result;
            if (realms == null || realms.Count < 1)
            {
                AnsiConsole.MarkupLine("Retreive realms list         [red]FAILED[/]");
                return false;
            }
            else
            {
                AnsiConsole.MarkupLine("Retreive realms list         [green]OK[/]");
            }

            bool worldAuthenticate = GameClient.ConnectToRealm(realms[0]).Result;
            if (!worldAuthenticate)
            {
                AnsiConsole.MarkupLine("WorldClient authentification [red]FAILED[/]");
                return false;
            }
            else
            {
                AnsiConsole.MarkupLine("WorldClient authentification [green]OK[/]");
            }

            List<Character> characters = GameClient.GetCharacters().Result;
            if (characters == null || characters.Count < 1)
            {
                AnsiConsole.MarkupLine("Retreive characters list     [red]FAILED[/]");
                return false;
            }
            else
            {
                AnsiConsole.MarkupLine("Retreive characters list     [green]OK[/]");
            }

            bool characterLogin = GameClient.EnterRealm(characters[0]).Result;
            if (!characterLogin)
            {
                AnsiConsole.MarkupLine("Character login              [red]FAILED[/]");
                return false;
            }
            else
            {
                AnsiConsole.MarkupLine("Character login              [green]OK[/]");
            }
            return true;
        }

        private static void Main()
        {
            Configuration configuration = Configuration.Load();
            while (!configuration.IsValid)
            {
                configuration.Host = AnsiConsole.Ask<string>("[green]AuthServer Host  :[/]", configuration.Host);
                configuration.Port = AnsiConsole.Ask<int>("[green]AuthServer Port  :[/]", configuration.Port != 0 ? configuration.Port : 3724);
                configuration.Login = AnsiConsole.Ask<string>("[green]Account login    :[/]", configuration.Login);
                configuration.Password = AnsiConsole.Ask<string>("[green]Account password :[/]", configuration.Password);
                configuration.DataPath = AnsiConsole.Ask<string>("[green]Data path        :[/]", configuration.DataPath);
                configuration.LogLevel = Select(new List<string>() { "DEBUG", "VERBOSE", "INFORMATION", "WARNING", "ERROR" }, "LogLevel");
                configuration.Save();
                Console.Clear();
            }

            LogLevel minLevel = LogLevel.INFORMATION;
            if (Enum.TryParse(configuration.LogLevel, out LogLevel level))
            {
                minLevel = level;
            }

            Running = new ManualResetEvent(false);
            Logger.RegisterHandler("spectre", new SpectreLoggerHandler(minLevel));
            Console.CancelKeyPress += ConsoleCancelKeyPress;
            AnsiConsole.MarkupLine("[underline white]TrinityCore GameClient .Net Lib Sample[/]");

            bool login = LoginCharacter(configuration);
            if (!login) return;

            Bot bot = new Bot(GameClient);
            bot.Start();

            Running.WaitOne();

            bot.Stop();
            AnsiConsole.MarkupLine("[white]Sending logout[/]");
            bool logout = GameClient.LogOut().Result;
            if (!logout) AnsiConsole.MarkupLine("[red]Unable to logout[/]");
        }

        private static T Select<T>(List<T> items, string type)
        {
            var selection = new SelectionPrompt<T>();
            selection.Title("Select your " + type);
            selection.PageSize(10);
            selection.MoreChoicesText("[grey](Move up and down to reveal more " + type + "s)[/]");
            selection.AddChoices(items);
            return AnsiConsole.Prompt(selection);
        }

        #endregion Private Methods
    }
}