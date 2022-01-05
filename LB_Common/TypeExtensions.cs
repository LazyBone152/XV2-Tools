using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LB_Common
{
    public static class TypeExtensions
    {

        /// <summary>
        /// Checks if this Type is an Integer value type.
        /// </summary>
        /// <returns>True if integer, false if not</returns>
        public static bool IsIntegerType(this Type type)
        {
            return type == typeof(int) || type == typeof(uint) || type == typeof(short) || type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(long) || type == typeof(ulong);
        }

        /// <summary>
        /// Checks if this Type is an floating-point value type.
        /// </summary>
        /// <returns>True if float, false if not</returns>
        public static bool IsFloatType(this Type type)
        {
            return type == typeof(float) || type == typeof(double);
        }

        public static bool IsString(this Type type)
        {
            return type == typeof(string);
        }

        public static bool IsBool(this Type type)
        {
            return type == typeof(bool);
        }

        public static bool IsUInt16(this Type type)
        {
            return type == typeof(ushort);
        }

        public static bool IsInt16(this Type type)
        {
            return type == typeof(short);
        }

        public static bool IsUInt32(this Type type)
        {
            return type == typeof(uint);
        }

        public static bool IsInt32(this Type type)
        {
            return type == typeof(int);
        }

        public static bool IsUInt8(this Type type)
        {
            return type == typeof(byte);
        }

        public static bool IsInt8(this Type type)
        {
            return type == typeof(sbyte);
        }

        public static bool IsUInt64(this Type type)
        {
            return type == typeof(ulong);
        }

        public static bool IsInt64(this Type type)
        {
            return type == typeof(long);
        }

        public static bool IsFloat(this Type type)
        {
            return type == typeof(float);
        }

        public static bool IsDouble(this Type type)
        {
            return type == typeof(double);
        }


    }
}
