using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IxMilia.Converters.Test
{
    public class MathExtensionTests
    {
        [Theory]
        [MemberData(nameof(CalcXAxisRotation_TestData))]
        public void CalcXAxisRotation_Test(double majorAxisX, double majorAxisY, double expected_radian, double expected_degree)
        {
            double radian = 0;
            double degree = 0;
            MathExtensions.CalcXAxisRotation(majorAxisX, majorAxisY, out radian);

            Assert.Equal(expected_radian, radian);
            Assert.Equal(expected_degree, radian.ToDegree());
            


        }

        public static IEnumerable<object[]> CalcXAxisRotation_TestData => GetCalcXAxisRotation_TestData();

        private static IEnumerable<object[]> GetCalcXAxisRotation_TestData()
        {

            yield return new object[] { 1, 0, 0, 0 };
            yield return new object[] { 1, 1, Math.PI / 4, 45 };
            yield return new object[] { 0, 1, 2 * Math.PI / 4, 90 };
            yield return new object[] { -1, 1, 3 * Math.PI / 4, 135 };
            yield return new object[] { -1, 0, Math.PI, 180 };
            yield return new object[] { -1, -1, 5 * Math.PI / 4, 225 };
            yield return new object[] { 0, -1, 6 * Math.PI / 4, 270 };
            yield return new object[] { 1, -1, 7 * Math.PI / 4, 315 };
        }
    }
}
