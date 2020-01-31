using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace EEPK_Organiser.NameList
{
    public class NameListFile
    {
        public string Name { get; set; }
        public string DisplayName
        {
            get
            {
                return string.Format("_{0}", Name);
            }
        }
        public string path { get; set; }
        public NameList File { get; set; }

        public NameList GetNameList()
        {
            if(File == null)
            {
                File = NameList.Load(path);
            }

            return File;
        }

    }

    public class NameList
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "NameListEntry")]
        public List<NameListEntry> Names { get; set; }

        public static NameList Load(string path)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(NameList), YAXSerializationOptions.DontSerializeNullObjects);
            return (NameList)serializer.DeserializeFromFile(path);
        }

        public void Save(string path)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(NameList));
            serializer.SerializeToFile(this, path);
        }

        public string GetName(ushort effectId)
        {
            if (Names == null) return null;

            foreach(var name in Names)
            {
                if (name.EffectID == effectId) return name.Description;
            }

            return null;
        }

    }

    public class NameListEntry
    {
        [YAXAttributeForClass]
        public ushort EffectID { get; set; }
        [YAXAttributeForClass]
        public string Description { get; set; }
    }
}
