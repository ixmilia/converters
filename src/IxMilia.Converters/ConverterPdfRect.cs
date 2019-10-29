using System;
using IxMilia.Pdf;

namespace IxMilia.Converters
{
    public class ConverterPdfRect
    {
        public PdfMeasurement Left { get; }
        public PdfMeasurement Right { get; }
        public PdfMeasurement Bottom { get; }
        public PdfMeasurement Top { get; }
        public PdfMeasurement Width => new PdfMeasurement(this.Right.AsPoints() - this.Left.AsPoints(), PdfMeasurementType.Point);
        public PdfMeasurement Height => new PdfMeasurement(this.Top.AsPoints() - this.Bottom.AsPoints(), PdfMeasurementType.Point);

        public ConverterPdfRect(PdfMeasurement left, PdfMeasurement right, PdfMeasurement bottom, PdfMeasurement top)
        {
            if (left.AsPoints() >= right.AsPoints())
            {
                throw new ArgumentOutOfRangeException(nameof(left), left, "left >= right");
            }
            if (bottom.AsPoints() >= top.AsPoints())
            {
                throw new ArgumentOutOfRangeException(nameof(bottom), bottom, "bottom >= top (PDF 'bottom' is zero)");
            }
            this.Left = left;
            this.Right = right;
            this.Bottom = bottom;
            this.Top = top;
        }

        public override string ToString()
        {
            return $"l={this.Left.AsPoints()}pt r={this.Right.AsPoints()}pt b={this.Bottom.AsPoints()}pt t={this.Top.AsPoints()}pt";
        }
    }
}
