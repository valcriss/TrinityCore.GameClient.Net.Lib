using System;

namespace TrinityCore.GameClient.Net.Lib.Network.Tools
{
    internal static class UintExtensions
    {
        internal static DateTime ToDateTime(this uint value)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dateTime.AddSeconds(value);
        }
    }
}
