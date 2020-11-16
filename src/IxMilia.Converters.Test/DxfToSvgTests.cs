using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
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
        public void RenderInsertTest()
        {
            var block = new DxfBlock();
            block.Name = "some-block";
            block.Entities.Add(new DxfLine(new DxfPoint(0.0, 0.0, 0.0), new DxfPoint(10.0, 10.0, 0.0)));

            var insert = new DxfInsert();
            insert.Name = "some-block";
            insert.Location = new DxfPoint(0.0, 5.0, 0.0);

            var file = new DxfFile();
            file.Blocks.Add(block);
            file.Entities.Add(insert);

            var expected = new XElement("g",
                new XAttribute("class", "dxf-insert some-block"),
                new XAttribute("transform", "translate(0.0 5.0) scale(1.0 1.0)"),
                new XElement("line",
                    new XAttribute("x1", "0.0"), new XAttribute("y1", "0.0"), new XAttribute("x2", "10.0"), new XAttribute("y2", "10.0"),
                    new XAttribute("stroke-width", "1.0px"),
                    new XAttribute("vector-effect", "non-scaling-stroke")));

            var actual = insert.ToXElement();
            AssertXElement(expected, actual);
        }

        [Fact]
        public void RenderClosedLwPolylineTest()
        {
            //   1,1 D
            //    ------------- 2,1 C
            //  /             |
            // |             /
            // ____________-
            // 0,0      1,0
            //  A        B
            var bulge90Degrees = Math.Sqrt(2.0) - 1.0;
            var vertices = new List<DxfLwPolylineVertex>()
            {
                new DxfLwPolylineVertex() { X = 0.0, Y = 0.0 }, // A
                new DxfLwPolylineVertex() { X = 1.0, Y = 0.0, Bulge = bulge90Degrees }, // B
                new DxfLwPolylineVertex() { X = 2.0, Y = 1.0 }, // C
                new DxfLwPolylineVertex() { X = 1.0, Y = 1.0, Bulge = bulge90Degrees } // D
            };
            var poly = new DxfLwPolyline(vertices);
            poly.IsClosed = true;
            var path = poly.GetSvgPath();
            Assert.Equal(5, path.Segments.Count);

            var start = (SvgMoveToPath)path.Segments[0];
            AssertClose(0.0, start.LocationX);
            AssertClose(0.0, start.LocationY);

            var segmentAB = (SvgLineToPath)path.Segments[1];
            AssertClose(1.0, segmentAB.LocationX);
            AssertClose(0.0, segmentAB.LocationY);

            var segmentBC = (SvgArcToPath)path.Segments[2];
            AssertClose(2.0, segmentBC.EndPointX);
            AssertClose(1.0, segmentBC.EndPointY);
            AssertClose(1.0, segmentBC.RadiusX);
            AssertClose(1.0, segmentBC.RadiusY);
            AssertClose(0.0, segmentBC.XAxisRotation);
            Assert.False(segmentBC.IsLargeArc);
            Assert.True(segmentBC.IsCounterClockwiseSweep);

            var segmentCD = (SvgLineToPath)path.Segments[3];
            AssertClose(1.0, segmentCD.LocationX);
            AssertClose(1.0, segmentCD.LocationY);

            var segmentDA = (SvgArcToPath)path.Segments[4];
            AssertClose(0.0, segmentDA.EndPointX);
            AssertClose(0.0, segmentDA.EndPointY);
            AssertClose(1.0, segmentDA.RadiusX);
            AssertClose(1.0, segmentDA.RadiusY);
            AssertClose(0.0, segmentDA.XAxisRotation);
            Assert.False(segmentDA.IsLargeArc);
            Assert.True(segmentDA.IsCounterClockwiseSweep);

            var expected = new XElement("path",
                new XAttribute("d", path.ToString()),
                new XAttribute("fill-opacity", "0"),
                new XAttribute("stroke-width", "1.0px"),
                new XAttribute("vector-effect", "non-scaling-stroke"));
            var actual = poly.ToXElement();
            AssertXElement(expected, actual);
        }

        [Fact]
        public void RenderOpenLwPolylineTest()
        {
            //   1,1 D
            //    ------------- 2,1 C
            //                |
            //               /
            // ____________-
            // 0,0      1,0
            //  A        B
            var bulge90Degrees = Math.Sqrt(2.0) - 1.0;
            var vertices = new List<DxfLwPolylineVertex>()
            {
                new DxfLwPolylineVertex() { X = 0.0, Y = 0.0 }, // A
                new DxfLwPolylineVertex() { X = 1.0, Y = 0.0, Bulge = bulge90Degrees }, // B
                new DxfLwPolylineVertex() { X = 2.0, Y = 1.0 }, // C
                new DxfLwPolylineVertex() { X = 1.0, Y = 1.0, Bulge = bulge90Degrees } // D
            };
            var poly = new DxfLwPolyline(vertices);
            poly.IsClosed = false;
            var path = poly.GetSvgPath();
            Assert.Equal(4, path.Segments.Count);

            var start = (SvgMoveToPath)path.Segments[0];
            AssertClose(0.0, start.LocationX);
            AssertClose(0.0, start.LocationY);

            var segmentAB = (SvgLineToPath)path.Segments[1];
            AssertClose(1.0, segmentAB.LocationX);
            AssertClose(0.0, segmentAB.LocationY);

            var segmentBC = (SvgArcToPath)path.Segments[2];
            AssertClose(2.0, segmentBC.EndPointX);
            AssertClose(1.0, segmentBC.EndPointY);
            AssertClose(1.0, segmentBC.RadiusX);
            AssertClose(1.0, segmentBC.RadiusY);
            AssertClose(0.0, segmentBC.XAxisRotation);
            Assert.False(segmentBC.IsLargeArc);
            Assert.True(segmentBC.IsCounterClockwiseSweep);

            var segmentCD = (SvgLineToPath)path.Segments[3];
            AssertClose(1.0, segmentCD.LocationX);
            AssertClose(1.0, segmentCD.LocationY);

            var expected = new XElement("path",
                new XAttribute("d", path.ToString()),
                new XAttribute("fill-opacity", "0"),
                new XAttribute("stroke-width", "1.0px"),
                new XAttribute("vector-effect", "non-scaling-stroke"));
            var actual = poly.ToXElement();
            AssertXElement(expected, actual);
        }

        [Fact]
        public void LwPolylineWithLargeArcTest1()
        {
            var vertices = new List<DxfLwPolylineVertex>()
            {
                new DxfLwPolylineVertex() { X = 0.6802950090711775, Y = 1.360590018142377, Bulge = 1.523362963416235 },
                new DxfLwPolylineVertex() { X = 1.176774337206015, Y = 0.2152040759172933 }
            };
            var poly = new DxfLwPolyline(vertices);
            var path = poly.GetSvgPath();
            Assert.Equal(2, path.Segments.Count);

            var move = (SvgMoveToPath)path.Segments[0];
            AssertClose(0.6802950090711775, move.LocationX);
            AssertClose(1.360590018142377, move.LocationY);

            var arc = (SvgArcToPath)path.Segments[1];
            AssertClose(1.176774337206015, arc.EndPointX);
            AssertClose(0.2152040759172933, arc.EndPointY);
            AssertClose(0.68029500907118867, arc.RadiusX);
            AssertClose(0.68029500907118867, arc.RadiusY);
            Assert.True(arc.IsCounterClockwiseSweep);
            Assert.True(arc.IsLargeArc);
            Assert.Equal(0.0, arc.XAxisRotation);
        }

        [Fact]
        public void LwPolylineWithLargeArcTest2()
        {
            var vertices = new List<DxfLwPolylineVertex>()
            {
                new DxfLwPolylineVertex() { X = 1.176774337206015, Y = 0.2152040759172933, Bulge = -0.1085213126826841 },
                new DxfLwPolylineVertex() { X = 1.501796342836956, Y = 0.2867624159371331 }
            };
            var poly = new DxfLwPolyline(vertices);
            var path = poly.GetSvgPath();
            Assert.Equal(2, path.Segments.Count);

            var move = (SvgMoveToPath)path.Segments[0];
            AssertClose(1.176774337206015, move.LocationX);
            AssertClose(0.2152040759172933, move.LocationY);

            var arc = (SvgArcToPath)path.Segments[1];
            AssertClose(1.501796342836956, arc.EndPointX);
            AssertClose(0.2867624159371331, arc.EndPointY);
            AssertClose(0.77571287053371341, arc.RadiusX);
            AssertClose(0.77571287053371341, arc.RadiusY);
            Assert.False(arc.IsCounterClockwiseSweep);
            Assert.False(arc.IsLargeArc);
            Assert.Equal(0.0, arc.XAxisRotation);
        }

        [Fact]
        public void EnsureValidShapeAsBareSvg()
        {
            // svg starts with 5 levels of `g`
            var element = new DxfToSvgConverter().Convert(new DxfFile(), new DxfToSvgConverterOptions(new ConverterDxfRect(), new ConverterSvgRect()));
            Assert.Equal("svg", element.Name.LocalName);
            var g = element.Elements().Single();
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal("g", g.Name.LocalName);
                g = g.Elements().Single();
            }
        }

        [Fact]
        public void EnsureValidShapeAsDiv()
        {
            var element = new DxfToSvgConverter().Convert(new DxfFile(), new DxfToSvgConverterOptions(new ConverterDxfRect(), new ConverterSvgRect(), svgId: "test-id"));
            Assert.Equal("div", element.Name.LocalName);
            Assert.Equal("test-id", element.Attribute("id").Value);
            var children = element.Elements().ToList();
            Assert.Equal(5, children.Count);

            var css = children[0];
            Assert.Equal("style", css.Name.LocalName);

            var controls = children[1];
            Assert.Equal("details", controls.Name.LocalName);
            Assert.Equal("Controls", controls.Element("summary").Value);

            var layers = children[2];
            Assert.Equal("details", layers.Name.LocalName);
            Assert.Equal("Layers", layers.Element("summary").Value);

            var svg = children[3];
            Assert.Equal("svg", svg.Name.LocalName);

            var script = children[4];
            Assert.Equal("script", script.Name.LocalName);
            Assert.Equal("text/javascript", script.Attribute("type").Value);
            Assert.Contains("function", script.Value);
            Assert.DoesNotContain("&gt;", script.Value);
        }
    }
}
