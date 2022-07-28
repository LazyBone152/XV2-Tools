using System.Collections.Generic;
using Xv2CoreLib.PUP;
using YAXLib;

namespace LB_Mod_Installer.Installer.Transformation
{
    public class TransformPowerUps
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TransformPowerUp")]
        public List<TransformPowerUp> PowerUps { get; set; }
    }

    public class TransformPowerUp
    {
        [YAXAttributeForClass]
        public string Key { get; set; }
        public PUP_Entry PupEntry { get; set; }
    }

}
