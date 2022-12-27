using System;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class Point
    {
        #region Public Properties

        public Point Direction
        {
            get
            {
                float length = Length;
                return new Point(X / length, Y / length, Z / length);
            }
        }

        public float DirectionOrientation
        {
            get { var dir = Direction; double orientation = Math.Atan2(dir.Y, dir.X); if (orientation < 0) orientation += 2.0 * Math.PI; return (float)orientation; }
        }

        public float Length
        {
            get { return (float)Math.Sqrt(X * X + Y * Y + Z * Z); }
        }

        #endregion Public Properties

        #region Public Fields

        public float X;
        public float Y;
        public float Z;

        #endregion Public Fields

        #region Public Constructors

        public Point(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion Public Constructors

        #region Public Methods

        public static Point operator -(Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static bool operator !=(Point a, Point b)
        {
            return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
        }

        public static Point operator *(Point point, float scale)
        {
            return new Point(point.X * scale, point.Y * scale, point.Z * scale);
        }

        public static Point operator +(Point a, Point b)
        {
            return new Point(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static bool operator ==(Point a, Point b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public override bool Equals(object obj)
        {
            return (Point)obj == this;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return "X: " + X + " | Y: " + Y + " | Z: " + Z;
        }

        #endregion Public Methods
    }
}