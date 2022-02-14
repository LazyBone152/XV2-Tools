using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using YAXLib;

namespace Xv2CoreLib.Eternity
{
    [YAXSerializeAs("Xv2PreBaked")]
    public class PrebakedFile
    {
        [YAXAttributeFor("OZARUS")]
        [YAXSerializeAs("value")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially)]
        public List<string> Ozarus { get; set; } = new List<string>();

        [YAXAttributeFor("AUTO_BTL_PORT")]
        [YAXSerializeAs("value")]
        public string XML_AutoBattlePortraitsString { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BodyShape")]
        public List<PrebakedBodyShape> BodyShapes { get; set; } = new List<PrebakedBodyShape>();

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CusAuraData")]
        public List<CusAuraData> CusAuras { get; set; } = new List<CusAuraData>();

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Alias")]
        public List<PreBakedAlias> PreBakedAliases { get; set; } = new List<PreBakedAlias>();

        [YAXAttributeFor("ANY_DUAL_SKILL")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string XML_AnyDualSkillListString { get; set; }

        //These are just for CaC2X2Ms as far as I can tell, so they are not parsed. The orignal XML structures are just saved back as they were read.
        private List<XElement> BcsColorMaps = null;

        //Parsed Lists:
        [YAXDontSerialize]
        public List<int> AutoBattlePortraits { get; set; } = new List<int>();
        [YAXDontSerialize]
        public List<int> AnyDualSkillList { get; set; } = new List<int>();

        #region LoadSave
        //Load:
        public static PrebakedFile Load(string path)
        {
            //Remove invalid comment. Otherwise, XDoc will throw an error.
            string text = File.ReadAllText(path);
            text = text.Replace("<!------------------------------------------------>", "<!--  -->");

            //Read xml
            XDocument xml;

            using (StringReader reader = new StringReader(text))
                xml = XDocument.Load(reader);

            //Find BcsColorMaps and remove it (if it exists)
            List<XElement> bcsColorMaps = new List<XElement>();

            foreach(var element in xml.Element("Xv2PreBaked").Elements())
            {
                if(element.Name == "ColorsMap")
                {
                    bcsColorMaps.Add(element);
                }
            }

            //Deserialize XML
            YAXSerializer serializer = new YAXSerializer(typeof(PrebakedFile), YAXSerializationOptions.DontSerializeNullObjects);
            PrebakedFile prebaked = (PrebakedFile)serializer.Deserialize(xml.Root);
            
            if(prebaked != null)
            {
                prebaked.BcsColorMaps = bcsColorMaps;
            }

            //Parse int lists
            prebaked.AutoBattlePortraits = HexConverter.ReadInt32Array(prebaked.XML_AutoBattlePortraitsString);
            prebaked.AnyDualSkillList = HexConverter.ReadInt32Array(prebaked.XML_AnyDualSkillListString);

            return prebaked;
        }

        //Save:
        public void SaveToDisk(string path)
        {
            var xml = Write();
            xml.Save(path);
        }

        public byte[] SaveToBytes()
        {
            byte[] bytes;
            var xml = Write();

            using(MemoryStream ms = new MemoryStream())
            {
                xml.Save(ms);
                bytes = ms.ToArray();
            }

            return bytes;
        }

        private XDocument Write()
        {
            //Create list string values
            XML_AutoBattlePortraitsString = HexConverter.ToSerializedArray(AutoBattlePortraits);
            XML_AnyDualSkillListString = HexConverter.ToSerializedArray(AnyDualSkillList);

            //Serialize XML
            YAXSerializer serializer = new YAXSerializer(typeof(PrebakedFile));
            XDocument xml = serializer.SerializeToXDocument(this);

            //Add BcsColorMaps back in
            if(BcsColorMaps != null)
            {
                var root = xml.Element("Xv2PreBaked");

                foreach(var colorMap in BcsColorMaps)
                {
                    root.Add(colorMap);
                }
            }

            return xml;
        }

        #endregion
    }

    [YAXSerializeAs("BodyShape")]
    public class PrebakedBodyShape
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("cms_id")]
        [YAXHexValue]
        public uint CmsEntryID { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("body_shape")]
        public uint BcsBody { get; set; }
    }

    [YAXSerializeAs("CusAuraData")]
    public class CusAuraData
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("cus_aura_id")]
        [YAXHexValue]
        public ushort CusAuraID { get; set; } //ID in "CUS_AURA" field in cus file
        [YAXAttributeForClass]
        [YAXSerializeAs("aur_aura_id")]
        [YAXHexValue]
        public ushort ActualAuraID { get; set; } //ID in .aur file

        [YAXAttributeFor("BEHAVIOUR_11")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte Behaviour_11 { get; set; }
        [YAXAttributeFor("INTEGER_2")]
        [YAXSerializeAs("value")]
        public uint Integer_2 { get; set; }

        [YAXAttributeFor("BEHAVIOUR_10")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte Behaviour_10 { get; set; }
        [YAXAttributeFor("INTEGER_3")]
        [YAXSerializeAs("value")]
        public uint Integer_3 { get; set; }

        [YAXAttributeFor("FORCE_TELEPORT")]
        [YAXSerializeAs("value")]
        public bool ForceTeleport { get; set; }
        [YAXAttributeFor("BEHAVIOUR_13")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte Behaviour_13 { get; set; }

        [YAXAttributeFor("BEHAVIOUR_66")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string Behaviour_66 { get; set; } //byte. Optional
        [YAXAttributeFor("REMOVE_HAIR_ACCESSORIES")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte RemoveHairAccessories { get; set; } = byte.MaxValue; // Troolean, 0xFF = default game behaviour, 0 = don't remove, 1 = remove

        [YAXAttributeFor("BCS_HAIR_COLOR")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint BcsHairColor { get; set; }
        [YAXAttributeFor("BCS_EYES_COLOR")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint BcsEyesColor { get; set; }
    }

    [YAXSerializeAs("Alias")]
    public class PreBakedAlias
    {
        // Fields for "this" char
        [YAXAttributeForClass]
        [YAXSerializeAs("cms_id")]
        [YAXHexValue]
        public uint CmsEntryID { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("cms_name")]
        public string CmsShortName { get; set; }

        // Fields for the char we are "impersonating".
        [YAXAttributeFor("TTC_FILES")]
        [YAXSerializeAs("value")]
        public string TTC_Files { get; set; } // Will load the ttc files of this char (audio, msg subs, and msg voice)
    }

}
