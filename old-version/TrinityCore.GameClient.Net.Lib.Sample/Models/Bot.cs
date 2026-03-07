using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TrinityCore.GameClient.Net.Lib.Components.Social;
using TrinityCore.GameClient.Net.Lib.Sample.Models.Commands;

namespace TrinityCore.GameClient.Net.Lib.Sample.Models
{
    public class Bot
    {
        #region Private Properties

        private List<Command> Commands { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public Bot()
        {
            Commands = new List<Command>()
            {
                new FaceCommand(),
                new PathCommand(),
                new SitStandCommand(),
                new SpeakCommand(),
                new WaveCommand(),
                new CheckCommand()
            };
            GameClient.Get<SocialComponent>().OnChatMessage += SocialOnChatMessage;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Close()
        {
            GameClient.Get<SocialComponent>().OnChatMessage -= SocialOnChatMessage;
        }

        #endregion Public Methods

        #region Private Methods

        private void SocialOnChatMessage(Components.Social.Commands.Incoming.MessageChatInfo chatMessage)
        {
            bool executed = Commands.Any(c => c.Handle(chatMessage.Message));
            if (executed)
            {
                Trace.WriteLine("Command executed");
            }
        }

        #endregion Private Methods
    }
}