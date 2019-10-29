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
                    switch (entity)
                    {
                        case DxfLine line:
                            {
                                var p1 = matrix.Transform(line.P1).ToPdfPoint(PdfMeasurementType.Point);
                                var p2 = matrix.Transform(line.P2).ToPdfPoint(PdfMeasurementType.Point);
                                AddPathItemToPage(
                                    new PdfLine(p1, p2,
                                        state: new PdfStreamState(
                                            strokeColor: null, // TODO: apply color
                                            strokeWidth: null))); // TODO: apply stroke
                            }
                            break;
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
    }
}
