using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace IxMilia.Converters.Test
{
    internal static class TestExtentions
    {
        public static void WriteToFile(this XElement xElement, string nameOfTest, string? alternativeFilename = null)
        {

            var svgXml = new XElement(DxfToSvgConverter.Xmlns + "svg"
                           //new XAttribute("style", "background: black;")
                           //new XAttribute("width", "10"),
                           //new XAttribute("height", "10")
                           );

            var svgG = new XElement(DxfToSvgConverter.Xmlns + "g",
                           new XAttribute("transform", " scale(1)")
                );

            svgG.Add(xElement);

            svgXml.Add(svgG);

            using (MemoryStream stream = new MemoryStream())
            {
                svgXml.SaveTo(stream);

                // Optionally, you can convert the MemoryStream to a byte array or perform other actions.
                byte[] svgBytes = stream.ToArray();
                string path = Assembly.GetExecutingAssembly().Location;
                path = Path.Combine(Path.GetDirectoryName(path), "UnitTests", $"{nameOfTest}");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                alternativeFilename = alternativeFilename != null ? $"{alternativeFilename}_" : "";

                string filePath = @$"{Path.Combine(path, nameOfTest)}_{alternativeFilename}.svg";
                File.WriteAllBytes(filePath, svgBytes);
            }
        }

        

      
        public static void RemoveAttribute(this XElement element, string attributeName)
        {
            // Find and remove the attribute by name
            XAttribute attributeToRemove = element.Attribute(attributeName);
            attributeToRemove?.Remove();
        }
        public static void AssertExpected(this XElement expected, XElement actual, string nameOfTest, string? alternativeFilename = null)
        {
            var g = new XElement(DxfToSvgConverter.Xmlns + "g");

            actual.SetAttributeValue("id", "actual");      
            g.Add(actual);

            expected.SetAttributeValue("id", "expected");
            expected.SetAttributeValue("stroke", "green");

            g.Add(expected);

            g.WriteToFile(nameOfTest, alternativeFilename);

            var expectedVal = expected.Attributes().ToList().Where(s => s.Name == "d").FirstOrDefault().Value;
            var actualVal = actual.Attributes().ToList().Where(s => s.Name == "d").FirstOrDefault().Value;
            Assert.Equal(expectedVal, actualVal);
        }
    }
}
