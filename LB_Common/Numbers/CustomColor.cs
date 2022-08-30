using System;
using YAXLib;

namespace LB_Common.Numbers
{
    [Serializable]
    public class CustomColor
    {
        public float[] Values = new float[4];

        [YAXAttributeForClass]
        public float R { get { return GetValue(0); } set { SetValue(0, value); } }
        [YAXAttributeForClass]
        public float G { get { return GetValue(1); } set { SetValue(1, value); } }
        [YAXAttributeForClass]
        public float B { get { return GetValue(2); } set { SetValue(2, value); } }
        [YAXAttributeForClass]
        public float A { get { return GetValue(3); } set { SetValue(3, value); } }

        public CustomColor() { }

        public CustomColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }


        public bool IsWhiteOrBlack()
        {
            return (R == 0f && G == 0f && B == 0f) || (R == 1f && G == 1f && B == 1f);
        }

        public bool IsBlack()
        {
            return (R == 0f && G == 0f && B == 0f);
        }

        public void SetValue(int idx, float value)
        {
            //Initialization of internal array is required here when using XML serialization
            if (Values == null) 
                Values = new float[4];

            Values[idx] = value;
        }

        public float GetValue(int idx)
        {
            //Initialization of internal array is required here when using XML serialization
            if (Values == null) 
                Values = new float[4];

            return Values[idx];
        }

        #region Operators
        public override bool Equals(object obj)
        {
            return this == (CustomColor)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash1 = 2256559;
                const int hash2 = 5023567;
                hash1 *= hash2 ^ R.GetHashCode();
                hash1 *= hash2 ^ G.GetHashCode();
                hash1 *= hash2 ^ B.GetHashCode();
                hash1 *= hash2 ^ A.GetHashCode();
                return hash1;
            }
        }

        public static bool operator ==(CustomColor a, CustomColor b)
        {
            return a?.R == b?.R && a?.G == b?.G && a?.B == b?.B && a?.A == b?.A;
        }

        public static bool operator !=(CustomColor a, CustomColor b)
        {
            return (a == b) ? false : true;
        }
        public static bool operator ==(CustomColor a, float b)
        {
            return a?.R == b && a?.G == b && a?.B == b && a?.A == b;
        }

        public static bool operator !=(CustomColor a, float b)
        {
            return (a == b) ? false : true;
        }

        public static CustomColor operator *(CustomColor a, float b)
        {
            return new CustomColor(a.R * b, a.G * b, a.B * b, a.A * b);
        }
        #endregion
    }
}
