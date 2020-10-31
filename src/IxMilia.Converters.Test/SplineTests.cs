using System.Linq;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class SplineTests : TestBase
    {
        private static void AssertClose(SplinePoint2 expected, SplinePoint2 actual)
        {
            Assert.True(
                AreClose(expected.X, actual.X) &&
                AreClose(expected.Y, actual.Y),
                $"Expected: {expected}\nActual: {actual}");
        }

        [Fact]
        public void InsertKnot()
        {
            var spline = new Spline2(
                3,
                Enumerable.Repeat(new SplinePoint2(0.0, 0.0), 8),
                new[] { 0.0, 0.0, 0.0, 0.0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.0, 1.0, 1.0 });
            spline.InsertKnot(0.5);
            Assert.Equal(9, spline.ControlPoints.Count());
            Assert.Equal(13, spline.KnotValues.Count());
            Assert.Equal(new[] { 0.0, 0.0, 0.0, 0.0, 0.2, 0.4, 0.5, 0.6, 0.8, 1.0, 1.0, 1.0, 1.0 }, spline.KnotValues);
        }

        [Fact]
        public void SplineToBezier()
        {
            var spline = new Spline2(
                3,
                new[]
                {
                    new SplinePoint2(59.1, 66.8),
                    new SplinePoint2(63.1, 81.7),
                    new SplinePoint2(127.2, 93.7),
                    new SplinePoint2(100.1, 12.9),
                    new SplinePoint2(55.4, 52.8),
                    new SplinePoint2(59.1, 66.8)
                },
                new[] { 0.0, 0.0, 0.0, 0.0, 0.36, 0.65, 1.0, 1.0, 1.0, 1.0 });
            var beziers = spline.ToBeziers();

            Assert.Equal(3, beziers.Count);

            AssertClose(new SplinePoint2(59.1, 66.8), beziers[0].Start);
            AssertClose(new SplinePoint2(63.1, 81.7), beziers[0].Control1);
            AssertClose(new SplinePoint2(98.6015384615385, 88.3461538461538), beziers[0].Control2);
            AssertClose(new SplinePoint2(109.037363313609, 75.2010840236686), beziers[0].End);

            AssertClose(new SplinePoint2(109.037363313609, 75.2010840236686), beziers[1].Start);
            AssertClose(new SplinePoint2(117.444, 64.612), beziers[1].Control1);
            AssertClose(new SplinePoint2(109.585, 41.18), beziers[1].Control2);
            AssertClose(new SplinePoint2(96.1092041015625, 36.5579833984375), beziers[1].End);

            AssertClose(new SplinePoint2(96.1092041015625, 36.5579833984375), beziers[2].Start);
            AssertClose(new SplinePoint2(79.8453125, 30.9796875), beziers[2].Control1);
            AssertClose(new SplinePoint2(55.4, 52.8), beziers[2].Control2);
            AssertClose(new SplinePoint2(59.1, 66.8), beziers[2].End);
        }
    }
}
