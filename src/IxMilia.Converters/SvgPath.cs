﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Converters
{
    public class SvgPath
    {
        public List<SvgPathSegment> Segments { get; }

        public SvgPath(IEnumerable<SvgPathSegment> segments)
        {
            Segments = segments.ToList();
        }

        public override string ToString()
        {
            return string.Join(" ", Segments);
        }

        public static SvgPath FromEllipse(double centerX, double centerY, double majorAxisX, double majorAxisY, double minorAxisRatio, double startAngle, double endAngle)
        {
            // large arc and counterclockwise computations all rely on the end angle being greater than the start
            while (endAngle < startAngle)
            {
                endAngle += Math.PI * 2.0;
            }

            var axisAngle = Math.Atan2(majorAxisY, majorAxisY);
            var majorAxisLength = Math.Sqrt(majorAxisX * majorAxisX + majorAxisY * majorAxisY);
            var minorAxisLength = majorAxisLength * minorAxisRatio;

            var startSin = Math.Sin(startAngle);
            var startCos = Math.Cos(startAngle);
            var startX = centerX + startCos * majorAxisLength;
            var startY = centerY + startSin * minorAxisLength;

            var endSin = Math.Sin(endAngle);
            var endCos = Math.Cos(endAngle);
            var endX = centerX + endCos * majorAxisLength;
            var endY = centerY + endSin * minorAxisLength;

            var enclosedAngle = endAngle - startAngle;
            var isLargeArc = (endAngle - startAngle) > Math.PI;
            var isCounterClockwise = endAngle > startAngle;

            var segments = new List<SvgPathSegment>();
            segments.Add(new SvgMoveToPath(startX, startY));
            var oneDegreeInRadians = Math.PI / 180.0;
            if (Math.Abs(Math.PI - enclosedAngle) <= oneDegreeInRadians)
            {
                // really close to a semicircle; split into to half arcs to avoid rendering artifacts
                var midAngle = (startAngle + endAngle) / 2.0;
                var midSin = Math.Sin(midAngle);
                var midCos = Math.Cos(midAngle);
                var midX = centerX + midCos * majorAxisLength;
                var midY = centerY + midSin * minorAxisLength;
                segments.Add(new SvgArcToPath(majorAxisLength, minorAxisLength, axisAngle, false, isCounterClockwise, midX, midY));
                segments.Add(new SvgArcToPath(majorAxisLength, minorAxisLength, axisAngle, false, isCounterClockwise, endX, endY));
            }
            else
            {
                // can be contained by just one arc
                segments.Add(new SvgArcToPath(majorAxisLength, minorAxisLength, axisAngle, isLargeArc, isCounterClockwise, endX, endY));
            }

            return new SvgPath(segments);
        }
    }

    public abstract class SvgPathSegment
    {
    }

    public class SvgMoveToPath : SvgPathSegment
    {
        public double LocationX { get; }
        public double LocationY { get; }

        public SvgMoveToPath(double locationX, double locationY)
        {
            LocationX = locationX;
            LocationY = locationY;
        }

        public override string ToString()
        {
            return string.Join(" ", new[]
            {
                "M", // move absolute
                LocationX.ToDisplayString(),
                LocationY.ToDisplayString()
            });
        }
    }

    public class SvgLineToPath : SvgPathSegment
    {
        public double LocationX { get; }
        public double LocationY { get; }

        public SvgLineToPath(double locationX, double locationY)
        {
            LocationX = locationX;
            LocationY = locationY;
        }

        public override string ToString()
        {
            return string.Join(" ", new[]
            {
                "L", // line absolute
                LocationX.ToDisplayString(),
                LocationY.ToDisplayString()
            });
        }
    }

    public class SvgArcToPath : SvgPathSegment
    {
        public double RadiusX { get; }
        public double RadiusY { get; }
        public double XAxisRotation { get; }
        public bool IsLargeArc { get; }
        public bool IsCounterClockwiseSweep { get; }
        public double EndPointX { get; }
        public double EndPointY { get; }

        public SvgArcToPath(double radiusX, double radiusY, double xAxisRotation, bool isLargeArc, bool isCounterClockwiseSweep, double endPointX, double endPointY)
        {
            RadiusX = radiusX;
            RadiusY = radiusY;
            XAxisRotation = xAxisRotation;
            IsLargeArc = isLargeArc;
            IsCounterClockwiseSweep = isCounterClockwiseSweep;
            EndPointX = endPointX;
            EndPointY = endPointY;
        }

        public override string ToString()
        {
            return string.Join(" ", new object[]
            {
                "A", // arc absolute
                RadiusX.ToDisplayString(),
                RadiusY.ToDisplayString(),
                XAxisRotation.ToDisplayString(),
                IsLargeArc ? 1 : 0,
                IsCounterClockwiseSweep ? 1 : 0,
                EndPointX.ToDisplayString(),
                EndPointY.ToDisplayString()
            });
        }
    }

    public class SvgCubicBezierToPath : SvgPathSegment
    {
        public double ControlPoint1X { get; }
        public double ControlPoint1Y { get; }
        public double ControlPoint2X { get; }
        public double ControlPoint2Y { get; }
        public double EndLocationX { get; }
        public double EndLocationY { get; }

        public SvgCubicBezierToPath(double controlPoint1X, double controlPoint1Y, double controlPoint2X, double controlPoint2Y, double endLocationX, double endLocationY)
        {
            ControlPoint1X = controlPoint1X;
            ControlPoint1Y = controlPoint1Y;
            ControlPoint2X = controlPoint2X;
            ControlPoint2Y = controlPoint2Y;
            EndLocationX = endLocationX;
            EndLocationY = endLocationY;
        }

        public override string ToString()
        {
            return string.Format("C {0} {1}, {2} {3}, {4} {5}",
                ControlPoint1X.ToDisplayString(),
                ControlPoint1Y.ToDisplayString(),
                ControlPoint2X.ToDisplayString(),
                ControlPoint2Y.ToDisplayString(),
                EndLocationX.ToDisplayString(),
                EndLocationY.ToDisplayString());
        }
    }
}
