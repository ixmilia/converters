using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using IxMilia.Pdf;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DxfToPdfTests : TestBase
    {
        private static Task<string> ConvertToString(DxfFile dxf, PdfMeasurement? pageWidth = null, PdfMeasurement? pageHeight = null, double scale = 1.0)
        {
            var options = new DxfToPdfConverterOptions(
                pageWidth ?? PdfMeasurement.Inches(8.5),
                pageHeight ?? PdfMeasurement.Inches(11.0),
                scale);
            return ConvertToString(dxf, options);
        }

        private static async Task<string> ConvertToString(DxfFile dxf, DxfToPdfConverterOptions options)
        {
            var converter = new DxfToPdfConverter();
            var pdf = await converter.Convert(dxf, options);
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
        public async Task EmptyTest()
        {
            var dxf = new DxfFile();
            var pdf = await ConvertToString(dxf);
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
        public async Task SimpleLineTest()
        {
            var dxf = new DxfFile();
            dxf.ActiveViewPort = new DxfViewPort("viewport-name")
            {
                LowerLeft = new DxfPoint(0.0, 0.0, 0.0),
                ViewHeight = 11.0,
            };
            // line from (0,0) to (8.5,11), but half scale
            dxf.Entities.Add(new DxfLine(new DxfPoint(0.0, 0.0, 0.0), new DxfPoint(8.5, 11.0, 0.0)));
            var pdf = await ConvertToString(dxf, scale: 0.5);
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
        public async Task SourceDestinationTransformTest()
        {
            var dxf = new DxfFile();
            // line from (2,2) to (3,3)
            dxf.Entities.Add(new DxfLine(new DxfPoint(2, 2, 0), new DxfPoint(3, 3, 0)));
            var options = new DxfToPdfConverterOptions(PdfMeasurement.Mm(210), PdfMeasurement.Mm(297),
                new ConverterDxfRect(2, 3, 2, 3),
                new ConverterPdfRect(PdfMeasurement.Points(100), PdfMeasurement.Points(200), PdfMeasurement.Points(300), PdfMeasurement.Points(400)));

            var pdf = await ConvertToString(dxf, options);
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

        [Fact]
        public async Task RenderClosedLwPolylineTest()
        {
            //   2,2 D
            //    ------------- 3,2 C
            //  /             |
            // |             /
            // ____________-
            // 1,1      2,1
            //  A        B
            var bulge90Degrees = Math.Sqrt(2.0) - 1.0;
            var vertices = new List<DxfLwPolylineVertex>()
            {
                new DxfLwPolylineVertex() { X = 1.0, Y = 1.0 }, // A
                new DxfLwPolylineVertex() { X = 2.0, Y = 1.0, Bulge = bulge90Degrees }, // B
                new DxfLwPolylineVertex() { X = 3.0, Y = 2.0 }, // C
                new DxfLwPolylineVertex() { X = 2.0, Y = 2.0, Bulge = bulge90Degrees } // D
            };
            var poly = new DxfLwPolyline(vertices);
            poly.IsClosed = true;
            var dxf = new DxfFile();
            dxf.Entities.Add(poly);

            var pdf = await ConvertToString(dxf);
            var expected = NormalizeCrLf(@"
stream
0 w
0 0 0 RG
0 0 0 rg
72.00 72.00 m
144.00 72.00 l
216.00 144.00 m
216.00 144.00 216.00 144.00 216.00 144.00 c
144.00 72.00 m
183.76 72.00 216.00 104.24 216.00 144.00 c
216.00 144.00 m
144.00 144.00 l
144.00 144.00 m
144.00 144.00 144.00 144.00 144.00 144.00 c
144.00 144.00 m
104.24 144.00 72.00 111.76 72.00 72.00 c
S
endstream".Trim());
            Assert.Contains(expected, pdf);
        }

        [Fact]
        public async Task RenderClosedPolylineTest()
        {
            //   2,2 D
            //    ------------- 3,2 C
            //  /             |
            // |             /
            // ____________-
            // 1,1      2,1
            //  A        B
            var bulge90Degrees = Math.Sqrt(2.0) - 1.0;
            var vertices = new List<DxfVertex>()
            {
                new DxfVertex(new DxfPoint(1.0, 1.0, 0.0)), // A
                new DxfVertex(new DxfPoint(2.0, 1.0, 0.0)) { Bulge = bulge90Degrees }, // B
                new DxfVertex(new DxfPoint(3.0, 2.0, 0.0)), // C
                new DxfVertex(new DxfPoint(2.0, 2.0, 0.0)) { Bulge = bulge90Degrees } // D
            };
            var poly = new DxfPolyline(vertices);
            poly.IsClosed = true;
            var dxf = new DxfFile();
            dxf.Entities.Add(poly);

            var pdf = await ConvertToString(dxf);
            var expected = NormalizeCrLf(@"
stream
0 w
0 0 0 RG
0 0 0 rg
72.00 72.00 m
144.00 72.00 l
216.00 144.00 m
216.00 144.00 216.00 144.00 216.00 144.00 c
144.00 72.00 m
183.76 72.00 216.00 104.24 216.00 144.00 c
216.00 144.00 m
144.00 144.00 l
144.00 144.00 m
144.00 144.00 144.00 144.00 144.00 144.00 c
144.00 144.00 m
104.24 144.00 72.00 111.76 72.00 72.00 c
S
endstream".Trim());
            Assert.Contains(expected, pdf);
        }

        [Fact]
        public async Task EmbeddedJpegTest()
        {
            var dxf = new DxfFile();
            var image = new DxfImage("image-path.jpg", new DxfPoint(1.0, 1.0, 0.0), 16, 16, new DxfVector(2.0, 2.0, 0.0));
            dxf.Entities.Add(image);
            var options = new DxfToPdfConverterOptions(
                PdfMeasurement.Inches(8.5),
                PdfMeasurement.Inches(11.0),
                1.0,
                contentResolver: path => Task.FromResult(new byte[]
                {
                    // content of image doesn't really matter
                    0x01, 0x02, 0x03, 0x04
                }));

            var pdf = await ConvertToString(dxf, options);
            Assert.Contains("144.00 0.00 0.00 144.00 72.00 72.00 cm", pdf); // transform
            Assert.Contains("/Im5 Do", pdf); // drawing instruction
            var expectedObjectHeader = NormalizeCrLf(@"
<</Type /XObject
  /Subtype /Image
  /Width 16
  /Height 16
  /ColorSpace /DeviceRGB
  /BitsPerComponent 8
  /Length 4
  /Filter [/DCTDecode]>>".Trim());
            Assert.Contains(expectedObjectHeader, pdf);
        }
    }
}
