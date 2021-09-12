using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Entities;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Entities;
using TrinityCore.GameClient.Net.Lib.World.Entities;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    class Program
    {
        private static Client Client { get; set; }
        static void Main(string[] args)
        {
            Configuration configuration = Configuration.Load();
            while (!configuration.IsValid)
            {
                configuration.Host = AnsiConsole.Ask<string>("[green]AuthServer Host  :[/]");
                configuration.Port = AnsiConsole.Ask<int>("[green]AuthServer Port  :[/]");
                configuration.Login = AnsiConsole.Ask<string>("[green]Account login    :[/]");
                configuration.Password = AnsiConsole.Ask<string>("[green]Account password :[/]");
                configuration.LogLevel = Select(new List<string>() { "VERBOSE", "DETAIL", "INFO", "WARNING" }, "LogLevel");
                configuration.Save();
            }



            Logger.Level = LogLevel.INFO;
            if (Enum.TryParse(configuration.LogLevel, out LogLevel level))
            {
                Logger.Level = level;
            }

            Logger.LogObject = new ConsoleLogger();
            Client = new Client(configuration.Host, configuration.Port, configuration.Login, configuration.Password);

            if (!Authenticate())
            {
                Console.ReadKey();
                return;
            }

            List<WorldServerInfo> worlds = GetWorlds();
            if (worlds == null)
            {
                Console.ReadKey();
                return;
            }

            WorldServerInfo selectedWorld = Select(worlds, "world");

            if (!Connect(selectedWorld))
            {
                Console.ReadKey();
                return;
            }

            List<Character> characters = GetCharacters();
            if (characters == null)
            {
                Console.ReadKey();
                return;
            }

            Character selectedCharacter = Select(characters, "character");

            if (!EnterWorld(selectedCharacter))
            {
                Console.ReadKey();
                return;
            }

            while (true)
            {
                System.Threading.Thread.Sleep(100);
                Player player = Client.GetPlayer();
                if (player != null)
                {
                    Entity closest = Client.GetOtherPlayers().OrderBy(c => (c.Movement.Position - player.Movement.Position).Length).FirstOrDefault();
                    if (closest != null)
                    {
                        Client.Face(closest.GetPosition());
                    }
                }

            }
        }

        private static bool Authenticate()
        {
            Logger.Log("[bold white]Starting Auth Login[/]", LogLevel.DETAIL);
            Logger.Log("Starting Authentication", LogLevel.DETAIL);
            bool authLogin = Client.Authenticate().Result;
            if (!authLogin)
            {
                Logger.Log("Authentication Failed", LogLevel.ERROR);
                return false;
            }

            Logger.Log("Authentication Success", LogLevel.SUCCESS);
            return true;
        }

        private static List<WorldServerInfo> GetWorlds()
        {
            return Client.GetWorlds().Result;
        }

        private static bool Connect(WorldServerInfo world)
        {
            Logger.Log("Starting World Connect : " + world.Name, LogLevel.DETAIL);
            bool worldLogin = Client.Connect(world).Result;

            if (!worldLogin)
            {
                Logger.Log("World Connect Failed", LogLevel.ERROR);
                return false;
            }

            Logger.Log("World Connect Success", LogLevel.SUCCESS);
            return true;
        }

        private static List<Character> GetCharacters()
        {
            return Client.GetCharacters().Result;
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

        private static bool EnterWorld(Character character)
        {
            Logger.Log("Starting Character Entering World : " + character.Name, LogLevel.DETAIL);
            bool characterLogin = Client.EnterWorld(character).Result;
            if (!characterLogin)
            {
                Logger.Log("Character Entering Failed", LogLevel.ERROR);
                return false;
            }
            Logger.Log("Character Entering Success", LogLevel.SUCCESS);
            return true;
        }

    }
}
