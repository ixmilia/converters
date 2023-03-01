using System;
using System.Linq;
using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DwgToDxfTests : TestBase
    {
        private static Task<DxfFile> Convert(DwgDrawing dwg, DxfAcadVersion targetVersion)
        {
            var converter = new DwgToDxfConverter();
            var options = new DwgToDxfConverterOptions(targetVersion);
            return converter.Convert(dwg, options);
        }

        private static async Task<TEntity> Convert<TEntity>(DwgEntity entity) where TEntity : DxfEntity
        {
            var dwg = new DwgDrawing();
            entity.Layer = dwg.CurrentLayer;
            dwg.ModelSpaceBlockRecord.Entities.Add(entity);
            var dxf = await Convert(dwg, DxfAcadVersion.R14);
            var resultEntity = dxf.Entities.Single();
            var typedEntity = (TEntity)resultEntity;
            return typedEntity;
        }

        [Fact]
        public async Task DrawingWithSingleEntityOnDefaultLayer()
        {
            var dwg = new DwgDrawing();
            dwg.ModelSpaceBlockRecord.Entities.Add(new DwgLine(new DwgPoint(1.0, 2.0, 0.0), new DwgPoint(3.0, 4.0, 0.0)) { Layer = dwg.CurrentLayer });

            var dxf = await Convert(dwg, DxfAcadVersion.R14);
            var line = (DxfLine)dxf.Entities.Single();
            Assert.Equal("0", dxf.Layers.Single().Name);
            Assert.Equal("0", line.Layer);
            Assert.Equal(new DxfPoint(1.0, 2.0, 0.0), line.P1);
            Assert.Equal(new DxfPoint(3.0, 4.0, 0.0), line.P2);
        }

        [Fact]
        public async Task ArcAnglesArePropertyConverted()
        {
            var arc = await Convert<DxfArc>(new DwgArc(new DwgPoint(1.0, 2.0, 3.0), 4.0, startAngle: 0.0, endAngle: Math.PI));
            Assert.Equal(new DxfPoint(1.0, 2.0, 3.0), arc.Center);
            Assert.Equal(4.0, arc.Radius);
            Assert.Equal(0.0, arc.StartAngle);
            Assert.Equal(180.0, arc.EndAngle);
        }

        [Fact]
        public async Task TextRotationAngleIsPropertyConverted()
        {
            var text = await Convert<DxfText>(new DwgText("sample-text") { InsertionPoint = new DwgPoint(1.0, 2.0, 3.0), Height = 4.0, RotationAngle = Math.PI });
            Assert.Equal(new DxfPoint(1.0, 2.0, 3.0), text.Location);
            Assert.Equal(4.0, text.TextHeight);
            Assert.Equal("sample-text", text.Value);
            Assert.Equal(180.0, text.Rotation);
        }

        [Fact]
        public async Task BlocksAndInsertsAreConverted()
        {
            // create dwg
            var drawing = new DwgDrawing();

            var line = new DwgLine(new DwgPoint(1.0, 2.0, 0.0), new DwgPoint(3.0, 4.0, 0.0));
            line.Layer = drawing.CurrentLayer;

            var blockHeader = DwgBlockHeader.CreateBlockRecordWithName("my-block", drawing.CurrentLayer);
            blockHeader.Entities.Add(line);
            drawing.BlockHeaders.Add(blockHeader);

            var insert = new DwgInsert(blockHeader);
            insert.Location = new DwgPoint(5.0, 6.0, 0.0);
            insert.Layer = drawing.CurrentLayer;

            drawing.ModelSpaceBlockRecord.Entities.Add(insert);

            // convert
            var dxf = await Convert(drawing, DxfAcadVersion.R14);

            // check
            var dxfBlock = dxf.Blocks.Single(b => b.Name == "my-block");
            var dxfLine = (DxfLine)dxfBlock.Entities.Single();
            Assert.Equal(new DxfPoint(1.0, 2.0, 0.0), dxfLine.P1);
            Assert.Equal(new DxfPoint(3.0, 4.0, 0.0), dxfLine.P2);
            Assert.Equal("0", dxfLine.Layer);

            var dxfInsert = (DxfInsert)dxf.Entities.Single();
            Assert.Equal(dxfBlock.Name, dxfInsert.Name);
            Assert.Equal(new DxfPoint(5.0, 6.0, 0.0), dxfInsert.Location);
        }
    }
}
