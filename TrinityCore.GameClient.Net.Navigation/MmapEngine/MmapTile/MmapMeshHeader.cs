using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile
{
    public class MmapMeshHeader
    {
        #region Public Properties

        public float[] Bmax { get; set; }
        public float[] Bmin { get; set; }
        public int DetailMeshCount { get; set; }
        public int DetailTriCount { get; set; }
        public int DetailVertCount { get; set; }
        public int Layer { get; set; }
        public int MaxLinkCount { get; set; }
        public int NodeCount { get; set; }
        public int OffMeshBase { get; set; }
        public int OffMeshConCount { get; set; }
        public int PolyCount { get; set; }
        public float QuantFactor { get; set; }
        public int TileMagic { get; set; }
        public int TileVersion { get; set; }
        public uint UserId { get; set; }
        public int VertCount { get; set; }
        public float WalkableClimb { get; set; }
        public float WalkableHeight { get; set; }
        public float WalkableRadius { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static MmapMeshHeader LoadFromMapBinaryReader(MapBinaryReader reader)
        {
            MmapMeshHeader header = new MmapMeshHeader();

            header.TileMagic = reader.ReadInt();
            header.TileVersion = reader.ReadInt();
            header.X = reader.ReadInt();
            header.Y = reader.ReadInt();
            header.Layer = reader.ReadInt();
            header.UserId = reader.ReadUint();
            header.PolyCount = reader.ReadInt();
            header.VertCount = reader.ReadInt();
            header.MaxLinkCount = reader.ReadInt();
            header.DetailMeshCount = reader.ReadInt();
            header.DetailVertCount = reader.ReadInt();
            header.DetailTriCount = reader.ReadInt();
            header.NodeCount = reader.ReadInt();
            header.OffMeshConCount = reader.ReadInt();
            header.OffMeshBase = reader.ReadInt();
            header.WalkableHeight = reader.ReadFloat();
            header.WalkableRadius = reader.ReadFloat();
            header.WalkableClimb = reader.ReadFloat();

            header.Bmin = reader.ReadFloats(3);
            header.Bmax = reader.ReadFloats(3);

            header.QuantFactor = reader.ReadFloat();

            return header;
        }

        #endregion Public Methods
    }
}

