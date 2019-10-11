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

        public DxfToPdfConverterOptions(PdfMeasurement pageWidth, PdfMeasurement pageHeight)
        {
            PageWidth = pageWidth;
            PageHeight = pageHeight;
        }
    }

    public class DxfToPdfConverter : IConverter<DxfFile, PdfFile>
    {
        private DxfToPdfConverterOptions Options { get; }

        public DxfToPdfConverter(DxfToPdfConverterOptions options)
        {
            Options = options;
        }

        public PdfFile Convert(DxfFile source)
        {
            // adapted from https://github.com/ixmilia/bcad/blob/master/src/IxMilia.BCad.FileHandlers/Plotting/Pdf/PdfPlotter.cs
            var extents = ExtentsViewPort(source);
            var matrix = CreateTransformationMatrix(extents);
            var pdf = new PdfFile();
            var page = new PdfPage(Options.PageWidth, Options.PageHeight);
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
                                var p1 = matrix.Transform(line.P1);
                                var p2 = matrix.Transform(line.P2);
                                AddPathItemToPage(
                                    new PdfLine(
                                        p1.ToPdfPoint(PdfMeasurementType.Inch),
                                        p2.ToPdfPoint(PdfMeasurementType.Inch),
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

        private Matrix4 CreateTransformationMatrix(DxfViewPort viewPort)
        {
            var scale = Options.PageHeight.ConvertTo(PdfMeasurementType.Inch).RawValue / viewPort.ViewHeight;
            var projectionMatrix = Matrix4.Identity
                * Matrix4.CreateScale(scale, scale, 1.0)
                * Matrix4.CreateTranslate(-viewPort.LowerLeft.X, -viewPort.LowerLeft.Y, 0.0);
            return projectionMatrix;
        }

        private DxfViewPort ExtentsViewPort(DxfFile dxf)
        {
            return new DxfViewPort()
            {
                LowerLeft = dxf.Header.MinimumDrawingExtents,
                ViewHeight = dxf.Header.MaximumDrawingExtents.Y - dxf.Header.MinimumDrawingExtents.Y
            };
        }
    }
}
