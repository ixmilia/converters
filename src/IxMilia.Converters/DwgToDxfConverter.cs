using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;

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

            // blocks
            foreach (var blockHeader in source.BlockHeaders.Values)
            {
                var dxfBlock = new DxfBlock()
                {
                    BasePoint = blockHeader.BasePoint.ToDxfPoint(),
                    Name = blockHeader.Name,
                    Layer = blockHeader.Block.Layer.Name,
                };
                foreach (var entity in blockHeader.Entities)
                {
                    var dxfEntity = ConvertEntity(entity);
                    dxfBlock.Entities.Add(dxfEntity);
                }

                result.Blocks.Add(dxfBlock);
            }

            // layers
            foreach (var layer in source.Layers.Values)
            {
                result.Layers.Add(new DxfLayer(layer.Name, layer.Color.ToDxfColor()));
            }

            // line types
            foreach (var lineType in source.LineTypes.Values)
            {
                var dxfLineType = new DxfLineType(lineType.Name)
                {
                    Description = lineType.Description,
                    TotalPatternLength = lineType.PatternLength
                };
                foreach (var dashInfo in lineType.DashInfos)
                {
                    var dashElement = new DxfLineTypeElement() { DashDotSpaceLength = dashInfo.DashLength };
                    dxfLineType.Elements.Add(dashElement);
                }

                result.LineTypes.Add(dxfLineType);
            }

            // entities
            foreach (var entity in source.ModelSpaceBlockRecord.Entities)
            {
                var dxfEntity = ConvertEntity(entity);
                if (dxfEntity is not null)
                {
                    result.Entities.Add(dxfEntity);
                }
            }

            return Task.FromResult(result);
        }

        private static DxfEntity ConvertEntity(DwgEntity entity)
        {
            return entity switch
            {
                DwgArc arc => arc.ToDxfArc(),
                DwgCircle circle => circle.ToDxfCircle(),
                DwgEllipse ellipse => ellipse.ToDxfEllipse(),
                DwgInsert insert => insert.ToDxfInsert(),
                DwgLine line => line.ToDxfLine(),
                DwgLocation location => location.ToDxfModelPoint(),
                DwgLwPolyline lwpolyline => lwpolyline.ToDxfLwPolyline(),
                DwgPolyline2D polyline2d => polyline2d.ToDxfPolyline(),
                DwgPolyline3D polyline3D => polyline3D.ToDxfPolyline(),
                DwgSpline spline => spline.ToDxfSpline(),
                DwgText text => text.ToDxfText(),
                _ => null,
            };
        }
    }
}
