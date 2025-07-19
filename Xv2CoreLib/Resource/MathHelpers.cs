using System;
using System.Numerics;

namespace Xv2CoreLib.Resource
{
    /// <summary>
    /// Common math functions not provided in the built-in <see cref="Math"/> namespace.
    /// </summary>
    public static class MathHelpers
    {
        public const float PI = 3.1415926535f;
        private const double FBXSDK_180_DIV_PI = 180.0 / Math.PI;
        public const double Epsilon = 0.00001;
        public const float Radians90Degrees = 1.570796f;

        public readonly static Vector3 Right = new Vector3(1, 0, 0);
        public readonly static Vector3 Left = new Vector3(-1, 0, 0);
        public readonly static Vector3 Down = new Vector3(0, -1f, 0);
        public readonly static Vector3 Up = new Vector3(0, 1f, 0);
        public readonly static Vector3 Forward = new Vector3(0, 0, -1f);
        public readonly static Vector3 Backward = new Vector3(0, 0, 1f);

        public static float ToDegrees(float radians)
        {
            return (180 / (float)Math.PI) * radians;
        }

        public static float ToRadians(float degrees)
        {
            return ((float)Math.PI / 180) * degrees;
        }

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

        public static bool IsEven(ulong value)
        {
            return (value % 2 == 0);
        }

        public static bool IsEven(int value)
        {
            return (value % 2 == 0);
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

        public static double Clamp(double min, double max, double value)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double CopySign(double value, double sign)
        {
            return sign >= 0 ? Math.Abs(value) : -Math.Abs(value);
        }

        public static bool FloatEquals(double value1, double value2)
        {
            return Math.Abs(value1 - value2) < Epsilon;
        }

        public static bool FloatEquals(float value1, float value2)
        {
            return Math.Abs(value1 - value2) < Epsilon;
        }

        public static float QuadraticBezier(float factor, float startPoint, float controlPoint, float endPoint)
        {
            return (float)((Math.Pow(1.0 - factor, 2.0) * startPoint) + (2.0 * (1.0 - factor) * factor * controlPoint) + (Math.Pow(factor, 2.0) * endPoint));
        }

        public static float CubicBezier(float factor, float startPoint, float controlPoint1, float controlPoint2, float endPoint)
        {
            return (float)((Math.Pow(1.0 - factor, 3.0) * startPoint) + (3.0 * Math.Pow(1.0 - factor, 2.0) * factor * controlPoint1) + (3.0 * (1.0 - factor) * Math.Pow(factor, 2.0) * controlPoint2) + (Math.Pow(factor, 3.0) * endPoint));
        }

        public static Quaternion EulerToQuaternion(Vector3 euler)
        {
            float roll = ToRadians(euler.X);
            float pitch = ToRadians(euler.Y);
            float yaw = ToRadians(euler.Z);

            float cy = (float)Math.Cos(yaw * 0.5f);
            float sy = (float)Math.Sin(yaw * 0.5f);
            float cp = (float)Math.Cos(pitch * 0.5f);
            float sp = (float)Math.Sin(pitch * 0.5f);
            float cr = (float)Math.Cos(roll * 0.5f);
            float sr = (float)Math.Sin(roll * 0.5f);

            return new Quaternion(
                sr * cp * cy - cr * sp * sy,
                cr * sp * cy + sr * cp * sy,
                cr * cp * sy - sr * sp * cy,
                cr * cp * cy + sr * sp * sy 
            );
        }

        public static Vector3 QuaternionToEuler(Quaternion q)
        {
            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            float roll = (float)Math.Atan2(sinr_cosp, cosr_cosp); // X-axis

            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            float pitch = (float)(Math.Abs(sinp) >= 1 ? CopySign(PI / 2, sinp) : Math.Asin(sinp)); // Y-axis

            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            float yaw = (float)Math.Atan2(siny_cosp, cosy_cosp); // Z-axis

            return new Vector3(roll, pitch, yaw) * (180f / PI); // Convert to degrees
        }

        public static Vector3 ExtractScaleFromMatrix(Matrix4x4 matrix)
        {
            return new Vector3(
                new Vector3(matrix.M11, matrix.M12, matrix.M13).Length(),  //X scale
                new Vector3(matrix.M21, matrix.M22, matrix.M23).Length(),  //Y scale
                new Vector3(matrix.M31, matrix.M32, matrix.M33).Length()   //Z scale
            );
        }

        public static Matrix4x4 Invert(Matrix4x4 matrix)
        {
            Matrix4x4.Invert(matrix, out var result);
            return result;
        }

        //Unused, testing;
        public static Quaternion AxisAngleToQuaternion(Vector3 axis, float angleDegrees)
        {
            float angleRadians = ToRadians(angleDegrees) * 0.5f;
            float sinHalfAngle = (float)Math.Sin(angleRadians);
            float cosHalfAngle = (float)Math.Cos(angleRadians);

            return new Quaternion(
                axis.X * sinHalfAngle,
                axis.Y * sinHalfAngle,
                axis.Z * sinHalfAngle,
                cosHalfAngle
            );
        }

        public static void QuaternionToAxisAngle(Quaternion q, out Vector3 axis, out float angleDegrees)
        {
            if (q.W > 1.0f)
                q = Quaternion.Normalize(q);

            float angleRadians = 2.0f * (float)Math.Acos(q.W);
            float sinHalfAngle = (float)Math.Sqrt(1.0f - q.W * q.W);

            if (sinHalfAngle < 0.0001f)
            {
                axis = new Vector3(1, 0, 0);
            }
            else
            {
                axis = new Vector3(q.X, q.Y, q.Z) / sinHalfAngle;
            }

            angleDegrees = ToDegrees(angleRadians);
        }

        public static Quaternion MultiplyQuaternions(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(
                q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y,
                q1.W * q2.Y + q1.Y * q2.W + q1.Z * q2.X - q1.X * q2.Z,
                q1.W * q2.Z + q1.Z * q2.W + q1.X * q2.Y - q1.Y * q2.X,
                q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z
            );
        }

    }
}