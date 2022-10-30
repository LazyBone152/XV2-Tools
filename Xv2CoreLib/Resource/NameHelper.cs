using System;
using System.Collections.Generic;
using System.Linq;

namespace Xv2CoreLib.Resource
{
    public static class NameHelper
    {
        public static string GetUniqueName<T>(string name, IList<T> names, int thisIdx = -1, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase) where T : class, IName
        {
            if (names == null) return name;

            string newName = name;
            int suffix = 1;
            IName thisNameObject = thisIdx != -1 ? names[thisIdx] : null;

            //Check if name already is suffixed with a number, and increment that if it does
            if (name.Contains('_'))
            {
                var splitName = name.Split('_');
                int existingSuffix;

                if(int.TryParse(splitName[splitName.Length -1], out existingSuffix))
                {
                    if(existingSuffix < 100)
                    {
                        suffix = existingSuffix;
                        name = newName.Remove(newName.Length - 1 - splitName[splitName.Length - 1].Length, splitName[splitName.Length - 1].Length + 1);
                        newName = $"{name}_{suffix}";
                    }
                }
            }

            while(names.FirstOrDefault(x => x.Name.Equals(newName, comparisonType) && x != thisNameObject) != null)
            {
                newName = $"{name}_{suffix}";
                suffix++;
            }

            return newName;
        }
    }

    public interface IName
    {
        string Name { get; set; }
    }
}
