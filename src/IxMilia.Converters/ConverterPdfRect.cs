using System;
using IxMilia.Pdf;

namespace IxMilia.Converters
{
    public class ConverterPdfRect
    {
        /// <summary>
        /// Creates a PDF destination rectangle from the given DXF rectangle and keeps the aspect ratio.
        /// Optionally, a <paramref name="margin"/> can be defined.
        /// </summary>
        /// <param name="srcRect">The DXF source rectangle.</param>
        /// <param name="maxWidth">The maximum width of the PDF output including <paramref name="margin"/>.</param>
        /// <param name="maxHeight">The maximum height of the PDF output including <paramref name="margin"/>.</param>
        /// <param name="margin">[optional] The margin.</param>
        /// <returns>The PDF destination rectangle.</returns>
        public static ConverterPdfRect KeepAspectRatio(ConverterDxfRect srcRect, PdfMeasurement maxWidth,
            PdfMeasurement maxHeight, PdfMeasurement margin = default(PdfMeasurement))
        {
            double maxWidthPts = maxWidth.AsPoints();
            double maxHeightPts = maxHeight.AsPoints();
            double marginPts = margin.AsPoints();

            double scaleX = (maxWidthPts - 2 * marginPts) / srcRect.Width;
            double scaleY = (maxHeightPts - 2 * marginPts) / srcRect.Height;
            double scale = Math.Min(scaleX, scaleY);

            PdfMeasurement left = margin;
            PdfMeasurement right = PdfMeasurement.Points(marginPts + scale * srcRect.Width);
            PdfMeasurement bottom = margin;
            PdfMeasurement top = PdfMeasurement.Points(marginPts + scale * srcRect.Height);
            return new ConverterPdfRect(left, right, bottom, top);
        }

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
