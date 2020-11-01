using System;
using System.Collections.Generic;
using System.Text;

namespace IxMilia.Converters
{
    public static class DoubleExtensions
    {
        public static bool IsCloseTo(this double a, double b)
        {
            return Math.Abs(a - b) < 1.0e-10;
        }
    }
}
