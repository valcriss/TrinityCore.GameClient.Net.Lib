using System;
using System.Numerics;

namespace TrinityCore.GameClient.Net.Lib.World.Navigation
{
    public class Position
    {
        public float O { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

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

        public Position Direction
        {
            get
            {
                float length = Length;
                Position point = new Position { X = X / length, Y = Y / length, Z = Z / length };
                return point;
            }
        }

        public static Position operator +(Position a, Position b)
        {
            Position point = new Position { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z };
            return point;
        }

        public static Position operator *(Position point, float scale)
        {
            Position point1 = new Position { X = point.X * scale, Y = point.Y * scale, Z = point.Z * scale };
            return point1;
        }

        public float GetOrientation(Position position)
        {
            return 0;// Path.GetOrientation(X, Y, Z, position.X, position.Y, position.Z);
        }

        public Position(Vector3 position, float orientation)
        {
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = orientation;
        }

        public static Position operator -(Position a, Position b)
        {

            var result = new Position(a.X - b.X, a.Y - b.Y, a.Z - b.Z, 0.0f);
            result.O = result.CalculateOrientation();

            return result;
        }

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

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

        public override string ToString()
        {
            return "{X:" + X + ", Y:" + Y + ", Z:" + Z + ", O:" + O + "}";
        }
    }
}
