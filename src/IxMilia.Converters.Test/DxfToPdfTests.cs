using System.IO;
using System.Text;
using IxMilia.Dxf;
using IxMilia.Pdf;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DxfToPdfTests : TestBase<DxfFile, PdfFile>
    {
        public override IConverter<DxfFile, PdfFile> GetConverter()
        {
            var options = new DxfToPdfConverterOptions(PdfMeasurement.Inches(8.5), PdfMeasurement.Inches(11.0));
            return new DxfToPdfConverter(options);
        }

        private string ConvertToString(DxfFile dxf)
        {
            var pdf = Convert(dxf);
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
    }
}
