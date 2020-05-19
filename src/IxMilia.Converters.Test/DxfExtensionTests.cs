using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class DxfExtensionTests : TestBase
    {
        private static void AssertClose(DxfPoint expected, DxfPoint actual)
        {
            Assert.True(
                AreClose(expected.X, actual.X) &&
                AreClose(expected.Y, actual.Y) &&
                AreClose(expected.Z, actual.Z),
                $"Expected: {expected}\nActual: {actual}");
        }

        [Fact]
        public void CircleAngleToPointTest()
        {
            var circle = new DxfCircle(new DxfPoint(1.0, 2.0, 3.0), 4.0);
            AssertClose(new DxfPoint(5.0, 2.0, 3.0), circle.GetPointFromAngle(0.0));
            AssertClose(new DxfPoint(1.0, 6.0, 3.0), circle.GetPointFromAngle(90.0));
            AssertClose(new DxfPoint(-3.0, 2.0, 3.0), circle.GetPointFromAngle(180.0));
            AssertClose(new DxfPoint(1.0, -2.0, 3.0), circle.GetPointFromAngle(270.0));
            AssertClose(new DxfPoint(5.0, 2.0, 3.0), circle.GetPointFromAngle(360.0));
        }
    }
}
