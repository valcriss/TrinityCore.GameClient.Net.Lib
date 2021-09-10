using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Clients
{
    public class ExtendableClient : BaseClient
    {
        private List<GameComponent> Components { get; set; }
        public ExtendableClient(string host, int port, string login, string password) : base(host, port, login, password)
        {
            Components = new List<GameComponent>();
        }

        public T AddComponent<T>(T component) where T : GameComponent, new()
        {
            if (WorldClient != null)
            {
                component.SetWorldClient(WorldClient);
                component.RegisterHandlers();
            }

            lock (Components)
            {
                Components.Add(component);
            }

            return component;
        }

        protected override Task<bool> Start()
        {
            foreach(GameComponent component in Components)
            {
                component.SetWorldClient(WorldClient); 
                component.RegisterHandlers();
            }
            return base.Start();
        }
    }
}
