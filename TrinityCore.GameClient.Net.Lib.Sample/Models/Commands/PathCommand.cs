using System;
using System.Linq;
using System.Threading.Tasks;
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
        #region Private Fields

        private readonly string[] commands;

        #endregion Private Fields

        #region Public Constructors

        public PathCommand()
        {
            commands = new string[]
            {
                "distance",
                "try",
                "run"
            };
        }

        #endregion Public Constructors

        #region Public Methods

        public override bool Handle(string command)
        {
            if (!commands.Contains(command)) return false;

            Position current = PlayerComponent.Position;
            Player other = GameClient.Get<EntitiesComponent>().FindPlayerByName("Daniel");
            Position otherPosition = other?.GetPosition();
            switch (command.ToLower())
            {
                case "distance":
                    if (otherPosition == null) return false;
                    GameClient.Get<SocialComponent>().Whisper("Daniel", "Distance (" + (otherPosition - current).Length + ")");
                    return true;

                case "try":
                    Position target = otherPosition;
                    Path p = PathFinding.FindPath(GameClient.Get<ZoneComponent>().WorldState.MapId, current.ToVector3(), target.ToVector3(), 7f);
                    if (p == null)
                    {
                        GameClient.Get<SocialComponent>().Whisper("Daniel", "No path found");
                    }
                    else
                    {
                        GameClient.Get<SocialComponent>().Whisper("Daniel", "Found a path with " + p.Points.Length + " points");
                    }
                    return true;

                case "run":
                    GameClient.Get<SocialComponent>().Whisper("Daniel", "Running path");
                    Task.Run(() =>
                    {
                        TravelState state = GameClient.Get<PlayerComponent>().Movement.MoveTo(otherPosition);
                        if (state == TravelState.ERROR)
                        {
                            return;
                        }
                        while (state != TravelState.DESTINATION_REACH)
                        {
                            System.Threading.Thread.Sleep(100);
                            state = GameClient.Get<PlayerComponent>().Movement.MoveTo(otherPosition);
                            if (state == TravelState.ERROR)
                            {
                                break;
                            }
                        }
                    });

                    return true;

                default:
                    return false;
            }
        }

        #endregion Public Methods
    }
}