using System;
using System.Diagnostics;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine;

internal static class NavTrace
{
    private static readonly bool Enabled = string.Equals(
        Environment.GetEnvironmentVariable("TRINITY_NAV_TRACE"),
        "1",
        StringComparison.Ordinal);

    public static void WriteLine(string message)
    {
        if (Enabled)
        {
            Trace.WriteLine(message);
        }
    }
}
