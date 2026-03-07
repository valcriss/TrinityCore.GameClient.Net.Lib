using TrinityCore.GameClient.Net.Lib.Components.Social.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Tools
{
    internal static class PlayerExtensions
    {
        #region Internal Methods

        internal static Language GetLanguage(this Race race)
        {
            switch (race)
            {
                case Race.Dwarf:
                case Race.Draenei:
                case Race.Gnome:
                case Race.Nightelf:
                case Race.Human:
                    return Language.LANG_COMMON;

                default:
                    return Language.LANG_ORCISH;
            }
        }

        #endregion Internal Methods
    }
}