using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib.Resource
{
    public static class RgbConverter
    {
        public static byte ConvertToByte(float color)
        {
            return (byte)(color * 255);
        }

        public static float ConvertToFloat(byte color)
        {
            return (float)color / 255;
        }
    }

}
