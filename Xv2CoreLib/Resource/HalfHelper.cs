using System;

namespace Xv2CoreLib.Resource
{
    public static class HalfHelper
    {
        public static byte[] GetBytes(float value)
        {
#if NET5_0_OR_GREATER
            return BitConverter.GetBytes((Half)value);
#else
            return Half.GetBytes((Half)value);
#endif
        }

        public static float ReadHalf(byte[] bytes, int offset)
        {
#if NET5_0_OR_GREATER
            return (float)BitConverter.ToHalf(bytes, offset);
#else
            return Half.ToHalf(bytes, offset);
#endif
        }
    }
}
