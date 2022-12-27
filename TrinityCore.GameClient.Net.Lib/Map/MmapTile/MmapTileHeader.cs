using TrinityCore.GameClient.Net.Lib.Map.Exceptions;
using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map.MmapTile
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

        public static MmapTileHeader FromBinaryReader(BinaryReader reader)
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

        #region Private Methods

        private static void CheckFile(string file)
        {
            if (!file.Exists())
            {
                throw new System.IO.FileNotFoundException();
            }
            else if (!file.CheckExtension(Constants.MMAP_TILE_FILE_EXTENSION))
            {
                throw new FileBadExtensionException(Constants.MMAP_TILE_FILE_EXTENSION);
            }
            else if (!file.CheckRegExp(Constants.MMAP_TILE_FILE_CHECK_REGEXP))
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_TILE_FILE_EXTENSION, Constants.MMAP_TILE_FILE_CHECK_REGEXP);
            }
        }

        #endregion Private Methods
    }
}