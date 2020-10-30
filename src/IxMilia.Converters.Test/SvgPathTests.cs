using Xunit;

namespace IxMilia.Converters.Test
{
    public class SvgPathTests : TestBase
    {
        [Fact]
        public void SvgPathToStringTest()
        {
            var path = new SvgPath(new SvgPathSegment[]
            {
                new SvgMoveToPath(1.0, 2.0),
                new SvgArcToPath(3.0, 4.0, 5.0, true, false, 6.0, 7.0)
            });
            Assert.Equal("M 1.0 2.0 A 3.0 4.0 5.0 1 0 6.0 7.0", path.ToString());

            path = new SvgPath(new SvgPathSegment[]
            {
                new SvgMoveToPath(1.0, 2.0),
                new SvgArcToPath(3.0, 4.0, 5.0, true, false, 6.0, 7.0),
                new SvgArcToPath(8.0, 9.0, 10.0, false, true, 11.0, 12.0)
            });
            Assert.Equal("M 1.0 2.0 A 3.0 4.0 5.0 1 0 6.0 7.0 A 8.0 9.0 10.0 0 1 11.0 12.0", path.ToString());
        }

        [Fact]
        public void SvgCubicBezierPathToStringTest()
        {
            var cubic = new SvgCubicBezierToPath(1.0, 2.0, 3.0, 4.0, 5.0, 6.0);
            var expected = "C 1.0 2.0, 3.0 4.0, 5.0 6.0";
            var actual = cubic.ToString();
            Assert.Equal(expected, actual);
        }
    }
}
