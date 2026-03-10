using System.Numerics;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile
{
    public class MmapMeshOffMeshConnection
    {
        #region Public Properties

        public Vector3 Connection1 { get; set; }
        public Vector3 Connection2 { get; set; }
        public byte Flags { get; set; }
        public ushort Poly { get; set; }
        public float Radius { get; set; }
        public byte Side { get; set; }
        public uint UserId { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static MmapMeshOffMeshConnection LoadMapBinaryReader(MapBinaryReader reader)
        {
            MmapMeshOffMeshConnection offMeshConnection = new MmapMeshOffMeshConnection();
            offMeshConnection.Connection1 = reader.ReadVector3();
            offMeshConnection.Connection2 = reader.ReadVector3();

            offMeshConnection.Radius = reader.ReadFloat();
            offMeshConnection.Poly = reader.ReadUShort();
            offMeshConnection.Flags = reader.ReadByte();

            offMeshConnection.Side = reader.ReadByte();
            offMeshConnection.UserId = reader.ReadUint();

            return offMeshConnection;
        }

        #endregion Public Methods
    }
}

