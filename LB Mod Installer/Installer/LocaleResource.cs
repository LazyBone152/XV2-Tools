using System.Collections.Generic;
using YAXLib;

namespace LB_Mod_Installer.Installer
{
    [YAXSerializeAs("Localisations")]
    public class LocaleResource
    {
        public const string PATH = "locale.xml";

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Local")]
        public List<Localisation> Localisations { get; set; } = new List<Localisation>();
    }
}
