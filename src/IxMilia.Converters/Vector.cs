using IxMilia.Dxf;
using IxMilia.Pdf;

namespace IxMilia.Converters
{
    public struct Vector
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Vector(double x, double y, double z)
            : this()
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static implicit operator Vector(DxfPoint point)
        {
            return new Vector(point.X, point.Y, point.Z);
        }

        public PdfPoint ToPdfPoint(PdfMeasurementType measurementType)
        {
            return new PdfPoint(new PdfMeasurement(X, measurementType), new PdfMeasurement(Y, measurementType));
        }

        public static bool operator ==(Vector p1, Vector p2)
        {
            if (object.ReferenceEquals(p1, p2))
                return true;
            if (((object)p1 == null) || ((object)p2 == null))
                return false;
            return p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
        }

        public static bool operator !=(Vector p1, Vector p2)
        {
            return !(p1 == p2);
        }

        public static Vector operator +(Vector p1, Vector p2)
        {
            return new Vector(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static Vector operator -(Vector p1, Vector p2)
        {
            return new Vector(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static Vector operator *(Vector p, double scalar)
        {
            return new Vector(p.X * scalar, p.Y * scalar, p.Z * scalar);
        }

        public static Vector operator /(Vector p, double scalar)
        {
            return new Vector(p.X / scalar, p.Y / scalar, p.Z / scalar);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is Vector)
            {
                return this == (Vector)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", X, Y, Z);
        }

        public static Vector Origin
        {
            get { return new Vector(0, 0, 0); }
        }
    }
}
