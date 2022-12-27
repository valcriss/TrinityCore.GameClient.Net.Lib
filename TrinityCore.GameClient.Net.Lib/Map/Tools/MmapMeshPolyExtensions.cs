using System.Collections.Generic;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Map.MmapTile;

namespace TrinityCore.GameClient.Net.Lib.Map.Tools
{
    public static class MmapMeshPolyExtensions
    {
        #region Public Methods

        public static List<Point> ToPoints(this List<MmapMeshPoly> polies)
        {
            List<Point> points = new List<Point>();
            foreach (MmapMeshPoly poly in polies)
            {
                Vector3 center = poly.Center();
                points.Add(new Point(center.Z, center.X, center.Y));
            }
            return points;
        }

        #endregion Public Methods
    }
}