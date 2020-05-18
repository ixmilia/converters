using System;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace IxMilia.Converters
{
    public static class DxfExtensions
    {
        public static DxfPoint GetPointFromAngle(this DxfCircle circle, double angleInDegrees)
        {
            var angleInRadians = angleInDegrees * Math.PI / 180.0;
            var sin = Math.Sin(angleInRadians);
            var cos = Math.Cos(angleInRadians);
            return new DxfPoint(cos * circle.Radius, sin * circle.Radius, 0.0) + circle.Center;
        }
    }
}
