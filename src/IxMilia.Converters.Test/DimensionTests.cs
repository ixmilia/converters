using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DimensionTests
    {
        [Theory]
        [InlineData(3.0, "3", 0, DrawingUnits.English, UnitFormat.Decimal)] // nearest whole number
        [InlineData(3.4, "3", 0, DrawingUnits.English, UnitFormat.Decimal)]
        [InlineData(3.5, "4", 0, DrawingUnits.English, UnitFormat.Decimal)]
        [InlineData(3.0, "3.000", 3, DrawingUnits.English, UnitFormat.Decimal)] // 3 decimal places
        [InlineData(3.14159, "3.142", 3, DrawingUnits.English, UnitFormat.Decimal)]
        [InlineData(3.14159, "3.1416", 4, DrawingUnits.English, UnitFormat.Decimal)] // 4 decimal places
        [InlineData(-3.0, "-3.0000", 4, DrawingUnits.English, UnitFormat.Decimal)] // negative value
        [InlineData(1.578E-12, "0.0000", 4, DrawingUnits.English, UnitFormat.Decimal)] // really close to zero
        [InlineData(0.0, "0'0\"", 0, DrawingUnits.English, UnitFormat.Architectural)] // nearest inch
        [InlineData(15.2, "1'3\"", 0, DrawingUnits.English, UnitFormat.Architectural)]
        [InlineData(0.0, "0'0\"", 3, DrawingUnits.English, UnitFormat.Architectural)] // nearest eighth inch
        [InlineData(15.2, "1'3-1/4\"", 3, DrawingUnits.English, UnitFormat.Architectural)]
        [InlineData(24.0, "2'0\"", 4, DrawingUnits.English, UnitFormat.Architectural)] // even feet
        [InlineData(0.125, "0'0-1/8\"", 4, DrawingUnits.English, UnitFormat.Architectural)] // only fractional inches
        [InlineData(36.625, "3'0-5/8\"", 4, DrawingUnits.English, UnitFormat.Architectural)] // feet and fractional inches, no whole inches
        [InlineData(15.99999999, "1'4\"", 4, DrawingUnits.English, UnitFormat.Architectural)] // near the upper limit
        [InlineData(-18.5, "-1'6-1/2\"", 4, DrawingUnits.English, UnitFormat.Architectural)] // negative value
        [InlineData(0.0, "0\"", 0, DrawingUnits.English, UnitFormat.Fractional)] // nearest inch
        [InlineData(15.2, "15\"", 0, DrawingUnits.English, UnitFormat.Fractional)]
        [InlineData(0.0, "0\"", 3, DrawingUnits.English, UnitFormat.Fractional)] // nearest eighth inch
        [InlineData(15.2, "15-1/4\"", 3, DrawingUnits.English, UnitFormat.Fractional)]
        [InlineData(24.0, "24\"", 4, DrawingUnits.English, UnitFormat.Fractional)] // even feet
        [InlineData(0.125, "0-1/8\"", 4, DrawingUnits.English, UnitFormat.Fractional)] // only fractional inches
        [InlineData(0.625, "0-5/8\"", 4, DrawingUnits.English, UnitFormat.Fractional)]
        [InlineData(36.625, "36-5/8\"", 4, DrawingUnits.English, UnitFormat.Fractional)] // feet and fractional inches, no whole inches
        [InlineData(15.99999999, "16\"", 4, DrawingUnits.English, UnitFormat.Fractional)] // near the upper limit
        [InlineData(-18.5, "-18-1/2\"", 4, DrawingUnits.English, UnitFormat.Fractional)] // negative value
        public void FormatUnits(double value, string expected, int unitPrecision, DrawingUnits drawingUnits, UnitFormat unitFormat)
        {
            var actual = DimensionExtensions.GenerateLinearDimensionText(value, drawingUnits, unitFormat, unitPrecision);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GenerateLinearDimensionsProperties))]
        public void GenerateLinearDimensions(Vector definitionPoint1, Vector definitionPoint2, Vector selectedDimensionLineLocation, bool isAligned, string displayText, double textWidth, DimensionSettings dimensionSettings, LinearDimensionProperties expected)
        {
            var actual = LinearDimensionProperties.BuildFromValues(definitionPoint1, definitionPoint2, selectedDimensionLineLocation, isAligned, displayText, textWidth, dimensionSettings);
            Assert.Equal(expected.DimensionLength, Round(actual.DimensionLength));
            Assert.Equal(expected.DimensionLineAngle, Round(actual.DimensionLineAngle));
            Assert.Equal(expected.DimensionLineStart, Round(actual.DimensionLineStart));
            Assert.Equal(expected.DimensionLineEnd, Round(actual.DimensionLineEnd));
            Assert.Equal(expected.TextLocation, Round(actual.TextLocation));
            Assert.Equal(expected.DimensionLineSegments, Round(actual.DimensionLineSegments));
            Assert.Equal(expected.DimensionTriangles, Round(actual.DimensionTriangles));
        }

        public static IEnumerable<object[]> GenerateLinearDimensionsProperties()
        {
            var sqrt2 = Math.Sqrt(2.0);

            // selection points 0,0 and 3,4, dimension line at 3,5; non-aligned dimension: only measure x-axis, generate arrows
            yield return new object[]
            {
                new Vector(0.0, 0.0, 0.0),
                new Vector(3.0, 4.0, 0.0),
                new Vector(3.0, 5.0, 0.0),
                false,
                "A",
                0.5,
                new DimensionSettings(
                    textHeight: 1.0,
                    extensionLineOffset: 0.25,
                    extensionLineExtension: 0.5,
                    dimensionLineGap: 0.125,
                    arrowSize: sqrt2,
                    tickSize: 0.0),
                new LinearDimensionProperties(
                    displayText: "A",
                    dimensionLength: 3.0,
                    dimensionLineAngle: 0.0,
                    dimensionLineStart: new Vector(0.0, 5.0, 0.0),
                    dimensionLineEnd: new Vector(3.0, 5.0, 0.0),
                    textLocation: new Vector(1.25, 4.5, 0.0),
                    dimensionLineSegments: new[]
                    {
                        (new Vector(0.0, 0.25, 0.0), new Vector(0.0, 5.5, 0.0)), // first extension line
                        (new Vector(3.0, 4.25, 0.0), new Vector(3.0, 5.5, 0.0)), // second extension line
                        (new Vector(1.4142, 5.0 ,0.0), new Vector(1.125, 5.0, 0.0)), // first half of dimension line
                        (new Vector(1.875, 5.0, 0.0), new Vector(1.5858, 5.0, 0.0)), // second half of dimension line
                    },
                    dimensionTriangles: new[]
                    {
                        (new Vector(0.0, 5.0, 0.0), new Vector(1.4142, 4.7643, 0.0), new Vector(1.4142, 5.2357, 0.0)),
                        (new Vector(3.0, 5.0, 0.0), new Vector(1.5858, 4.7643, 0.0), new Vector(1.5858, 5.2357, 0.0)),
                    }
                ),
            };

            // selection points 0,0 and 3,4, dimension line at 3,5; non-aligned dimension: only measure x-axis, generate ticks
            yield return new object[]
            {
                new Vector(0.0, 0.0, 0.0),
                new Vector(3.0, 4.0, 0.0),
                new Vector(3.0, 5.0, 0.0),
                false,
                "A",
                0.5,
                new DimensionSettings(
                    textHeight: 1.0,
                    extensionLineOffset: 0.25,
                    extensionLineExtension: 0.5,
                    dimensionLineGap: 0.125,
                    arrowSize: sqrt2,
                    tickSize: sqrt2),
                new LinearDimensionProperties(
                    displayText: "A",
                    dimensionLength: 3.0,
                    dimensionLineAngle: 0.0,
                    dimensionLineStart: new Vector(0.0, 5.0, 0.0),
                    dimensionLineEnd: new Vector(3.0, 5.0, 0.0),
                    textLocation: new Vector(1.25, 4.5, 0.0),
                    dimensionLineSegments: new[]
                    {
                        (new Vector(0.0, 0.25, 0.0), new Vector(0.0, 5.5, 0.0)), // first extension line
                        (new Vector(3.0, 4.25, 0.0), new Vector(3.0, 5.5, 0.0)), // second extension line
                        (new Vector(0.0, 5.0, 0.0), new Vector(1.125, 5.0, 0.0)), // first half of dimension line
                        (new Vector(1.875, 5.0, 0.0), new Vector(3.0, 5.0, 0.0)), // second half of dimension line
                        (new Vector(-0.5, 4.5, 0.0), new Vector(0.5, 5.5, 0.0)), // first tick
                        (new Vector(2.5, 4.5, 0.0), new Vector(3.5, 5.5, 0.0)), // second tick
                    },
                    dimensionTriangles: Array.Empty<(Vector, Vector, Vector)>()
                ),
            };

            // selection points 0,0 and 3,4, dimension line at 3,5; aligned dimension: measure full diagonal, generate arrows
            yield return new object[]
            {
                new Vector(0.0, 0.0, 0.0),
                new Vector(3.0, 4.0, 0.0),
                new Vector(3.0, 5.0, 0.0),
                true,
                "A",
                0.5,
                new DimensionSettings(
                    textHeight: 1.0,
                    extensionLineOffset: 0.25,
                    extensionLineExtension: 0.5,
                    dimensionLineGap: 0.125,
                    arrowSize: sqrt2,
                    tickSize: 0.0),
                new LinearDimensionProperties(
                    displayText: "A",
                    dimensionLength: 5.0,
                    dimensionLineAngle: 0.9273, // ~53*
                    dimensionLineStart: new Vector(-0.48, 0.36, 0.0),
                    dimensionLineEnd:new Vector(2.52, 4.36, 0.0),
                    textLocation: new Vector(1.27, 1.86, 0.0),
                    dimensionLineSegments: new[]
                    {
                        (new Vector(-0.2, 0.15, 0.0), new Vector(-0.88, 0.66, 0.0)), // first extension line
                        (new Vector(2.8, 4.15, 0.0), new Vector(2.12, 4.66, 0.0)), // second extension line
                        (new Vector(0.3685, 1.4914, 0.0), new Vector(0.795, 2.06, 0.0)), // first half of dimension line
                        (new Vector(1.245, 2.66, 0.0), new Vector(1.6715, 3.2286, 0.0)), // second half of dimension line
                    },
                    dimensionTriangles: new[]
                    {
                        (new Vector(-0.48, 0.36, 0.0), new Vector(0.5571, 1.3499, 0.0), new Vector(0.18, 1.6328, 0.0)),
                        (new Vector(2.52, 4.36, 0.0), new Vector(1.86, 3.0872, 0.0), new Vector(1.4829, 3.3701, 0.0)),
                    }
                ),
            };

            // selection points 0,0 and 3,4, dimension line at 3,5; aligned dimension: measure full diagonal, generate ticks
            yield return new object[]
            {
                new Vector(0.0, 0.0, 0.0),
                new Vector(3.0, 4.0, 0.0),
                new Vector(3.0, 5.0, 0.0),
                true,
                "A",
                0.5,
                new DimensionSettings(
                    textHeight: 1.0,
                    extensionLineOffset: 0.25,
                    extensionLineExtension: 0.5,
                    dimensionLineGap: 0.125,
                    arrowSize: sqrt2,
                    tickSize: sqrt2),
                new LinearDimensionProperties(
                    displayText: "A",
                    dimensionLength: 5.0,
                    dimensionLineAngle: 0.9273, // ~53*
                    dimensionLineStart: new Vector(-0.48, 0.36, 0.0),
                    dimensionLineEnd:new Vector(2.52, 4.36, 0.0),
                    textLocation: new Vector(1.27, 1.86, 0.0),
                    dimensionLineSegments: new[]
                    {
                        (new Vector(-0.2, 0.15, 0.0), new Vector(-0.88, 0.66, 0.0)), // first extension line
                        (new Vector(2.8, 4.15, 0.0), new Vector(2.12, 4.66, 0.0)), // second extension line
                        (new Vector(-0.48, 0.36, 0.0), new Vector(0.795, 2.06, 0.0)), // first half of dimension line
                        (new Vector(1.245, 2.66, 0.0), new Vector(2.52, 4.36, 0.0)), // second half of dimension line
                        (new Vector(-0.38, -0.34, 0.0), new Vector(-0.58, 1.06, 0.0)), // first tick
                        (new Vector(2.62, 3.66, 0.0), new Vector(2.42, 5.06, 0.0)), // second tick
                    },
                    dimensionTriangles: Array.Empty<(Vector, Vector, Vector)>()
                ),
            };
        }

        private static double Round(double value) => double.Round(value, 4);

        private static Vector Round(Vector value) => new Vector(Round(value.X), Round(value.Y), Round(value.Z));

        private static (Vector, Vector) Round((Vector a, Vector b) value) => ((Round(value.a), Round(value.b)));

        private static (Vector, Vector, Vector) Round((Vector a, Vector b, Vector c) value) => ((Round(value.a), Round(value.b), Round(value.c)));

        private static (Vector, Vector)[] Round((Vector, Vector)[] value) => value.Select(Round).ToArray();

        private static (Vector, Vector, Vector)[] Round((Vector, Vector, Vector)[] value) => value.Select(Round).ToArray();
    }
}
