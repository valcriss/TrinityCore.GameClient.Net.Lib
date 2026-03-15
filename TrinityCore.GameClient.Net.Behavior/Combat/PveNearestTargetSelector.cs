using TrinityCore.GameClient.Net.Behavior.Abstractions;
using TrinityCore.GameClient.Net.GameState.Model;

namespace TrinityCore.GameClient.Net.Behavior.Combat;

public sealed class PveNearestTargetSelector : ITargetSelector
{
    public NearbyUnitSnapshot? Select(WorldSnapshot snapshot, TargetSelectionOptions options)
    {
        var player = snapshot.Player;
        if (player is null)
        {
            return null;
        }

        var maxLevel = player.Level + options.MaxTargetLevelDelta;
        var maxDistanceSq = options.AggroRadius * options.AggroRadius;

        return snapshot.NearbyUnits
            .Where(x => x.IsCreature)
            .Where(x => x.Guid != 0)
            .Where(x => !x.Level.HasValue || x.Level.Value <= maxLevel)
            .Where(x => x.X.HasValue && x.Y.HasValue && x.Z.HasValue)
            .Select(x => new
            {
                Unit = x,
                DistanceSq = DistanceSquared(player.X, player.Y, player.Z, x.X!.Value, x.Y!.Value, x.Z!.Value)
            })
            .Where(x => x.DistanceSq <= maxDistanceSq)
            .OrderBy(x => x.DistanceSq)
            .ThenBy(x => x.Unit.Guid)
            .Select(x => x.Unit)
            .FirstOrDefault();
    }

    private static float DistanceSquared(float ax, float ay, float az, float bx, float by, float bz)
    {
        var dx = ax - bx;
        var dy = ay - by;
        var dz = az - bz;
        return (dx * dx) + (dy * dy) + (dz * dz);
    }
}
