using TrinityCore.GameClient.Net.Lib.Components.Social;

namespace TrinityCore.GameClient.Net.Lib.Sample.Models.Commands
{
    internal class SpeakCommand : Command
    {
        #region Public Methods

        public override bool Handle(string command)
        {
            if (command.ToLower() == "speak")
            {
                GameClient.Get<SocialComponent>().Say("Bonjour (Say)");
                GameClient.Get<SocialComponent>().Yell("Bonjour (Yell)");
                GameClient.Get<SocialComponent>().Whisper("Daniel", "Bonjour (Whisper)");
                return true;
            }
            return false;
        }

        #endregion Public Methods
    }
}