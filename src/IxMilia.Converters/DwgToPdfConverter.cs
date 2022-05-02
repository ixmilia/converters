using System;
using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Pdf;

// Due to the deep similarities between DXF<->DWG files, it only makes sense to implement the DWG->* converters as DWG->DXF->*
namespace IxMilia.Converters
{
    public class ConverterDwgRect : ConverterDxfRect
    {
        public ConverterDwgRect()
            : base()
        {
        }

        public ConverterDwgRect(double left, double right, double bottom, double top)
            : base(left, right, bottom, top)
        {
        }
    }

    public class DwgToPdfConverterOptions : DxfToPdfConverterOptions
    {
        public DwgToPdfConverterOptions(PdfMeasurement pageWidth, PdfMeasurement pageHeight, double scale, Func<string, Task<byte[]>> contentResolver = null)
            : base(pageWidth, pageHeight, scale, contentResolver)
        {
        }

        public DwgToPdfConverterOptions(PdfMeasurement pageWidth, PdfMeasurement pageHeight, ConverterDwgRect dwgSource, ConverterPdfRect pdfDestination, Func<string, Task<byte[]>> contentResolver = null)
            : base(pageWidth, pageHeight, dwgSource, pdfDestination, contentResolver)
        {
        }

        public class DwgToPdfConverter : IConverter<DwgDrawing, PdfFile, DwgToPdfConverterOptions>
        {
            public async Task<PdfFile> Convert(DwgDrawing source, DwgToPdfConverterOptions options)
            {
                var dxfConverter = new DwgToDxfConverter();
                var dxfConverterOptions = new DwgToDxfConverterOptions(source.FileHeader.Version.ToDxfVersion());
                var dxfFile = await dxfConverter.Convert(source, dxfConverterOptions);

                var pdfConverter = new DxfToPdfConverter();
                var pdfConverterOptions = options;
                var pdfFile = await pdfConverter.Convert(dxfFile, pdfConverterOptions);

                return pdfFile;
            }
        }
    }
}
