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
            Running = true;
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
                Thread.Sleep(1000);
                Player other = GameClient.Entities.FindPlayerByName("Daniel");
                if (other != null)
                {
                    if (GameClient.Player.Face(other)) continue;



                }
            }
        }
    }
}
