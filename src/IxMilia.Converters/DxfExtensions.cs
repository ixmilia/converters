using System;
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

        public static DwgVector ToDwgVector(this DxfVector vector) => new DwgVector(vector.X, vector.Y, vector.Z);

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

        public static DwgLine ToDwgLine(this DxfLine line)
        {
            return new DwgLine(line.P1.ToDwgPoint(), line.P2.ToDwgPoint())
            {
                Extrusion = line.ExtrusionDirection.ToDwgVector(),
                Thickness = line.Thickness,
            }.WithCommonProperties(line);
        }

        public static TDwgEntity WithCommonProperties<TDwgEntity>(this TDwgEntity entity, DxfEntity parent) where TDwgEntity : DwgEntity
        {
            // layer and linetype are handled elsewhere
            entity.Color = parent.Color.ToDwgColor();
            entity.LineTypeScale = parent.LineTypeScale;
            return entity;
        }
    }
}
