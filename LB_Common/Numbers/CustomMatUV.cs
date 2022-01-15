using System;

namespace LB_Common.Numbers
{
    [Serializable]
    public class CustomMatUV
    {
        public float U { get; set; }
        public float V { get; set; }

        public CustomMatUV() { }

        public CustomMatUV(float u, float v)
        {
            U = u;
            V = v;
        }

        public float GetValue(int idx)
        {
            switch (idx)
            {
                case 0:
                    return U;
                case 1:
                    return V;
            }
            return 0;
        }

        #region Operators
        public override bool Equals(object obj)
        {
            return this == (CustomMatUV)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash1 = 2256559;
                const int hash2 = 5023567;
                hash1 *= hash2 ^ U.GetHashCode();
                hash1 *= hash2 ^ V.GetHashCode();
                return hash1;
            }
        }

        public static bool operator ==(CustomMatUV a, CustomMatUV b)
        {
            return a?.U == b?.U && a?.V == b?.V;
        }

        public static bool operator !=(CustomMatUV a, CustomMatUV b)
        {
            return (a == b) ? false : true;
        }

        #endregion
    }
}
