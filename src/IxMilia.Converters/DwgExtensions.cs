using System;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace IxMilia.Converters
{
    public static class DwgExtensions
    {
        public const double RadiansToDegrees = 180.0 / Math.PI;

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

        public static DxfLine ToDxfLine(this DwgLine line)
        {
            return new DxfLine(line.P1.ToDxfPoint(), line.P2.ToDxfPoint())
            {
                ExtrusionDirection = line.Extrusion.ToDxfVector(),
                Thickness = line.Thickness,
            }.WithCommonProperties(line);
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
            if (drawing.LineTypes.TryGetValue(lineTypeName, out var lineType))
            {
                return lineType;
            }

            return drawing.CurrentEntityLineType;
        }

        public static DwgLayer LayerOrCurrent(this DwgDrawing drawing, string layerName)
        {
            if (drawing.Layers.TryGetValue(layerName, out var layer))
            {
                return layer;
            }

            return drawing.CurrentLayer;
        }
    }
}
