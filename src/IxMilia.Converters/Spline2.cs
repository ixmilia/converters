using System;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Converters
{
    public struct SplinePoint2
    {
        public double X;
        public double Y;

        public SplinePoint2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"({X.ToDisplayString()}, {Y.ToDisplayString()})";
        }

        public static bool operator ==(SplinePoint2 a, SplinePoint2 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(SplinePoint2 a, SplinePoint2 b)
        {
            return !(a == b);
        }

        public static SplinePoint2 operator +(SplinePoint2 a, SplinePoint2 b)
        {
            return new SplinePoint2(a.X + b.X, a.Y + b.Y);
        }

        public static SplinePoint2 operator *(SplinePoint2 p, double scalar)
        {
            return new SplinePoint2(p.X * scalar, p.Y * scalar);
        }

        public override bool Equals(object? obj)
        {
            return obj is SplinePoint2 point &&
                this == point;
        }

        public override int GetHashCode()
        {
            int hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }
    }

    public struct Bezier2
    {
        public SplinePoint2 Start;
        public SplinePoint2 Control1;
        public SplinePoint2 Control2;
        public SplinePoint2 End;

        public Bezier2(SplinePoint2 start, SplinePoint2 control1, SplinePoint2 control2, SplinePoint2 end)
        {
            Start = start;
            Control1 = control1;
            Control2 = control2;
            End = end;
        }
    }

    public class Spline2
    {
        // Measured in drawing units
        public const double DefaultApproxTolerance = 0.01;

        private List<SplinePoint2> _controlPoints;
        private List<double> _knotValues;

        public int Degree { get; }
        public IEnumerable<SplinePoint2> ControlPoints => _controlPoints;
        public IEnumerable<double> KnotValues => _knotValues;

        public Spline2(int degree, IEnumerable<SplinePoint2> controlPoints, IEnumerable<double> knotValues)
        {
            if (degree < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(degree), "Degree must be greater than or equal to 2.");
            }

            if (controlPoints == null)
            {
                throw new ArgumentNullException(nameof(controlPoints));
            }

            if (knotValues == null)
            {
                throw new ArgumentNullException(nameof(knotValues));
            }

            Degree = degree;
            _controlPoints = controlPoints.ToList();
            _knotValues = knotValues.ToList();

            ValidateValues();
        }

        private void ValidateValues()
        {
            if (_controlPoints.Count < Degree + 1)
            {
                throw new InvalidOperationException("There must be at least one more control point than the degree of the curve.");
            }

            if (_knotValues.Count < 1)
            {
                throw new InvalidOperationException("Minimum knot value count is 1.");
            }

            if (_knotValues.Count != _controlPoints.Count + Degree + 1)
            {
                throw new InvalidOperationException("Invalid combination of knot value count, control point count, and degree.");
            }

            // knot values must be ascending
            var lastKnotValue = _knotValues[0];
            foreach (var kv in _knotValues.Skip(1))
            {
                if (kv < lastKnotValue)
                {
                    throw new InvalidOperationException($"Knot values must be ascending.  Found values {lastKnotValue} -> {kv}.");
                }

                lastKnotValue = kv;
            }
        }

        public void InsertKnot(double t)
        {
            // find the knot span that contains t
            var knotInsertionIndex = _knotValues.Count(k => k < t);

            // replace points at index [k-p, k]
            // first new point is _controlPoints[index - degree]
            var lowerIndex = knotInsertionIndex - Degree;
            var upperIndex = knotInsertionIndex;
            var pointsToInsert = new List<SplinePoint2>();
            for (int i = lowerIndex; i < upperIndex; i++)
            {
                var a = (t - _knotValues[i]) / (_knotValues[i + Degree] - _knotValues[i]);
                var q = _controlPoints[i - 1] * (1.0 - a) + _controlPoints[i] * a;
                pointsToInsert.Add(q);
            }

            // insert new values
            _knotValues.Insert(knotInsertionIndex, t);
            var newControlPoints = new List<SplinePoint2>();
            newControlPoints.AddRange(_controlPoints.Take(lowerIndex));
            newControlPoints.AddRange(pointsToInsert);
            newControlPoints.AddRange(_controlPoints.Skip(lowerIndex + pointsToInsert.Count - 1));
            _controlPoints = newControlPoints;
            ValidateValues();
        }

        public IList<Bezier2> ToBeziers()
        {
            return ToBeziers(DefaultApproxTolerance);
        }

        public IList<Bezier2> ToBeziers(double approximationTolerance)
        {
            if (double.IsNaN(approximationTolerance) || approximationTolerance <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(approximationTolerance), "Tolerance must not be stupid");
            }

            var expectedIdenticalKnots = Degree + 1;
            var result = new Spline2(Degree, ControlPoints, KnotValues);

            for (int offset = 0; ; offset++)
            {
                // get next set of values
                var values = result.KnotValues.Skip(offset * expectedIdenticalKnots).Take(expectedIdenticalKnots).ToList();

                if (values.Count == 0 && result.KnotValues.Count() % expectedIdenticalKnots == 0)
                {
                    // done
                    break;
                }

                var expectedValue = values[0];
                int missingValueCount;
                if (values.Count < expectedIdenticalKnots)
                {
                    // not enough values
                    missingValueCount = expectedIdenticalKnots - values.Count;
                }
                else if (values.Count < expectedIdenticalKnots || values.Any(v => v != expectedValue))
                {
                    // not all the same
                    missingValueCount = expectedIdenticalKnots - values.Count(v => v == expectedValue);
                }
                else
                {
                    missingValueCount = 0;
                }

                for (int i = 0; i < missingValueCount; i++)
                {
                    result.InsertKnot(expectedValue);
                }
            }

            var points = result.ControlPoints.ToList();
            var curves = new List<Bezier2>();
            for (int startIndex = 0; startIndex < points.Count; startIndex += expectedIdenticalKnots)
            {
                if (Degree == 2)
                {
                    var start = points[startIndex];
                    var control = points[startIndex + 1];
                    var end = points[startIndex + 2];
                    curves.Add(new Bezier2(start,
                        start * (1.0 / 3.0) + control * (2.0 / 3.0),
                        end * (1.0 / 3.0) + control * (2.0 / 3.0), end));
                }
                else if (Degree == 3)
                {
                    curves.Add(new Bezier2(points[startIndex], points[startIndex + 1], points[startIndex + 2], points[startIndex + 3]));
                }
                else
                {
                    var span = points.GetRange(startIndex, expectedIdenticalKnots).ToArray();
                    if (span.Any(p => double.IsNaN(p.X) || double.IsInfinity(p.X) || double.IsNaN(p.Y) || double.IsInfinity(p.Y)))
                    {
                        throw new InvalidOperationException("Spline control points must be finite.");
                    }

                    ApproximateBezier(span, approximationTolerance * approximationTolerance, curves, 0);
                }
            }

            return curves;
        }

        private static void ApproximateBezier(SplinePoint2[] points, double toleranceSquared, List<Bezier2> curves, int depth)
        {
            var degree = points.Length - 1;
            var start = points[0];
            var end = points[degree];
            var derivativeScale = degree / 3.0;
            var cubic = new Bezier2(start,
                start * (1.0 - derivativeScale) + points[1] * derivativeScale,
                end * (1.0 - derivativeScale) + points[degree - 1] * derivativeScale, end);

            // Elevate the cubic to the original degree. The maximum distance between corresponding control points bounds the error for the entire curve
            var elevated = new[] { cubic.Start, cubic.Control1, cubic.Control2, cubic.End };
            while (elevated.Length < points.Length)
            {
                var next = new SplinePoint2[elevated.Length + 1];
                next[0] = elevated[0];
                next[next.Length - 1] = elevated[elevated.Length - 1];
                for (int i = 1; i < elevated.Length; i++)
                {
                    var alpha = (double)i / elevated.Length;
                    next[i] = elevated[i - 1] * alpha + elevated[i] * (1.0 - alpha);
                }

                elevated = next;
            }

            var withinTolerance = true;
            for (int i = 0; i < points.Length; i++)
            {
                var dx = points[i].X - elevated[i].X;
                var dy = points[i].Y - elevated[i].Y;
                if (dx * dx + dy * dy > toleranceSquared)
                {
                    withinTolerance = false;
                    break;
                }
            }

            if (withinTolerance)
            {
                if (curves.Count >= 100000)
                {
                    throw new InvalidOperationException("Spline too big.");
                }

                curves.Add(cubic);
                return;
            }

            if (depth >= 32)
            {
                throw new InvalidOperationException("Spline couldn't reach tolerance.");
            }

            // Split at t=0.5 using de Castelau, keep shared endpoint
            var work = (SplinePoint2[])points.Clone();
            var left = new SplinePoint2[points.Length];
            var right = new SplinePoint2[points.Length];
            left[0] = start;
            right[degree] = end;
            for (int level = 1; level <= degree; level++)
            {
                for (int i = 0; i <= degree - level; i++)
                {
                    work[i] = work[i] * 0.5 + work[i + 1] * 0.5;
                }

                left[level] = work[0];
                right[degree - level] = work[degree - level];
            }

            ApproximateBezier(left, toleranceSquared, curves, depth + 1);
            ApproximateBezier(right, toleranceSquared, curves, depth + 1);
        }
    }
}
