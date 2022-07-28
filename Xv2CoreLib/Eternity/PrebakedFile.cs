using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using YAXLib;

namespace Xv2CoreLib.Eternity
{
    [YAXSerializeAs("Xv2PreBaked")]
    public class PrebakedFile
    {
        public const string PATH = "pre-baked.xml";

        [YAXAttributeFor("OZARUS")]
        [YAXSerializeAs("value")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially)]
        public List<string> Ozarus { get; set; } = new List<string>();

        [YAXAttributeFor("AUTO_BTL_PORT")]
        [YAXSerializeAs("value")]
        public string XML_AutoBattlePortraitsString { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BodyShape")]
        [YAXDontSerializeIfNull]
        public List<PrebakedBodyShape> BodyShapes { get; set; } = new List<PrebakedBodyShape>();

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CusAuraData")]
        [YAXDontSerializeIfNull]
        public List<CusAuraData> CusAuras { get; set; } = new List<CusAuraData>();

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Alias")]
        [YAXDontSerializeIfNull]
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
            string text = File.ReadAllText(path);
            return Parse(text);
        }

        private static PrebakedFile Parse(string xmlText)
        {
            //Remove invalid comment. Otherwise, XDoc will throw an error.
            xmlText = xmlText.Replace("<!------------------------------------------------>", "<!--  -->");

            //Read xml
            XDocument xml;

            using (StringReader reader = new StringReader(xmlText))
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
            prebaked.LoadLists();

            return prebaked;
        }

        public void LoadLists()
        {
            AutoBattlePortraits = HexConverter.ReadInt32Array(XML_AutoBattlePortraitsString);
            AnyDualSkillList = HexConverter.ReadInt32Array(XML_AnyDualSkillListString);
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
            Sort();

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

        public void Sort()
        {
            AutoBattlePortraits?.Sort();
            AnyDualSkillList?.Sort();
            BodyShapes?.Sort((x, y) => (int)x.CmsEntryID - (int)y.CmsEntryID);
            CusAuras?.Sort((x, y) => x.CusAuraID - y.CusAuraID);
            PreBakedAliases?.Sort((x, y) => (int)x.CmsEntryID - (int)y.CmsEntryID);
        }
        #endregion

        #region CusAura
        private List<int> ReservedCusAuraIds = new List<int>();

        public int GetFreeCusAuraID(int sequenceSize)
        {
            int id = CusAuraData.CUS_DATA_ID_START;

            while(IsCusAuraSequenceUsed(id, sequenceSize))
            {
                id++;
            }

            //Keep track of reserved IDs so they are not re-assigned
            for (int i = 0; i < sequenceSize; i++)
                ReservedCusAuraIds.Add(id + i);

            return id;
        }

        private bool IsCusAuraSequenceUsed(int id, int sequenceSize)
        {
            if (CusAuras.Any(x => x.CusAuraID >= id && x.CusAuraID < id + sequenceSize))
                return true;

            if (ReservedCusAuraIds.Any(x => x >= id && x < id + sequenceSize))
                return true;

            return false;
        }

        #endregion

        #region Install
        public List<string> InstallOzarus(List<string> ozarus)
        {
            List<string> ids = new List<string>();

            foreach(var ozaru in ozarus)
            {
                if (!Ozarus.Contains(ozaru))
                {
                    Ozarus.Add(ozaru);
                    ids.Add(ozaru);
                }
            }

            return ids;
        }

        public List<string> InstallAutoBattlePortraits(List<int> autoBattlePortraits)
        {
            List<string> ids = new List<string>();

            foreach (var auto in autoBattlePortraits)
            {
                if (!AutoBattlePortraits.Contains(auto))
                {
                    AutoBattlePortraits.Add(auto);
                    ids.Add(auto.ToString());
                }
            }

            return ids;
        }

        public List<string> InstallAnyDualSkill(List<int> anyDualSkill)
        {
            List<string> ids = new List<string>();

            foreach (var dualSkill in anyDualSkill)
            {
                if (!AnyDualSkillList.Contains(dualSkill))
                {
                    AnyDualSkillList.Add(dualSkill);
                    ids.Add(dualSkill.ToString());
                }
            }

            return ids;
        }
        
        public List<string> InstallCusAuras(List<CusAuraData> cusAuras)
        {
            List<string> ids = new List<string>();

            foreach (var cusAura in cusAuras)
            {
                InstallCusAura(cusAura);
                ids.Add(cusAura.CusAuraID.ToString());
            }

            return ids;
        }

        public List<string> InstallBodyShape(List<PrebakedBodyShape> bodyShapes)
        {
            List<string> ids = new List<string>();

            foreach (var bodyShape in bodyShapes)
            {
                InstallBodyShape(bodyShape);
                ids.Add(bodyShape.CmsEntryID.ToString());
            }

            return ids;
        }

        public List<string> InstallAlias(List<PreBakedAlias> aliases)
        {
            List<string> ids = new List<string>();

            foreach (var alias in aliases)
            {
                InstallAlias(alias);
                ids.Add(alias.CmsEntryID.ToString());
            }

            return ids;
        }

        public void InstallCusAura(CusAuraData cusAura)
        {
            int index = CusAuras.IndexOf(CusAuras.FirstOrDefault(x => x.CusAuraID == cusAura.CusAuraID));

            if(index == -1)
            {
                CusAuras.Add(cusAura);
            }
            else
            {
                CusAuras[index] = cusAura;
            }
        }

        public void InstallBodyShape(PrebakedBodyShape bodyShape)
        {
            int index = BodyShapes.IndexOf(BodyShapes.FirstOrDefault(x => x.CmsEntryID == bodyShape.CmsEntryID));

            if (index == -1)
            {
                BodyShapes.Add(bodyShape);
            }
            else
            {
                BodyShapes[index] = bodyShape;
            }
        }

        public void InstallAlias(PreBakedAlias alias)
        {
            int index = PreBakedAliases.IndexOf(PreBakedAliases.FirstOrDefault(x => x.CmsEntryID == alias.CmsEntryID));

            if (index == -1)
            {
                PreBakedAliases.Add(alias);
            }
            else
            {
                PreBakedAliases[index] = alias;
            }
        }


        public void UninstallOzarus(List<string> ids)
        {
            Ozarus.RemoveAll(x => ids.Contains(x));
        }

        public void UninstallAutoBattlePortraits(List<string> ids)
        {
            AutoBattlePortraits.RemoveAll(x => ids.Contains(x.ToString()));
        }

        public void UninstallAnyDualSkill(List<string> ids)
        {
            AnyDualSkillList.RemoveAll(x => ids.Contains(x.ToString()));
        }
        
        public void UninstallCusAuras(List<string> ids)
        {
            foreach(var stringId in ids)
            {
                int id;

                if(int.TryParse(stringId, out id))
                {
                    UninstallCusAura(id);
                }
            }
        }

        public void UninstallBodyShapes(List<string> ids)
        {
            foreach (var stringId in ids)
            {
                int id;

                if (int.TryParse(stringId, out id))
                {
                    UninstallBodyShape(id);
                }
            }
        }

        public void UninstallAliases(List<string> ids)
        {
            foreach (var stringId in ids)
            {
                int id;

                if (int.TryParse(stringId, out id))
                {
                    UninstallAlias(id);
                }
            }
        }

        public void UninstallCusAura(int cusAuraId)
        {
            CusAuras.RemoveAll(x => x.CusAuraID == cusAuraId);
        }

        public void UninstallBodyShape(int cmsId)
        {
            BodyShapes.RemoveAll(x => x.CmsEntryID == cmsId);
        }

        public void UninstallAlias(int cmsId)
        {
            PreBakedAliases.RemoveAll(x => x.CmsEntryID == cmsId);
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
        /// <summary>
        /// Custom CusAura IDs start from this point. Do not allow any IDs below this.
        /// </summary>
        public const int CUS_DATA_ID_START = 0x30;

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

        public CusAuraData() { }

        public CusAuraData(int cusAuraId)
        {
            CusAuraID = (ushort)cusAuraId;
        }
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
