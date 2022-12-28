using System;
using System.Collections.Generic;
using System.Linq;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Player;
using TrinityCore.GameClient.Net.Lib.Components.Social;
using TrinityCore.GameClient.Net.Lib.Components.Zone;
using TrinityCore.GameClient.Net.Lib.Map;
using TrinityCore.GameClient.Net.Lib.Map.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Sample.Models.Commands
{
    internal class PathCommand : Command
    {
        #region Private Properties

        private List<Position> StoredPositions { get; set; }

        private string TestCaseFile { get; set; }

        #endregion Private Properties

        #region Private Fields

        private string[] commands;

        #endregion Private Fields

        #region Public Constructors

        public PathCommand()
        {
            StoredPositions = new List<Position>();
            commands = new string[]
            {
                "clear",
                "list",
                "store",
                "distance",
                "try",
                "run"
            };
            TestCaseFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PathFindingFindPathTestCases.csv");
        }

        #endregion Public Constructors

        #region Public Methods

        public override bool Handle(string command)
        {
            if (!commands.Contains(command)) return false;

            Position current = GameClient.Get<PlayerComponent>().Position;
            Player other = GameClient.Get<EntitiesComponent>().FindPlayerByName("Daniel");
            Position otherPosition = other?.GetPosition();
            switch (command.ToLower())
            {
                case "clear":
                    StoredPositions.Clear();
                    GameClient.Get<SocialComponent>().Yell("Positions cleared");
                    return true;

                case "list":
                    if (StoredPositions.Count > 0)
                    {
                        string list = string.Empty;
                        foreach (Position position in StoredPositions)
                        {
                            string value = $"[X:{position.X}, Y:{position.Y}, Z:{position.Z}] ";
                            if ((list + value).Length > 250)
                            {
                                GameClient.Get<SocialComponent>().Yell(list);
                                list = value;
                            }
                            else
                            {
                                list = list + value;
                            }
                        }
                        if (list.Length > 0)
                        {
                            GameClient.Get<SocialComponent>().Yell(list);
                        }
                    }
                    else
                    {
                        GameClient.Get<SocialComponent>().Yell("No position stored for the moment");
                    }
                    return true;

                case "store":
                    if (otherPosition == null) return false;
                    StoredPositions.Add(otherPosition);
                    GameClient.Get<SocialComponent>().Yell("Position stored (" + StoredPositions.Count + " positions so far)");
                    return true;

                case "distance":
                    if (otherPosition == null) return false;
                    GameClient.Get<SocialComponent>().Yell("Distance (" + (otherPosition - current).Length + ")");
                    return true;

                case "try":
                    if (StoredPositions.Count == 0)
                    {
                        GameClient.Get<SocialComponent>().Yell("No position stored for the moment");
                    }

                    Position target = StoredPositions[0];
                    Path p = GameClient.Get<ZoneComponent>().Atlas.PathFinding.FindPath(GameClient.Get<ZoneComponent>().WorldState.MapId, current.ToVector3(), target.ToVector3(), 7f);
                    if (p == null)
                    {
                        GameClient.Get<SocialComponent>().Yell("No path found");
                        System.IO.File.AppendAllText(TestCaseFile, $"{GameClient.Get<ZoneComponent>().WorldState.MapId};{current.X};{current.Y};{current.Z};{target.X};{target.Y};{target.Z}\n");
                    }
                    else
                    {
                        GameClient.Get<SocialComponent>().Yell("Found a path with " + p.Points.Length + " points");
                    }
                    return true;

                case "run":
                    if (StoredPositions.Count == 0)
                    {
                        GameClient.Get<SocialComponent>().Yell("No position stored for the moment");
                    }
                    return true;

                default:
                    return false;
            }
        }

        #endregion Public Methods
    }
}