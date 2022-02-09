using TrinityCore.GameClient.Net.Lib.Network.World;

namespace TrinityCore.GameClient.Net.Lib.Components
{
    public abstract class Component
    {
        #region Protected Properties

        protected WorldClient WorldClient { get; set; }

        #endregion Protected Properties

        #region Protected Constructors

        protected Component(WorldClient worldClient)
        {
            WorldClient = worldClient;
        }

        #endregion Protected Constructors
    }
}