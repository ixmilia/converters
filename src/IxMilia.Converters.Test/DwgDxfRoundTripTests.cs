using System.Linq;
using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DwgDxfRoundTripTests
    {
        // very specific tests exist in both `DwgToDxfTests.cs` and `DxfToDwgTests.cs`, but most of the coverage can be
        // "free" if drawings are round-tripped
        private static async Task<DxfFile> RoundTrip(DxfFile dxf)
        {
            var dwgConverter = new DxfToDwgConverter();
            var dwg = await dwgConverter.Convert(dxf, new DxfToDwgConverterOptions(DwgVersionId.R14));

            var dxfConverter = new DwgToDxfConverter();
            var roundTrippedDxf = await dxfConverter.Convert(dwg, new DwgToDxfConverterOptions(dxf.Header.Version));

            return roundTrippedDxf;
        }

        private static async Task<TEntity> RoundTrip<TEntity>(TEntity entity) where TEntity : DxfEntity
        {
            var dxfFile = new DxfFile();
            dxfFile.Entities.Add(entity);
            var roundTrippedDxf = await RoundTrip(dxfFile);
            var resultEntity = roundTrippedDxf.Entities.Single();
            var typedEntity = (TEntity)resultEntity;
            return typedEntity;
        }

        [Fact]
        public async Task RoundTripCommonEntityValues()
        {
            var e1 = new DxfLine();
            var e2 = await RoundTrip(e1);

            // these properties are common to all entities
            Assert.Equal(e1.Color, e2.Color);
            Assert.Equal(e1.Elevation, e2.Elevation);
            Assert.Equal(e1.Layer, e2.Layer);
            Assert.Equal(e1.LineTypeName, e2.LineTypeName);
            Assert.Equal(e1.LineTypeScale, e2.LineTypeScale);
            Assert.Equal(e1.Transparency, e2.Transparency);
        }

        [Fact]
        public async Task RoundTripArc()
        {
            var e1 = new DxfArc(new DxfPoint(1.0, 2.0, 3.0), 4.0, 0.0, 90.0) { Thickness = 5.0 };
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.Center, e2.Center);
            Assert.Equal(e1.Radius, e2.Radius);
            Assert.Equal(e1.StartAngle, e2.StartAngle);
            Assert.Equal(e1.EndAngle, e2.EndAngle);
            Assert.Equal(e1.Normal, e2.Normal);
            Assert.Equal(e1.Thickness, e2.Thickness);
        }

        [Fact]
        public async Task RoundTripCircle()
        {
            var e1 = new DxfCircle(new DxfPoint(1.0, 2.0, 3.0), 4.0) { Thickness = 5.0 };
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.Center, e2.Center);
            Assert.Equal(e1.Radius, e2.Radius);
            Assert.Equal(e1.Normal, e2.Normal);
            Assert.Equal(e1.Thickness, e2.Thickness);
        }

        [Fact]
        public async Task RoundTripLine()
        {
            var e1 = new DxfLine(new DxfPoint(1.0, 2.0, 3.0), new DxfPoint(4.0, 5.0, 6.0)) { Thickness = 7.0 };
            var e2 = await RoundTrip(e1);
            Assert.Equal(e1.P1, e2.P1);
            Assert.Equal(e1.P2, e2.P2);
            Assert.Equal(e1.Thickness, e2.Thickness);
        }
    }
}
