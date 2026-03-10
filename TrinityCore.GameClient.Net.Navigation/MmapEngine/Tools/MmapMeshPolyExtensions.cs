using System.Collections.Generic;
using System.Numerics;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools
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

