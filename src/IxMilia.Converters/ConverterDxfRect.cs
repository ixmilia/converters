using System;
using IxMilia.Dxf;

namespace IxMilia.Converters
{
    public struct ConverterDxfRect
    {
        public double Left { get; }
        public double Right { get; }
        public double Bottom { get; }
        public double Top { get; }
        public double Width => this.Right - this.Left;
        public double Height => this.Top - this.Bottom;

        public ConverterDxfRect(double left, double right, double bottom, double top)
        {
            if (left >= right)
            {
                throw new ArgumentOutOfRangeException(nameof(left), left, "left >= right");
            }
            if (bottom >= top)
            {
                throw new ArgumentOutOfRangeException(nameof(bottom), bottom, "bottom >= top");
            }
            this.Left = left;
            this.Right = right;
            this.Bottom = bottom;
            this.Top = top;
        }

        public ConverterDxfRect(DxfBoundingBox bbox)
            : this(bbox.MinimumPoint.X, bbox.MaximumPoint.X, bbox.MinimumPoint.Y, bbox.MaximumPoint.Y)
        {
        }

        public override string ToString()
        {
            return $"l={this.Left} r={this.Right} b={this.Bottom} t={this.Top}";
        }
    }
}
