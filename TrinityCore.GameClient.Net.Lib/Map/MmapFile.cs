using System;
using System.Collections.Generic;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Map.Exceptions;
using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class MmapFile
    {
        #region Public Properties

        public string File { get; set; }
        public int MapId { get; set; }
        public uint MaxPoly { get; set; }
        public uint MaxTiles { get; set; }
        public Vector3 Origin { get; set; }
        public float TileHeight { get; set; }
        public float TileWidth { get; set; }

        #endregion Public Properties

        #region Private Constructors

        private MmapFile(string file, int mapId, Vector3 origin, float tileWidth, float tileHeight, uint maxTiles, uint maxPoly)
        {
            File = file;
            MapId = mapId;
            Origin = origin;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            MaxTiles = maxTiles;
            MaxPoly = maxPoly;
        }

        #endregion Private Constructors

        #region Public Methods

        /// <summary>
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        /// <exception cref="FileBadExtensionException"></exception>
        public static MmapFile Load(string file)
        {
            CheckFile(file);

            int mapId = GetMapIdFromFilename(file);

            BinaryReader reader = BinaryReader.FromFile(file);

            if (reader.Length != Constants.MMAP_FILE_EXPECTED_LENGTH)
            {
                throw new FileBadLengthException(reader.Length, Constants.MMAP_FILE_EXPECTED_LENGTH);
            }

            Vector3 origin = reader.ReadVector3();
            float tileWidth = reader.ReadFloat();
            float tileHeight = reader.ReadFloat();
            uint maxTiles = reader.ReadUint();
            uint maxPoly = reader.ReadUint();

            return new MmapFile(file, mapId, origin, tileWidth, tileHeight, maxTiles, maxPoly);
        }

        public MmapTileFile GetMmapTileFileFromKey(string key)
        {
            MmapTileFile fromCache = MmapTileFileCache.Instance.Get(key);
            if (fromCache != null) return fromCache;
            string filename = key + "." + Constants.MMAP_TILE_FILE_EXTENSION;
            string file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(File), filename);
            if (System.IO.File.Exists(file))
            {
                MmapTileFile tile = MmapTileFile.Load(file);
                MmapTileFileCache.Instance.Set(key, tile);
                return tile;
            }
            return null;
        }

        public MmapTileFile GetMmapTileFileFromVector3(float x, float y, float z)
        {
            return GetMmapTileFileFromVector3(new Vector3(x, y, z));
        }

        public MmapTileFile GetMmapTileFileFromVector3(Vector3 position)
        {
            string key = GetMmapTileKeyFromVector3(position);
            return GetMmapTileFileFromKey(key);
        }

        public string GetMmapTileKeyFromVector3(float x, float y, float z)
        {
            return GetMmapTileKeyFromVector3(new Vector3(x, y, z));
        }

        public string GetMmapTileKeyFromVector3(Vector3 position)
        {
            int tileX = (int)(32 - position.X / TileWidth);
            int tileY = (int)(32 - position.Y / TileHeight);
            return MapId.ToString("000") + tileX.ToString("00") + tileY.ToString("00");
        }

        public List<MmapTileFile> GetMmapTiles()
        {
            List<MmapTileFile> list = new List<MmapTileFile>();

            foreach (string file in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(File), MapId.ToString("000") + "*." + Constants.MMAP_TILE_FILE_EXTENSION))
            {
                list.Add(MmapTileFile.Load(file));
            }

            return list;
        }

        #endregion Public Methods

        #region Private Methods

        private static void CheckFile(string file)
        {
            if (!file.Exists())
            {
                throw new System.IO.FileNotFoundException();
            }
            else if (!file.CheckExtension(Constants.MMAP_FILE_EXTENSION))
            {
                throw new FileBadExtensionException(Constants.MMAP_FILE_EXTENSION);
            }
            else if (!file.CheckRegExp(Constants.MMAP_FILE_CHECK_REGEXP))
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_FILE_EXTENSION, Constants.MMAP_FILE_CHECK_REGEXP);
            }
        }

        private static int GetMapIdFromFilename(string file)
        {
            try
            {
                return int.Parse(System.IO.Path.GetFileNameWithoutExtension(file));
            }
            catch (Exception)
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_FILE_EXTENSION, Constants.MMAP_FILE_CHECK_REGEXP);
            }
        }

        #endregion Private Methods
    }
}