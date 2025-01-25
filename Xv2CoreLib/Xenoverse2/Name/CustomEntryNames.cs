using System;
using System.Collections.Generic;
using System.IO;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.App;
using YAXLib;

namespace Xv2CoreLib
{
    public class CustomEntryNames
    {
        [YAXDictionary(EachPairName = "Entry", KeyName = "ID", ValueName = "Name", SerializeKeyAs = YAXNodeTypes.Attribute, SerializeValueAs = YAXNodeTypes.Attribute)]
        public Dictionary<int, string> Names { get; set; } = new Dictionary<int, string>();

        private static CustomEntryNames Default = new CustomEntryNames();

        public CustomEntryNames() { }

        public static void LoadNames(string path, object file)
        {
#if !DEBUG
            try
#endif
            {
                CustomEntryNames names = Load(path);

                if (names == null) 
                    names = Default;

                if (file is EffectContainerFile eepk)
                {
                    names.Apply(eepk.Effects, path);
                }
                else if (file is BAC_File bac)
                {
                    names.Apply(bac.BacEntries, path);
                }
                else if (file is BSA_File bsa)
                {
                    names.Apply(bsa.BSA_Entries, path);
                }
                else if (file is BDM_File bdm)
                {
                    names.Apply(bdm.BDM_Entries, path);
                }
            }
#if !DEBUG
            catch { }
#endif
        }

        public static void SaveNames(string path, object file)
        {
            if (SettingsManager.Instance.CurrentApp != Application.XenoKit) return;

#if !DEBUG
            try
#endif
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"XenoKit/Names/{path}.xml");

                if (file is EffectContainerFile eepk)
                {
                    SaveNamesInternal(fullPath, eepk.Effects);
                }
                else if (file is BAC_File bac)
                {
                    SaveNamesInternal(fullPath, bac.BacEntries);
                }
                else if (file is BSA_File bsa)
                {
                    SaveNamesInternal(fullPath, bsa.BSA_Entries);
                }
                else if (file is BDM_File bdm)
                {
                    SaveNamesInternal(fullPath, bdm.BDM_Entries);
                }
            }
#if !DEBUG
            catch { }
#endif
        }

        private static void SaveNamesInternal<T>(string path, IList<T> names) where T : IUserDefinedName
        {
            CustomEntryNames customNames = new CustomEntryNames();
            customNames.CreateNames(names);

            if(customNames.Names.Count > 0)
            {
                customNames.Save(path);
            }
            else if(File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch { }
            }
        }

        private void Apply<T>(IList<T> entries, string filePath) where T : IUserDefinedName
        {
            //Backwards compatibility with old EEPK name lists
            LegacyNameList legacyNameList = null;

#if !DEBUG
            try
#endif
            {
                if (Path.GetExtension(filePath) == ".eepk")
                {
                    string legacyNamePath = SettingsManager.Instance.GetAppFolder(Application.EepkOrganiser) + $"/namelist/{Path.GetFileNameWithoutExtension(filePath)}.xml";

                    if (File.Exists(legacyNamePath))
                    {
                        legacyNameList = LegacyNameList.Load(legacyNamePath);
                    }
                }
            }
#if !DEBUG
            catch { }
#endif

            foreach (var entry in entries)
            {
                if (Names.TryGetValue(entry.SortID, out string name))
                {
                    entry.UserDefinedName = name;
                }
                else if (legacyNameList != null)
                {
                    entry.UserDefinedName = legacyNameList.GetName((ushort)entry.SortID);
                }
            }
        }

        private void CreateNames<T>(IList<T> entries) where T : IUserDefinedName
        {
            foreach (IUserDefinedName entry in entries)
            {
                if (entry.HasUserDefinedName)
                {
                    Names.Add(entry.SortID, entry.UserDefinedName);
                }
            }
        }

        private static CustomEntryNames Load(string path)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"XenoKit/Names/{path}.xml");

            if (File.Exists(fullPath))
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CustomEntryNames), YAXSerializationOptions.DontSerializeNullObjects);
                return (CustomEntryNames)serializer.DeserializeFromFile(fullPath);
            }

            return null;
        }

        private void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            YAXSerializer serializer = new YAXSerializer(typeof(CustomEntryNames));
            serializer.SerializeToFile(this, path);
        }

        public string GetName(int id)
        {
            string name;
            if (Names.TryGetValue(id, out name))
                return name;
            return null;
        }

        public void SetName(int id, string name)
        {
            if (!Names.TryGetValue(id, out _))
            {
                Names.Add(id, name);
            }
        }
    }
}
