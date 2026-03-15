using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile
{
    public class MmapMeshTriangle
    {
        #region Public Properties

        public Vector3[] Points { get; set; }
        public ushort[] Vertices { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public MmapMeshTriangle()
        {
            Vertices = new ushort[3];
            Points = new Vector3[3];
        }

        #endregion Public Constructors

        #region Public Methods

        public Vector3 Center()
        {
            return CalculateCentroid(Points.ToList());
        }

        public bool PointInTriangle(Vector3 position)
        {
            bool b1 = Sign(position, Points[0], Points[1]) < 0.0f;
            bool b2 = Sign(position, Points[1], Points[2]) < 0.0f;
            bool b3 = Sign(position, Points[2], Points[0]) < 0.0f;
            return b1 == b2 && b2 == b3;
        }

        #endregion Public Methods

        #region Private Methods

        private static Vector3 CalculateCentroid(List<Vector3> verticies)
        {
            var s = new Vector3();
            var areaTotal = 0.0f;

            var p1 = verticies[0];
            var p2 = verticies[1];

            for (var i = 2; i < verticies.Count; i++)
            {
                var p3 = verticies[i];
                var edge1 = p3 - p1;
                var edge2 = p3 - p2;

                var crossProduct = Vector3.Cross(edge1, edge2);
                var area = crossProduct.Length() / 2;

                s.X += area * (p1.X + p2.X + p3.X) / 3;
                s.Y += area * (p1.Y + p2.Y + p3.Y) / 3;
                s.Z += area * (p1.Z + p2.Z + p3.Z) / 3;

                areaTotal += area;
                p2 = p3;
            }

            var point = new Vector3
            {
                X = s.X / areaTotal,
                Y = s.Y / areaTotal,
                Z = s.Z / areaTotal
            };

            return point;
        }

        private static float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.X - p3.X) * (p2.Z - p3.Z) - (p2.X - p3.X) * (p1.Z - p3.Z);
        }

        #endregion Private Methods
    }
}

