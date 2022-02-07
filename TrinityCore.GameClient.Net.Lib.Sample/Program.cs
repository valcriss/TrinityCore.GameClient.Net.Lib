using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading;
using TrinityCore.GameClient.Net.Lib.Network.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    class Program
    {
        private const string HOSTNAME = "danielsilvestre.fr";
        private const int PORT = 3724;
        private const string USERNAME = "test";
        private const string PASSWORD = "test";

        private static AuthClient AuthClient { get; set; }
        private static WorldClient WorldClient { get; set; }
        private static ManualResetEvent Running { get; set; }

        static void Main(string[] args)
        {
            Running = new ManualResetEvent(false);
            Console.CancelKeyPress += ConsoleCancelKeyPress;
            AnsiConsole.MarkupLine("[underline white]TrinityCore GameClient .Net Lib Sample[/]");

            LoginCharacter();

            Running.WaitOne();

            AnsiConsole.MarkupLine("[white]Sending logout[/]");
            bool logout = WorldClient.LogOut().Result;
        }

        private static void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Running.Set();
        }

        private static bool LoginCharacter()
        {
            AuthClient = new AuthClient(HOSTNAME, PORT, USERNAME, PASSWORD);
            bool authAuthenticate = AuthClient.Authenticate().Result;

            if (!authAuthenticate)
            {
                AnsiConsole.MarkupLine("AuthClient authentification  [red]FAILED[/]");
                return false;
            }
            else
            {
                AnsiConsole.MarkupLine("AuthClient authentification  [green]OK[/]");
            }

            List<WorldServerInfo> realms = AuthClient.GetRealms().Result;
            if (realms == null || realms.Count < 1)
            {
                AnsiConsole.MarkupLine("Retreive realms list         [red]FAILED[/]");
                return false;
            }
            else
            {
                AnsiConsole.MarkupLine("Retreive realms list         [green]OK[/]");
            }

            WorldClient = new WorldClient(realms[0], AuthClient.Credentials);
            bool worldAuthenticate = WorldClient.Authenticate().Result;
            if (!worldAuthenticate)
            {
                AnsiConsole.MarkupLine("WorldClient authentification [red]FAILED[/]");
                return false;
            }
            else
            {
                AnsiConsole.MarkupLine("WorldClient authentification [green]OK[/]");
            }

            List<Character> characters = WorldClient.GetCharacters().Result;
            if (characters == null || characters.Count < 1)
            {
                AnsiConsole.MarkupLine("Retreive characters list     [red]FAILED[/]");
                return false;
            }
            else
            {
                AnsiConsole.MarkupLine("Retreive characters list     [green]OK[/]");
            }

            bool characterLogin = WorldClient.LoginCharacter(characters[0]).Result;
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
    }
}
