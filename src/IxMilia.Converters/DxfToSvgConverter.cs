using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace IxMilia.Converters
{
    public struct DxfToSvgConverterOptions
    {
        public ConverterDxfRect DxfSource { get; }
        public ConverterSvgRect SvgDestination { get; }
        public string SvgId { get; }

        public DxfToSvgConverterOptions(ConverterDxfRect dxfSource, ConverterSvgRect svgDestination, string svgId = null)
        {
            DxfSource = dxfSource;
            SvgDestination = svgDestination;
            SvgId = svgId;
        }
    }

    public class DxfToSvgConverter : IConverter<DxfFile, XElement, DxfToSvgConverterOptions>
    {
        public static XNamespace Xmlns = "http://www.w3.org/2000/svg";

        public XElement Convert(DxfFile source, DxfToSvgConverterOptions options)
        {
            // adapted from https://github.com/ixmilia/bcad/blob/master/src/IxMilia.BCad.FileHandlers/Plotting/Svg/SvgPlotter.cs

            var root = new XElement(Xmlns + "svg",
                new XAttribute("width", options.SvgDestination.ElementWidth.ToDisplayString()),
                new XAttribute("height", options.SvgDestination.ElementHeight.ToDisplayString()),
                new XAttribute("viewBox", $"{options.DxfSource.Left.ToDisplayString()} {options.DxfSource.Bottom.ToDisplayString()} {options.DxfSource.Width.ToDisplayString()} {options.DxfSource.Height.ToDisplayString()}"),
                new XAttribute("version", "1.1"));

            // y-axis in svg increases going down the screen, but decreases in dxf
            root.Add(new XComment(" all entities are drawn in world coordinates and this root group controls the final view "));
            var world = new XElement(Xmlns + "g",
                new XAttribute("transform", $"translate(0.0 {options.DxfSource.Height.ToDisplayString()}) scale(1.0 -1.0)"),
                new XAttribute("class", "svg-viewport"));
            root.Add(world);

            foreach (var layer in source.Layers.OrderBy(l => l.Name))
            {
                var autoColor = DxfColor.FromIndex(0);
                world.Add(new XComment($" layer '{layer.Name}' "));
                var g = new XElement(Xmlns + "g",
                    new XAttribute("stroke", (layer.Color ?? autoColor).ToRGBString()),
                    new XAttribute("fill", (layer.Color ?? autoColor).ToRGBString()));
                foreach (var entity in source.Entities.Where(e => e.Layer == layer.Name))
                {
                    var element = entity.ToXElement();
                    if (element != null)
                    {
                        g.Add(element);
                    }
                }

                world.Add(g);
            }

            root = TransformToHtmlDiv(root, options.SvgId);
            return root;
        }

        private static XElement TransformToHtmlDiv(XElement svg, string svgId)
        {
            if (string.IsNullOrWhiteSpace(svgId))
            {
                return svg;
            }

            // add id to svg object
            svg.Add(new XAttribute("id", svgId));

            // add navigation controls
            var controls = new XElement(Xmlns + "g",
                new XAttribute("transform", "translate(0 0)"),
                SvgButton(0, "-", "doZoom(1)"), // zoom out button
                SvgButton(1, "+", "doZoom(-1)"), // zoom in button
                SvgButton(2, "<", "doPan(-1, 0)"), // pan left
                SvgButton(3, ">", "doPan(1, 0)"), // pan right
                SvgButton(4, "^", "doPan(0, -1)"), // pan up
                SvgButton(5, "v", "doPan(0, 1)")); // pan down
            svg.Add(controls);

            // add css
            var css = new XElement("style", GetCss(ButtonSize));

            // add javascript
            var script = new XElement("script", new XAttribute("type", "text/javascript"), new XRawText(GetJavascriptControls(svgId)));

            // build final element
            var div = new XElement("div", svg, css, script);
            return div;
        }

        private const int ButtonSize = 24;

        private static IEnumerable<XElement> SvgButton(int xOrder, string text, string action)
        {
            yield return new XElement(Xmlns + "rect",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("class", "svg-button"),
                new XAttribute("transform", $"translate({xOrder * ButtonSize} 0)"));
            yield return new XElement(Xmlns + "text",
                new XAttribute("x", 0),
                new XAttribute("y", 0),
                new XAttribute("transform", $"translate({xOrder * ButtonSize} {ButtonSize})"),
                new XAttribute("class", "svg-button-text"),
                text);
            // clickable overlay
            yield return new XElement(Xmlns + "rect",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("class", "svg-button-overlay"),
                new XAttribute("transform", $"translate({xOrder * ButtonSize} 0)"),
                new XAttribute("onclick", action));
        }

        private static string GetJavascriptControls(string svgId)
        {
            var assembly = typeof(DxfToSvgConverter).GetTypeInfo().Assembly;
            using (var jsStream = assembly.GetManifestResourceStream("IxMilia.Converters.SvgJavascriptControls.js"))
            using (var streamReader = new StreamReader(jsStream))
            {
                var contents = Environment.NewLine + streamReader.ReadToEnd();
                contents = contents.Replace("$DRAWING-ID$", svgId);
                return contents;
            }
        }

        private static string GetCss(int buttonSize)
        {
            var assembly = typeof(DxfToSvgConverter).GetTypeInfo().Assembly;
            using (var jsStream = assembly.GetManifestResourceStream("IxMilia.Converters.SvgStyles.css"))
            using (var streamReader = new StreamReader(jsStream))
            {
                var contents = Environment.NewLine + streamReader.ReadToEnd();
                contents = contents.Replace("$BUTTON-SIZE$", buttonSize.ToString());
                return contents;
            }
        }

        private class XRawText : XText
        {
            public XRawText(string text)
                : base(text)
            {
            }

            public override void WriteTo(XmlWriter writer)
            {
                writer.WriteRaw(Value);
            }
        }
    }

    public static class SvgExtensions
    {
        public static void SaveTo(this XElement document, Stream output)
        {
            var settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  "
            };
            using (var writer = XmlWriter.Create(output, settings))
            {
                document.WriteTo(writer);
            }
        }

        public static void SaveTo(this XElement document, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                document.SaveTo(fileStream);
            }
        }

        public static string ToRGBString(this DxfColor color)
        {
            var intValue = color.IsIndex
                ? color.ToRGB()
                : 0; // fall back to black
            var r = (intValue >> 16) & 0xFF;
            var g = (intValue >> 8) & 0xFF;
            var b = intValue & 0xFF;
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        internal static string ToDisplayString(this double value)
        {
            return value.ToString("0.0##############", CultureInfo.InvariantCulture);
        }

        public static XElement ToXElement(this DxfEntity entity)
        {
            // elements are simply flattened in the z plane; the world transform in the main function handles the rest
            switch (entity)
            {
                case DxfArc arc:
                    return arc.ToXElement();
                case DxfCircle circle:
                    return circle.ToXElement();
                case DxfEllipse el:
                    return el.ToXElement();
                case DxfLine line:
                    return line.ToXElement();
                case DxfLwPolyline poly:
                    return poly.ToXElement();
                default:
                    return null;
            }
        }

        public static XElement ToXElement(this DxfArc arc)
        {
            var path = arc.GetSvgPath();
            return new XElement(DxfToSvgConverter.Xmlns + "path",
                new XAttribute("d", path.ToString()),
                new XAttribute("fill-opacity", 0))
                .AddStroke(arc.Color)
                .AddStrokeWidth(arc.Thickness)
                .AddVectorEffect();
        }

        public static XElement ToXElement(this DxfCircle circle)
        {
            return new XElement(DxfToSvgConverter.Xmlns + "ellipse",
                new XAttribute("cx", circle.Center.X.ToDisplayString()),
                new XAttribute("cy", circle.Center.Y.ToDisplayString()),
                new XAttribute("rx", circle.Radius.ToDisplayString()),
                new XAttribute("ry", circle.Radius.ToDisplayString()),
                new XAttribute("fill-opacity", 0))
                .AddStroke(circle.Color)
                .AddStrokeWidth(circle.Thickness)
                .AddVectorEffect();
        }

        public static XElement ToXElement(this DxfEllipse ellipse)
        {
            XElement baseShape;
            if (IsCloseTo(ellipse.StartParameter, 0.0) && IsCloseTo(ellipse.EndParameter, Math.PI * 2.0))
            {
                baseShape = new XElement(DxfToSvgConverter.Xmlns + "ellipse",
                    new XAttribute("cx", ellipse.Center.X.ToDisplayString()),
                    new XAttribute("cy", ellipse.Center.Y.ToDisplayString()),
                    new XAttribute("rx", ellipse.MajorAxis.Length.ToDisplayString()),
                    new XAttribute("ry", ellipse.MinorAxis().Length.ToDisplayString()));
            }
            else
            {
                var path = ellipse.GetSvgPath();
                baseShape = new XElement(DxfToSvgConverter.Xmlns + "path",
                    new XAttribute("d", path.ToString()));
            }

            baseShape.Add(new XAttribute("fill-opacity", 0));
            return baseShape
                .AddStroke(ellipse.Color)
                .AddStrokeWidth(1.0)
                .AddVectorEffect();
        }

        public static XElement ToXElement(this DxfLine line)
        {
            return new XElement(DxfToSvgConverter.Xmlns + "line",
                new XAttribute("x1", line.P1.X.ToDisplayString()),
                new XAttribute("y1", line.P1.Y.ToDisplayString()),
                new XAttribute("x2", line.P2.X.ToDisplayString()),
                new XAttribute("y2", line.P2.Y.ToDisplayString()))
                .AddStroke(line.Color)
                .AddStrokeWidth(line.Thickness)
                .AddVectorEffect();
        }

        public static XElement ToXElement(this DxfLwPolyline poly)
        {
            var path = poly.GetSvgPath();
            return new XElement(DxfToSvgConverter.Xmlns + "path",
                new XAttribute("d", path.ToString()),
                new XAttribute("fill-opacity", 0))
                .AddStroke(poly.Color)
                .AddStrokeWidth(1.0)
                .AddVectorEffect();
        }

        private static IEnumerable<SvgPathSegment> FromPolylineVertices(DxfLwPolylineVertex last, DxfLwPolylineVertex next)
        {
            var dx = next.X - last.X;
            var dy = next.Y - last.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (last.Bulge == 0.0 || IsCloseTo(dist, 1.0e-10))
            {
                // line or a really short arc
                return new[] { new SvgLineToPath(next.X, next.Y) };
            }

            // given the following diagram:
            //
            //                p1
            //               -)
            //            -  |  )
            //        -      |    )
            //    -          |     )
            // O ------------|C----T
            //    -          |     )
            //        -      |    )
            //            -  |  )
            //               -)
            //               p2
            //
            // where O is the center of the circle, C is the midpoint between p1 and p2, calculate
            // the hypotenuse of the triangle Op1C to get the radius

            var includedAngle = Math.Atan(Math.Abs(last.Bulge)) * 4.0;
            var isCounterClockwise = last.Bulge > 0.0;

            // find radius
            var oppositeLength = dist / 2.0;
            var radius = oppositeLength / Math.Sin(includedAngle / 2.0);

            // find vector from the center to point C
            var vx = -dy;
            var vy = dx;
            var vlp = Math.Sqrt(vx * vx + vy * vy);
            var vxn = vx / vlp;
            var vyn = vy / vlp;
            var vl = Math.Sqrt(radius * radius - (dist * dist / 4.0));

            // now backtrack from point C to the center
            var cx = (next.X + last.X) / 2.0;
            var cy = (next.Y + last.Y) / 2.0;
            var centerX = cx + vxn * vl;
            var centerY = cy + vyn * vl;

            // from there we can get the start/end angles
            var startAngle = Math.Atan2(last.Y - centerY, last.X - centerX);
            var endAngle = Math.Atan2(next.Y - centerY, next.X - centerX);

            // then compute like before
            var segments = SvgPath.FromEllipse(centerX, centerY, radius, 0.0, 1.0, startAngle, endAngle).Segments;
            return segments.Skip(1); // don't return the first MoveTo
        }

        internal static SvgPath GetSvgPath(this DxfArc arc)
        {
            var startAngle = arc.StartAngle * Math.PI / 180.0;
            var endAngle = arc.EndAngle * Math.PI / 180.0;
            return SvgPath.FromEllipse(arc.Center.X, arc.Center.Y, arc.Radius, 0.0, 1.0, startAngle, endAngle);
        }

        internal static SvgPath GetSvgPath(this DxfEllipse ellipse)
        {
            return SvgPath.FromEllipse(ellipse.Center.X, ellipse.Center.Y, ellipse.MajorAxis.X, ellipse.MajorAxis.Y, ellipse.MinorAxisRatio, ellipse.StartParameter, ellipse.EndParameter);
        }

        internal static SvgPath GetSvgPath(this DxfLwPolyline poly)
        {
            var first = poly.Vertices.First();
            var segments = new List<SvgPathSegment>();
            segments.Add(new SvgMoveToPath(first.X, first.Y));
            var last = first;
            foreach (var next in poly.Vertices.Skip(1))
            {
                segments.AddRange(FromPolylineVertices(last, next));
                last = next;
            }

            if (poly.IsClosed)
            {
                segments.AddRange(FromPolylineVertices(last, first));
            }

            return new SvgPath(segments);
        }

        private static XElement AddStroke(this XElement element, DxfColor color)
        {
            if (color.IsIndex)
            {
                var stroke = element.Attribute("stroke");
                var colorString = color.ToRGBString();
                if (stroke == null)
                {
                    element.Add(new XAttribute("stroke", colorString));
                }
                else
                {
                    stroke.Value = colorString;
                }
            }

            return element;
        }

        private static XElement AddStrokeWidth(this XElement element, double strokeWidth)
        {
            element.Add(new XAttribute("stroke-width", $"{Math.Max(strokeWidth, 1.0).ToDisplayString()}px"));
            return element;
        }

        private static XElement AddVectorEffect(this XElement element)
        {
            element.Add(new XAttribute("vector-effect", "non-scaling-stroke"));
            return element;
        }

        private static bool IsCloseTo(double a, double b)
        {
            return Math.Abs(a - b) < 1.0e-10;
        }
    }
}
