using System;
using System.Numerics;

namespace Xv2CoreLib.Resource
{
    /// <summary>
    /// Common math functions not provided in the built-in <see cref="Math"/> namespace.
    /// </summary>
    public static class MathHelpers
    {
        private const double FBXSDK_180_DIV_PI = 180.0 / Math.PI;
        public const double Epsilon = 0.00001;
        public const float Radians90Degrees = 1.570796f;

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

        /// <summary>
        /// Returns a value with the magnitude of x and the sign of y.
        /// </summary>
        /// <param name="x">A number whose magnitude is used in the result.</param>
        /// <param name="y">A number whose sign is the used in the result.</param>
        /// <returns>A value with the magnitude of x and the sign of y.</returns>
        public static double CopySign(double x, double y)
        {
            //Implements Math.CopySign from newer .NET versions
            if ((x > 0.0 && y > 0.0) || (x < 0.0 && y < 0.0))
                return x;

            return -x;
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

        //Testing for EMA. Not good to use
        public static Quaternion EulerAnglesToQuaternion(Vector3 eulerAngles)
        {
            //https://stackoverflow.com/questions/70462758/c-sharp-how-to-convert-quaternions-to-euler-angles-xyz

            float cy = (float)Math.Cos(eulerAngles.Z * 0.5);
            float sy = (float)Math.Sin(eulerAngles.Z * 0.5);
            float cp = (float)Math.Cos(eulerAngles.Y * 0.5);
            float sp = (float)Math.Sin(eulerAngles.Y * 0.5);
            float cr = (float)Math.Cos(eulerAngles.X * 0.5);
            float sr = (float)Math.Sin(eulerAngles.X * 0.5);

            return new Quaternion
            {
                W = (cr * cp * cy + sr * sp * sy),
                X = (sr * cp * cy - cr * sp * sy),
                Y = (cr * sp * cy + sr * cp * sy),
                Z = (cr * cp * sy - sr * sp * cy)
            };

        }

        public static Vector3 QuaternionToEulerAngles(Quaternion quaternion)
        {
            //https://stackoverflow.com/questions/70462758/c-sharp-how-to-convert-quaternions-to-euler-angles-xyz

            Vector3 angles = new Vector3();

            // roll / x
            double sinr_cosp = 2 * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
            double cosr_cosp = 1 - 2 * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch / y
            double sinp = 2 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
            if (Math.Abs(sinp) >= 1)
            {
                angles.Y = (float)CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.Y = (float)Math.Asin(sinp);
            }

            // yaw / z
            double siny_cosp = 2 * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
            double cosy_cosp = 1 - 2 * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }

        public static Vector3 QuaternionToEulerAngles_OLD(Quaternion rot)
        {
            //convert into a matrix3x3
            double[] m0 = new double[3];
            double[] m1 = new double[3];
            double[] m2 = new double[3];
            quadToRotationMatrix(rot, ref m0, ref m1, ref m2);

            //convert matrix3x3 into EulerAngle
            double[] angles = MatrixToEulerAnglesZYX(m0, m1, m2);

            //angles is in ZYX format, so the values need to be flipped.
            return new Vector3((float)angles[2], (float)angles[1], (float)angles[0]);
        }

        private static double[] MatrixToEulerAnglesZYX(double[] m0, double[] m1, double[] m2)
        {
            // rot =  cy*cz           cz*sx*sy-cx*sz  cx*cz*sy+sx*sz
            //        cy*sz           cx*cz+sx*sy*sz -cz*sx+cx*sy*sz
            //       -sy              cy*sx           cx*cy

            for (int i = 0; i < 3; i++)      //few corrections, due to the float precision on quaternion.
            {
                if ((m0[i] < -1) && (Math.Abs(m0[i] - (-1)) < 0.000001))
                    m0[i] = -1;
                if ((m0[i] > 1) && (Math.Abs(m0[i] - 1) < 0.000001))
                    m0[i] = 1;

                if ((m1[i] < -1) && (Math.Abs(m1[i] - (-1)) < 0.000001))
                    m1[i] = -1;
                if ((m1[i] > 1) && (Math.Abs(m1[i] - 1) < 0.000001))
                    m1[i] = 1;

                if ((m2[i] < -1) && (Math.Abs(m2[i] - (-1)) < 0.000001))
                    m2[i] = -1;
                if ((m2[i] > 1) && (Math.Abs(m2[i] - 1) < 0.000001))
                    m2[i] = 1;
            }

            double[] YPR_angles = new double[3];

            YPR_angles[1] = Math.Asin(-m2[0]) * FBXSDK_180_DIV_PI;
            if (YPR_angles[1] < 90.0)
            {
                if (YPR_angles[1] > -90.0)
                {
                    YPR_angles[0] = Math.Atan2(m1[0], m0[0]) * FBXSDK_180_DIV_PI;
                    YPR_angles[2] = Math.Atan2(m2[1], m2[2]) * FBXSDK_180_DIV_PI;

                    return YPR_angles;
                }
                else
                {
                    // WARNING.  Not a unique solution.
                    double fRmY = Math.Atan2(-m0[1], m0[2]) * FBXSDK_180_DIV_PI;
                    YPR_angles[2] = 0.0;  // any angle works
                                          //YPR_angles[0] = YPR_angles[2] - fRmY;
                    YPR_angles[0] = fRmY - YPR_angles[2];

                    return YPR_angles;
                }
            }
            else
            {
                // WARNING.  Not a unique solution.
                double fRpY = Math.Atan2(-m0[1], m0[2]);
                YPR_angles[2] = 0.0;  // any angle works
                YPR_angles[0] = fRpY - YPR_angles[2];

                return YPR_angles;
            }
        }

        private static void quadToRotationMatrix(Quaternion rot, ref double[] m0, ref double[] m1, ref double[] m2)
        {
            double[] orient = new double[] { rot.X, rot.Y, rot.Z, rot.W };

            //normalize quaternion as in https://www.andre-gaschler.com/rotationconverter/ , else we could have infinite + weird result on matrixToEulerAnglesZYX, because of float precision on quaternion.
            double a = Math.Sqrt(orient[0] * orient[0] + orient[1] * orient[1] + orient[2] * orient[2] + orient[3] * orient[3]);
            if (0 == a)
            {
                orient[0] = orient[1] = orient[2] = 0;
                orient[3] = 1;
            }
            else
            {
                a = 1.0 / a;
                orient[0] *= a;
                orient[1] *= a;
                orient[2] *= a;
                orient[3] *= a;
            }



            double fTx = orient[0] + orient[0];
            double fTy = orient[1] + orient[1];
            double fTz = orient[2] + orient[2];
            double fTwx = fTx * orient[3];
            double fTwy = fTy * orient[3];
            double fTwz = fTz * orient[3];
            double fTxx = fTx * orient[0];
            double fTxy = fTy * orient[0];
            double fTxz = fTz * orient[0];
            double fTyy = fTy * orient[1];
            double fTyz = fTz * orient[1];
            double fTzz = fTz * orient[2];

            m0[0] = 1.0 - (fTyy + fTzz);
            m0[1] = fTxy - fTwz;
            m0[2] = fTxz + fTwy;
            m1[0] = fTxy + fTwz;
            m1[1] = 1.0 - (fTxx + fTzz);
            m1[2] = fTyz - fTwx;
            m2[0] = fTxz - fTwy;
            m2[1] = fTyz + fTwx;
            m2[2] = 1.0 - (fTxx + fTyy);
        }

    }
}