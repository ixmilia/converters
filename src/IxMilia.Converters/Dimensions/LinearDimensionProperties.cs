using System;

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


        public LinearDimensionProperties(
            string displayText,
            double dimensionLength,
            double dimensionLineAngle,
            Vector dimensionLineStart,
            Vector dimensionLineEnd,
            Vector textLocation,
            (Vector start, Vector end)[] dimensionLineSegments)
        {
            DisplayText = displayText;
            DimensionLength = dimensionLength;
            DimensionLineAngle = dimensionLineAngle;
            DimensionLineStart = dimensionLineStart;
            DimensionLineEnd = dimensionLineEnd;
            TextLocation = textLocation;
            DimensionLineSegments = dimensionLineSegments;
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
            var (dimensionLineLocation1, dimensionLineLocation2, textMidPoint) = DimensionExtensions.GetDimensionLineAndTextLocation(definitionPoint1, definitionPoint2, selectedDimensionLineLocation, isAligned: isAligned);
            var dimensionLine1Vector = (dimensionLineLocation1 - definitionPoint1).Normalize();
            var dimensionLine2Vector = (dimensionLineLocation2 - definitionPoint2).Normalize();
            var dimensionLine1Start = definitionPoint1 + (dimensionLine1Vector * dimensionSettings.ExtensionLineOffset);
            var dimensionLine1End = dimensionLineLocation1 + (dimensionLine1Vector * dimensionSettings.ExtensionLineExtension);
            var dimensionLine2Start = definitionPoint2 + (dimensionLine2Vector * dimensionSettings.ExtensionLineOffset);
            var dimensionLine2End = dimensionLineLocation2 + (dimensionLine2Vector * dimensionSettings.ExtensionLineExtension);

            var sqrt2 = Math.Sqrt(2.0);
            var tickHalfVector = new Vector((dimensionLine1Vector.Y - dimensionLine1Vector.X) / sqrt2, (dimensionLine1Vector.X + dimensionLine1Vector.Y) / sqrt2, 0.0).Normalize() * dimensionSettings.ArrowSize * 0.5;
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
            var textGapPoint1 = textMidPoint - (normalizedDimensionMeasurementVector * (textGapWidth / 2.0));
            var textGapPoint2 = textMidPoint + (normalizedDimensionMeasurementVector * (textGapWidth / 2.0));

            var textLeft = textMidPoint - (correctedDimensionMeasurementVector * textWidth / 2.0);
            var textUpAngle = correctedDimensionLineAngle + Math.PI / 2.0; // add 90*
            var textUpVector = new Vector(Math.Cos(textUpAngle), Math.Sin(textUpAngle), 0.0);
            var candidateTextBottomLocation = textLeft - (textUpVector * dimensionSettings.TextHeight * 0.5);
            var candidateTextTopLocation = textLeft + (textUpVector * dimensionSettings.TextHeight * 0.5);
            var topBottomDiff = (candidateTextTopLocation - textLeft).Length - (candidateTextBottomLocation - textLeft).Length;
            var textLocation = topBottomDiff > 1E-6 ? candidateTextTopLocation : candidateTextBottomLocation;

            return new LinearDimensionProperties(
                displayText,
                dimensionMeasurementVector.Length,
                correctedDimensionLineAngle,
                dimensionLineLocation1,
                dimensionLineLocation2,
                textLocation,
                new[]
                {
                    (dimensionLine1Start, dimensionLine1End),
                    (dimensionLine2Start, dimensionLine2End),
                    (dimensionLineLocation1, textGapPoint1), // left half of line
                    (textGapPoint2, dimensionLineLocation2), // right half of line
                    (tickLine1Start, tickLine1End),
                    (tickLine2Start, tickLine2End),
                });
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
