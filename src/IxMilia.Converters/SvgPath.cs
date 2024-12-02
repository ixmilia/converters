using IxMilia.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using static IxMilia.Dxf.Entities.DxfHatch;

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

        public static SvgPath FromEllipse2(double centerX, double centerY, double majorAxisX, double majorAxisY, double minorAxisRatio, double startAngle, double endAngle, bool firstIsMove = true, int angleResolutionInDegree = 10)
        {
            while (endAngle < startAngle)
            {
                endAngle += Math.PI * 2.0;
            }
            MathExtensions.CalcXAxisRotation(majorAxisX, majorAxisY, out var axisAngle, true);

            var majorAxisLength = MathExtensions.Magnitude(majorAxisX, majorAxisY);
            var minorAxisLength = majorAxisLength * minorAxisRatio;

            // calc line points
            double angleResolution = ((double)angleResolutionInDegree).ToRadian();
            double angleSpan = Math.Abs(endAngle - startAngle);
            int calculatedPoints = (int)Math.Ceiling(angleSpan / angleResolution);

            List<SvgEllipseLineToPath> points = new List<SvgEllipseLineToPath>();
            for (int i = 0; i < calculatedPoints; i++)
            {
                var angle = startAngle + i * angleResolution;
                var startX = centerX + Math.Cos(angle) * majorAxisLength;
                var startY = centerY + Math.Sin(angle) * minorAxisLength;
                points.Add(new SvgEllipseLineToPath(angle, startX, startY));
            }

            //add end point
            var endX = centerX + Math.Cos(endAngle) * majorAxisLength;
            var endY = centerY + Math.Sin(endAngle) * minorAxisLength;
            points.Add(new SvgEllipseLineToPath(endAngle, endX, endY));


            // transform points to given axis angle of the majorAxis
            List<SvgEllipseLineToPath> transfPoints = new List<SvgEllipseLineToPath>();
            for (int i = 0; i < points.Count; i++)
            {
                transfPoints.Add(points[i].TransformAngle(centerX, centerY, axisAngle));
            }


            // in some cases the ellipse is a part of a path 
            var segments = new List<SvgPathSegment>();
            if (firstIsMove)
            {
                segments.Add(new SvgMoveToPath(transfPoints.First().LocationX, transfPoints.First().LocationY));
                segments.AddRange(transfPoints.Skip(1));
            }
            else
            {
                segments.AddRange(transfPoints);
            }

            return new SvgPath(segments);
        }

        public static SvgPath FromHatch(IList<BoundaryPathEdgeBase> edges)
        {
            List<SvgPathSegment> transfPoints = new List<SvgPathSegment>();

            for (int i = 0; i < edges.Count(); i++)
            {
                switch (edges[i])
                {
                    case LineBoundaryPathEdge lineBoundaryPathEdge:
                        // Code for LineBoundaryPathEdge case
                        transfPoints.Add(new SvgLineToPath(lineBoundaryPathEdge.StartPoint.X, lineBoundaryPathEdge.StartPoint.Y));
                        transfPoints.Add(new SvgLineToPath(lineBoundaryPathEdge.EndPoint.X, lineBoundaryPathEdge.EndPoint.Y));
                        break;

                    case SplineBoundaryPathEdge splineBoundaryPathEdge:
                        // Code for OtherType case
                        foreach (var controlPoint in splineBoundaryPathEdge.ControlPoints)
                        {
                            transfPoints.Add(new SvgLineToPath(controlPoint.Point.X, controlPoint.Point.Y));
                        }

                        break;

                    case EllipticArcBoundaryPathEdge ellipticArcBoundaryPathEdge:
                        // Code for OtherType case
                        //throw new NotImplementedException();
                        var ellips = FromEllipse2(ellipticArcBoundaryPathEdge.Center.X, ellipticArcBoundaryPathEdge.Center.Y,
                                            ellipticArcBoundaryPathEdge.MajorAxis.X, ellipticArcBoundaryPathEdge.MajorAxis.Y,
                                            ellipticArcBoundaryPathEdge.MinorAxisRatio,
                                            ellipticArcBoundaryPathEdge.StartAngle.ToRadian(), ellipticArcBoundaryPathEdge.EndAngle.ToRadian(), false);

                        transfPoints.AddRange(ellips.Segments);

                        break;

                    case BoundaryPathEdgeBase boundaryPathEdgeBase:
                        // Code for OtherType case
                        // openpoint - implement case
                        //throw new NotImplementedException();
                        break;

                    default:
                        // Default case
                        // openpoint - implement case
                        //throw new NotImplementedException();

                        break;
                }
            }

            var segments = new List<SvgPathSegment>();
            if (transfPoints.Count > 0)
            {

                if (transfPoints.First().GetType() == typeof(SvgLineToPath))
                {
                    var temp = (SvgLineToPath)transfPoints.First();
                    segments.Add(new SvgMoveToPath(temp.LocationX, temp.LocationY));
                }
                else if (transfPoints.First().GetType() == typeof(SvgEllipseLineToPath))
                {
                    var temp = (SvgEllipseLineToPath)transfPoints.First();
                    segments.Add(new SvgMoveToPath(temp.LocationX, temp.LocationY));
                }
                else
                {
                    throw new NotImplementedException();
                }
                segments.AddRange(transfPoints.Skip(1));
            }
            else
            {
                bool stop = false;
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

    public class SvgEllipseLineToPath : SvgLineToPath
    {
        public double AngleRadian { get; }
        public double AngleDegree { get; }
        public SvgEllipseLineToPath(double angleRadian, double locationX, double locationY) : base(locationX, locationY)
        {
            AngleRadian = angleRadian;
            AngleDegree = angleRadian.ToDegree();
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
