using System.Reflection;
using System.Linq;

namespace System
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Create a deep-copy of the object via reflection (wrapper extension method for FastCloner.FastCloner.DeepClone).
        /// </summary>
        public static T Copy<T>(this T obj)
        {
            if (obj == null) return obj;
            return FastCloner.FastCloner.DeepClone(obj);
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

        /// <summary>
        /// Invokes NotifyPropertyChanged for a specific property on this object. NOTE: Requires NotifyPropertyChanged to be public!
        /// </summary>
        public static void NotifyPropsChanged(this object instance, string propertyName)
        {
            foreach (var prop in instance.GetType().GetProperties())
            {
                if(prop.Name == propertyName)
                {
                    MethodInfo function = instance.GetType().GetMethod("NotifyPropertyChanged");

                    if (function != null)
                        function.Invoke(instance, new object[] { prop.Name });
                }
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
}