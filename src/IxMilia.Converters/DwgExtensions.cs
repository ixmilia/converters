using System;
using System.Linq;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace IxMilia.Converters
{
    public static class DwgExtensions
    {
        public const double RadiansToDegrees = 180.0 / Math.PI;
        
        public static DxfAcadVersion ToDxfVersion(this DwgVersionId version)
        {
            switch (version)
            {
                case DwgVersionId.R13:
                    return DxfAcadVersion.R13;
                case DwgVersionId.R14:
                    return DxfAcadVersion.R14;
                default:
                    throw new ArgumentOutOfRangeException(nameof(version));
            }
        }

        public static DxfColor ToDxfColor(this DwgColor color) => DxfColor.FromRawValue(color.RawValue);

        public static DxfPoint ToDxfPoint(this DwgPoint point) => new DxfPoint(point.X, point.Y, point.Z);

        public static DxfVector ToDxfVector(this DwgVector vector) => new DxfVector(vector.X, vector.Y, vector.Z);

        public static DxfArc ToDxfArc(this DwgArc arc)
        {
            return new DxfArc(arc.Center.ToDxfPoint(), arc.Radius, arc.StartAngle * RadiansToDegrees, arc.EndAngle * RadiansToDegrees)
            {
                Normal = arc.Extrusion.ToDxfVector(),
                Thickness = arc.Thickness,
            }.WithCommonProperties(arc);
        }

        public static DxfCircle ToDxfCircle(this DwgCircle circle)
        {
            return new DxfCircle(circle.Center.ToDxfPoint(), circle.Radius)
            {
                Normal = circle.Extrusion.ToDxfVector(),
                Thickness = circle.Thickness,
            }.WithCommonProperties(circle);
        }

        public static DxfEllipse ToDxfEllipse(this DwgEllipse ellipse)
        {
            return new DxfEllipse(ellipse.Center.ToDxfPoint(), ellipse.MajorAxis.ToDxfVector(), ellipse.MinorAxisRatio)
            {
                StartParameter = ellipse.StartAngle,
                EndParameter = ellipse.EndAngle,
                Normal = ellipse.Extrusion.ToDxfVector(),
            }.WithCommonProperties(ellipse);
        }

        public static DxfInsert ToDxfInsert(this DwgInsert insert)
        {
            return new DxfInsert()
            {
                Layer = insert.Layer.Name,
                LineTypeName = insert.LineType?.Name,
                Location = insert.Location.ToDxfPoint(),
                Name = insert.BlockHeader.Name,
            }.WithCommonProperties(insert);
        }

        public static DxfLine ToDxfLine(this DwgLine line)
        {
            return new DxfLine(line.P1.ToDxfPoint(), line.P2.ToDxfPoint())
            {
                ExtrusionDirection = line.Extrusion.ToDxfVector(),
                Thickness = line.Thickness,
            }.WithCommonProperties(line);
        }

        public static DxfModelPoint ToDxfModelPoint(this DwgLocation location)
        {
            return new DxfModelPoint(location.Point.ToDxfPoint())
            {
                Thickness = location.Thickness
            }.WithCommonProperties(location);
        }

        public static DxfLwPolyline ToDxfLwPolyline(this DwgLwPolyline lwpolyline)
        {
            return new DxfLwPolyline(lwpolyline.Vertices.Select(v => new DxfLwPolylineVertex() { X = v.X, Y = v.Y, StartingWidth = v.StartWidth, EndingWidth = v.EndWidth, Bulge = v.Bulge }))
            {
                Thickness = lwpolyline.Thickness,
            }.WithCommonProperties(lwpolyline);
        }

        public static DxfPolyline ToDxfPolyline(this DwgPolyline2D polyline)
        {
            return new DxfPolyline(polyline.Vertices.Select(v => new DxfVertex(v.Point.ToDxfPoint()) { StartingWidth = v.StartWidth, EndingWidth = v.EndWidth, Bulge = v.Bulge }))
            {
                Is3DPolyline = false,
                Thickness = polyline.Thickness,
                Normal = polyline.Extrusion.ToDxfVector(),
            }.WithCommonProperties(polyline);
        }

        public static DxfPolyline ToDxfPolyline(this DwgPolyline3D polyline)
        {
            return new DxfPolyline(polyline.Vertices.Select(v => new DxfVertex(v.Point.ToDxfPoint())))
            {
                Is3DPolyline = true,
            }.WithCommonProperties(polyline);
        }

        public static DxfSpline ToDxfSpline(this DwgSpline spline)
        {
            var result = new DxfSpline()
            {
                DegreeOfCurve = spline.Degree,
                ControlPointTolerance = spline.ControlTolerance,
                FitTolerance = spline.FitTolerance,
                KnotTolerance = spline.KnotTolerance,
            }.WithCommonProperties(spline);
            foreach (var cp in spline.ControlPoints)
            {
                result.ControlPoints.Add(new DxfControlPoint(cp.Point.ToDxfPoint(), cp.Weight));
            }

            foreach (var fp in spline.FitPoints)
            {
                result.FitPoints.Add(fp.ToDxfPoint());
            }

            foreach (var k in spline.KnotValues)
            {
                result.KnotValues.Add(k);
            }

            return result;
        }

        public static DxfText ToDxfText(this DwgText text)
        {
            return new DxfText(text.InsertionPoint.ToDxfPoint(), text.Height, text.Value)
            {
                Elevation = text.Elevation,
                Normal = text.Extrusion.ToDxfVector(),
                HorizontalTextJustification = (DxfHorizontalTextJustification)text.HorizontalAlignment,
                VerticalTextJustification = (DxfVerticalTextJustification)text.VerticalAlignment,
                Rotation = text.RotationAngle,
            }.WithCommonProperties(text);
        }

        public static TDxfEntity WithCommonProperties<TDxfEntity>(this TDxfEntity entity, DwgEntity parent) where TDxfEntity : DxfEntity
        {
            entity.Color = parent.Color.ToDxfColor();
            entity.Layer = parent.Layer.Name;
            entity.LineTypeName = parent.LineType?.Name;
            entity.LineTypeScale = parent.LineTypeScale;
            return entity;
        }

        public static DwgLineType EnsureLineType(this DwgDrawing drawing, string name)
        {
            if (!drawing.LineTypes.ContainsKey(name))
            {
                var dwgLineType = new DwgLineType(name)
                {
                    // TODO
                };
                drawing.LineTypes.Add(dwgLineType);
            }

            return drawing.LineTypes[name];
        }

        public static DwgLayer EnsureLayer(this DwgDrawing drawing, string layerName, DwgColor color, string lineTypeName)
        {
            if (!drawing.Layers.ContainsKey(layerName))
            {
                var newLayer = new DwgLayer(layerName)
                {
                    Color = color,
                    LineType = drawing.EnsureLineType(lineTypeName),
                };
                drawing.Layers.Add(newLayer);
            }

            return drawing.Layers[layerName];
        }

        public static DwgLineType LineTypeOrCurrent(this DwgDrawing drawing, string lineTypeName)
        {
            if (lineTypeName != null &&
                drawing.LineTypes.TryGetValue(lineTypeName, out var lineType))
            {
                return lineType;
            }

            return drawing.CurrentEntityLineType;
        }

        public static DwgLayer LayerOrCurrent(this DwgDrawing drawing, string layerName)
        {
            if (layerName != null &&
                drawing.Layers.TryGetValue(layerName, out var layer))
            {
                return layer;
            }

            return drawing.CurrentLayer;
        }
    }
}
