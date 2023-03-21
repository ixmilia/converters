using System;
using System.Collections.Generic;
using System.Linq;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace IxMilia.Converters
{
    public static class DxfExtensions
    {
        public const double DegreesToRadians = Math.PI / 180.0;
        
        public static DxfPoint GetPointFromAngle(this DxfCircle circle, double angleInDegrees)
        {
            var angleInRadians = angleInDegrees * DegreesToRadians;
            var sin = Math.Sin(angleInRadians);
            var cos = Math.Cos(angleInRadians);
            return new DxfPoint(cos * circle.Radius, sin * circle.Radius, 0.0) + circle.Center;
        }

        public static DwgColor ToDwgColor(this DxfColor color) => DwgColor.FromRawValue(color.RawValue);

        public static DwgPoint ToDwgPoint(this DxfPoint point) => new DwgPoint(point.X, point.Y, point.Z);

        public static Vector ToVector(this DxfPoint point) => new Vector(point.X, point.Y, point.Z);

        public static DxfPoint ToDxfPoint(this Vector v) => new DxfPoint(v.X, v.Y, v.Z);

        public static DwgVector ToDwgVector(this DxfVector vector) => new DwgVector(vector.X, vector.Y, vector.Z);

        private static TDimension ToDimension<TDimension>(DxfDimensionBase dim, DwgDrawing drawing, TDimension result) where TDimension : DwgDimension
        {
            var layer = drawing.EnsureLayer(dim.Layer, DwgColor.FromIndex(1), dim.LineTypeName);
            result.AnonymousBlock = drawing.EnsureBlock(layer, "*D0");
            result.DimensionStyle = drawing.EnsureDimensionStyle(dim.DimensionStyleName);
            return result.WithCommonProperties(dim);
        }

        public static DwgDimensionAligned ToDwgAlignedDimension(this DxfAlignedDimension aligned, DwgDrawing drawing)
        {
            return ToDimension(aligned, drawing, new DwgDimensionAligned()
            {
                FirstDefinitionPoint = aligned.DefinitionPoint1.ToDwgPoint(),
                SecondDefinitionPoint = aligned.DefinitionPoint2.ToDwgPoint(),
                ThirdDefinitionPoint = aligned.DefinitionPoint3.ToDwgPoint(),
                Text = aligned.Text,
                TextMidpoint = aligned.TextMidPoint.ToDwgPoint(),
            });
        }

        public static DwgArc ToDwgArc(this DxfArc arc)
        {
            return new DwgArc(arc.Center.ToDwgPoint(), arc.Radius, arc.StartAngle * DegreesToRadians, arc.EndAngle * DegreesToRadians)
            {
                Extrusion = arc.Normal.ToDwgVector(),
                Thickness = arc.Thickness,
            }.WithCommonProperties(arc);
        }

        public static DwgCircle ToDwgCircle(this DxfCircle circle)
        {
            return new DwgCircle(circle.Center.ToDwgPoint(), circle.Radius)
            {
                Extrusion = circle.Normal.ToDwgVector(),
                Thickness = circle.Thickness,
            }.WithCommonProperties(circle);
        }

        public static DwgEllipse ToDwgEllipse(this DxfEllipse ellipse)
        {
            return new DwgEllipse(ellipse.Center.ToDwgPoint(), ellipse.MajorAxis.ToDwgVector(), ellipse.MinorAxisRatio, ellipse.StartParameter, ellipse.EndParameter)
            {
                Extrusion = ellipse.Normal.ToDwgVector(),
            };
        }

        public static DwgInsert ToDwgInsert(this DxfInsert insert, DwgDrawing drawing)
        {
            return new DwgInsert(drawing.BlockHeaders[insert.Name])
            {
                Layer = drawing.EnsureLayer(insert.Layer, DwgColor.FromIndex(1), insert.LineTypeName),
                LineType = drawing.EnsureLineType(insert.LineTypeName),
                Location = insert.Location.ToDwgPoint(),
            }.WithCommonProperties(insert);
        }

        public static DwgLine ToDwgLine(this DxfLine line)
        {
            return new DwgLine(line.P1.ToDwgPoint(), line.P2.ToDwgPoint())
            {
                Extrusion = line.ExtrusionDirection.ToDwgVector(),
                Thickness = line.Thickness,
            }.WithCommonProperties(line);
        }

        public static DwgLocation ToDwgLocation(this DxfModelPoint modelPoint)
        {
            return new DwgLocation(modelPoint.Location.ToDwgPoint())
            {
                Thickness = modelPoint.Thickness,
            }.WithCommonProperties(modelPoint);
        }

        public static DwgLwPolyline ToDwgLwPolyline(this DxfLwPolyline lwpolyline)
        {
            return new DwgLwPolyline(lwpolyline.Vertices.Select(v => new DwgLwPolylineVertex(v.X, v.Y, v.StartingWidth, v.EndingWidth, v.Bulge)))
            {
                Thickness = lwpolyline.Thickness,
            }.WithCommonProperties(lwpolyline);
        }

        public static DwgEntity ToDwgPolyline(this DxfPolyline polyline)
        {
            if (polyline.Is3DPolyline)
            {
                return polyline.ToDwgPolyline3D();
            }
            else
            {
                return polyline.ToDwgPolyline2D();
            }
        }

        public static DwgDimensionOrdinate ToDwgRotatedDimension(this DxfRotatedDimension rotated, DwgDrawing drawing)
        {
            return ToDimension(rotated, drawing, new DwgDimensionOrdinate()
            {
                FirstDefinitionPoint = rotated.DefinitionPoint1.ToDwgPoint(),
                SecondDefinitionPoint = rotated.DefinitionPoint2.ToDwgPoint(),
                ThirdDefinitionPoint = rotated.DefinitionPoint3.ToDwgPoint(),
                Text = rotated.Text,
                TextMidpoint = rotated.TextMidPoint.ToDwgPoint(),
            });
        }

        private static DwgPolyline2D ToDwgPolyline2D(this DxfPolyline polyline)
        {
            return new DwgPolyline2D(polyline.Vertices.Select(v => new DwgVertex2D(v.Location.ToDwgPoint())))
            {
                Thickness = polyline.Thickness,
            }.WithCommonProperties(polyline);
        }

        private static DwgPolyline3D ToDwgPolyline3D(this DxfPolyline polyline)
        {
            return new DwgPolyline3D(polyline.Vertices.Select(v => new DwgVertex3D(v.Location.ToDwgPoint())))
            {
                // other properties?
            }.WithCommonProperties(polyline);
        }

        public static DwgSpline ToDwgSpline(this DxfSpline spline)
        {
            var result = new DwgSpline()
            {
                Degree = spline.DegreeOfCurve,
                ControlTolerance = spline.ControlPointTolerance,
                FitTolerance = spline.FitTolerance,
                KnotTolerance = spline.KnotTolerance,
            }.WithCommonProperties(spline);
            result.ControlPoints.AddRange(spline.ControlPoints.Select(cp => new DwgControlPoint(cp.Point.ToDwgPoint(), cp.Weight)));
            result.FitPoints.AddRange(spline.FitPoints.Select(fp => fp.ToDwgPoint()));
            result.KnotValues.AddRange(spline.KnotValues);
            return result;
        }

        public static DwgText ToDwgText(this DxfText text)
        {
            return new DwgText(text.Value)
            {
                InsertionPoint = text.Location.ToDwgPoint(),
                Elevation = text.Elevation,
                Extrusion = text.Normal.ToDwgVector(),
                Height = text.TextHeight,
                HorizontalAlignment = (DwgHorizontalTextJustification)text.HorizontalTextJustification,
                VerticalAlignment = (DwgVerticalTextJustification)text.VerticalTextJustification,
                RotationAngle = text.Rotation * DegreesToRadians,
            }.WithCommonProperties(text);
        }

        public static TDwgEntity WithCommonProperties<TDwgEntity>(this TDwgEntity entity, DxfEntity parent) where TDwgEntity : DwgEntity
        {
            // layer and linetype are handled elsewhere
            entity.Color = parent.Color.ToDwgColor();
            entity.LineTypeScale = parent.LineTypeScale;
            return entity;
        }

        public static DimensionSettings ToDimensionSettings(this DxfDimStyle dimStyle)
        {
            return new DimensionSettings(
                textHeight: dimStyle.DimensioningTextHeight,
                extensionLineOffset: dimStyle.DimensionExtensionLineOffset,
                extensionLineExtension: dimStyle.DimensionExtensionLineExtension,
                dimensionLineGap: dimStyle.DimensionLineGap,
                arrowSize: dimStyle.DimensioningArrowSize);
        }

        public static UnitFormat ToUnitFormat(this DxfUnitFormat unitFormat)
        {
            switch (unitFormat)
            {
                case DxfUnitFormat.Architectural:
                case DxfUnitFormat.ArchitecturalStacked:
                    return UnitFormat.Architectural;
                case DxfUnitFormat.Decimal:
                case DxfUnitFormat.Engineering:
                case DxfUnitFormat.Scientific:
                    return UnitFormat.Decimal;
                case DxfUnitFormat.Fractional:
                case DxfUnitFormat.FractionalStacked:
                    return UnitFormat.Fractional;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unitFormat));
            }
        }

        public static DrawingUnits ToDrawingUnits(this DxfDrawingUnits drawingUnits)
        {
            switch (drawingUnits)
            {
                case DxfDrawingUnits.English:
                    return DrawingUnits.English;
                case DxfDrawingUnits.Metric:
                    return DrawingUnits.Metric;
                default:
                    throw new ArgumentOutOfRangeException(nameof(drawingUnits));
            }
        }
    }
}
