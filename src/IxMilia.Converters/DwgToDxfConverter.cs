using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;

namespace IxMilia.Converters
{
    public struct DwgToDxfConverterOptions
    {
        public DxfAcadVersion TargetVersion { get; set; }

        public DwgToDxfConverterOptions(DxfAcadVersion targetVersion)
        {
            TargetVersion = targetVersion;
        }
    }

    public class DwgToDxfConverter : IConverter<DwgDrawing, DxfFile, DwgToDxfConverterOptions>
    {
        public Task<DxfFile> Convert(DwgDrawing source, DwgToDxfConverterOptions options)
        {
            var result = new DxfFile();
            result.Layers.Clear();
            result.Header.Version = options.TargetVersion;
            result.Header.CurrentLayer = source.CurrentLayer.Name;

            result.ActiveViewPort.LowerLeft = source.ViewPorts["*ACTIVE"].LowerLeft.ToDxfPoint();
            result.ActiveViewPort.ViewHeight = source.ViewPorts["*ACTIVE"].Height;

            // TODO: convert the other things
            foreach (var layer in source.Layers.Values)
            {
                result.Layers.Add(new DxfLayer(layer.Name, layer.Color.ToDxfColor()));
            }

            // entities
            foreach (var entity in source.ModelSpaceBlockRecord.Entities)
            {
                switch (entity)
                {
                    case DwgArc arc:
                        result.Entities.Add(arc.ToDxfArc());
                        break;
                    case DwgCircle circle:
                        result.Entities.Add(circle.ToDxfCircle());
                        break;
                    case DwgEllipse ellipse:
                        result.Entities.Add(ellipse.ToDxfEllipse());
                        break;
                    case DwgLine line:
                        result.Entities.Add(line.ToDxfLine());
                        break;
                    case DwgLocation location:
                        result.Entities.Add(location.ToDxfModelPoint());
                        break;
                    case DwgLwPolyline lwpolyline:
                        result.Entities.Add(lwpolyline.ToDxfLwPolyline());
                        break;
                    case DwgPolyline2D polyline2d:
                        result.Entities.Add(polyline2d.ToDxfPolyline());
                        break;
                    case DwgPolyline3D polyline3d:
                        result.Entities.Add(polyline3d.ToDxfPolyline());
                        break;
                    case DwgSpline spline:
                        result.Entities.Add(spline.ToDxfSpline());
                        break;
                    case DwgText text:
                        result.Entities.Add(text.ToDxfText());
                        break;
                }
            }

            return Task.FromResult(result);
        }
    }
}
