using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using YAXLib;
using Xv2CoreLib.Resource.UndoRedo;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib
{
    public static class ArrayExtensions
    {
        public static byte[] DeepCopy(this byte[] array)
        {
            byte[] newArray = new byte[array.Length];

            for (int i = 0; i < array.Length; i++)
                newArray[i] = array[i];

            return newArray;
        }

        public static bool Exists<T>(this IList<T> list, object _object) where T : class
        {
            for(int i = 0; i < list.Count; i++)
            {
                if (list[i] == _object)
                    return true;
            }

            return false;
        }
    }

    public static class EnumEx
    {
        public static T SetFlag<T>(this Enum value, T append, bool state)
        {
            if (state)
                return SetFlag(value, append);
            else
                return RemoveFlag(value, append);
        }

        /// <summary>
        /// Includes an enumerated type and returns the new value
        /// </summary>
        public static T SetFlag<T>(this Enum value, T append)
        {
            Type type = value.GetType();

            // determine the values
            object result = value;
            var parsed = new Value(append, type);
            if (parsed.Signed != null)
            {
                result = Convert.ToInt64(value) | (long)parsed.Signed;
            }

            // return the final value
            return (T)Enum.Parse(type, result.ToString());
        }

        /// <summary>
        /// Removes an enumerated type and returns the new value
        /// </summary>
        public static T RemoveFlag<T>(this Enum value, T remove)
        {
            Type type = value.GetType();

            // determine the values
            object result = value;
            var parsed = new Value(remove, type);
            if (parsed.Signed != null)
            {
                result = Convert.ToInt64(value) & ~(long)parsed.Signed;
            }

            // return the final value
            return (T)Enum.Parse(type, result.ToString());
        }

        /// <summary>
        /// Checks if an enumerated type contains a value
        /// </summary>
        public static bool Has<T>(this Enum value, T check)
        {
            Type type = value.GetType();

            //determine the values
            var parsed = new Value(check, type);
            return parsed.Signed != null &&
                (Convert.ToInt64(value) & (long)parsed.Signed) == (long)parsed.Signed;
        }

        /// <summary>
        /// Checks if an enumerated type is missing a value
        /// </summary>
        public static bool Missing<T>(this Enum obj, T value)
        {
            return !Has(obj, value);
        }

        /// <summary>
        /// Internal class to simplfy narrowing values between a 
        /// ulong and long since either value should cover any 
        /// lesser value.
        /// </summary>
        private class Value
        {
            public readonly long? Signed;

            // cached comparisons for tye to use
            private static readonly Type _uInt64 = typeof(ulong);
            private static readonly Type _uInt32 = typeof(long);

            public Value(object value, Type type)
            {
                //make sure it is even an enum to work with
                if (!type.IsEnum)
                {
                    throw new
                    ArgumentException("Value provided is not an enumerated type!");
                }

                //then check for the enumerated value
                Type compare = Enum.GetUnderlyingType(type);

                //if this is an unsigned long then the only
                //value that can hold it would be a ulong
                if (compare == _uInt32 || compare == _uInt64)
                {
                    Unsigned = Convert.ToUInt64(value);
                }
                //otherwise, a long should cover anything else
                else
                {
                    Signed = Convert.ToInt64(value);
                }
            }

            public ulong? Unsigned { get; set; }
        }
    }

    public static class ArrayConvert
    {
        public static List<string> ConvertToStringList(List<int> list)
        {
            List<string> stringList = new List<string>();

            foreach (var value in list)
            {
                stringList.Add(value.ToString());
            }

            return stringList;
        }

        public static List<int> ConvertToInt32List(List<string> list)
        {
            List<int> convertedList = new List<int>();

            foreach (var value in list)
            {
                convertedList.Add(int.Parse(value));
            }

            return convertedList;
        }

        public static List<int> ConvertToIntList(IEnumerable<ushort> list)
        {
            List<int> newList = new List<int>();

            foreach (var value in list)
                newList.Add((int)value);

            return newList;
        }

        public static List<int> ConvertToIntList(IEnumerable<short> list)
        {
            List<int> newList = new List<int>();

            foreach (var value in list)
                newList.Add((int)value);

            return newList;
        }

        public static int[] ConvertToIntArray(short[] array)
        {
            int[] newArray = new int[array.Length];

            for(int i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }

            return newArray;
        }

        public static int[] ConvertToIntArray(ushort[] array)
        {
            int[] newArray = new int[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }

            return newArray;
        }

    }

    public static class Utils
    {
        public static void RemoveAll<K, V>(this IDictionary<K, V> dict, Func<K, V, bool> match)
        {
            foreach (var key in dict.Keys.ToArray()
                    .Where(key => match(key, dict[key])))
                dict.Remove(key);
        }

        /// <summary>
        /// Remove tailing zeros from a version in string format.
        /// </summary>
        public static string FormatVersionString(string version)
        {
            try
            {
                string[] split = version.Split('.');
                if (split[2] == "0" && split[3] == "0")
                {
                    return string.Format("{0}.{1}", split[0], split[1]);
                }
                else if (split[3] == "0")
                {
                    return string.Format("{0}.{1}.{2}", split[0], split[1], split[2]);
                }
                else
                {
                    return string.Format("{0}.{1}.{2}.{3}", split[0], split[1], split[2], split[3]);
                }
            }
            catch
            {
                return version;
            }
        }

        public static int TryParseInt(string value)
        {
            int val;
            if (int.TryParse(value, out val))
                return val;
            return -1;
        }

        public static bool IsFileWriteLocked(string path)
        {
            try
            {
                using (FileStream stream = new FileInfo(path).Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch
            {
                return true;
            }

            return false;
        }

        public static bool CompareSplitString(string originalString, char splitParam, int index, string compareParam)
        {
            var splitStr = originalString.Split(splitParam);

            if (splitStr.Length >= index)
                return splitStr[index] == compareParam;

            return false;
        }

        /// <summary>
        /// Copies primitive values (including strings) from one object to another, and creates an undoable stack.
        /// </summary>
        public static List<IUndoRedo> CopyValues<T>(T instance, T copyFrom, params string[] exclusions)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            PropertyInfo[] properties = instance.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if ((prop.PropertyType == typeof(string) || prop.PropertyType.IsPrimitive || prop.PropertyType.IsValueType)
                    && (prop.SetMethod != null && prop.GetMethod != null) && !exclusions.Contains(prop.Name))
                {
                    object oldValue = prop.GetValue(instance);
                    object newValue = prop.GetValue(copyFrom);
                    undos.Add(new UndoableProperty<T>(prop.Name, instance, oldValue, newValue));
                    prop.SetValue(instance, newValue);
                }
            }

            return undos;
        }

        public static bool IsListNullOrEmpty(IList list)
        {
            if (list == null) return true;
            return list.Count == 0;
        }

        public static string GetPathWithoutExtension(string path)
        {
            return string.Format("{0}/{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        }

        public static string ResolveRelativePath(string path)
        {
            return Path.GetFullPath(path)
            .Substring(Directory.GetCurrentDirectory().Length + 1);

            //string newPath = Path.GetFullPath(path);
            //string workingDir = Path.GetFullPath(Environment.CurrentDirectory);
            //newPath = newPath.Remove(0, workingDir.Length - 1);
            //return newPath;
        }

        public static bool ComparePaths(string path1, string path2)
        {
            return SanitizePath(path1) == SanitizePath(path2);
        }

        public static bool CompareCharaFolder(string path, string charaCode)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            path = SanitizePath(path);

            return path[6] == charaCode[0] && path[7] == charaCode[1] && path[8] == charaCode[2];
        }

        public static string SanitizePath(string path)
        {
            //Remove trailing slash, if present. This is important for comparing paths in CPKs, as they are stored without a trailing slash.
            if (path[path.Length - 1] == Path.DirectorySeparatorChar || path[path.Length - 1] == Path.AltDirectorySeparatorChar)
            {
                path = path.Remove(path.Length - 1);
            }

            return path.Replace(@"//", @"/").Replace(@"\\", @"/").Replace(@"\", @"/").
                Replace(Path.DirectorySeparatorChar, '/').
                Replace(Path.AltDirectorySeparatorChar, '/').
                Replace(string.Format("{0}{0}", Path.DirectorySeparatorChar), "/").
                Replace(string.Format("{0}{0}", Path.AltDirectorySeparatorChar), "/");
        }

        public static string PathRemoveRoot(string path)
        {
            path = SanitizePath(path);
            if (path.StartsWith("/")) path = path.Remove(0);
            return path.Substring(path.IndexOf("/") + 1);
        }

        /// <summary>
        /// Returns a relative path starting from "data". 
        /// </summary>
        public static string GetRelativePath(string path)
        {
            path = path.Replace(@"//", @"/").Replace(@"\\", @"/").Replace(@"\", @"/");
            int index = path.IndexOf("data/");
            if (index == -1) return path;
            return path.Remove(0, index + 5);
        }

        public static T[] GetRange<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static bool StringToBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return string.Equals(value, "true", StringComparison.InvariantCultureIgnoreCase);
        }

        public static string BoolToString(bool value)
        {
            return (value) ? "True" : "False";
        }

        public static string GetStringUtf(byte[] bytes, int offset)
        {
            return StringEx.GetString(bytes, offset, false, StringEx.EncodingType.UTF8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetString(List<byte> bytes, int offset, int size)
        {
            return StringEx.GetString(bytes, offset, false, StringEx.EncodingType.ASCII, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetString(List<byte> bytes, int offset)
        {
            return StringEx.GetString(bytes, offset, true, StringEx.EncodingType.ASCII);
        }

        public static double BytesToKilobytes(int bytes)
        {
            return bytes / 1000f;
        }

        public static double KilobytesToMegabytes(double kilobytes)
        {
            return kilobytes / 1000f;
        }

        public static double BytesToMegabytes(double bytes)
        {
            return bytes / 1000000f;
        }

        public static int ConvertToInt(BitArray bits)
        {
            if (bits.Count != 32)
            {
                throw new ArgumentException("bits is either to small or to large (an int needs 32 bits)");
            }
            byte[] bytes = new byte[4];
            bits.CopyTo(bytes, 0);
            return BitConverter.ToInt32(bytes, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CalculatePadding(int fileSize, int byteAlignment)
        {
            //if (byteAlignment <= 0)
            //    throw new ArgumentException("CalculatePadding: Alignment must be greater than zero.", nameof(byteAlignment));

            int remainder = (fileSize % byteAlignment);
            return remainder == 0 ? 0 : byteAlignment - remainder;
        }

        public static bool CompareArray(byte[] array1, byte[] array2)
        {
            if (array1 == null && array2 == null) return true;
            if (array1 == null && array2 != null) return false;
            if (array2 == null && array1 != null) return false;
            if (array1.Length != array2.Length) return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i]) return false;
            }

            return true;
        }

        public static List<byte> GetStringBytes(string str, int minSize = -1, int maxSize = -1)
        {
            if (str == null)
            {
                str = string.Empty;
            }
            List<byte> strBytes = Encoding.ASCII.GetBytes(str).ToList();

            if (minSize == -1)
            {
                strBytes.Add(0);
            }
            else
            {
                int bytesToAdd = minSize - str.Length;
                if (bytesToAdd > 0)
                {
                    strBytes.AddRange(new byte[bytesToAdd]);
                }
                else
                {
                    strBytes.Add(0);
                }

                if (strBytes.Count > maxSize && maxSize != -1)
                {
                    strBytes.RemoveRange(4, strBytes.Count - 4);
                }
            }

            return strBytes;
        }

        public static string CleanPath(string path)
        {
            return path.Replace(@"//", @"/").Replace(@"\\", @"/").Replace(@"\", @"/");
        }

        public static void WaitForInputThenQuit()
        {
            Console.WriteLine("\nPress enter to exit.");
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static byte ConvertToByte(BitArray bits)
        {
            if (bits.Count != 8)
            {
                throw new ArgumentException("bits is either to small or to large (a byte needs 8 bits)");
            }
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }

        public static byte[] ConvertToByteArray(BitArray bits, int byteCount)
        {
            if (bits.Count / 8 != byteCount)
            {
                throw new ArgumentException("bits is either to small or to large (a byte needs 8 bits)");
            }
            byte[] bytes = new byte[byteCount];

            bits.CopyTo(bytes, 0);

            return bytes;
        }

        /// <summary>
        /// Replaces data starting at a certain index of a list with that of another list. The total amount of data replaced depends on the size of the inserted list.
        /// </summary>
        public static List<byte> ReplaceRange(List<byte> list, byte[] insertedData, int startIndex)
        {
            if (insertedData.Length > list.Count)
            {
                throw new InvalidOperationException("Cannot insert more data than is in the original list");
            }

            for (int i = 0; i < insertedData.Count(); i++)
            {
                list[i + startIndex] = insertedData[i];
            }
            return list;
        }

        /// <summary>
        /// Replaces data starting at a certain index of a list with that of another list. The total amount of data replaced depends on the size of the inserted list.
        /// </summary>
        public static byte[] ReplaceRange(byte[] list, byte[] insertedData, int startIndex)
        {
            if (insertedData.Count() > list.Count())
            {
                throw new InvalidOperationException("Cannot insert more data than is in the original list");
            }

            for (int i = 0; i < insertedData.Count(); i++)
            {
                list[i + startIndex] = insertedData[i];
            }
            return list;
        }

    }

    /// <summary>
    /// Extra static methods for BitConverter.
    /// </summary>
    public static class BitConverter_Ex
    {
        public static ushort[] ToUInt16Array(byte[] bytes)
        {
            if (bytes == null) return new ushort[0];
            int count = bytes.Length / 2;

            ushort[] ints = new ushort[count];

            for (int i = 0; i < count; i++)
            {
                ints[i] = BitConverter.ToUInt16(bytes, i * 2);
            }

            return ints;
        }

        public static int[] ToInt32Array(byte[] bytes, int index, int count)
        {
            int[] ints = new int[count];


            for (int i = 0; i < count * 4; i += 4)
            {
                ints[i / 4] = BitConverter.ToInt32(bytes, index + i);
            }

            return ints;
        }

        public static short[] ToInt16Array(byte[] bytes, int index, int count)
        {
            short[] ints = new short[count];

            for (int i = 0; i < count * 2; i += 2)
            {
                ints[i / 2] = BitConverter.ToInt16(bytes, index + i);
            }

            return ints;
        }

        public static ushort[] ToUInt16Array(byte[] bytes, int index, int count)
        {
            ushort[] ints = new ushort[count];

            for (int i = 0; i < count * 2; i += 2)
            {
                ints[i / 2] = BitConverter.ToUInt16(bytes, index + i);
            }

            return ints;
        }

        public static ulong[] ToUInt64Array(byte[] bytes, int index, int count)
        {
            ulong[] ints = new ulong[count];

            for (int i = 0; i < count * 8; i += 8)
            {
                ints[i / 8] = BitConverter.ToUInt64(bytes, index + i);
            }

            return ints;
        }

        public static bool ToBoolean(byte[] bytes, int index)
        {
            return (bytes[index] == 0) ? false : true;
        }

        public static bool ToBoolean(byte bytes)
        {
            return (bytes == 0) ? false : true;
        }

        public static bool ToBooleanFromInt32(byte[] bytes, int index)
        {
            return (BitConverter.ToInt32(bytes, index) == 0) ? false : true;
        }

        public static float[] ToFloat32Array(byte[] bytes, int index, int count)
        {
            float[] floats = new float[count];

            for (int i = 0; i < count * 4; i += 4)
            {
                floats[i / 4] = BitConverter.ToSingle(bytes, index + i);
            }

            return floats;
        }

        //GetBytes methods
        /// <summary>
        /// Converts a boolean value into a byte
        /// </summary>=
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetBytes(bool value)
        {
            return value ? (byte)1 : (byte)0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetBytes_Bool32(bool value)
        {
            return new byte[4] { value ? (byte)1 : (byte)0, 0, 0, 0 };
        }

        public static byte[] GetBytes(ushort[] srcArray, int fixedSize = -1)
        {
            if (fixedSize < -1)
                throw new ArgumentOutOfRangeException(nameof(fixedSize));

            if (srcArray == null && fixedSize == -1)
                throw new ArgumentNullException(nameof(srcArray), $"{nameof(srcArray)} cannot be null with fixedSize = -1.");

            int arraySize = fixedSize == -1 ? srcArray.Length : fixedSize;

            byte[] buffer = new byte[arraySize * sizeof(ushort)];
            Span<byte> span = buffer;

            for (int i = 0; i < arraySize; i++)
            {
                if (srcArray != null && i < srcArray.Length)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(i * sizeof(ushort), sizeof(ushort)), srcArray[i]);
                }
            }

            return buffer;
        }

        public static byte[] GetBytes(short[] srcArray, int fixedSize = -1)
        {
            if (fixedSize < -1)
                throw new ArgumentOutOfRangeException(nameof(fixedSize));

            if (srcArray == null && fixedSize == -1)
                throw new ArgumentNullException(nameof(srcArray), $"{nameof(srcArray)} cannot be null with fixedSize = -1.");

            int arraySize = fixedSize == -1 ? srcArray.Length : fixedSize;

            byte[] buffer = new byte[arraySize * sizeof(short)];
            Span<byte> span = buffer;

            for (int i = 0; i < arraySize; i++)
            {
                if (srcArray != null && i < srcArray.Length)
                {
                    BinaryPrimitives.WriteInt16LittleEndian(span.Slice(i * sizeof(short), sizeof(short)), srcArray[i]);
                }
            }

            return buffer;
        }

        public static byte[] GetBytes(int[] srcArray, int fixedSize = -1)
        {
            if (fixedSize < -1)
                throw new ArgumentOutOfRangeException(nameof(fixedSize));

            if (srcArray == null && fixedSize == -1)
                throw new ArgumentNullException(nameof(srcArray), $"{nameof(srcArray)} cannot be null with fixedSize = -1.");

            int arraySize = fixedSize == -1 ? srcArray.Length : fixedSize;

            byte[] buffer = new byte[arraySize * sizeof(int)];
            Span<byte> span = buffer;

            for (int i = 0; i < arraySize; i++)
            {
                if(srcArray != null && i < srcArray.Length)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(span.Slice(i * sizeof(int), sizeof(int)), srcArray[i]);
                }
            }

            return buffer;
        }

        public static byte[] GetBytes(uint[] srcArray, int fixedSize = -1)
        {
            if (fixedSize < -1)
                throw new ArgumentOutOfRangeException(nameof(fixedSize));

            if (srcArray == null && fixedSize == -1)
                throw new ArgumentNullException(nameof(srcArray), $"{nameof(srcArray)} cannot be null with fixedSize = -1.");

            int arraySize = fixedSize == -1 ? srcArray.Length : fixedSize;

            byte[] buffer = new byte[arraySize * sizeof(uint)];
            Span<byte> span = buffer;

            for (int i = 0; i < arraySize; i++)
            {
                if (srcArray != null && i < srcArray.Length)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(i * sizeof(uint), sizeof(uint)), srcArray[i]);
                }
            }

            return buffer;
        }

        public static byte[] GetBytes(ulong[] srcArray, int fixedSize = -1)
        {
            if (fixedSize < -1)
                throw new ArgumentOutOfRangeException(nameof(fixedSize));

            if (srcArray == null && fixedSize == -1)
                throw new ArgumentNullException(nameof(srcArray), $"{nameof(srcArray)} cannot be null with fixedSize = -1.");

            int arraySize = fixedSize == -1 ? srcArray.Length : fixedSize;

            byte[] buffer = new byte[arraySize * sizeof(ulong)];
            Span<byte> span = buffer;

            for (int i = 0; i < arraySize; i++)
            {
                if (srcArray != null && i < srcArray.Length)
                {
                    BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(i * sizeof(ulong), sizeof(ulong)), srcArray[i]);
                }
            }

            return buffer;
        }

        public static byte[] GetBytes(long[] srcArray, int fixedSize = -1)
        {
            if (fixedSize < -1)
                throw new ArgumentOutOfRangeException(nameof(fixedSize));

            if (srcArray == null && fixedSize == -1)
                throw new ArgumentNullException(nameof(srcArray), $"{nameof(srcArray)} cannot be null with fixedSize = -1.");

            int arraySize = fixedSize == -1 ? srcArray.Length : fixedSize;

            byte[] buffer = new byte[arraySize * sizeof(long)];
            Span<byte> span = buffer;

            for (int i = 0; i < arraySize; i++)
            {
                if (srcArray != null && i < srcArray.Length)
                {
                    BinaryPrimitives.WriteInt64LittleEndian(span.Slice(i * sizeof(long), sizeof(long)), srcArray[i]);
                }
            }

            return buffer;
        }

        public static byte[] GetBytes(float[] srcArray, int fixedSize = -1)
        {
            if (fixedSize < -1)
                throw new ArgumentOutOfRangeException(nameof(fixedSize));

            if (srcArray == null && fixedSize == -1)
                throw new ArgumentNullException(nameof(srcArray), $"{nameof(srcArray)} cannot be null with fixedSize = -1.");

            int arraySize = fixedSize == -1 ? srcArray.Length : fixedSize;

            byte[] buffer = new byte[arraySize * sizeof(float)];
            Span<byte> span = buffer;

            for (int i = 0; i < arraySize; i++)
            {
                if (srcArray != null && i < srcArray.Length)
                {
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(span.Slice(i * sizeof(float), sizeof(float)), srcArray[i]);
                }
            }

            return buffer;
        }

    }

    public static class File_Ex
    {
        public static object LoadXml(string path, Type type)
        {
            YAXSerializer serializer = new YAXSerializer(type, YAXSerializationOptions.DontSerializeNullObjects);
            return serializer.DeserializeFromFile(path);
        }

        public static void CopyAll(string sourceDir, string destDir, bool allowOverwrite)
        {
            string[] fileNames = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

            for (int i = 0; i < fileNames.Count(); i++)
            {
                string copyPath = (string)fileNames[i].Clone();
                string destPath = String.Format("{0}/{1}", destDir, Utils.CleanPath(fileNames[i].Remove(0, sourceDir.Count())));

                if (!File.Exists(destPath) || allowOverwrite == true)
                {
                    File.Copy(copyPath, destPath, true);
                }
            }

        }

    }

    /// <summary>
    /// Provides static methods for converting to and from hexadecimal strings (prefixed with 0x).\n\n
    /// Note: Consider using the YAXHexValue attribute instead!
    /// </summary>
    public static class HexConverter
    {
        /// <summary>
        /// Converts the a UInt32 value to a Hexadecimal string ("0x{value}")
        /// </summary>
        public static string GetHexString(uint value)
        {
            return String.Format("0x{0}", value.ToString("x"));
        }

        /// <summary>
        /// Converts the a Int32 value to a Hexadecimal string ("0x{value}")
        /// </summary>
        public static string GetHexString(int value)
        {
            return String.Format("0x{0}", value.ToString("x"));
        }

        /// <summary>
        /// Converts the a Int16 value to a Hexadecimal string ("0x{value}")
        /// </summary>
        public static string GetHexString(short value)
        {
            return String.Format("0x{0}", value.ToString("x"));
        }

        /// <summary>
        /// Converts the a Int8 value to a Hexadecimal string ("0x{value}")
        /// </summary>
        public static string GetHexString(byte value)
        {
            return String.Format("0x{0}", value.ToString("x"));
        }

        /// <summary>
        /// Converts a hexadecimal string into an Int32.
        /// </summary>
        public static int ToInt32(string value)
        {
            if (value[1] != 'x' || value[0] != '0')
            {
                throw new Exception(String.Format("{0} is not a valid hexadecimal value.", value));
            }

            string[] splitValue = value.Split('x');
            return Int32.Parse(splitValue[1], NumberStyles.HexNumber);
        }

        /// <summary>
        /// Converts a hexadecimal string into an Int16.
        /// </summary>
        public static short ToInt16(string value)
        {
            if (value[1] != 'x' || value[0] != '0')
            {
                throw new Exception(String.Format("{0} is not a valid hexadecimal value.", value));
            }
            string[] splitValue = value.Split('x');
            return Int16.Parse(splitValue[1], NumberStyles.HexNumber);
        }

        /// <summary>
        /// Converts a hexadecimal string into an Int8.
        /// </summary>
        public static byte ToInt8(string value)
        {
            if (value[1] != 'x' || value[0] != '0')
            {
                throw new Exception(String.Format("{0} is not a valid hexadecimal value.", value));
            }
            string[] splitValue = value.Split('x');
            return byte.Parse(splitValue[1], NumberStyles.HexNumber);
        }

        public static string ToSerializedArray(List<int> values)
        {
            StringBuilder str = new StringBuilder();

            for (int i = 0; i < values.Count; i++)
            {
                str.Append($"{HexConverter.GetHexString(values[i])}");

                if (i != values.Count - 1)
                {
                    str.Append($", ");
                }
            }

            return str.ToString();
        }

        public static List<int> ReadInt32Array(string array)
        {
            List<int> intValues = new List<int>();

            if (!string.IsNullOrWhiteSpace(array))
            {
                string[] values = array.Split(',');

                foreach (var value in values)
                {
                    intValues.Add(HexConverter.ToInt32(value.Trim(' ')));
                }

            }

            return intValues;
        }
    }

    public static class Int4Converter
    {
        /// <summary>
        /// Takes in a Int8, and returns an array of two Int4s.
        /// </summary>
        public static byte[] ToInt4(byte value)
        {
            BitArray bits = new BitArray(new byte[1] { value });
            BitArray a = (BitArray)bits.Clone();
            BitArray b = (BitArray)bits.Clone();

            //Removing unneeded bits
            for (int i = 0; i < 4; i++)
            {
                a[4 + i] = false;
            }
            for (int i = 0; i < 4; i++)
            {
                b[0 + i] = b[4 + i];
            }
            for (int i = 0; i < 4; i++)
            {
                b[4 + i] = false;
            }

            return new byte[2] { Utils.ConvertToByte(a), Utils.ConvertToByte(b) };
        }

        /// <summary>
        /// Takes in two int4 values (from ToInt4 method) and merges them into a byte.
        /// </summary>
        public static byte GetByte(byte a, byte b, string a_name = "default_a", string b_name = "default_b")
        {
            if (a > 15)
            {
                Console.WriteLine(String.Format("Warning! a uint4 cannot contain a value greater than 15, or less than 0! (attribute name: {0})", a_name));
                Console.ReadLine();
            }
            if (b > 15)
            {
                Console.WriteLine(String.Format("Warning! a uint4 cannot contain a value greater than 15, or less than 0! (attribute name: {0})", b_name));
                Console.ReadLine();
            }

            BitArray composedBits = new BitArray(new byte[1]);
            BitArray aBits = new BitArray(new byte[1] { a });
            BitArray bBits = new BitArray(new byte[1] { b });

            for (int i = 0; i < 4; i++)
            {
                composedBits[i] = aBits[i];
            }
            for (int i = 0; i < 4; i++)
            {
                composedBits[i + 4] = bBits[i];
            }

            return Utils.ConvertToByte(composedBits);
        }

    }

    public static class Assertion
    {

        public static bool ValidateFileSignature(List<byte> bytes, string expectedSignature, bool writeXml, int signatureLength = 4)
        {
            string foundSignature = String.Empty;
            try
            {
                foundSignature = Utils.GetString(bytes, 0, signatureLength);
            }
            catch
            {
                foundSignature = "?Invalid?";
            }

            if (foundSignature != expectedSignature)
            {
                if (writeXml)
                {
                    throw new Exception(String.Format("Signature check failed. Expected \"{0}\", found \"{1}\".\nParse failed.", expectedSignature, foundSignature));
                }
                return false;
            }
            return true;
        }


        /// <summary>
        /// Displays an error message if the inputed string cannot be parsed into a number.
        /// </summary>
        public static void ValidateNumericString(string value, string xml_section_name, string xml_attribute_name)
        {
            if (value.All(Char.IsNumber) == false)
            {
                throw new Exception(String.Format("\"{0}\" is not a valid parameter for {1} on {2}", value, xml_attribute_name, xml_section_name));
            }

        }

        /// <summary>
        /// Displays an error message stating the paramater (value) is invalid.
        /// </summary>
        public static void InvalidBoolean(string value, string xml_section_name, string xml_attribute_name)
        {
            throw new Exception(String.Format("\"{0}\" is not a valid parameter for {1} on {2}", value, xml_attribute_name, xml_section_name));
        }

        public static void AssertArraySize<T>(T[] array, int size, string xml_section_name, string xml_attribute_name)
        {
            if (array.Count() != size)
            {
                throw new Exception(String.Format("Array size mismatch. (XML Element: {2}, XML Attribute: {3})\nExpected = {0}\nFound = {1}", size, array.Count(), xml_section_name, xml_attribute_name));
            }
        }

        public static void AssertArraySize<T>(List<T> array, int size, string xml_section_name, string xml_attribute_name)
        {
            if (array.Count != size)
            {
                throw new Exception(String.Format("Array size mismatch. (XML Element: {2}, XML Attribute: {3})\nExpected = {0}\nFound = {1}", size, array.Count(), xml_section_name, xml_attribute_name));
            }
        }

        public static void AssertStringSize(string _str, int size, string xml_section_name, string xml_attribute_name)
        {
            if (_str.Count() > size)
            {
                throw new Exception(String.Format("The string ({1}) has too many characters. Maximum allowed length is {0}. (XML Element: {2}, XML Attribute: {3})", size, _str, xml_section_name, xml_attribute_name));
            }
        }

    }

    public static class StringWriter
    {
        /// <summary>
        /// Contains all the information needed to write a string to file with the WritePointerStrings method.
        /// </summary>
        public struct StringInfo
        {
            /// <summary>
            /// This is the string that will be written to file. 
            /// </summary>
            public string StringToWrite { get; set; }
            /// <summary>
            /// This is the position of the offset to overwrite with a pointer to the string
            /// </summary>
            public int Offset { get; set; }
            /// <summary>
            /// This is the offset that the pointer will be relative to.
            /// </summary>
            public int RelativeOffset { get; set; }

        }

        /// <summary>
        /// Writes strings to a binary file.
        /// </summary>
        /// <param name="strings">Strings to write, including any offset info</param>
        /// <param name="bytes">The byte list containing the files data</param>
        /// <param name="compressStrings">If true, identical strings will be compressed down into only one instance, and only written to file once.</param>
        public static void WriteStrings(List<StringInfo> strings, List<byte> bytes, bool compressStrings = false)
        {
            List<StringInfo> stringsToWrite = new List<StringInfo>();

            if (compressStrings)
            {
                foreach(StringInfo stringInfo in strings)
                {
                    if(stringsToWrite.All(x => x.StringToWrite != stringInfo.StringToWrite))
                    {
                        stringsToWrite.Add(stringInfo);
                    }
                }
            }
            else
            {
                stringsToWrite = strings;
            }

            foreach(StringInfo _string  in stringsToWrite)
            {
                if (string.IsNullOrWhiteSpace(_string.StringToWrite)) continue;

                int offset = bytes.Count;
                bytes.AddRange(Encoding.ASCII.GetBytes(_string.StringToWrite));
                bytes.Add(0);

                //Update offsets
                foreach(StringInfo offsets in strings.Where(x => x.StringToWrite == _string.StringToWrite))
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(offset - offsets.RelativeOffset), offsets.Offset);
                }
            }
        }

        [Obsolete(@"Replaced by WriteStrings method. This should only be used for the few old parsers that made use of it originally, as null/empty strings were handled differently in those.")]
        public static List<byte> WritePointerStrings(List<StringInfo> _strings, List<byte> bytes)
        {
            if (_strings != null)
            {
                for (int i = 0; i < _strings.Count; i++)
                {
                    if ((_strings[i].StringToWrite == "NULL" || string.IsNullOrWhiteSpace(_strings[i].StringToWrite)) == false)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - _strings[i].RelativeOffset), _strings[i].Offset);
                        bytes.AddRange(Encoding.ASCII.GetBytes(_strings[i].StringToWrite));
                        bytes.Add(0);
                    }
                }
            }
            return bytes;
        }

        /// <summary>
        /// Writes a fixed length string, and adds padding to ensure that it is always the same length (null bytes). If length exceeds max length, an error will be displayed and the application will exit.
        /// </summary>
        public static List<byte> WriteFixedLengthString(string _string, int maxLength, string parameterName, List<byte> bytes)
        {
            if (_string.Length > maxLength)
            {
                Console.WriteLine(String.Format("{0} exceeds the maximum allowed length of {1} for parameter {2}", _string, maxLength, parameterName));
                Utils.WaitForInputThenQuit();
            }
            bytes.AddRange(Encoding.ASCII.GetBytes(_string));
            if (maxLength != _string.Length)
            {
                bytes.AddRange(new byte[maxLength - _string.Length]);
            }
            return bytes;
        }

    }

    public static class PtrWriter
    {
        public struct Ptr
        {
            public int Offset { get; set; }
            public int RelativeTo { get; set; }
        }

        public static List<byte> WritePointers(List<Ptr> ptrs, List<byte> bytes)
        {
            foreach (var ptr in ptrs)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - ptr.RelativeTo), ptr.Offset);
            }
            return bytes;
        }
    }

    public static class TimeHelper
    {
        public static DateTime ToDateTime(ulong time)
        {
            string strTime = time.ToString();

            //Validation
            if (time == 0) return new DateTime(1, 1, 1, 0, 0, 0);
            if (strTime.Length != 14) return new DateTime(1, 1, 1, 0, 0, 0);
            //if (strTime.Length != 14) throw new InvalidDataException(String.Format("Could not parse the saved time. Expected either 1 or 14 characters, but found {0}. (Value = {1})", strTime.Length, time));

            try
            {
                //char arrays
                char[] year = new char[4];
                char[] month = new char[2];
                char[] day = new char[2];
                char[] hour = new char[2];
                char[] minute = new char[2];
                char[] seconds = new char[2];

                //Copy to char arrays
                strTime.CopyTo(0, year, 0, 4);
                strTime.CopyTo(4, month, 0, 2);
                strTime.CopyTo(6, day, 0, 2);
                strTime.CopyTo(8, hour, 0, 2);
                strTime.CopyTo(10, minute, 0, 2);
                strTime.CopyTo(12, seconds, 0, 2);

                //create ints
                int yearInt = int.Parse(new string(year));
                int monthInt = int.Parse(new string(month));
                int dayInt = int.Parse(new string(day));
                int hourInt = int.Parse(new string(hour));
                int minuteInt = int.Parse(new string(minute));
                int secondInt = int.Parse(new string(seconds));

                //Console.WriteLine(String.Format("Year: {0}\nMonth: {1}\nDay: {2}\nHour: {3}\nMinute: {4}\nSecond: {5}", yearInt.ToString(), monthInt.ToString(), dayInt.ToString(), hourInt.ToString(), minuteInt.ToString(), secondInt.ToString()));

                //Creating DateTime
                return new DateTime(yearInt, monthInt, dayInt, hourInt, minuteInt, secondInt);
            }
            catch
            {
                return new DateTime(1, 0, 0);
            }

        }

        public static ulong ToUInt64(DateTime time)
        {
            //first, validate the year (if it is 1, then return 0)
            if (time.Year == 1) return 0;

            //calculate the time
            StringBuilder timeStr = new StringBuilder();

            string year = time.Year.ToString("D4");
            string month = time.Month.ToString("D2");
            string day = time.Day.ToString("D2");
            string hour = time.Hour.ToString("D2");
            string minute = time.Minute.ToString("D2");
            string second = time.Second.ToString("D2");


            //Validation
            if (year.Length != 4 || month.Length != 2 || day.Length != 2 || hour.Length != 2 || minute.Length != 2 || second.Length != 2)
            {
                Console.WriteLine(year.ToArray());
                Console.WriteLine(month.ToArray());
                Console.WriteLine(day.ToArray());
                Console.WriteLine(hour.ToArray());
                Console.WriteLine(minute.ToArray());
                Console.WriteLine(second.ToArray());
                throw new InvalidDataException("Year, Month, Day, Hour, Minute and Second properties are invalid.");
            }

            timeStr.Append(year);
            timeStr.Append(month);
            timeStr.Append(day);
            timeStr.Append(hour);
            timeStr.Append(minute);
            timeStr.Append(second);

            return ulong.Parse(timeStr.ToString());
        }
    }

}
