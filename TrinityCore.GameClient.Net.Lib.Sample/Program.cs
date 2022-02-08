using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading;
using TrinityCore.GameClient.Net.Lib.Logging;
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

        #region Private Fields

        private const string HOSTNAME = "danielsilvestre.fr";
        private const string PASSWORD = "test";
        private const int PORT = 3724;
        private const string USERNAME = "test";

        #endregion Private Fields

        #region Private Methods

        private static void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Running.Set();
        }

        private static bool LoginCharacter()
        {
            GameClient = new GameClient(USERNAME, PASSWORD, HOSTNAME, PORT);
            bool authAuthenticate = GameClient.Authenticate().Result;

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

        private static void Main(string[] args)
        {
            Running = new ManualResetEvent(false);
            Logger.RegisterHandler("spectre", new SpectreLoggerHandler());
            Console.CancelKeyPress += ConsoleCancelKeyPress;
            AnsiConsole.MarkupLine("[underline white]TrinityCore GameClient .Net Lib Sample[/]");

            bool login = LoginCharacter();
            if (!login) return;

            Running.WaitOne();

            AnsiConsole.MarkupLine("[white]Sending logout[/]");
            bool logout = GameClient.LogOut().Result;
            if (!logout) AnsiConsole.MarkupLine("[red]Unable to logout[/]");
        }

        #endregion Private Methods
    }
}