using IxMilia.Pdf;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class ConverterPdfRectTests
    {
        [Theory]
        [InlineData(16, 9)]
        [InlineData(9, 16)]
        public void AspectRatioIsKept(int maxWidth, int maxHeight)
        {
            ConverterDxfRect src = new ConverterDxfRect(0, 5, 0, 5);
            PdfMeasurement pdfMaxWidth = PdfMeasurement.Points(maxWidth);
            PdfMeasurement pdfMaxHeight = PdfMeasurement.Points(maxHeight);
            ConverterPdfRect sut = ConverterPdfRect.KeepAspectRatio(src, pdfMaxWidth, pdfMaxHeight);

            Assert.True(sut.Width.AsPoints() <= maxWidth);
            Assert.True(sut.Height.AsPoints() <= maxHeight);

            Assert.Equal(src.Width, src.Height); // otherwise test below is wrong
            if (maxWidth > maxHeight)
            {
                Assert.Equal(pdfMaxHeight, sut.Height);
            }
            else
            {
                Assert.Equal(pdfMaxWidth, sut.Width);
            }
        }
    }
}