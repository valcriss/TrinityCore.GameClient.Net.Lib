using System;
using System.Numerics;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    public class Position
    {
        #region Public Properties

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public float O { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        #endregion Public Properties

        #region Internal Properties

        internal Position Direction
        {
            get
            {
                float length = Length;
                Position point = new Position { X = X / length, Y = Y / length, Z = Z / length };
                return point;
            }
        }

        internal Vector3 Vector3 => new Vector3(X, Y, Z);

        #endregion Internal Properties

        #region Public Constructors

        public Position()
        {
        }

        public Position(float x, float y, float z, float o)
        {
            X = x;
            Y = y;
            Z = z;
            O = o;
        }

        #endregion Public Constructors

        #region Internal Constructors

        internal Position(Vector3 position, float orientation)
        {
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = orientation;
        }

        #endregion Internal Constructors

        #region Public Methods

        public static Position operator -(Position a, Position b)
        {
            var result = new Position(a.X - b.X, a.Y - b.Y, a.Z - b.Z, 0.0f);
            result.O = result.CalculateOrientation();

            return result;
        }

        public static Position operator *(Position point, float scale)
        {
            Position point1 = new Position { X = point.X * scale, Y = point.Y * scale, Z = point.Z * scale };
            return point1;
        }

        public static Position operator +(Position a, Position b)
        {
            Position point = new Position { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z };
            return point;
        }

        public override string ToString()
        {
            return "{X:" + X + ", Y:" + Y + ", Z:" + Z + ", O:" + O + "}";
        }

        #endregion Public Methods

        #region Private Methods

        private float CalculateOrientation()
        {
            double orientation;
            if (X == 0)
            {
                if (Y > 0)
                    orientation = Math.PI / 2;
                else
                    orientation = 3 * Math.PI / 2;
            }
            else if (Y == 0)
            {
                if (X > 0)
                    orientation = 0;
                else
                    orientation = Math.PI;
            }
            else
            {
                orientation = Math.Atan2(Y, X);
                if (orientation < 0)
                    orientation += 2 * Math.PI;
            }

            return (float)orientation;
        }

        #endregion Private Methods
    }
}