using System;

namespace LB_Common.Numbers
{
    [Serializable]
    public class CustomColor
    {
        public readonly float[] Values = new float[4];

        public float R { get { return Values[0]; } set { Values[0] = value; } }
        public float G { get { return Values[1]; } set { Values[1] = value; } }
        public float B { get { return Values[2]; } set { Values[2] = value; } }
        public float A { get { return Values[3]; } set { Values[3] = value; } }

        public CustomColor() { }

        public CustomColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public float GetValue(int idx)
        {
            switch (idx)
            {
                case 0:
                    return R;
                case 1:
                    return G;
                case 2:
                    return B;
                case 3:
                    return A;
            }
            return 0;
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

        public static CustomColor operator *(CustomColor a, float b)
        {
            return new CustomColor(a.R * b, a.G * b, a.B * b, a.A * b);
        }
        #endregion
    }
}
