using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;


namespace IxMilia.Converters
{
    public static class MathExtensions
    {
        public static double ToDegree(this double radian)
        {
            return radian * 180 / Math.PI;
        }

        public static double ToRadian(this double degree)
        {
            return degree * Math.PI / 180;
        }

        public static double Magnitude(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        public static int GetQuadrant(double x, double y)
        {
            if (x >= 0 && y >= 0)
            {
                return 1; // Quadrant 1
            }
            else if (x < 0 && y >= 0)
            {
                return 2; // Quadrant 2
            }
            else if (x < 0 && y < 0)
            {
                return 3; // Quadrant 3
            }
            else
            {
                return 4; // Quadrant 4
            }
        }

        public static void CalcXAxisRotation(double majorAxisX, double majorAxisY, out double axisAngle,  bool considerQuadrants = true)
        {
            int quadrant = MathExtensions.GetQuadrant(majorAxisX, majorAxisY);
            axisAngle = Math.Atan2(majorAxisY, majorAxisX);

            if (considerQuadrants)
            {
                axisAngle = quadrant <= 2 ? axisAngle : axisAngle + Math.PI * 2;
            }
        }

    }
}
