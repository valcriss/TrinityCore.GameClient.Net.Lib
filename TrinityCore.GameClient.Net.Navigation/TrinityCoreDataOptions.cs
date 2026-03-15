namespace TrinityCore.GameClient.Net.Navigation;

public sealed class TrinityCoreDataOptions
{
    public string? DataDirectory { get; set; }

    public bool UseVmapLineOfSight { get; set; } = true;

    public bool UseVmapHeightProjection { get; set; }

    public bool UseVmapDetours { get; set; }
}
