using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Display.Models
{
    public abstract class Order
    {
        public bool Complete { get; set; }
        protected Client Client { get; set; }
        public Order(Client client)
        {
            Client = client;
        }

        public abstract bool Process();
        
    }
}
