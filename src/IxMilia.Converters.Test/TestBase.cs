using System;
using Xunit;

namespace IxMilia.Converters.Test
{
    public abstract class TestBase
    {
        private const double Epsilon = 1.0e-10;

        public static bool AreClose(double expected, double actual)
        {
            var delta = Math.Abs(expected - actual);
            return delta < Epsilon;
        }

        public static void AssertClose(double expected, double actual)
        {
            Assert.True(AreClose(expected, actual), $"Expected: {expected}\nActual: {actual}");
        }

        public static string NormalizeCrLf(string value)
        {
            return value.Replace("\r", "").Replace("\n", "\r\n");
        }
    }
}
