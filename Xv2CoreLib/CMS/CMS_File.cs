using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.CMS
{

    [YAXSerializeAs("CMS")]
    public class CMS_File : ISorting
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Entry")]
        public List<CMS_Entry> CMS_Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            CMS_Entries.Sort((x, y) => x.SortID - y.SortID);
        }

        public CMS_Entry GetEntry(int id)
        {
            return GetEntry(id.ToString());
        }

        public CMS_Entry GetEntry(string id)
        {
            foreach (var entry in CMS_Entries)
            {
                if (entry.Index == id) return entry;
            }

            return null;
        }

        public CMS_Entry GetEntryByCharaCode(string charaCode)
        {
            return CMS_Entries.FirstOrDefault(x => x.ShortName == charaCode);
        }

        public static CMS_File Load(byte[] rawBytes)
        {
            return new Parser(rawBytes).GetCmsFile();
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public string CharaIdToCharaCode(int charaId)
        {
            string charaIdStr = charaId.ToString();
            CMS_Entry entry = CMS_Entries.FirstOrDefault(x => x.Index == charaIdStr);

            if (entry != null)
            {
                return entry.ShortName;
            }

            return string.Empty;
        }

        public int CharaCodeToCharaId(string charaCode)
        {
            CMS_Entry entry = CMS_Entries.FirstOrDefault(x => x.ShortName?.Equals(charaCode, StringComparison.OrdinalIgnoreCase) == true);

            if (entry != null)
            {
                return entry.ID;
            }

            return -1;
        }

        public CMS_Entry CreateDummyEntry()
        {
            for (int i = 0; i < 0x99; i++)
            {
                string format = i < 0x10 ? "0" : "";
                string dummyName = $"X{format}{i.ToString("x")}";

                if (dummyName.Length != 3)
                    throw new Exception($"CMS_File.CreateDummyEntry: dummyName is supposed to be 3 characters, but was {dummyName.Length}.");

                if (GetEntryByCharaCode(dummyName) == null)
                {
                    CMS_Entry dummy = CMS_Entry.CreateDummyEntry(AssignNewID(), dummyName);

                    if (dummy.ID >= 500)
                    {
                        throw new Exception("CMS_File.CreateDummyEntry: A suitable dummy entry could not be created because too many characters are installed!\n\nUninstall some older character mods, and then try again.");
                    }

                    return dummy;
                }
            }

            throw new Exception("CMS_File.CreateDummyEntry: A dummy CMS entry could not be created.");
        }

        private int AssignNewID()
        {
            int id = 150;

            while (true)
            {
                if (CMS_Entries.FirstOrDefault(x => x.ID == id) == null)
                {
                    break;
                }

                id++;
            }

            return id;
        }

        public CMS_Entry AssignDummyEntryForSkill(CUS.CUS_File cusFile, CUS.CUS_File.SkillType skillType, List<int> assignedIds = null, List<IUndoRedo> undos = null)
        {
            //Find dummy CMS entry
            CMS_Entry dummyCmsEntry = null;

            foreach (CMS_Entry cmsEntry in CMS_Entries)
            {
                if (cmsEntry.IsDummyEntry())
                {
                    if (!cusFile.IsSkillIdRangeUsed(cmsEntry, skillType, assignedIds))
                    {
                        dummyCmsEntry = cmsEntry;
                        break;
                    }
                }
            }

            //If no suitable dummy was found, create a new one
            if (dummyCmsEntry == null)
            {
                dummyCmsEntry = CreateDummyEntry();
                CMS_Entries.Add(dummyCmsEntry);

                undos?.Add(new UndoableListAdd<CMS_Entry>(CMS_Entries, dummyCmsEntry));
            }

            return dummyCmsEntry;
        }

        public string GetSkillOwner(int skillId2)
        {
            int cmsId = skillId2 / 10;

            //If chara ID belongs to a CAC, it is owned by "CMN" instead
            if (cmsId >= 100 && cmsId < 109)
                return "CMN";

            return CMS_Entries.FirstOrDefault(x => x.ID == cmsId)?.ShortName;
        }
    }

    [YAXSerializeAs("Entry")]
    public class CMS_Entry : IInstallable
    {
        #region WrapperProperties
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }
        [YAXDontSerialize]
        public int ID { get { return int.Parse(Index); } set { Index = value.ToString(); } }
        [YAXDontSerialize]
        public string ShortName { get { return Str_04; } set { Str_04 = value; } }
        [YAXDontSerialize]
        public string BcsPath { get { return Str_32; } set { Str_32 = value; } }
        [YAXDontSerialize]
        public string EanPath { get { return Str_36; } set { Str_36 = value; } }
        [YAXDontSerialize]
        public string FceEanPath { get { return Str_44; } set { Str_44 = value; } }
        [YAXDontSerialize]
        public string FcePath { get { return Str_48; } set { Str_48 = value; } }
        [YAXDontSerialize]
        public string CamEanPath { get { return Str_56; } set { Str_56 = value; } }
        [YAXDontSerialize]
        public string BacPath { get { return Str_60; } set { Str_60 = value; } }
        [YAXDontSerialize]
        public string BcmPath { get { return Str_64; } set { Str_64 = value; } }
        [YAXDontSerialize]
        public string BaiPath { get { return Str_68; } set { Str_68 = value; } }
        [YAXDontSerialize]
        public string BdmPath { get { return Str_80; } set { Str_80 = value; } }

        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public string Index { get; set; } //int32
        [YAXAttributeForClass]
        [YAXSerializeAs("ShortName")]
        public string Str_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public Int64 I_08 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_16 { get; set; }
        [YAXAttributeFor("LoadCamDist")]
        [YAXSerializeAs("value")]
        public ushort I_20 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_24 { get; set; }
        [YAXAttributeFor("I_26")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ushort I_26 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_28 { get; set; }
        [YAXAttributeFor("BCS")]
        [YAXSerializeAs("value")]
        public string Str_32 { get; set; }
        [YAXAttributeFor("EAN")]
        [YAXSerializeAs("value")]
        public string Str_36 { get; set; }
        [YAXAttributeFor("FCE_EAN")]
        [YAXSerializeAs("value")]
        public string Str_44 { get; set; }
        [YAXAttributeFor("FCE")]
        [YAXSerializeAs("value")]
        public string Str_48 { get; set; }
        [YAXAttributeFor("CAM_EAN")]
        [YAXSerializeAs("value")]
        public string Str_56 { get; set; }
        [YAXAttributeFor("BAC")]
        [YAXSerializeAs("value")]
        public string Str_60 { get; set; }
        [YAXAttributeFor("BCM")]
        [YAXSerializeAs("value")]
        public string Str_64 { get; set; }
        [YAXAttributeFor("BAI")]
        [YAXSerializeAs("value")]
        public string Str_68 { get; set; }
        [YAXAttributeFor("BDM")]
        [YAXSerializeAs("value")]
        public string Str_80 { get; set; }


        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "MsgComponent")]
        public List<MSG.Msg_Component> MsgComponents { get; set; } //Only for LB Mod Installer

        public bool IsSelfReference(string path)
        {
            return (ShortName == path || path == string.Format("../{0}/{0}", ShortName));
        }

        public bool IsDummyEntry()
        {
            if (!string.IsNullOrWhiteSpace(Str_32)) return false;
            if (!string.IsNullOrWhiteSpace(Str_36)) return false;
            if (!string.IsNullOrWhiteSpace(Str_44)) return false;
            if (!string.IsNullOrWhiteSpace(Str_48)) return false;
            if (!string.IsNullOrWhiteSpace(Str_56)) return false;
            if (!string.IsNullOrWhiteSpace(Str_60)) return false;
            if (!string.IsNullOrWhiteSpace(Str_64)) return false;
            if (!string.IsNullOrWhiteSpace(Str_68)) return false;
            if (!string.IsNullOrWhiteSpace(Str_80)) return false;

            return true;
        }

        public static CMS_Entry CreateDummyEntry(int id = 0, string charaCode = null)
        {
            return new CMS_Entry()
            {
                ShortName = charaCode,
                ID = id,
                I_16 = 47818,
                I_20 = 47818,
                I_22 = 47818,
                I_24 = 47818,
                I_26 = 47818,
                I_28 = 0x3fffffff
            };
        }
    }

}
