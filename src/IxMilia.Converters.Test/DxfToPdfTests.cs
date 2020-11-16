using System.IO;
using System.Text;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using IxMilia.Pdf;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DxfToPdfTests : TestBase
    {
        private static string ConvertToString(DxfFile dxf, PdfMeasurement? pageWidth = null, PdfMeasurement? pageHeight = null, double scale = 1.0)
        {
            var options = new DxfToPdfConverterOptions(
                pageWidth ?? PdfMeasurement.Inches(8.5),
                pageHeight ?? PdfMeasurement.Inches(11.0),
                scale);
            return ConvertToString(dxf, options);
        }

        private static string ConvertToString(DxfFile dxf, DxfToPdfConverterOptions options)
        {
            var converter = new DxfToPdfConverter();
            var pdf = converter.Convert(dxf, options);
            using (var ms = new MemoryStream())
            {
                pdf.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                var sb = new StringBuilder();
                int b;
                while ((b = ms.ReadByte()) >= 0)
                {
                    sb.Append((char)b);
                }

                return sb.ToString();
            }
        }

        [Fact]
        public void EmptyTest()
        {
            var dxf = new DxfFile();
            var pdf = ConvertToString(dxf);
            var expectedEmptyStream = NormalizeCrLf(@"
stream
0 w
0 0 0 RG
0 0 0 rg
S
endstream".Trim());
            Assert.Contains(expectedEmptyStream, pdf);
        }

        [Fact]
        public void SimpleLineTest()
        {
            var dxf = new DxfFile();
            dxf.ActiveViewPort = new DxfViewPort("viewport-name")
            {
                LowerLeft = new DxfPoint(0.0, 0.0, 0.0),
                ViewHeight = 11.0,
            };
            // line from (0,0) to (8.5,11), but half scale
            dxf.Entities.Add(new DxfLine(new DxfPoint(0.0, 0.0, 0.0), new DxfPoint(8.5, 11.0, 0.0)));
            var pdf = ConvertToString(dxf, scale: 0.5);
            // expected line from lower left of sheet to center
            var expected = NormalizeCrLf(@"
stream
0 w
0 0 0 RG
0 0 0 rg
0.00 0.00 m
306.00 396.00 l
S
endstream".Trim());
            Assert.Contains(expected, pdf);
        }

        [Fact]
        public void SourceDestinationTransformTest()
        {
            var dxf = new DxfFile();
            // line from (2,2) to (3,3)
            dxf.Entities.Add(new DxfLine(new DxfPoint(2, 2, 0), new DxfPoint(3, 3, 0)));
            var options = new DxfToPdfConverterOptions(PdfMeasurement.Mm(210), PdfMeasurement.Mm(297),
                new ConverterDxfRect(2, 3, 2, 3),
                new ConverterPdfRect(PdfMeasurement.Points(100), PdfMeasurement.Points(200), PdfMeasurement.Points(300), PdfMeasurement.Points(400)));

            var pdf = ConvertToString(dxf, options);
            // expected line from (100,300)pt to (200,400)pt
            var expected = NormalizeCrLf(@"
stream
0 w
0 0 0 RG
0 0 0 rg
100.00 300.00 m
200.00 400.00 l
S
endstream".Trim());
            Assert.Contains(expected, pdf);
        }
    }
}
