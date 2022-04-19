using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DxfToDwgTests : TestBase
    {
        private static Task<DwgDrawing> Convert(DxfFile dxf, DwgVersionId targetVersion)
        {
            var converter = new DxfToDwgConverter();
            var options = new DxfToDwgConverterOptions(targetVersion);
            return converter.Convert(dxf, options);
        }

        private static async Task<TEntity> Convert<TEntity>(DxfEntity entity) where TEntity : DwgEntity
        {
            var dxfFile = new DxfFile();
            dxfFile.Entities.Add(entity);
            var dwg = await Convert(dxfFile, DwgVersionId.R14);
            var resultEntity = dwg.ModelSpaceBlockRecord.Entities.Single();
            var typedEntity = (TEntity)resultEntity;
            return typedEntity;
        }

        [Fact]
        public async Task DrawingWithSingleEntityOnDefaultLayer()
        {
            var dxf = new DxfFile();
            dxf.Entities.Add(new DxfLine(new DxfPoint(1.0, 2.0, 0.0), new DxfPoint(3.0, 4.0, 0.0))
            {
                Layer = "0"
            });

            var dwg = await Convert(dxf, DwgVersionId.R14);
            var line = (DwgLine)dwg.ModelSpaceBlockRecord.Entities.Single();
            Assert.Same(dwg.Layers.Values.Single(), line.Layer);
            Assert.Equal("0", line.Layer.Name);
            Assert.Equal(new DwgPoint(1.0, 2.0, 0.0), line.P1);
            Assert.Equal(new DwgPoint(3.0, 4.0, 0.0), line.P2);
        }

        [Fact]
        public async Task ArcAnglesArePropertyConverted()
        {
            var arc = await Convert<DwgArc>(new DxfArc(new DxfPoint(1.0, 2.0, 3.0), 4.0, startAngle: 0.0, endAngle: 180.0));
            Assert.Equal(new DwgPoint(1.0, 2.0, 3.0), arc.Center);
            Assert.Equal(4.0, arc.Radius);
            Assert.Equal(0.0, arc.StartAngle);
            Assert.Equal(Math.PI, arc.EndAngle);
        }

        [Fact]
        public async Task LayersAndLineTypesAreAddedToABareDXF()
        {
            var dxfText = string.Join("\r\n", new[]
            {
                "  0", "SECTION",
                "  2", "ENTITIES",
                "  0", "LINE",
                "  8", "layer-that-does-not-exist", // this layer isn't defined here, but should end up in the DWG
                "  6", "line-type-that-does-not-exist", // this line type isn't defined here, but should end up in the DWG
                "  0", "ENDSEC",
                "  0", "EOF",
            });
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, new UTF8Encoding(false));
            writer.WriteLine(dxfText);
            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            var dxf = DxfFile.Load(ms);
            var dwg = await Convert(dxf, DwgVersionId.R14);
            var line = (DwgLine)dwg.ModelSpaceBlockRecord.Entities.Single();
            Assert.Equal("layer-that-does-not-exist", line.Layer.Name);
            Assert.Equal("line-type-that-does-not-exist", line.LineType.Name);
        }
    }
}
