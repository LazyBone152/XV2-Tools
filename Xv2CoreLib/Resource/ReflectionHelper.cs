using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib.Resource
{
    public static class ReflectionHelper
    {
        public static bool GetBoolProp(object instance, string propertyName, bool defaultValue = false, bool allowNull = true)
        {
            if (instance == null && allowNull) return defaultValue;
            object prop = GetProperty(instance, propertyName);

            try
            {
                return (bool)prop;
            }
            catch { }

            return defaultValue;
        }

        public static string GetStringProp(object instance, string propertyName, string defaultValue = null, bool allowNull = true)
        {
            if (instance == null && allowNull) return defaultValue;
            object prop = GetProperty(instance, propertyName);

            try
            {
                return (string)prop;
            }
            catch { }

            return defaultValue;
        }

        public static int GetIntProp(object instance, string propertyName, int defaultValue = 0, bool allowNull = true)
        {
            if (instance == null && allowNull) return defaultValue;
            object prop = GetProperty(instance, propertyName);

            try
            {
                return (int)prop;
            }
            catch { }

            return defaultValue;
        }

        public static object GetProperty(object instance, string propertyName)
        {
            if (instance == null) throw new NullReferenceException("ReflectionHelper.GetProperty: instance was null.");

            foreach(var prop in instance.GetType().GetProperties())
            {
                if(prop.Name == propertyName && prop.GetGetMethod() != null)
                {
                    return prop.GetValue(instance);
                }
            }

            return null;
        }
    }
}
