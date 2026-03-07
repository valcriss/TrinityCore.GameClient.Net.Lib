using TrinityCore.GameClient.Net.Lib.Components.Player;

namespace TrinityCore.GameClient.Net.Lib.Sample.Models.Commands
{
    internal class SitStandCommand : Command
    {
        #region Public Methods

        public override bool Handle(string command)
        {
            switch (command.ToLower())
            {
                case "sit":
                    GameClient.Get<PlayerComponent>().Movement.Stand(false);
                    return true;

                case "stand":
                    GameClient.Get<PlayerComponent>().Movement.Stand(true);
                    return true;

                default:
                    return false;
            }
        }

        #endregion Public Methods
    }
}