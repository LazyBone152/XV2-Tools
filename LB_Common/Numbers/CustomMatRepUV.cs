using System;

namespace LB_Common.Numbers
{
    [Serializable]
    public class CustomMatRepUV
    {
        public bool U { get; set; }
        public bool V { get; set; }

        public CustomMatRepUV() { }

        public CustomMatRepUV(bool u, bool v)
        {
            U = v;
            V = v;
        }

        public bool GetValue(int idx)
        {
            switch (idx)
            {
                case 0:
                    return U;
                case 1:
                    return V;
            }
            return false;
        }
    }
}
