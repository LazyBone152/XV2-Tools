using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using Xv2CoreLib.Eternity;

namespace LB_Mod_Installer.Installer.Transformation
{
    [YAXSerializeAs("TransformDefines")]
    public class TransformationDefines
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Transformation")]
        public List<TransformationDefine> Transformations { get; set; } 

    }

    [YAXSerializeAs("Transformation")]
    public class TransformationDefine
    {
        [YAXAttributeForClass]
        public string Key { get; set; }

        [YAXAttributeFor("Bac")]
        [YAXSerializeAs("Path")]
        public string BacPath { get; set; }
        [YAXAttributeFor("Bac")]
        [YAXSerializeAs("Entry")]
        public int BacEntry { get; set; }

        [YAXAttributeFor("VfxPath")]
        [YAXSerializeAs("value")]
        public string VfxPath { get; set; }
        [YAXAttributeFor("EanPath")]
        [YAXSerializeAs("value")]
        public string EanPath { get; set; }
        [YAXAttributeFor("CamEanPath")]
        [YAXSerializeAs("value")]
        public string CamEanPath { get; set; }
        [YAXAttributeFor("NameTexturePath")]
        [YAXSerializeAs("value")]
        public string NameTexturePath { get; set; }

        [YAXDontSerializeIfNull]
        public CusAuraData CusAuraData { get; set; }
    }
}
