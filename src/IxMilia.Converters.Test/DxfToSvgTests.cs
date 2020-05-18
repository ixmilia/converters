using System;
using System.Linq;
using System.Xml.Linq;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DxfToSvgTests : TestBase<DxfFile, XElement>
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
            var expected = new XElement("path",
                new XAttribute("d", "M 5.0 2.0 a 4.0 4.0 0 0 1 -4.0 4.0"),
                new XAttribute("fill-opacity", "0"),
                new XAttribute("stroke-width", "1.0px"),
                new XAttribute("vector-effect", "non-scaling-stroke"));
            var actual = arc.ToXElement();
            AssertXElement(expected, actual);
        }

        [Fact]
        public void ArcFlagsSizeTest1()
        {
            // arc from 270->0 (360)
            var arc = new DxfArc(new DxfPoint(1.0, 2.0, 3.0), 4.0, 270.0, 0.0);
            var element = arc.ToXElement();
            var arcParts = element.Attribute("d").Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal("0", arcParts[7]); // not large arc
            Assert.Equal("1", arcParts[8]); // counterclockwise sweep
        }

        [Fact]
        public void ArcFlagsSizeTest2()
        {
            // arc from 350->10
            var arc = new DxfArc(new DxfPoint(1.0, 2.0, 3.0), 4.0, 350.0, 10.0);
            var element = arc.ToXElement();
            var arcParts = element.Attribute("d").Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal("0", arcParts[7]); // not large arc
            Assert.Equal("1", arcParts[8]); // counterclockwise sweep
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
