namespace TrinityCore.GameClient.Net.Lib.World.Navigation
{
    internal static class PositionExtensions
    {
        internal static float GetPathLength(this Position[] positions)
        {
            float total = 0;
            for (int i = 0; i < positions.Length - 1; i++)
            {
                Position p1 = positions[i];
                Position p2 = positions[i + 1];
                total += (p2 - p1).Length;
            }

            return total;
        }
    }
}
