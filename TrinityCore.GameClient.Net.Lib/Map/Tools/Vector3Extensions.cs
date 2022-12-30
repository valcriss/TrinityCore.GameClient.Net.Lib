using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Map.Tools
{
    public static class Vector3Extensions
    {
        #region Public Methods

        public static Vector3 ToFileFormat(this Vector3 position)
        {
            return new Vector3(position.Y, position.Z, position.X);
        }

        public static Point ToPoint(this Vector3 position)
        {
            return new Point(position.X, position.Y, position.Z);
        }

        public static Vector3 ToVector3(this Point position)
        {
            return new Vector3(position.X, position.Y, position.Z);
        }

        public static Vector3 ToVector3(this Position position)
        {
            return new Vector3(position.X, position.Y, position.Z);
        }

        public static Vector3 ToWorldFormat(this Vector3 position)
        {
            return new Vector3(position.Z, position.X, position.Y);
        }

        #endregion Public Methods
    }
}