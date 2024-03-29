﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DwgDxfRoundTripTests
    {
        // very specific tests exist in both `DwgToDxfTests.cs` and `DxfToDwgTests.cs`, but most of the coverage can be
        // "free" if drawings are round-tripped
        private static async Task<DxfFile> RoundTrip(DxfFile dxf, bool roundTripThroughStreams = false)
        {
            var dwgConverter = new DxfToDwgConverter();
            var dwg = await dwgConverter.Convert(dxf, new DxfToDwgConverterOptions(DwgVersionId.R14));

            if (roundTripThroughStreams)
            {
                using var ms = new MemoryStream();
                dwg.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                dwg = DwgDrawing.Load(ms);
            }

            var dxfConverter = new DwgToDxfConverter();
            var roundTrippedDxf = await dxfConverter.Convert(dwg, new DwgToDxfConverterOptions(dxf.Header.Version));

            return roundTrippedDxf;
        }

        private static async Task<TEntity> RoundTrip<TEntity>(TEntity entity, bool roundTripThroughStreams = false) where TEntity : DxfEntity
        {
            var dxfFile = new DxfFile();
            dxfFile.Entities.Add(entity);
            var roundTrippedDxf = await RoundTrip(dxfFile, roundTripThroughStreams);
            var resultEntity = roundTrippedDxf.Entities.Single();
            var typedEntity = (TEntity)resultEntity;
            return typedEntity;
        }

        [Fact]
        public async Task RoundTripHeaderVariables()
        {
            var d1 = new DxfFile();
            d1.Header.FilletRadius = 6.0;
            var d2 = await RoundTrip(d1);
            Assert.Equal(6.0, d2.Header.FilletRadius);
        }

        [Fact]
        public async Task RoundTripActiveViewPort()
        {
            var d1 = new DxfFile();
            d1.ActiveViewPort.LowerLeft = new DxfPoint(1.0, 2.0, 3.0);
            d1.ActiveViewPort.ViewHeight = 10.0;
            var d2 = await RoundTrip(d1);
            Assert.Equal(d1.ActiveViewPort.LowerLeft, d2.ActiveViewPort.LowerLeft);
            Assert.Equal(d1.ActiveViewPort.ViewHeight, d2.ActiveViewPort.ViewHeight);
        }

        [Fact]
        public async Task RoundTripCommonEntityValues()
        {
            var e1 = new DxfLine();
            var e2 = await RoundTrip(e1);

            // these properties are common to all entities
            Assert.Equal(e1.Color, e2.Color);
            Assert.Equal(e1.Elevation, e2.Elevation);
            Assert.Equal(e1.Layer, e2.Layer);
            Assert.Equal(e1.LineTypeName, e2.LineTypeName);
            Assert.Equal(e1.LineTypeScale, e2.LineTypeScale);
            Assert.Equal(e1.Transparency, e2.Transparency);
        }

        [Fact]
        public async Task RoundTripCustomLineType()
        {
            var d1 = new DxfFile();
            var lineType = new DxfLineType("custom");
            lineType.Elements.Add(new DxfLineTypeElement() { DashDotSpaceLength = 0.5 });
            lineType.Elements.Add(new DxfLineTypeElement() { DashDotSpaceLength = 0.25 });
            d1.LineTypes.Add(lineType);

            var d2 = await RoundTrip(d1);
            var roundTrippedLineType = d2.LineTypes.Single(lt => lt.Name == "custom");
            Assert.Equal(2, roundTrippedLineType.Elements.Count);
            Assert.Equal(0.5, roundTrippedLineType.Elements[0].DashDotSpaceLength);
            Assert.Equal(0.25, roundTrippedLineType.Elements[1].DashDotSpaceLength);
        }

        [Fact]
        public async Task RoundTripDimensionStyle()
        {
            var d1 = new DxfFile();
            var ds1 = new DxfDimStyle("my-dimension-style")
            {
                DimensionLineGap = 42.0,
            };
            d1.DimensionStyles.Add(ds1);

            var d2 = await RoundTrip(d1);
            var ds2 = d2.DimensionStyles.Single(d => d.Name == ds1.Name);
            Assert.Equal(ds1.DimensionLineGap, ds2.DimensionLineGap);
        }

        [Fact]
        public async Task RoundTripMultipleEntities()
        {
            var dxf = new DxfFile();
            dxf.Entities.Add(new DxfArc());
            dxf.Entities.Add(new DxfCircle());
            dxf.Entities.Add(new DxfLine());

            var roundTrippedDxf = await RoundTrip(dxf, roundTripThroughStreams: true);
            Assert.Equal(3, roundTrippedDxf.Entities.Count);
            Assert.IsType<DxfArc>(roundTrippedDxf.Entities[0]);
            Assert.IsType<DxfCircle>(roundTrippedDxf.Entities[1]);
            Assert.IsType<DxfLine>(roundTrippedDxf.Entities[2]);
        }

        [Fact]
        public async Task RoundTripArc()
        {
            var e1 = new DxfArc(new DxfPoint(1.0, 2.0, 3.0), 4.0, 0.0, 90.0) { Thickness = 5.0 };
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.Center, e2.Center);
            Assert.Equal(e1.Radius, e2.Radius);
            Assert.Equal(e1.StartAngle, e2.StartAngle);
            Assert.Equal(e1.EndAngle, e2.EndAngle);
            Assert.Equal(e1.Normal, e2.Normal);
            Assert.Equal(e1.Thickness, e2.Thickness);
        }

        [Fact]
        public async Task RoundTripCircle()
        {
            var e1 = new DxfCircle(new DxfPoint(1.0, 2.0, 3.0), 4.0) { Thickness = 5.0 };
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.Center, e2.Center);
            Assert.Equal(e1.Radius, e2.Radius);
            Assert.Equal(e1.Normal, e2.Normal);
            Assert.Equal(e1.Thickness, e2.Thickness);
        }

        [Fact]
        public async Task RoundTripEllipse()
        {
            var e1 = new DxfEllipse(new DxfPoint(1.0, 2.0, 3.0), new DxfVector(1.0, 0.0, 0.0), 0.5) { StartParameter = 0.0, EndParameter = Math.PI };
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.Center, e2.Center);
            Assert.Equal(e1.MajorAxis, e2.MajorAxis);
            Assert.Equal(e1.MinorAxisRatio, e2.MinorAxisRatio);
            Assert.Equal(e1.StartParameter, e2.StartParameter);
            Assert.Equal(e1.EndParameter, e2.EndParameter);
            Assert.Equal(e1.Normal, e2.Normal);
        }

        [Fact]
        public async Task RoundTripLine()
        {
            var e1 = new DxfLine(new DxfPoint(1.0, 2.0, 3.0), new DxfPoint(4.0, 5.0, 6.0)) { Thickness = 7.0 };
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.P1, e2.P1);
            Assert.Equal(e1.P2, e2.P2);
            Assert.Equal(e1.Thickness, e2.Thickness);
        }

        [Fact]
        public async Task RoundTripLocation()
        {
            var e1 = new DxfModelPoint(new DxfPoint(1.0, 2.0, 3.0)) { Thickness = 4.0 };
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.Location, e2.Location);
            Assert.Equal(e1.Thickness, e2.Thickness);
        }

        [Fact]
        public async Task RoundTripLwPolyline()
        {
            var e1 = new DxfLwPolyline(new[]
            {
                new DxfLwPolylineVertex() { X = 1.0, Y = 2.0, StartingWidth = 0.0, EndingWidth = 0.0, Bulge = 0.0 },
                new DxfLwPolylineVertex() { X = 3.0, Y = 4.0, StartingWidth = 0.0, EndingWidth = 0.0, Bulge = 0.0 },
            })
            {
                Thickness = 5.0,
            };
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.Vertices.Count, e2.Vertices.Count);
            foreach (var pair in e1.Vertices.Zip(e2.Vertices))
            {
                Assert.Equal(pair.First.X, pair.Second.X);
                Assert.Equal(pair.First.Y, pair.Second.Y);
                Assert.Equal(pair.First.StartingWidth, pair.Second.StartingWidth);
                Assert.Equal(pair.First.EndingWidth, pair.Second.EndingWidth);
                Assert.Equal(pair.First.Bulge, pair.Second.Bulge);
            }

            Assert.Equal(e1.Thickness, e2.Thickness);
        }

        [Fact]
        public async Task RoundTripPolyline()
        {
            var e1 = new DxfPolyline(new[]
            {
                new DxfVertex(new DxfPoint(1.0, 2.0, 0.0)),
                new DxfVertex(new DxfPoint(3.0, 4.0, 0.0)),
            });
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.Vertices.Count, e2.Vertices.Count);
            for (int i = 0; i < e1.Vertices.Count; i++)
            {
                Assert.Equal(e1.Vertices[i].Location, e2.Vertices[i].Location);
            }
        }

        [Fact]
        public async Task RoundTripSpline()
        {
            var e1 = new DxfSpline()
            {
                DegreeOfCurve = 3
            };
            e1.ControlPoints.Add(new DxfControlPoint(new DxfPoint(3.0, 0.0, 0.0)));
            e1.ControlPoints.Add(new DxfControlPoint(new DxfPoint(4.0, 0.0, 0.0)));
            e1.ControlPoints.Add(new DxfControlPoint(new DxfPoint(4.0, 0.0, 0.0)));
            e1.ControlPoints.Add(new DxfControlPoint(new DxfPoint(4.0, 1.0, 0.0)));
            e1.KnotValues.Add(0.0);
            e1.KnotValues.Add(0.0);
            e1.KnotValues.Add(0.0);
            e1.KnotValues.Add(0.0);
            e1.KnotValues.Add(1.0);
            e1.KnotValues.Add(1.0);
            e1.KnotValues.Add(1.0);
            e1.KnotValues.Add(1.0);
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.DegreeOfCurve, e2.DegreeOfCurve);
            Assert.Equal(e1.ControlPoints, e2.ControlPoints);
            Assert.Equal(e1.KnotValues, e2.KnotValues);
        }

        [Fact]
        public async Task RoundTripText()
        {
            var e1 = new DxfText(new DxfPoint(0.0, -1.0, 0.0), 1.0, "abcd");
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.Location, e2.Location);
            Assert.Equal(e1.TextHeight, e2.TextHeight);
            Assert.Equal(e1.Value, e2.Value);
        }

        [Fact]
        public async Task RoundTripRotatedDimension()
        {
            var e1 = new DxfRotatedDimension()
            {
                DefinitionPoint1 = new DxfPoint(1.0, 2.0, 3.0),
                DefinitionPoint2 = new DxfPoint(4.0, 5.0, 6.0),
                DefinitionPoint3 = new DxfPoint(7.0, 8.0, 9.0),
                Text = "some-text",
                TextMidPoint = new DxfPoint(11.0, 22.0, 0.0),
            };
            var e2 = await RoundTrip(e1, roundTripThroughStreams: true);
            Assert.Equal(e1.DefinitionPoint1, e2.DefinitionPoint1);
            Assert.Equal(e1.DefinitionPoint2, e2.DefinitionPoint2);
            Assert.Equal(e1.DefinitionPoint3, e2.DefinitionPoint3);
            Assert.Equal(e1.Text, e2.Text);
            Assert.Equal(e1.TextMidPoint, e2.TextMidPoint);
        }

        [Fact]
        public async Task RoundTripAlignedDimension()
        {
            var e1 = new DxfAlignedDimension()
            {
                DefinitionPoint1 = new DxfPoint(1.0, 2.0, 3.0),
                DefinitionPoint2 = new DxfPoint(4.0, 5.0, 6.0),
                DefinitionPoint3 = new DxfPoint(7.0, 8.0, 9.0),
                Text = "some-text",
                TextMidPoint = new DxfPoint(11.0, 22.0, 0.0),
            };
            var e2 = await RoundTrip(e1, roundTripThroughStreams: true);
            Assert.Equal(e1.DefinitionPoint1, e2.DefinitionPoint1);
            Assert.Equal(e1.DefinitionPoint2, e2.DefinitionPoint2);
            Assert.Equal(e1.DefinitionPoint3, e2.DefinitionPoint3);
            Assert.Equal(e1.Text, e2.Text);
            Assert.Equal(e1.TextMidPoint, e2.TextMidPoint);
        }

        [Fact]
        public async Task RoundTripSolid()
        {
            var e1 = new DxfSolid()
            {
                FirstCorner = new DxfPoint(1.0, 2.0, 0.0),
                SecondCorner = new DxfPoint(3.0, 4.0, 0.0),
                ThirdCorner = new DxfPoint(5.0, 6.0, 0.0),
                FourthCorner = new DxfPoint(7.0, 8.0, 0.0),
            };
            var e2 = await RoundTrip(e1, roundTripThroughStreams: true);
            Assert.Equal(e1.FirstCorner, e2.FirstCorner);
            Assert.Equal(e1.SecondCorner, e2.SecondCorner);
            Assert.Equal(e1.ThirdCorner, e2.ThirdCorner);
            Assert.Equal(e1.FourthCorner, e2.FourthCorner);
        }
    }
}
