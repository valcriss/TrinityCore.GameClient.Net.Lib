using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Player;
using TrinityCore.GameClient.Net.Lib.Components.Social;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;
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
            };
            GameClient.Get<SocialComponent>().OnChatMessage += SocialOnChatMessage;
        }

        #endregion Public Constructors

        #region Private Methods

        private void SocialOnChatMessage(Components.Social.Commands.Incoming.MessageChatInfo chatMessage)
        {
            foreach(Command command in Commands)
            {
                if (command.Handle(chatMessage.Message)) break;
            }
        }

        #endregion Private Methods
    }
}