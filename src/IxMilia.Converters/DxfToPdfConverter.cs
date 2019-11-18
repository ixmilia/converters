using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            CreateTransformations(source.ActiveViewPort, options, out Matrix4 scale, out Matrix4 affine);
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
                    foreach (IPdfPathItem pathItem in ConvertEntity(entity, layer, affine, scale))
                    {
                        AddPathItemToPage(pathItem);
                    }
                }
            }

            if (builder.Items.Count > 0)
            {
                page.Items.Add(builder.ToPath());
            }

            return pdf;
        }

        /// <summary>
        /// Creates an <paramref name="affine"/> transform for DXF to PDF coordinate transformation and
        /// a <paramref name="scale"/> transform for relative values like radii.
        /// </summary>
        /// <param name="viewPort">The DXF view port.</param>
        /// <param name="options">The converter options.</param>
        /// <param name="scale">[out] The (relative) scale transform.</param>
        /// <param name="affine">[out] The (absolute) affine transform including scale.</param>
        private static void CreateTransformations(DxfViewPort viewPort, DxfToPdfConverterOptions options,
            out Matrix4 scale, out Matrix4 affine)
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
                scale = Matrix4.CreateScale(scaleX, scaleY, 0.0);
                affine = Matrix4.CreateTranslate(+pdfOffsetX, +pdfOffsetY, 0.0)
                    * scale
                    * Matrix4.CreateTranslate(-dxfOffsetX, -dxfOffsetY, 0.0);
                return;
            }
            // TODO this code assumes DXF unit inch - use actual unit from header instead!
            // scale depends on the unit, output "pdf points" with 72 DPI
            const double dotsPerInch = 72;
            scale = Matrix4.CreateScale(options.Scale * dotsPerInch, options.Scale * dotsPerInch, 0.0);
            affine = Matrix4.Identity
                * scale
                * Matrix4.CreateTranslate(-viewPort.LowerLeft.X, -viewPort.LowerLeft.Y, 0.0);
        }

        #region Entity Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<IPdfPathItem> ConvertEntity(DxfEntity entity, DxfLayer layer, Matrix4 affine, Matrix4 scale)
        {
            var pdfStreamState = new PdfStreamState(
                strokeColor: GetPdfColor(entity, layer),
                strokeWidth: GetStrokeWidth(entity, layer));
            switch (entity)
            {
                case DxfLine line:
                    return ConvertLine(line, affine, pdfStreamState);
                case DxfArc arc:
                    return ConvertArc(arc, affine, scale, pdfStreamState);
                case DxfCircle circle:
                    return ConvertCircle(circle, affine, scale, pdfStreamState);
                case DxfLwPolyline lwPolyline:
                    return ConvertPolyline(lwPolyline, affine, scale, pdfStreamState);
                default:
                    return new IPdfPathItem[0];
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<IPdfPathItem> ConvertLine(DxfLine line, Matrix4 affine, PdfStreamState pdfStreamState)
        {
            var p1 = affine.Transform(line.P1).ToPdfPoint(PdfMeasurementType.Point);
            var p2 = affine.Transform(line.P2).ToPdfPoint(PdfMeasurementType.Point);
            yield return new PdfLine(p1, p2, pdfStreamState);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<IPdfPathItem> ConvertCircle(DxfCircle circle, Matrix4 affine, Matrix4 scale, PdfStreamState pdfStreamState)
        {
            // a circle becomes an ellipse, unless aspect ratio is kept.
            var center = affine.Transform(circle.Center).ToPdfPoint(PdfMeasurementType.Point);
            var radius = scale.Transform(new Vector(circle.Radius, circle.Radius, circle.Radius))
                .ToPdfPoint(PdfMeasurementType.Point);
            yield return new PdfEllipse(center, radius.X, radius.Y, state: pdfStreamState);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<IPdfPathItem> ConvertArc(DxfArc arc, Matrix4 affine, Matrix4 scale, PdfStreamState pdfStreamState)
        {
            var center = affine.Transform(arc.Center).ToPdfPoint(PdfMeasurementType.Point);
            var radius = scale.Transform(new Vector(arc.Radius, arc.Radius, arc.Radius))
                .ToPdfPoint(PdfMeasurementType.Point);
            const double rotation = 0;
            double startAngleRad = arc.StartAngle * Math.PI / 180;
            double endAngleRad = arc.EndAngle * Math.PI / 180;
            yield return new PdfEllipse(center, radius.X, radius.Y, rotation, startAngleRad, endAngleRad, pdfStreamState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<IPdfPathItem> ConvertPolyline(DxfLwPolyline lwPolyline, Matrix4 affine,
            Matrix4 scale, PdfStreamState pdfStreamState)
        {
            IList<DxfLwPolylineVertex> vertices = lwPolyline.Vertices;
            int n = vertices.Count;
            DxfLwPolylineVertex vertex = vertices[0];
            for (int i = 1; i < n; i++)
            {
                DxfLwPolylineVertex next = vertices[i];
                yield return ConvertPolylineSegment(vertex, next, affine, scale, pdfStreamState);
                vertex = next;
            }
            if (lwPolyline.IsClosed)
            {
                var next = vertices[0];
                var p1 = affine.Transform(new Vector(vertex.X, vertex.Y, 0))
                    .ToPdfPoint(PdfMeasurementType.Point);
                var p2 = affine.Transform(new Vector(next.X, next.Y, 0))
                    .ToPdfPoint(PdfMeasurementType.Point);
                yield return new PdfLine(p1, p2, pdfStreamState);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IPdfPathItem ConvertPolylineSegment(DxfLwPolylineVertex vertex, DxfLwPolylineVertex next, Matrix4 affine,
            Matrix4 scale, PdfStreamState pdfStreamState)
        {
            var p1 = affine.Transform(new Vector(vertex.X, vertex.Y, 0))
                .ToPdfPoint(PdfMeasurementType.Point);
            var p2 = affine.Transform(new Vector(next.X, next.Y, 0))
                .ToPdfPoint(PdfMeasurementType.Point);
            if (vertex.Bulge.Equals(0.0))
            {
                return new PdfLine(p1, p2, pdfStreamState);
            }

            double dx = next.X - vertex.X;
            double dy = next.Y - vertex.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 1e-10)
            {
                // segment is very short, avoid numerical problems
                return new PdfLine(p1, p2, pdfStreamState);
            }

            double alpha = 4.0 * Math.Atan(vertex.Bulge);
            double radius = length / (2.0 * Math.Abs(Math.Sin(alpha * 0.5)));

            double bulgeFactor = Math.Sign(vertex.Bulge) * Math.Cos(alpha * 0.5) * radius;
            double normalX = -(dy / length) * bulgeFactor;
            double normalY = +(dx / length) * bulgeFactor;

            // calculate center (dxf coordinate system), start and end angle
            double cx = (vertex.X + next.X) / 2 + normalX;
            double cy = (vertex.Y + next.Y) / 2 + normalY;
            double startAngle;
            double endAngle;
            if (vertex.Bulge > 0) // counter-clockwise
            {
                startAngle = Math.Atan2(vertex.Y - cy, vertex.X - cx);
                endAngle = Math.Atan2(next.Y - cy, next.X - cx);
            }
            else // clockwise: flip start and end angle
            {
                startAngle = Math.Atan2(next.Y - cy, next.X - cx);
                endAngle = Math.Atan2(vertex.Y - cy, vertex.X - cx);
            }

            // transform to PDF coordinate system
            var center = affine.Transform(new Vector(cx, cy, 0)).ToPdfPoint(PdfMeasurementType.Point);
            var pdfRadius = scale.Transform(new Vector(radius, radius, radius)).ToPdfPoint(PdfMeasurementType.Point);
            const double rotation = 0;
            return new PdfEllipse(center, pdfRadius.X, pdfRadius.Y, rotation,
                startAngle, endAngle, pdfStreamState);
        }

        #endregion

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
