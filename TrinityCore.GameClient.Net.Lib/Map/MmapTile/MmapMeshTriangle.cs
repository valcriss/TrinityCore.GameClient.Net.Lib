using System.Numerics;

namespace TrinityCore.GameClient.Net.Lib.Map.MmapTile
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
            return new Vector3(
                (Points[0].X + Points[1].X + Points[2].X) / 3,
                (Points[0].Y + Points[1].Y + Points[2].Y) / 3,
                (Points[0].Z + Points[1].Z + Points[2].Z) / 3
            );
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

        private float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.X - p3.X) * (p2.Z - p3.Z) - (p2.X - p3.X) * (p1.Z - p3.Z);
        }

        #endregion Private Methods
    }
}