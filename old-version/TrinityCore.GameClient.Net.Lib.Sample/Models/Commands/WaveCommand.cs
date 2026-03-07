using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Social;
using TrinityCore.GameClient.Net.Lib.Components.Social.Enums;

namespace TrinityCore.GameClient.Net.Lib.Sample.Models.Commands
{
    internal class WaveCommand : Command
    {
        #region Public Methods

        public override bool Handle(string command)
        {
            if (command.ToLower() == "wave")
            {
                Player other = GameClient.Get<EntitiesComponent>().FindPlayerByName("Daniel");
                GameClient.Get<SocialComponent>().Emote(TextEmotes.TEXT_EMOTE_WAVE, other);
            }
            return false;
        }

        #endregion Public Methods
    }
}