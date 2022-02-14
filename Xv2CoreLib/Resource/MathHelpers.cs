using System;

namespace Xv2CoreLib.Resource
{
    /// <summary>
    /// Common math functions not provided in the built-in <see cref="Math"/> namespace.
    /// </summary>
    public static class MathHelpers
    {
        public static double ConvertRadiansToDegrees(double radians)
        {
            return (180 / Math.PI) * radians;
        }

        public static double ConvertDegreesToRadians(double degrees)
        {
            return (Math.PI / 180) * degrees;
        }

        public static float Lerp(float value1, float value2, float amount)
        {
            return value1 + ((value2 - value1) * amount);
        }

        public static bool IsPowerOfTwo(int value)
        {
            return (value & (value - 1)) == 0 && value != 0;
        }

        public static int Clamp(int min, int max, int value)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Clamp(float min, float max, float value)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
