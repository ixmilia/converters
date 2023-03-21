using System;

namespace IxMilia.Converters
{
    public static class DimensionExtensions
    {
        public static (Vector dimensionLineLocation1, Vector dimensionLineLocation2, Vector textMidPoint) GetDimensionLineAndTextLocation(Vector firstPoint, Vector secondPoint, Vector selectedDimensionLineLocation, bool isAligned)
        {
            Vector dimensionLineLocation1;
            Vector dimensionLineLocation2;

            if (isAligned)
            {
                // we'll be rotated a bit
                dimensionLineLocation1 = GetRealDimensionLineLocation(firstPoint, secondPoint, selectedDimensionLineLocation);
                var measurementVector = secondPoint - firstPoint;
                dimensionLineLocation2 = dimensionLineLocation1 + measurementVector;
            }
            else
            {
                // aligned on either X or Y axis
                var selectionVector = selectedDimensionLineLocation - secondPoint;
                var dx = Math.Abs(selectionVector.X);
                var dy = Math.Abs(selectionVector.Y);
                if (dx > dy)
                {
                    // moving in x axis, dimension is y axis
                    dimensionLineLocation1 = new Vector(selectedDimensionLineLocation.X, firstPoint.Y, 0.0);
                    dimensionLineLocation2 = new Vector(selectedDimensionLineLocation.X, secondPoint.Y, 0.0);
                }
                else
                {
                    // moving in y axis, dimension is in x axis
                    dimensionLineLocation1 = new Vector(firstPoint.X, selectedDimensionLineLocation.Y, 0.0);
                    dimensionLineLocation2 = new Vector(secondPoint.X, selectedDimensionLineLocation.Y, 0.0);
                }
            }

            var textMidPoint = (dimensionLineLocation1 + dimensionLineLocation2) / 2.0;
            return (dimensionLineLocation1, dimensionLineLocation2, textMidPoint);
        }

        private static Vector GetRealDimensionLineLocation(Vector firstPoint, Vector secondPoint, Vector selectedDimensionLineLocation)
        {
            var dimensionVector = secondPoint - firstPoint;
            var locationClosestPoint = ClosestPoint(firstPoint, secondPoint, selectedDimensionLineLocation, withinBounds: false);
            var offsetDistance = (selectedDimensionLineLocation - locationClosestPoint).Length;
            var dimensionExtensionVector = dimensionVector.Cross(new Vector(0.0, 0.0, 1.0)).Normalize();
            if ((selectedDimensionLineLocation - firstPoint).Dot(dimensionExtensionVector) < 0.0)
            {
                // make sure we're extending the right direction
                dimensionExtensionVector *= -1.0;
            }

            var dimensionLineLocation = firstPoint + dimensionExtensionVector * offsetDistance;
            return dimensionLineLocation;
        }

        public static Vector ClosestPoint(Vector lineP1, Vector lineP2, Vector point, bool withinBounds = true)
        {
            var epsilon = 1.0E-12;
            var v = lineP1;
            var w = lineP2;
            var p = point;
            var wv = w - v;
            var l2 = wv.LengthSquared;
            if (Math.Abs(l2) < epsilon)
                return v;
            var t = (p - v).Dot(wv) / l2;
            if (withinBounds)
            {
                t = Math.Max(t, 0.0 - epsilon);
                t = Math.Min(t, 1.0 + epsilon);
            }

            var result = v + (wv) * t;
            return result;
        }

        public static string GenerateLinearDimensionText(double value, DrawingUnits drawingUnits, UnitFormat unitFormat, int unitPrecision)
        {
            var prefix = Math.Sign(value) < 0 ? "-" : string.Empty;
            value = Math.Abs(value);
            var formatted = unitFormat switch
            {
                UnitFormat.Architectural => FormatArchitectural(value, unitPrecision),
                UnitFormat.Fractional => FormatFractional(value, unitPrecision),
                UnitFormat.Decimal => FormatScalar(value, unitPrecision),
                _ => throw new ArgumentOutOfRangeException(nameof(unitFormat))
            };

            return prefix + formatted;
        }

        private static string FormatScalar(double value, int unitPrecision)
        {
            var formatTail = new string('0', unitPrecision);
            var formatted = value.ToString($"0.{formatTail}");
            return formatted;
        }

        private static string FormatArchitectural(double value, int unitPrecision)
        {
            var feet = (int)value / 12;
            var inches = value - (feet * 12.0);
            var fractional = FormatFractional(inches, unitPrecision);
            return $"{feet}'{fractional}";
        }

        private static string FormatFractional(double value, int unitPrecision)
        {
            if (value < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be positive");
            }

            var roundingAmount = Math.Pow(2.0, -(unitPrecision + 1));
            var normalizedValue = value + roundingAmount;
            var whole = (int)normalizedValue;
            var fractionalPart = normalizedValue - whole;

            var denominator = (int)Math.Pow(2.0, unitPrecision);
            var numerator = (int)(fractionalPart * denominator);

            if (numerator == denominator)
            {
                numerator = denominator = 0;
            }

            ReduceFraction(ref numerator, ref denominator);
            var fractional = numerator != 0
                ? $"-{numerator}/{denominator}"
                : string.Empty;

            var formatted = $"{whole}{fractional}\"";
            return formatted;
        }

        private static void ReduceFraction(ref int numerator, ref int denominator)
        {
            if (numerator == 0 || denominator == 0)
                return;
            while (numerator % 2 == 0 && denominator % 2 == 0)
            {
                numerator /= 2;
                denominator /= 2;
            }
        }
    }
}
