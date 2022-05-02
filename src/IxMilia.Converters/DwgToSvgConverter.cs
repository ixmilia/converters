using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using IxMilia.Dwg;

// Due to the deep similarities between DXF<->DWG files, it only makes sense to implement the DWG->* converters as DWG->DXF->*
namespace IxMilia.Converters
{
    public class DwgToSvgConverterOptions : DxfToSvgConverterOptions
    {
        public DwgToSvgConverterOptions(ConverterDwgRect dwgRect, ConverterSvgRect svgRect, string svgId = null, Func<string, Task<string>> imageHrefResolver = null)
            : base(dwgRect, svgRect, svgId, imageHrefResolver)
        {
        }
    }

    public class DwgToSvgConverter : IConverter<DwgDrawing, XElement, DwgToSvgConverterOptions>
    {
        public async Task<XElement> Convert(DwgDrawing source, DwgToSvgConverterOptions options)
        {
            var dxfConverter = new DwgToDxfConverter();
            var dxfConverterOptions = new DwgToDxfConverterOptions(source.FileHeader.Version.ToDxfVersion());
            var dxfFile = await dxfConverter.Convert(source, dxfConverterOptions);

            var svgConverter = new DxfToSvgConverter();
            var svgConverterOptions = new DxfToSvgConverterOptions(options.DxfRect, options.SvgRect, options.SvgId, options.ImageHrefResolver);
            var svg = await svgConverter.Convert(dxfFile, svgConverterOptions);

            return svg;
        }
    }
}
