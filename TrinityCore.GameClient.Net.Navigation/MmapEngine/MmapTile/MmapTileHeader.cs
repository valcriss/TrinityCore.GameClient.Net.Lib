using TrinityCore.GameClient.Net.Navigation.MmapEngine.Exceptions;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile
{
    public class MmapTileHeader
    {
        #region Public Properties

        public uint MMapVersion { get; set; }
        public uint NavMeshVersion { get; set; }
        public byte[] Padding { get; set; }
        public uint Size { get; set; }
        public uint TileMagic { get; set; }
        public byte UseLiquids { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static MmapTileHeader FromMapBinaryReader(MapBinaryReader reader)
        {
            if (reader.Length < Constants.MMAP_TILE_FILE_HEADER_SIZE)
            {
                throw new FileBadLengthException(reader.Length, Constants.MMAP_TILE_FILE_HEADER_SIZE);
            }

            MmapTileHeader header = new MmapTileHeader();

            header.TileMagic = reader.ReadUint();
            header.NavMeshVersion = reader.ReadUint();
            header.MMapVersion = reader.ReadUint();
            header.Size = reader.ReadUint();
            header.UseLiquids = reader.ReadByte();
            header.Padding = reader.ReadBytes(3);

            return header;
        }

        #endregion Public Methods
    }
}

