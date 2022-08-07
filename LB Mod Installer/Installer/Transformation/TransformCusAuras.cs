using System.Collections.Generic;
using Xv2CoreLib.Eternity;
using YAXLib;

namespace LB_Mod_Installer.Installer.Transformation
{
    public class TransformCusAuras
    {

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CusAura")]
        public List<TransformCusAura> CusAuras { get; set; }
    }

    public class TransformCusAura
    {

        [YAXAttributeForClass]
        public string Key { get; set; }
        public CusAuraData CusAuraData { get; set; }
    }
}
