using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using YAXLib;
using System.Security.Cryptography;
using Xv2CoreLib.Resource.UndoRedo;
using System.Reflection;
using System.ComponentModel;

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

            foreach(var value in list)
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
    
        public static List<int> ConvertToIntList(List<ushort> list)
        {
            List<int> newList = new List<int>();

            foreach (var value in list)
                newList.Add((int)value);

            return newList;
        }
    }

    public static class Xv2ColorConverter
    {
        public static float ConvertColor(int RGB_Int_Value)
        {
            return (float)RGB_Int_Value / 255;
        }

        public static byte ConvertColor(float RGB_Float_Value)
        {
            return (byte)(RGB_Float_Value * 255);
        }
    }

    public static class StringEx
    {
        public enum EncodingType
        {
            ASCII,
            UTF8,
            Unicode 
        }
        
        /// <summary>
        /// Search for a string at the inputted index and return it. Supports ASCII and UTF8 encoding as well as fixed length strings and null terminated ones.
        /// </summary>
        public static string GetString(List<byte> bytes, int index, bool useNullText = true, EncodingType encodingType = EncodingType.ASCII, int maxSize = int.MaxValue, bool useNullTerminator = true)
        {
            if (index == 0)
            {
                return (useNullText) ? "NULL" : "";
            }

            if (index > bytes.Count - 1)
            {
                throw new IndexOutOfRangeException(String.Format("GetString: index is out of range.\nIndex = {0}\nSize = {1}", index, bytes.Count));
            }

            //Get size
            int desiredSize = maxSize;
            if (useNullTerminator)
            {

                maxSize = GetStringSize(bytes, index);

                if (maxSize > desiredSize)
                {
                    maxSize = desiredSize;
                }
            }

            if(maxSize == 0)
            {
                return (useNullText) ? "NULL" : "";
            }

            if (encodingType == EncodingType.ASCII)
            {
                string value = Encoding.ASCII.GetString(bytes.GetRange(index, maxSize).ToArray());
                return (string.IsNullOrWhiteSpace(value) && useNullText) ? "NULL" : value;
            }
            else if (encodingType == EncodingType.UTF8)
            {
                string value = Encoding.UTF8.GetString(bytes.GetRange(index, maxSize).ToArray());
                return (string.IsNullOrWhiteSpace(value) && useNullText) ? "NULL" : value;
            }
            else
            {
                throw new Exception("GetString: Unsupported EncodingType = " + encodingType);
            }
        }
        
        /// <summary>
        /// Search for a string at the inputted index and return it. Supports ASCII and UTF8 encoding as well as fixed length strings and null terminated ones.
        /// </summary>
        public static string GetString(byte[] bytes, int index, bool useNullText = true, EncodingType encodingType = EncodingType.ASCII, int maxSize = int.MaxValue, bool useNullTerminator = true, bool allowZeroIndex = false)
        {
            if (index == 0 && !allowZeroIndex)
            {
                return (useNullText) ? "NULL" : "";
            }

            if (index > bytes.Length - 1)
            {
                throw new IndexOutOfRangeException(String.Format("GetString: index is out of range.\nIndex = {0}\nSize = {1}", index, bytes.Length));
            }

            //Get string size
            int desiredSize = maxSize;

            if (useNullTerminator)
            {
                maxSize = GetStringSize(bytes, index, (encodingType == EncodingType.Unicode));

                if (maxSize > desiredSize)
                {
                    maxSize = desiredSize;
                }
            }

            if (maxSize == 0)
            {
                return (useNullText) ? "NULL" : "";
            }

            if (encodingType == EncodingType.ASCII)
            {
                string value = Encoding.ASCII.GetString(bytes, index, maxSize);
                return (string.IsNullOrWhiteSpace(value) && useNullText) ? "NULL" : value;
            }
            else if (encodingType == EncodingType.UTF8)
            {
                string value = Encoding.UTF8.GetString(bytes, index, maxSize);
                return (string.IsNullOrWhiteSpace(value) && useNullText) ? "NULL" : value;
            }
            else
            {
                string value = Encoding.Unicode.GetString(bytes, index, maxSize);
                return (string.IsNullOrWhiteSpace(value) && useNullText) ? "NULL" : value;
                //throw new Exception("GetString: Unsupported EncodingType = " + encodingType);
            }
        }
        
        private static int GetStringSize(byte[] bytes, int index, bool unicode = false)
        {
            if (unicode)
            {
                for(int i = index; i < bytes.Length; i += 2)
                {
                    if (i >= bytes.Length) break;

                    if (bytes[i] == 0 && bytes[i + 1] == 0)
                    {
                        return i - index;

                    }
                }
            }
            else
            {
                for (int i = index; i < bytes.Length; i++)
                {
                    if (bytes[i] == 0)
                    {
                        return i - index;
                        
                    }
                }
            }

            throw new InvalidDataException(String.Format("GetStringSize: Could not find the null terminator byte.\nIndex = {0}\nPosition = {1}", index, bytes.Length - 1));
        }

        private static int GetStringSize(List<byte> bytes, int index)
        {
            for (int i = index; i < bytes.Count; i++)
            {
                if (bytes[i] == 0)
                    return i - index;
            }

            throw new InvalidDataException(String.Format("GetStringSize: Could not find the null terminator byte.\nIndex = {0}\nPosition = {1}", index, bytes.Count - 1));
        }

        public static int GetPaddedUnicodeStringByteSize(string unicodeString, int blockSize)
        {
            int size = unicodeString.Length * 2;
            size += 1; //Null terminator
            return Utils.CalculatePadding(size, blockSize) + size;
        }
    
        public static byte[] WriteFixedSizeString(string str, int maxSize)
        {
            //Trim if too long
            if (str.Length > maxSize)
            {
                str = str.Substring(0, maxSize);
            }

            //Write name, and pad it out to max size
            List<byte> bytes = new List<byte>(maxSize - str.Length);
            bytes.AddRange(Encoding.ASCII.GetBytes(str));
            bytes.AddRange(new byte[maxSize - str.Length]);

            return bytes.ToArray();
        }
    }
    
    public static class Utils
    {
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

            foreach(var prop in properties)
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

        public static string SanitizePath(string path)
        {
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

        public static string GetStringUtf(List<byte> bytes, int offset)
        {
            return StringEx.GetString(bytes, offset, true, StringEx.EncodingType.UTF8);
        }

        public static string GetStringUtf(byte[] bytes, int offset)
        {
            return StringEx.GetString(bytes, offset, false, StringEx.EncodingType.UTF8);
        }

        public static string GetString(List<byte> bytes, int offset, int size)
        {
            return StringEx.GetString(bytes, offset, false, StringEx.EncodingType.ASCII, size);
        }

        public static string GetString(List<byte> bytes, int offset)
        {
            return StringEx.GetString(bytes, offset, true, StringEx.EncodingType.ASCII);
        }

        /// <summary>
        /// Retreives a string from the given byte array and offset. Will have a set size instead of terminating at the first null ASCII value. Support unicode strings. [OLD]
        /// </summary>
        public static string GetString(List<byte> bytes, int offset, int size, bool unicode)
        {
            if (size == 1 && unicode == false)
            {
                return "";
            }
            if (size == 2 && unicode == true)
            {
                return "";
            }

            if (offset + size > bytes.Count())
            {
                throw new Exception("ERROR in GetString: given offset + size is greater than size of given list. Load failed.");
            }

            List<byte> stringList = bytes.GetRange(offset, size);

            if (unicode == true)
            {
                stringList.RemoveRange(size - 2, 2);
                return Encoding.Unicode.GetString(stringList.ToArray());
            }
            else
            {
                return Encoding.ASCII.GetString(stringList.ToArray());
            }
        }

        /// <summary>
        /// EEPK File Only: Return the number associated with a file type for an Asset Entry
        /// </summary>
        public static string GetEepkFileTypeNumber(string input)
        {
            //todo: rewrite eepk parser and remove them mess
            string extension = Path.GetExtension(input);
            switch (extension)
            {
                case ".emo":
                    return 0.ToString();
                case ".emm":
                    return 1.ToString();
                case ".emb":
                    return 2.ToString();
                case ".ema":
                    return 3.ToString();
                case ".emp":
                    return 4.ToString();
                case ".etr":
                    return 5.ToString();
                case ".ecf":
                    return 7.ToString();
                default:
                    throw new Exception("Undefined file format reference encountered (" + extension + ")\nDeserialization failed.");
            }
        }


        /// <summary>
        /// Resizes an array by adding elements to the end or removing them.
        /// </summary>
        /// <returns></returns>
        public static byte[] ResizeArray(byte[] array, int newSize)
        {
            if (newSize == array.Length) return array;

            List<byte> bytes = array.ToList();
            
            if(newSize > array.Length)
            {
                //Add elements
                for(int i = 0; i < newSize - array.Length; i++)
                {
                    bytes.Add(0);
                }
            }
            else
            {
                //Remove them
                bytes.RemoveRange(newSize, array.Length - newSize);
            }

            if(bytes.Count != newSize) throw new InvalidDataException("ResizeArray: bytes.Count != newSize");

            return bytes.ToArray();
        }

        /// <summary>
        /// Resizes an array by adding elements to the end or removing them.
        /// </summary>
        /// <returns></returns>
        public static List<byte> ResizeArray(List<byte> array, int newSize)
        {
            if (newSize == array.Count) return array;
            int originalSize = array.Count;

            if (newSize > originalSize)
            {
                //Add elements
                for (int i = 0; i < newSize - originalSize; i++)
                {
                    array.Add(0);
                }
            }
            else
            {
                //Remove them
                array.RemoveRange(newSize, originalSize - newSize);
            }

            if (array.Count != newSize) throw new InvalidDataException("ResizeArray: bytes.Count != newSize");

            return array;
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


        public static int CalculatePadding(int fileSize, int byteAlignment)
        {
            //return (byteAlignment - (fileSize % byteAlignment)) % byteAlignment;

            //Using floats results in overflows when dealing with large files
            double f_offset = fileSize;
            int padding = 0;

            while (f_offset / byteAlignment != Math.Floor(f_offset / byteAlignment))
            {
                f_offset += 1d;
                padding += 1;
            }
            return (int)padding;
            
        }

        public static byte[] PadBytes(byte[] bytes, int minSize)
        {
            
            if (bytes.Length >= minSize) return bytes;

            List<byte> _bytes = bytes.ToList();

            if(_bytes.Count() < minSize)
            {
                while(_bytes.Count() < minSize)
                {
                    _bytes.Add(0);
                }
            }

            return _bytes.ToArray();
        }

        public static bool CompareArray(byte[] array1, byte[] array2)
        {
            if (array1 == null && array2 == null) return true;
            if (array1 == null && array2 != null) return false;
            if (array2 == null && array1 != null) return false;
            if (array1.Length != array2.Length) return false;

            for(int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i]) return false;
            }

            return true;
        }

        public static bool CompareList(List<byte> array1, List<byte> array2)
        {
            if (array1 == null && array2 == null) return true;
            if (array1 == null && array2 != null) return false;
            if (array2 == null && array1 != null) return false;
            if (array1.Count != array2.Count) return false;

            for (int i = 0; i < array1.Count; i++)
            {
                if (array1[i] != array2[i]) return false;
            }

            return true;
        }


        public static byte[] GetRangeFromByteArray(byte[] bytes, int index, int count)
        {
            byte[] ret = new byte[count];

            for(int i = index; i < index + count; i++)
            {
                ret[i - index] = bytes[i];
            }

            return ret;
        }

        public static int RoundOff(int i)
        {
            return ((int)Math.Round(i / 10.0)) * 10;
        }

        public static int GetSetBitCount(long lValue)
        {
            int iCount = 0;

            //Loop the value while there are still bits
            while (lValue != 0)
            {
                //Remove the end bit
                lValue = lValue & (lValue - 1);

                //Increment the count
                iCount++;
            }

            //Return the count
            return iCount;
        }

        /// <summary>
        /// Searches all common directories for a Xenoverse 2 installation.
        /// </summary>
        /// <returns></returns>
        public static string FindGameDirectory()
        {
            List<string> alphabet = new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "O", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

            bool found = false;
            try
            {
                string GameDirectoryPath = String.Empty;

                if (File.Exists("DB Xenoverse 2/bin/DBXV2.exe"))
                {
                    //At same level as DB Xenoverse 2 folder
                    GameDirectoryPath = System.IO.Path.GetFullPath("DB Xenoverse 2");
                    found = true;
                }
                else if (File.Exists("../bin/DBXV2.exe") && found == false)
                {
                    //In data folder
                    GameDirectoryPath = System.IO.Path.GetFullPath("..");
                    found = true;
                }
                else if (File.Exists("bin/DBXV2.exe") && found == false)
                {
                    //In DB Xenoverse 2 root directory
                    GameDirectoryPath = Directory.GetCurrentDirectory();
                    found = true;
                }
                else if (found == false)
                {
                    foreach (string letter in alphabet)
                    {
                        string _path = String.Format(@"{0}:{1}Program Files (x86){1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                        if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                        {
                            GameDirectoryPath = _path;
                            found = true;
                        }
                    }

                    if (found == false)
                    {
                        foreach (string letter in alphabet)
                        {
                            string _path = String.Format(@"{0}:{1}Program Files{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                            if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                            {
                                GameDirectoryPath = _path;
                                found = true;
                            }
                        }
                    }

                    if (found == false)
                    {
                        foreach (string letter in alphabet)
                        {
                            string _path = String.Format(@"{0}:{1}Games{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                            if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                            {
                                GameDirectoryPath = _path;
                                found = true;
                            }
                        }
                    }


                    if (found == false)
                    {
                        foreach (string letter in alphabet)
                        {
                            string _path = String.Format(@"{0}:{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                            if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                            {
                                GameDirectoryPath = _path;
                                found = true;
                            }
                        }
                    }

                    if (found == false)
                    {
                        foreach (string letter in alphabet)
                        {
                            string _path = String.Format(@"{0}:{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2", letter, System.IO.Path.DirectorySeparatorChar);
                            if (File.Exists(String.Format("{0}{1}bin{1}DBXV2.exe", _path, System.IO.Path.DirectorySeparatorChar)) && found == false)
                            {
                                GameDirectoryPath = _path;
                                found = true;
                            }
                        }
                    }
                }
                return GameDirectoryPath;
            }
            catch
            {
                return null;
            }

        }

        public static List<byte> GetStringBytes(string str, int minSize = -1, int maxSize = -1)
        {
            if(str == null)
            {
                str = string.Empty;
            }
            List<byte> strBytes = Encoding.ASCII.GetBytes(str).ToList();

            if(minSize == -1)
            {
                strBytes.Add(0);
            }
            else
            {
                int bytesToAdd = minSize - str.Length;
                if(bytesToAdd > 0)
                {
                    strBytes.AddRange(new byte[bytesToAdd]);
                }
                else
                {
                    strBytes.Add(0);
                }

                if(strBytes.Count > maxSize && maxSize != -1)
                {
                    strBytes.RemoveRange(4, strBytes.Count - 4);
                }
            }

            return strBytes;
        }

        public static string CloneString (string str)
        {
            return String.Format("{0}", str);
        }

        public static List<int> AddUnique(List<int> _list, int value)
        {
            if(_list.IndexOf(value) == -1)
            {
                _list.Add(value);
            }

            return _list;
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
            if (insertedData.Count() >= list.Count())
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
        /// EEPK File: For getting the number of FILES in an Asset Entry, by counting backwards how many NULLS are in the array, and subtracting them from the count. The count stops on the first file name it reaches (the last one). NULLS inbetween the file names are preserved in the count.
        /// </summary>
        public static int GetTotalNonNullValues(List<string> value)
        {
            int actualCountOfFiles = value.Count();
            for (int i = value.Count(); i > 0; i--)
            {
                if (value[i - 1] == "NULL")
                {
                    actualCountOfFiles--;
                }
                else
                {
                    break;
                }
            }
            return actualCountOfFiles;
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
            

            for(int i = 0; i < count * 4; i+=4)
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
        public static byte GetBytes(bool _bool)
        {
            if(_bool == true)
            {
                return 1;
            } else
            {
                return 0;
            }
        }

        public static byte[] GetBytes_Bool32(bool _bool)
        {
            if (_bool == true)
            {
                return new byte[4] { 1, 0,0,0};
            }
            else
            {
                return new byte[4] { 0, 0, 0, 0 };
            }
        }
        
        public static byte[] GetBytes(int[] intArray)
        {
            List<byte> bytes = new List<byte>();

            foreach(int i in intArray)
            {
                bytes.AddRange(BitConverter.GetBytes(i));
            }

            return bytes.ToArray();
        }

        public static byte[] GetBytes(float[] intArray)
        {
            List<byte> bytes = new List<byte>();

            foreach (float i in intArray)
            {
                bytes.AddRange(BitConverter.GetBytes(i));
            }

            return bytes.ToArray();
        }

        public static byte[] GetBytes(short[] intArray)
        {
            List<byte> bytes = new List<byte>();

            foreach (short i in intArray)
            {
                bytes.AddRange(BitConverter.GetBytes(i));
            }

            return bytes.ToArray();
        }

        public static byte[] GetBytes(ushort[] intArray)
        {
            List<byte> bytes = new List<byte>();

            foreach (ushort i in intArray)
            {
                bytes.AddRange(BitConverter.GetBytes(i));
            }

            return bytes.ToArray();
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
            if(value[1] != 'x' || value[0] != '0')
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

            for(int i = 0; i < values.Count; i++)
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
            for(int i = 0; i < 4; i++)
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
            if(a > 15)
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
            if(value.All(Char.IsNumber) == false)
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


            public string AdditionalInfo1 { get; set; }
            public int AdditionalInfo2 { get; set; }
        }


        public static List<byte> WritePointerStrings(List<StringInfo> _strings, List<byte> bytes)
        {
            if (_strings != null)
            {
                for (int i = 0; i < _strings.Count(); i++)
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
            if(_string.Length > maxLength)
            {
                Console.WriteLine(String.Format("{0} exceeds the maximum allowed length of {1} for parameter {2}", _string, maxLength, parameterName ));
                Utils.WaitForInputThenQuit();
            }
            bytes.AddRange(Encoding.ASCII.GetBytes(_string));
            if(maxLength != _string.Length)
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
            foreach(var ptr in ptrs)
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
