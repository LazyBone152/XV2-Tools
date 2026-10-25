using System;
using System.Collections.Generic;
using System.Windows;

namespace LB_Common.Utils
{
    public static class CustomClipboard
    {
        public static bool UseLocalClipboard = true;
        private static string StoredFormat = null;
        private static object StoredObject = null;

        public static bool ContainsData(string format)
        {
            return UseLocalClipboard ? StoredFormat == format : Clipboard.ContainsData(format);
        }

        public static void SetData<T>(string format, T value)
        {
            if (UseLocalClipboard)
            {
                T copy = FastCloner.FastCloner.DeepClone(value);
                StoredFormat = format;
                StoredObject = copy;
            }
            else
            {
                //I have re-enabled BinaryFormatter for .NET 10 via a package and setting some project settings
#if NET10_0_OR_GREATER_disabled
                Clipboard.SetDataAsJson<T>(format, value);
#else
                Clipboard.SetData(format, value);
#endif
                
            }
        }

        public static T GetData<T>(string format) where T : class
        {
            return TryGetData(format, out T result) ? result : null;
        }

        public static bool TryGetData<T>(string format, out T value) where T : class
        {
            if (UseLocalClipboard)
            {
                if(StoredFormat == format)
                {
                    value = StoredObject as T;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
            else
            {
                if (Clipboard.ContainsData(format))
                {
#if NET10_0_OR_GREATER_disabled
                    if(Clipboard.TryGetData(format, out T outValue))
                    {
                        value = outValue;
                        return true;
                    }
#else
                    value = Clipboard.GetData(format) as T;
                    return true;
#endif
                }

                value = null;
                return false;
            }

        }
    }
}
