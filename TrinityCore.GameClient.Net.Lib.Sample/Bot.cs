using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;

namespace TrinityCore.GameClient.Net.Lib.Sample
{
    public class Bot
    {
        private GameClient GameClient { get; set; }
        private Thread RunningThread { get; set; }
        private bool Running { get; set; }
        public Bot(GameClient gameClient)
        {
            GameClient = gameClient;
            GameClient.Social.OnChatMessage += SocialOnChatMessage;
            GameClient.Social.OnTextEmote += SocialOnTextEmote;
            Running = true;
        }

        private void SocialOnTextEmote(Components.Social.Commands.Incoming.TextEmoteInfo textEmote)
        {
            if(textEmote.TextEmote == Components.Social.Commands.Enums.TextEmotes.TEXT_EMOTE_WAVE)
            {
                GameClient.Social.Emote(Components.Social.Commands.Enums.TextEmotes.TEXT_EMOTE_DANCE);
            }
        }

        private void SocialOnChatMessage(Components.Social.Commands.Incoming.MessageChatInfo chatMessage)
        {
            if (chatMessage.Message.ToLower() == "stand")
            {
                GameClient.Player.Stand(true);
            }
            else if (chatMessage.Message.ToLower() == "sit")
            {
                GameClient.Player.Stand(false);
            }
            else if (chatMessage.Message.ToLower() == "face")
            {
                Player other = GameClient.Entities.FindPlayerByName("Daniel");
                if (other != null)
                {
                    GameClient.Player.Face(other);
                }
            }
            else if (chatMessage.Message.ToLower() == "speak")
            {
                GameClient.Social.Say(Components.Social.Commands.Enums.Language.LANG_COMMON, "Bonjour (Say)");
                GameClient.Social.Yell(Components.Social.Commands.Enums.Language.LANG_COMMON, "Bonjour (Yell)");
                GameClient.Social.Whisper(Components.Social.Commands.Enums.Language.LANG_COMMON, "Daniel", "Bonjour (Whisper)");
            }
            else if(chatMessage.Message.ToLower() == "wave")
            {
                Player other = GameClient.Entities.FindPlayerByName("Daniel");
                GameClient.Social.Emote(Components.Social.Commands.Enums.TextEmotes.TEXT_EMOTE_WAVE, other);
            }
        }

        public void Start()
        {
            RunningThread = new Thread(Run);
            RunningThread.Start();
        }

        public void Stop()
        {
            Running = false;
        }

        private void Run()
        {
            while (Running)
            {
                Thread.Sleep(100);
            }
        }
    }
}
