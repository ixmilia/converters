using System;
using System.Linq;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using IxMilia.Pdf;

namespace IxMilia.Converters
{
    public class DxfToPdfConverterOptions
    {
        public PdfMeasurement PageWidth { get; }
        public PdfMeasurement PageHeight { get; }
        public double Scale { get; }
        public ConverterDxfRect DxfSource { get; }
        public ConverterPdfRect PdfDestination { get; }

        public DxfToPdfConverterOptions(PdfMeasurement pageWidth, PdfMeasurement pageHeight, double scale)
        {
            PageWidth = pageWidth;
            PageHeight = pageHeight;
            Scale = scale;
            this.DxfSource = null;
            this.PdfDestination = null;
        }

        public DxfToPdfConverterOptions(PdfMeasurement pageWidth, PdfMeasurement pageHeight, ConverterDxfRect dxfSource, ConverterPdfRect pdfDestination)
        {
            PageWidth = pageWidth;
            PageHeight = pageHeight;
            Scale = 1d;
            this.DxfSource = dxfSource ?? throw new ArgumentNullException(nameof(dxfSource));
            this.PdfDestination = pdfDestination ?? throw new ArgumentNullException(nameof(pdfDestination));
        }
    }

    public class DxfToPdfConverter : IConverter<DxfFile, PdfFile, DxfToPdfConverterOptions>
    {
        public PdfFile Convert(DxfFile source, DxfToPdfConverterOptions options)
        {
            // adapted from https://github.com/ixmilia/bcad/blob/master/src/IxMilia.BCad.FileHandlers/Plotting/Pdf/PdfPlotter.cs
            var matrix = CreateTransformationMatrix(source.ActiveViewPort, options);
            var pdf = new PdfFile();
            var page = new PdfPage(options.PageWidth, options.PageHeight);
            pdf.Pages.Add(page);

            var builder = new PdfPathBuilder();
            void AddPathItemToPage(IPdfPathItem pathItem)
            {
                builder.Add(pathItem);
            }
            //void AddStreamItemToPage(PdfStreamItem streamItem)
            //{
            //    if (builder.Items.Count > 0)
            //    {
            //        page.Items.Add(builder.ToPath());
            //        builder = new PdfPathBuilder();
            //    }

            //    page.Items.Add(streamItem);
            //}

            foreach (var layer in source.Layers)
            {
                foreach (var entity in source.Entities.Where(e => e.Layer == layer.Name))
                {
                    var pdfStreamState = new PdfStreamState(
                        strokeColor: GetPdfColor(entity, layer),
                        strokeWidth: GetStrokeWidth(entity, layer));
                    switch (entity)
                    {
                        case DxfLine line:
                        {
                            var p1 = matrix.Transform(line.P1).ToPdfPoint(PdfMeasurementType.Point);
                            var p2 = matrix.Transform(line.P2).ToPdfPoint(PdfMeasurementType.Point);
                            AddPathItemToPage(new PdfLine(p1, p2, pdfStreamState));
                            break;
                        }
                        default:
                            break;
                    }
                }
            }

            if (builder.Items.Count > 0)
            {
                page.Items.Add(builder.ToPath());
            }

            return pdf;
        }

        private static Matrix4 CreateTransformationMatrix(DxfViewPort viewPort, DxfToPdfConverterOptions options)
        {
            if (options.DxfSource != null && options.PdfDestination != null)
            {
                // user supplied source and destination rectangles, no trouble with units
                double pdfOffsetX = options.PdfDestination.Left.AsPoints();
                double pdfOffsetY = options.PdfDestination.Bottom.AsPoints();
                double scaleX = options.PdfDestination.Width.AsPoints() / options.DxfSource.Width;
                double scaleY = options.PdfDestination.Height.AsPoints() / options.DxfSource.Height;
                double dxfOffsetX = options.DxfSource.Left;
                double dxfOffsetY = options.DxfSource.Bottom;
                return Matrix4.CreateTranslate(+pdfOffsetX, +pdfOffsetY, 0.0)
                    * Matrix4.CreateScale(scaleX, scaleY, 0.0)
                    * Matrix4.CreateTranslate(-dxfOffsetX, -dxfOffsetY, 0.0);
            }
            // TODO this code assumes DXF unit inch - use actual unit from header instead!
            // scale depends on the unit, output "pdf points" with 72 DPI
            const double dotsPerInch = 72;
            var projectionMatrix = Matrix4.Identity
                * Matrix4.CreateScale(options.Scale * dotsPerInch, options.Scale * dotsPerInch, 0.0)
                * Matrix4.CreateTranslate(-viewPort.LowerLeft.X, -viewPort.LowerLeft.Y, 0.0);
            return projectionMatrix;
        }
        
        #region Color and Stroke Width Conversion

        private static PdfColor GetPdfColor(DxfEntity entity, DxfLayer layer)
        {
            int rgb = entity.Color24Bit;
            if (rgb > 0)
            {
                return ToPdfColor(rgb);
            }
            DxfColor c = GetFinalDxfColor(entity, layer);
            if (c != null && c.IsIndex)
            {
                rgb = c.ToRGB();
                return ToPdfColor(rgb);
            }
            // default to black, probably not correct.
            return new PdfColor(0, 0, 0);
        }

        private static PdfColor ToPdfColor(int rgb)
        {
            byte r = (byte)(rgb >> 16);
            byte g = (byte)(rgb >> 8);
            byte b = (byte)rgb;

            // It seems DXF does not distinguish white/black:
            // both map to index=7 which is (r=255,g=255,b=255)
            // but white stroke on white background is crap.
            // This doesn't feel right, better ideas?
            if (r == byte.MaxValue && g == byte.MaxValue && b == byte.MaxValue)
            {
                return new PdfColor(0, 0, 0);
            }
            return new PdfColor(r / 255.0, g / 255.0, b / 255.0);
        }

        private static DxfColor GetFinalDxfColor(DxfEntity entity, DxfLayer layer)
        {
            DxfColor c = entity.Color;
            if (c == null || c.IsByLayer)
            {
                return layer.Color;
            }
            if (c.IsIndex)
            {
                return c;
            }
            // we could build a Dictionary<DxfBlock, DxfColor> for the c.IsByBlock case
            // not sure how to retrieve color for the remaining cases
            return null;
        }

        private static PdfMeasurement GetStrokeWidth(DxfEntity entity, DxfLayer layer)
        {
            DxfLineWeight lw = new DxfLineWeight {Value = entity.LineweightEnumValue};
            DxfLineWeightType type = lw.LineWeightType;
            if (type == DxfLineWeightType.ByLayer)
            {
                lw = layer.LineWeight;
            }
            if (lw.Value == 0)
            {
                return default(PdfMeasurement);
            }
            if (lw.Value < 0)
            {
                return PdfMeasurement.Points(1); // smallest valid stroke width
            }
            // TODO What is the meaning of this short? Some default app-dependent table?
            return PdfMeasurement.Points(lw.Value);
        }

        #endregion
    }
}
