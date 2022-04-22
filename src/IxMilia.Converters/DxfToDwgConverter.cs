using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace IxMilia.Converters
{
    public struct DxfToDwgConverterOptions
    {
        public DwgVersionId TargetVersion { get; set; }

        public DxfToDwgConverterOptions(DwgVersionId targetVersion)
        {
            TargetVersion = targetVersion;
        }
    }

    public class DxfToDwgConverter : IConverter<DxfFile, DwgDrawing, DxfToDwgConverterOptions>
    {
        public Task<DwgDrawing> Convert(DxfFile source, DxfToDwgConverterOptions options)
        {
            var target = new DwgDrawing();
            target.FileHeader.Version = options.TargetVersion;
            target.LineTypes.Clear();
            target.Layers.Clear();

            // TODO: all the other things like header values, etc.

            ConvertActiveViewPortSettings(source, target);
            ConvertLineTypes(source, target);
            ConvertLayers(source, target);
            ConvertEntities(source, target);

            return Task.FromResult(target);
        }

        private static void ConvertActiveViewPortSettings(DxfFile source, DwgDrawing target)
        {
            if (source.ActiveViewPort is object)
            {
                target.ViewPorts["*ACTIVE"].LowerLeft = source.ActiveViewPort.LowerLeft.ToDwgPoint();
                target.ViewPorts["*ACTIVE"].Height = source.ActiveViewPort.ViewHeight;
            }
        }

        private static void ConvertLineTypes(DxfFile source, DwgDrawing target)
        {
            foreach (var lineType in source.LineTypes)
            {
                var dwgLineType = new DwgLineType(lineType.Name)
                {
                    Description = lineType.Description,
                    PatternLength = lineType.TotalPatternLength,
                };
                target.LineTypes.Add(dwgLineType);
            }
            target.CurrentEntityLineType = target.LineTypeOrCurrent(source.Header.CurrentEntityLineType);
        }

        private static void ConvertLayers(DxfFile source, DwgDrawing target)
        {
            foreach (var layer in source.Layers)
            {
                var dwgLayer = new DwgLayer(layer.Name)
                {
                    Color = layer.Color.ToDwgColor(),
                    LineType = target.LineTypeOrCurrent(layer.LineTypeName),
                };
                target.Layers.Add(dwgLayer);
            }

            target.CurrentLayer = target.LayerOrCurrent(source.Header.CurrentLayer);
            target.ModelSpaceBlockRecord.Block.Layer = target.LayerOrCurrent(target.ModelSpaceBlockRecord.Block.Layer.Name);
            target.ModelSpaceBlockRecord.EndBlock.Layer = target.LayerOrCurrent(target.ModelSpaceBlockRecord.Block.Layer.Name);
            target.PaperSpaceBlockRecord.Block.Layer = target.LayerOrCurrent(target.PaperSpaceBlockRecord.Block.Layer.Name);
            target.PaperSpaceBlockRecord.EndBlock.Layer = target.LayerOrCurrent(target.PaperSpaceBlockRecord.Block.Layer.Name);
        }

        private static void ConvertEntities(DxfFile source, DwgDrawing target)
        {
            foreach (var entity in source.Entities)
            {
                switch (entity)
                {
                    case DxfArc arc:
                        AddToDrawing(arc.ToDwgArc(), arc.Layer, entity.LineTypeName);
                        break;
                    case DxfCircle circle:
                        AddToDrawing(circle.ToDwgCircle(), circle.Layer, entity.LineTypeName);
                        break;
                    case DxfEllipse ellipse:
                        AddToDrawing(ellipse.ToDwgEllipse(), ellipse.Layer, ellipse.LineTypeName);
                        break;
                    case DxfLine line:
                        AddToDrawing(line.ToDwgLine(), line.Layer, entity.LineTypeName);
                        break;
                    case DxfModelPoint modelPoint:
                        AddToDrawing(modelPoint.ToDwgLocation(), modelPoint.Layer, entity.LineTypeName);
                        break;
                    case DxfLwPolyline lwpolyline:
                        AddToDrawing(lwpolyline.ToDwgLwPolyline(), lwpolyline.Layer, entity.LineTypeName);
                        break;
                    case DxfPolyline polyline:
                        AddToDrawing(polyline.ToDwgPolyline(), polyline.Layer, entity.LineTypeName);
                        break;
                    case DxfSpline spline:
                        AddToDrawing(spline.ToDwgSpline(), spline.Layer, entity.LineTypeName);
                        break;
                    case DxfText text:
                        AddToDrawing(text.ToDwgText(), text.Layer, text.LineTypeName);
                        break;
                    default:
                        // TODO: everything else
                        break;
                }
            }

            void AddToDrawing(DwgEntity entity, string layerName, string lineTypeName)
            {
                entity.Layer = target.EnsureLayer(layerName, DwgColor.FromIndex(1), lineTypeName);
                entity.LineType = target.EnsureLineType(lineTypeName);
                target.ModelSpaceBlockRecord.Entities.Add(entity);
            }
        }
    }
}
