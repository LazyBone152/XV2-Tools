using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib
{
    //The old namelist system used in EEPK Organiser
    //XenoKit will load these if present and use it as a fallback if no user defined name is found in the new system (for EEPK files)

    [YAXSerializeAs("NameList")]
    public class LegacyNameList
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "NameListEntry")]
        public List<LegacyNameListEntry> Names { get; set; }

        public static LegacyNameList Load(string path)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(LegacyNameList), YAXSerializationOptions.DontSerializeNullObjects);
            return (LegacyNameList)serializer.DeserializeFromFile(path);
        }

        public string GetName(ushort effectId)
        {
            if (Names == null) return null;

            foreach (var name in Names)
            {
                if (name.EffectID == effectId) return name.Description;
            }

            return null;
        }

    }

    [YAXSerializeAs("NameListEntry")]
    public class LegacyNameListEntry
    {
        [YAXAttributeForClass]
        public ushort EffectID { get; set; }
        [YAXAttributeForClass]
        public string Description { get; set; }
    }
}
