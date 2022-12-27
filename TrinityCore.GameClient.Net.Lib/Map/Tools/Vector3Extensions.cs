using System.Numerics;

namespace TrinityCore.GameClient.Net.Lib.Map.Tools
{
    public static class Vector3Extensions
    {
        #region Public Methods

        public static Vector3 ToFileFormat(this Vector3 position)
        {
            return new Vector3(position.Y, position.Z, position.X);
        }

        #endregion Public Methods
    }
}