using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Social;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Sample.Models.Commands
{
    internal class PathCommand : Command
    {
        #region Private Properties

        private List<Position> StoredPositions { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public PathCommand()
        {
            StoredPositions = new List<Position>();
        }

        #endregion Public Constructors

        #region Public Methods

        public override bool Handle(string command)
        {
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
                    Player other = GameClient.Get<EntitiesComponent>().FindPlayerByName("Daniel");
                    if (other != null)
                    {
                        Position position = other.GetPosition();
                        StoredPositions.Add(position);
                        GameClient.Get<SocialComponent>().Yell("Position stored (" + StoredPositions.Count + " positions so far)");
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
            return false;
        }

        #endregion Public Methods
    }
}