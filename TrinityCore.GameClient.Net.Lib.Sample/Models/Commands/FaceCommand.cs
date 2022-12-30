using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Player;

namespace TrinityCore.GameClient.Net.Lib.Sample.Models.Commands
{
    internal class FaceCommand : Command
    {
        #region Public Methods

        public override bool Handle(string command)
        {
            if (command.ToLower() == "face")
            {
                Player other = GameClient.Get<EntitiesComponent>().FindPlayerByName("Daniel");
                if (other != null)
                {
                    GameClient.Get<PlayerComponent>().Movement.Face(other);
                    return true;
                }
            }
            return false;
        }

        #endregion Public Methods
    }
}