using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DxfToSvgTests : TestBase
    {
        private static void AssertXElement(XElement expected, XElement actual)
        {
            Assert.Equal(expected.Name.LocalName, actual.Name.LocalName); // too lazy to specify the xmlns in each test
            var expectedAttributes = expected.Attributes().ToList();
            var actualAttributes = actual.Attributes().ToList();
            Assert.Equal(expectedAttributes.Count, actualAttributes.Count);
            for (int i = 0; i < expectedAttributes.Count; i++)
            {
                var expectedAttribute = expectedAttributes[i];
                var actualAttribute = actualAttributes[i];
                Assert.Equal(expectedAttribute.Name, actualAttribute.Name);
                Assert.Equal(expectedAttribute.Value, actualAttribute.Value);

                var expectedChildren = expected.Elements().ToList();
                var actualChildren = actual.Elements().ToList();
                Assert.Equal(expectedChildren.Count, actualChildren.Count);
                for (int j = 0; j < expectedChildren.Count; j++)
                {
                    var expectedChild = expectedChildren[j];
                    var actualChild = actualChildren[j];
                    AssertXElement(expectedChild, actualChild);
                }
            }
        }

        [Fact]
        public void RenderArcTest()
        {
            var arc = new DxfArc(new DxfPoint(1.0, 2.0, 3.0), 4.0, 0.0, 90.0);
            var path = arc.GetSvgPath();
            Assert.Equal(2, path.Segments.Count);

            var first = (SvgMoveToPath)path.Segments[0];
            AssertClose(5.0, first.LocationX);
            AssertClose(2.0, first.LocationY);

            var arcPathSegment = (SvgArcToPath)path.Segments[1];
            AssertClose(1.0, arcPathSegment.EndPointX);
            AssertClose(6.0, arcPathSegment.EndPointY);
            Assert.Equal(4.0, arcPathSegment.RadiusX);
            Assert.Equal(4.0, arcPathSegment.RadiusY);
            Assert.Equal(0.0, arcPathSegment.XAxisRotation);
            Assert.False(arcPathSegment.IsLargeArc);
            Assert.True(arcPathSegment.IsCounterClockwiseSweep);

            var expected = new XElement("path",
                new XAttribute("d", path.ToString()),
                new XAttribute("fill-opacity", "0"),
                new XAttribute("stroke-width", "1.0px"),
                new XAttribute("vector-effect", "non-scaling-stroke"));
            var actual = arc.ToXElement();
            AssertXElement(expected, actual);
        }

        [Fact]
        public void ArcPathOf180DegreesTest()
        {
            var arc = new DxfArc(new DxfPoint(0.0, 0.0, 0.0), 1.0, 0.0, 180.0);
            var path = arc.GetSvgPath();

            Assert.Equal(3, path.Segments.Count);

            var move = (SvgMoveToPath)path.Segments[0];
            AssertClose(1.0, move.LocationX);
            AssertClose(0.0, move.LocationY);

            // 180 degree arcs are difficult to render; split it into two 90s
            var first = (SvgArcToPath)path.Segments[1];
            AssertClose(0.0, first.EndPointX);
            AssertClose(1.0, first.EndPointY);
            Assert.Equal(1.0, first.RadiusX);
            Assert.Equal(1.0, first.RadiusY);
            Assert.Equal(0.0, first.XAxisRotation);
            Assert.False(first.IsLargeArc);
            Assert.True(first.IsCounterClockwiseSweep);
            var second = (SvgArcToPath)path.Segments[2];
            AssertClose(-1.0, second.EndPointX);
            AssertClose(0.0, second.EndPointY);
            Assert.Equal(1.0, second.RadiusX);
            Assert.Equal(1.0, second.RadiusY);
            Assert.Equal(0.0, second.XAxisRotation);
            Assert.False(second.IsLargeArc);
            Assert.True(second.IsCounterClockwiseSweep);
        }

        [Fact]
        public void ArcFlagsSizeTest1()
        {
            // arc from 270->0 (360)
            var arc = new DxfArc(new DxfPoint(1.0, 2.0, 3.0), 4.0, 270.0, 0.0);
            var path = arc.GetSvgPath();
            Assert.Equal(2, path.Segments.Count);
            var arcTo = (SvgArcToPath)path.Segments[1];
            Assert.False(arcTo.IsLargeArc);
            Assert.True(arcTo.IsCounterClockwiseSweep);
        }

        [Fact]
        public void ArcFlagsSizeTest2()
        {
            // arc from 350->10
            var arc = new DxfArc(new DxfPoint(1.0, 2.0, 3.0), 4.0, 350.0, 10.0);
            var path = arc.GetSvgPath();
            Assert.Equal(2, path.Segments.Count);
            var arcTo = (SvgArcToPath)path.Segments[1];
            Assert.False(arcTo.IsLargeArc);
            Assert.True(arcTo.IsCounterClockwiseSweep);
        }

        [Fact]
        public void RenderCircleTest()
        {
            var circle = new DxfCircle(new DxfPoint(1.0, 2.0, 3.0), 4.0);
            var expected = new XElement("ellipse",
                new XAttribute("cx", "1.0"), new XAttribute("cy", "2.0"),
                new XAttribute("rx", "4.0"), new XAttribute("ry", "4.0"),
                new XAttribute("fill-opacity", "0"),
                new XAttribute("stroke-width", "1.0px"),
                new XAttribute("vector-effect", "non-scaling-stroke"));
            var actual = circle.ToXElement();
            AssertXElement(expected, actual);
        }

        [Fact]
        public void RenderEllipseTest()
        {
            var ellipse = new DxfEllipse(new DxfPoint(1.0, 2.0, 3.0), new DxfVector(1.0, 0.0, 0.0), 0.5)
            {
                StartParameter = 0.0,
                EndParameter = Math.PI / 2.0 // 90 degrees
            };
            var path = ellipse.GetSvgPath();
            Assert.Equal(2, path.Segments.Count);
            
            var move = (SvgMoveToPath)path.Segments[0];
            AssertClose(2.0, move.LocationX);
            AssertClose(2.0, move.LocationY);

            var arcSegment = (SvgArcToPath)path.Segments[1];
            AssertClose(1.0, arcSegment.EndPointX);
            AssertClose(2.5, arcSegment.EndPointY);
            Assert.Equal(1.0, arcSegment.RadiusX);
            Assert.Equal(0.5, arcSegment.RadiusY);
            Assert.Equal(0.0, arcSegment.XAxisRotation);
            Assert.False(arcSegment.IsLargeArc);
            Assert.True(arcSegment.IsCounterClockwiseSweep);

            var expected = new XElement("path",
                new XAttribute("d", path.ToString()),
                new XAttribute("fill-opacity", "0"),
                new XAttribute("stroke-width", "1.0px"),
                new XAttribute("vector-effect", "non-scaling-stroke"));
            var actual = ellipse.ToXElement();
            AssertXElement(expected, actual);
        }

        [Fact]
        public void EllipsePathOf180DegreesTest()
        {
            var ellipse = new DxfEllipse(new DxfPoint(0.0, 0.0, 0.0), new DxfVector(1.0, 0.0, 0.0), 0.5)
            {
                StartParameter = 0.0,
                EndParameter = Math.PI
            };
            var path = ellipse.GetSvgPath();
            Assert.Equal(3, path.Segments.Count);

            var move = (SvgMoveToPath)path.Segments[0];
            AssertClose(1.0, move.LocationX);
            AssertClose(0.0, move.LocationY);

            // 180 degree ellipses are difficult to render; split it into two 90s
            var first = (SvgArcToPath)path.Segments[1];
            AssertClose(0.0, first.EndPointX);
            AssertClose(0.5, first.EndPointY);
            Assert.Equal(1.0, first.RadiusX);
            Assert.Equal(0.5, first.RadiusY);
            Assert.Equal(0.0, first.XAxisRotation);
            Assert.False(first.IsLargeArc);
            Assert.True(first.IsCounterClockwiseSweep);
            var second = (SvgArcToPath)path.Segments[2];
            AssertClose(-1.0, second.EndPointX);
            AssertClose(0.0, second.EndPointY);
            Assert.Equal(1.0, second.RadiusX);
            Assert.Equal(0.5, second.RadiusY);
            Assert.Equal(0.0, second.XAxisRotation);
            Assert.False(second.IsLargeArc);
            Assert.True(second.IsCounterClockwiseSweep);
        }

        [Fact]
        public void EllipseAndClosedShapeTest()
        {
            var ellipse = new DxfEllipse(new DxfPoint(0.0, 0.0, 0.0), new DxfVector(1.0, 0.0, 0.0), 0.5);
            var expected = new XElement("ellipse",
                new XAttribute("cx", "0.0"), new XAttribute("cy", "0.0"),
                new XAttribute("rx", "1.0"), new XAttribute("ry", "0.5"),
                new XAttribute("fill-opacity", "0"),
                new XAttribute("stroke-width", "1.0px"),
                new XAttribute("vector-effect", "non-scaling-stroke"));
            var actual = ellipse.ToXElement();
            AssertXElement(expected, actual);
        }

        [Fact]
        public void RenderLineTest()
        {
            var line = new DxfLine(new DxfPoint(1.0, 2.0, 3.0), new DxfPoint(4.0, 5.0, 6.0));
            var expected = new XElement("line",
                new XAttribute("x1", "1.0"), new XAttribute("y1", "2.0"), new XAttribute("x2", "4.0"), new XAttribute("y2", "5.0"),
                new XAttribute("stroke-width", "1.0px"),
                new XAttribute("vector-effect", "non-scaling-stroke"));
            var actual = line.ToXElement();
            AssertXElement(expected, actual);
        }

        [Fact]
        public void SvgArcPathToStringTest()
        {
            var path = new SvgPath(new SvgPathSegment[]
            {
                new SvgMoveToPath(1.0, 2.0),
                new SvgArcToPath(3.0, 4.0, 5.0, true, false, 6.0, 7.0)
            });
            Assert.Equal("M 1.0 2.0 A 3.0 4.0 5.0 1 0 6.0 7.0", path.ToString());

            path = new SvgPath(new SvgPathSegment[]
            {
                new SvgMoveToPath(1.0, 2.0),
                new SvgArcToPath(3.0, 4.0, 5.0, true, false, 6.0, 7.0),
                new SvgArcToPath(8.0, 9.0, 10.0, false, true, 11.0, 12.0)
            });
            Assert.Equal("M 1.0 2.0 A 3.0 4.0 5.0 1 0 6.0 7.0 A 8.0 9.0 10.0 0 1 11.0 12.0", path.ToString());
        }

        [Fact]
        public void EnsureValidShapeAsBareSvg()
        {
            var element = new DxfToSvgConverter().Convert(new DxfFile(), new DxfToSvgConverterOptions(new ConverterDxfRect(), new ConverterSvgRect()));
            Assert.Equal("svg", element.Name.LocalName);
        }

        [Fact]
        public void EnsureValidShapeAsDiv()
        {
            var element = new DxfToSvgConverter().Convert(new DxfFile(), new DxfToSvgConverterOptions(new ConverterDxfRect(), new ConverterSvgRect(), svgId: "test-id"));
            Assert.Equal("div", element.Name.LocalName);
            var children = element.Elements().ToList();
            Assert.Equal(3, children.Count);

            var svg = children[0];
            Assert.Equal("svg", svg.Name.LocalName);
            Assert.Equal("test-id", svg.Attribute("id").Value);
            var svgGroups = svg.Elements().ToList();
            Assert.Equal(2, svgGroups.Count);
            Assert.Equal("svg-viewport", svgGroups[0].Attribute("class").Value);

            var css = children[1];
            Assert.Equal("style", css.Name.LocalName);

            var script = children[2];
            Assert.Equal("script", script.Name.LocalName);
            Assert.Equal("text/javascript", script.Attribute("type").Value);
            Assert.Contains("function", script.Value);
            Assert.DoesNotContain("&gt;", script.Value);
        }
    }
}
