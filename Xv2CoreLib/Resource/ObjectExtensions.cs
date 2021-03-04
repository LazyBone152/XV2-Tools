using System.Collections.Generic;
using System.Reflection;
using System.ArrayExtensions;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace System
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Create a deep-copy of the object via binary serialization. Object must be marked with [Serializable] attribute, as well as all referenced objects.
        /// </summary>
        public static T Copy<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(ms);
            }
        }

        //Notify
        /// <summary>
        /// Invokes NotifyPropertyChanged for all properties on this object. NOTE: Requires NotifyPropertyChanged to be public!
        /// </summary>
        public static void NotifyPropsChanged(this object instance)
        {
            foreach (var prop in instance.GetType().GetProperties())
            {
                MethodInfo function = instance.GetType().GetMethod("NotifyPropertyChanged");

                if(function != null)
                    function.Invoke(instance, new object[] { prop.Name });
            }
        }
    
        public static bool Compare(this object instance, object compareObj, params string[] exclusions)
        {
            foreach(var prop in instance.GetType().GetProperties())
            {
                if ((prop.PropertyType == typeof(string) || prop.PropertyType.IsPrimitive || prop.PropertyType.IsValueType)
                    && (prop.SetMethod != null && prop.GetMethod != null) && !exclusions.Contains(prop.Name))
                {
                    if (prop.GetValue(instance) != prop.GetValue(compareObj)) return false;
                }
            }

            return true;
        }
    }

    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }
        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    namespace ArrayExtensions
    {
        public static class ArrayExtensions
        {
            public static void ForEach(this Array array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new ArrayTraverse(array);
                do action(array, walker.Position);
                while (walker.Step());
            }
        }

        internal class ArrayTraverse
        {
            public int[] Position;
            private int[] maxLengths;

            public ArrayTraverse(Array array)
            {
                maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i)
                {
                    maxLengths[i] = array.GetLength(i) - 1;
                }
                Position = new int[array.Rank];
            }

            public bool Step()
            {
                for (int i = 0; i < Position.Length; ++i)
                {
                    if (Position[i] < maxLengths[i])
                    {
                        Position[i]++;
                        for (int j = 0; j < i; j++)
                        {
                            Position[j] = 0;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }

}