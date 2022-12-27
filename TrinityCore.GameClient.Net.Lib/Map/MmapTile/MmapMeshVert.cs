using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map.MmapTile
{
    public class MmapMeshVert
    {
        #region Public Properties

        public Vector3 Vector3 { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static MmapMeshVert LoadBinaryReader(BinaryReader reader)
        {
            MmapMeshVert vert = new MmapMeshVert();
            vert.X = reader.ReadFloat();
            vert.Y = reader.ReadFloat();
            vert.Z = reader.ReadFloat();
            vert.Vector3 = new Vector3(vert.X, vert.Y, vert.Z);
            return vert;
        }

        #endregion Public Methods
    }
}