using TrinityCore.GameClient.Net.Lib.Network.World;

namespace TrinityCore.GameClient.Net.Lib.Components
{
    public abstract class Component
    {
        #region Public Properties

        public WorldClient WorldClient { get; set; }

        #endregion Public Properties

        #region Protected Constructors

        protected Component(WorldClient worldClient)
        {
            WorldClient = worldClient;
        }

        #endregion Protected Constructors

        #region Public Methods

        public virtual void Close()
        {
        }

        #endregion Public Methods
    }
}