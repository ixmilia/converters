using System;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Converters
{
    public class LinearDimensionProperties
    {
        public string DisplayText { get; }
        public double DimensionLength { get; }
        public double DimensionLineAngle { get; }
        public Vector DimensionLineStart { get; }
        public Vector DimensionLineEnd { get; }
        public Vector TextLocation { get; }
        public (Vector Start, Vector End)[] DimensionLineSegments { get; }
        public (Vector P1, Vector P2, Vector P3)[] DimensionTriangles { get; }


        public LinearDimensionProperties(
            string displayText,
            double dimensionLength,
            double dimensionLineAngle,
            Vector dimensionLineStart,
            Vector dimensionLineEnd,
            Vector textLocation,
            (Vector start, Vector end)[] dimensionLineSegments,
            (Vector p1, Vector p2, Vector p3)[] dimensionTriangles)
        {
            DisplayText = displayText;
            DimensionLength = dimensionLength;
            DimensionLineAngle = dimensionLineAngle;
            DimensionLineStart = dimensionLineStart;
            DimensionLineEnd = dimensionLineEnd;
            TextLocation = textLocation;
            DimensionLineSegments = dimensionLineSegments;
            DimensionTriangles = dimensionTriangles;
        }

        public static LinearDimensionProperties BuildFromValues(
            Vector definitionPoint1,
            Vector definitionPoint2,
            Vector selectedDimensionLineLocation,
            bool isAligned,
            DrawingUnits drawingUnits,
            UnitFormat unitFormat,
            int unitPrecision,
            DimensionSettings dimensionSettings,
            Func<string, double> getTextWidth)
        {
            // compute once with unknown text and width...
            var dimensionProperties = BuildFromValues(
                definitionPoint1,
                definitionPoint2,
                selectedDimensionLineLocation,
                isAligned,
                null,
                0.0,
                dimensionSettings);

            // ...generate the text...
            var displayText = DimensionExtensions.GenerateLinearDimensionText(
                dimensionProperties.DimensionLength,
                drawingUnits,
                unitFormat,
                unitPrecision);

            // ...and re-create with known width
            var textWidth = getTextWidth(displayText);
            var finalDimensionProperties = BuildFromValues(
                definitionPoint1,
                definitionPoint2,
                selectedDimensionLineLocation,
                isAligned,
                displayText,
                textWidth,
                dimensionSettings);
            return finalDimensionProperties;
        }

        public static LinearDimensionProperties BuildFromValues(
            Vector definitionPoint1,
            Vector definitionPoint2,
            Vector selectedDimensionLineLocation,
            bool isAligned,
            string displayText,
            double textWidth,
            DimensionSettings dimensionSettings)
        {
            var tickSize = dimensionSettings.TickSize;
            var arrowSize = tickSize == 0.0 ? dimensionSettings.ArrowSize : 0.0;

            var (dimensionLineLocation1, dimensionLineLocation2, textMidPoint) = DimensionExtensions.GetDimensionLineAndTextLocation(definitionPoint1, definitionPoint2, selectedDimensionLineLocation, isAligned: isAligned);
            var dimensionLine1Vector = (dimensionLineLocation1 - definitionPoint1).Normalize();
            var dimensionLine2Vector = (dimensionLineLocation2 - definitionPoint2).Normalize();
            var dimensionLine1Start = definitionPoint1 + (dimensionLine1Vector * dimensionSettings.ExtensionLineOffset);
            var dimensionLine1End = dimensionLineLocation1 + (dimensionLine1Vector * dimensionSettings.ExtensionLineExtension);
            var dimensionLine2Start = definitionPoint2 + (dimensionLine2Vector * dimensionSettings.ExtensionLineOffset);
            var dimensionLine2End = dimensionLineLocation2 + (dimensionLine2Vector * dimensionSettings.ExtensionLineExtension);

            var tickHalfVector = dimensionLine1Vector.RotateAboutOrigin(-Math.PI / 4.0).Normalize() * tickSize * 0.5;
            var tickLine1Start = dimensionLineLocation1 - tickHalfVector;
            var tickLine1End = dimensionLineLocation1 + tickHalfVector;
            var tickLine2Start = dimensionLineLocation2 - tickHalfVector;
            var tickLine2End = dimensionLineLocation2 + tickHalfVector;

            var dimensionMeasurementVector = (dimensionLineLocation2 - dimensionLineLocation1);
            var dimensionLineAngle = Math.Atan2(dimensionMeasurementVector.Y, dimensionMeasurementVector.X);
            var correctedDimensionLineAngle = CorrectTextRotationAngle(dimensionLineAngle);
            var correctedDimensionMeasurementVector = new Vector(Math.Cos(correctedDimensionLineAngle), Math.Sin(correctedDimensionLineAngle), 0.0);

            var textGapWidth = textWidth == 0.0 ? 0.0 : textWidth + (dimensionSettings.DimensionLineGap * 2.0);
            var normalizedDimensionMeasurementVector = dimensionMeasurementVector.Normalize();
            var dimensionLineLocationOffset1 = dimensionLineLocation1 + (normalizedDimensionMeasurementVector * arrowSize);
            var dimensionLineLocationOffset2 = dimensionLineLocation2 - (normalizedDimensionMeasurementVector * arrowSize);
            var textGapPoint1 = textMidPoint - (normalizedDimensionMeasurementVector * (textGapWidth / 2.0));
            var textGapPoint2 = textMidPoint + (normalizedDimensionMeasurementVector * (textGapWidth / 2.0));

            var textLeft = textMidPoint - (correctedDimensionMeasurementVector * textWidth / 2.0);
            var textUpAngle = correctedDimensionLineAngle + Math.PI / 2.0; // add 90*
            var textUpVector = new Vector(Math.Cos(textUpAngle), Math.Sin(textUpAngle), 0.0);
            var candidateTextBottomLocation = textLeft - (textUpVector * dimensionSettings.TextHeight * 0.5);
            var candidateTextTopLocation = textLeft + (textUpVector * dimensionSettings.TextHeight * 0.5);
            var topBottomDiff = (candidateTextTopLocation - textLeft).Length - (candidateTextBottomLocation - textLeft).Length;
            var textLocation = topBottomDiff > 1E-6 ? candidateTextTopLocation : candidateTextBottomLocation;

            var triangleWidthFactor = 1.0 / 3.0;
            var idealTriangle1P1 = new Vector(0.0, 0.0, 0.0);
            var idealTriangle1P2 = new Vector(1.0, -triangleWidthFactor / 2.0, 0.0);
            var idealTriangle1P3 = new Vector(1.0, triangleWidthFactor / 2.0, 0.0);
            var idealTriangle2P1 = idealTriangle1P1 * -1.0;
            var idealTriangle2P2 = idealTriangle1P3 * -1.0; // swapping p2/p3 to simulate 180* rotation
            var idealTriangle2P3 = idealTriangle1P2 * -1.0;

            var angleCos = Math.Cos(dimensionLineAngle);
            var angleSin = Math.Sin(dimensionLineAngle);
            var triangle1P1 = idealTriangle1P1.RotateAboutOrigin(dimensionLineAngle) * arrowSize + dimensionLineLocation1;
            var triangle1P2 = idealTriangle1P2.RotateAboutOrigin(dimensionLineAngle) * arrowSize + dimensionLineLocation1;
            var triangle1P3 = idealTriangle1P3.RotateAboutOrigin(dimensionLineAngle) * arrowSize + dimensionLineLocation1;
            var triangle2P1 = idealTriangle2P1.RotateAboutOrigin(dimensionLineAngle) * arrowSize + dimensionLineLocation2;
            var triangle2P2 = idealTriangle2P2.RotateAboutOrigin(dimensionLineAngle) * arrowSize + dimensionLineLocation2;
            var triangle2P3 = idealTriangle2P3.RotateAboutOrigin(dimensionLineAngle) * arrowSize + dimensionLineLocation2;

            var dimensionLineSegments = new List<(Vector Start, Vector End)>()
            {
                (dimensionLine1Start, dimensionLine1End),
                (dimensionLine2Start, dimensionLine2End),
                (dimensionLineLocationOffset1, textGapPoint1), // left half of line
                (textGapPoint2, dimensionLineLocationOffset2), // right half of line
                (tickLine1Start, tickLine1End),
                (tickLine2Start, tickLine2End),
            };
            var dimensionTriangles = new List<(Vector P1, Vector P2, Vector P3)>()
            {
                (triangle1P1, triangle1P2, triangle1P3),
                (triangle2P1, triangle2P2, triangle2P3),
            };

            return new LinearDimensionProperties(
                displayText,
                dimensionMeasurementVector.Length,
                correctedDimensionLineAngle,
                dimensionLineLocation1,
                dimensionLineLocation2,
                textLocation,
                dimensionLineSegments.Where(s => s.Start != s.End).ToArray(),
                dimensionTriangles.Where(t => !(t.P1 == t.P2 && t.P1 == t.P3)).ToArray());
        }

        private static double CorrectTextRotationAngle(double angle)
        {
            var ninety = Math.PI / 2.0;
            var eightyNine = ninety * 89.0 / 90.0;
            var oneEighty = Math.PI;
            if (angle > -eightyNine && angle <= ninety)
            {
                return angle;
            }

            if (angle > ninety)
            {
                return -(oneEighty - angle);
            }

            return angle + oneEighty;
        }
    }
}
