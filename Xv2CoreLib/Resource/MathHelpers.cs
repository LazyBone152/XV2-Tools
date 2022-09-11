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

        public static Vector3 QuaternionToEulerAngles(Quaternion rot)
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