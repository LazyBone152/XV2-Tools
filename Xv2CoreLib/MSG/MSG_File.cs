using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using System.Net;

namespace Xv2CoreLib.MSG
{
    [YAXSerializeAs("MSG")]
    public class MSG_File
    {
        [YAXAttributeForClass]
        public bool unicode_names { get; set; }
        [YAXAttributeForClass]
        public bool unicode_msg { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Msg_Entry")]
        public List<MSG_Entry> MSG_Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static MSG_File Load(string path)
        {
            return new Parser(path, false).GetMsgFile();
        }

        public static MSG_File Load(byte[] rawBytes)
        {
            return new Parser(rawBytes).GetMsgFile();
        }
        
        /// <summary>
        /// Check whether a entry name already exists in the file.
        /// </summary>
        public bool nameExists(string name) {
            if (MSG_Entries != null) {
                for (int i = 0; i < MSG_Entries.Count(); i++)
                {
                    if (MSG_Entries[i].Name == name)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        public static MSG_File DefaultMsgFile() {
            return new MSG_File()
            {
                unicode_msg = true,
                unicode_names = false,
                MSG_Entries = new List<MSG_Entry>()
            };
        }

        public int NextID()
        {
            int nextID = 0;

            if (MSG_Entries != null)
            {
                while (IsIdUsed(nextID) == true)
                {
                    nextID++;
                }
            }
            

            return nextID;
        }

        public int GetLowestUnusedID()
        {
            int nextID = 0;
            if (MSG_Entries != null)
            {
                while (IsIdUsed(nextID) == true)
                {
                    nextID++;
                }
            }
            
            return nextID;
        }

        public bool IsIdUsed(int id)
        {
            string _id = id.ToString();

            for (int i = 0; i < MSG_Entries.Count(); i++)
            {
                if (_id == MSG_Entries[i].Index)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the Index of the given name. If it is not found, -1 is returned.
        /// </summary>
        public int GetIndexOfUsedName(string name)
        {
            if (MSG_Entries != null)
            {
                for (int i = 0; i < MSG_Entries.Count(); i++)
                {
                    if (MSG_Entries[i].Name == name)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Check whether a entry content already exists in the file.
        /// </summary>
        public int GetIndexOfUsedContent(string content)
        {
            if (MSG_Entries != null)
            {
                for (int i = 0; i < MSG_Entries.Count(); i++)
                {
                    if (MSG_Entries[i].Msg_Content[0].Text == content)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public int GetIndexOfUsedId(int id)
        {
            string _id = id.ToString();
            for (int i = 0; i < MSG_Entries.Count(); i++)
            {
                if (_id == MSG_Entries[i].Index)
                {
                    return i;
                }
            }
            return -1;
        }
        
        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public MSG_Entry GetEntry(string name)
        {
            if (MSG_Entries != null)
            {
                for (int i = 0; i < MSG_Entries.Count(); i++)
                {
                    if (MSG_Entries[i].Name == name)
                    {
                        return MSG_Entries[i];
                    }
                }
            }

            return null;
        }

        public string GetEntryText(string name)
        {
            if (MSG_Entries != null)
            {
                for (int i = 0; i < MSG_Entries.Count(); i++)
                {
                    if (MSG_Entries[i].Name == name)
                    {
                        return WebUtility.HtmlDecode(MSG_Entries[i].Msg_Content[0].Text);
                    }
                }
            }

            return null;
        }

        public string GetEntryText(int index)
        {
            if (MSG_Entries != null)
            {
                if(index <= MSG_Entries.Count - 1)
                {
                    return WebUtility.HtmlDecode(MSG_Entries[index].Msg_Content[0].Text);
                }
            }

            return null;
        }

        public int StringCount()
        {
            int count = 0;

            foreach(var msgEntry in MSG_Entries)
            {
                if(msgEntry.Msg_Content != null)
                {
                    foreach (var text in msgEntry.Msg_Content)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public void AddEntryAtId(MSG_Entry entry, int id)
        {
            string _id = id.ToString();
            for(int i = 0; i < MSG_Entries.Count; i++)
            {
                if(MSG_Entries[i].Index == _id)
                {
                    MSG_Entries[i] = entry;
                    return;
                }
            }

            MSG_Entries.Add(entry);
        }

        /// <summary>
        /// Remove all MsgEntries matching the designated name that dont match the id.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="id"></param>
        public void RemoveDuplicateEntries(string name, int id)
        {
            string _id = id.ToString();
            List<MSG_Entry> msgEntriesToDelete = new List<MSG_Entry>();

            for (int i = 0; i < MSG_Entries.Count; i++)
            {
                if (MSG_Entries[i].Index != _id && MSG_Entries[i].Name == name)
                {
                    msgEntriesToDelete.Add(MSG_Entries[i]);
                }
            }

            foreach(var entry in msgEntriesToDelete)
            {
                MSG_Entries.Remove(entry);
            }
            
        }

        /// <summary>
        /// Synchronize msgFiles by copying over missing msg Entries.
        /// </summary>
        /// <param name="msgFiles"></param>
        public static void SynchronizeMsgFiles(IEnumerable<MSG_File> msgFiles)
        {
            foreach (var msgFile in msgFiles)
            {
                foreach (var msgFile2 in msgFiles)
                {
                    msgFile.AddIfMissing(msgFile2.MSG_Entries);
                }
            }
        }

        /// <summary>
        /// Add MSG_Entries if they do not already exist (ID match).
        /// </summary>
        /// <param name="msgEntries">The entries to add.</param>
        public void AddIfMissing(List<MSG_Entry> msgEntries)
        {
            foreach (var msgEntry in msgEntries)
            {
                if (MSG_Entries.FindIndex(m => m.Index == msgEntry.Index) == -1)
                {
                    MSG_Entries.Add(msgEntry);
                }
            }
        }

        public int AddEntry(string name, string message)
        {
            int id = GetLowestUnusedID();
            MSG_Entries.Add(new MSG_Entry() { Name = name, Index = id.ToString(), Msg_Content = new List<Msg_Line>() { new Msg_Line() { Text = message } } });
            return id;
        }


        #region Get
        public string GetCharacterName(string shortName)
        {
            string tempName = String.Format("chara_{0}_000", shortName);

            foreach(var entry in MSG_Entries)
            {
                if (entry.Name == tempName)
                {
                    return WebUtility.HtmlDecode(entry.Msg_Content[0].Text);
                }
            }

            return null;
        }

        public string GetSkillName(int skillID2, CUS.CUS_File.SkillType skillType)
        {
            string name = GetMsgEntryName_SkillName(skillID2, skillType);

            foreach (var entry in MSG_Entries)
            {
                if (entry.Name == name)
                {
                    return WebUtility.HtmlDecode(entry.Msg_Content[0].Text);
                }
            }

            return null;
        }

        public string GetSkillDesc(int skillID2, CUS.CUS_File.SkillType skillType)
        {
            string name = GetMsgEntryName_SkillDesc(skillID2, skillType);

            foreach (var entry in MSG_Entries)
            {
                if (entry.Name == name)
                {
                    return WebUtility.HtmlDecode(entry.Msg_Content[0].Text);
                }
            }

            return null;
        }

        public string GetAwokenStageName(int skillID2, int stage)
        {
            var entry = MSG_Entries.FirstOrDefault(x => x.Name == String.Format("BHD_MET_{0}_{1}", skillID2.ToString("D4"), stage));
            if (entry != null) return entry.DecodedString();
            return null;
        }

        public string[] GetAwokenStageNames(int skillID2, int count)
        {
            List<string> names = new List<string>();

            for(int i = 0; i < count; i++)
            {
                var entry = MSG_Entries.FirstOrDefault(x => x.Name == String.Format("BHD_MET_{0}_{1}", skillID2.ToString("D4"), i));
                if (entry != null)
                    names.Add(entry.DecodedString());
                else
                    break;
            }

            return names.ToArray();
        }

        public string GetArtworkName(int idbId)
        {
            string tempName = String.Format("gallery_illust_{0}", idbId.ToString("D4"));
            string tempName2 = String.Format("gallery_illust_{0}", idbId.ToString("D3"));

            foreach (var entry in MSG_Entries)
            {
                if (entry.Name == tempName || entry.Name == tempName2)
                {
                    return WebUtility.HtmlDecode(entry.Msg_Content[0].Text);
                }
            }

            return null;
        }
        
        public string GetPetName(int idbId)
        {
            string tempName = String.Format("float_pet_{0}", idbId.ToString("D4"));
            string tempName2 = String.Format("float_pet_{0}", idbId.ToString("D3"));

            foreach (var entry in MSG_Entries)
            {
                if (entry.Name == tempName || entry.Name == tempName2)
                {
                    return WebUtility.HtmlDecode(entry.Msg_Content[0].Text);
                }
            }

            return null;
        }


        private static string GetMsgEntryName_SkillName(int skillID2, CUS.CUS_File.SkillType skillType)
        {
            string name;
            switch (skillType)
            {
                case CUS.CUS_File.SkillType.Super:
                    name = String.Format("spe_skill_{0}", skillID2.ToString("D4"));
                    break;
                case CUS.CUS_File.SkillType.Ultimate:
                    name = String.Format("ult_{0}", skillID2.ToString("D4"));
                    break;
                case CUS.CUS_File.SkillType.Evasive:
                    name = String.Format("avoid_skill_{0}", skillID2.ToString("D4"));
                    break;
                case CUS.CUS_File.SkillType.Blast:
                    throw new InvalidDataException("GetSkillName: Blast was passed in as skillType, but these skills dont have names.");
                case CUS.CUS_File.SkillType.Awoken:
                    name = String.Format("met_skill_{0}", skillID2.ToString("D4"));
                    break;
                default:
                    throw new InvalidDataException("GetSkillName: Unknown skillType = " + skillType);
            }

            return name;
        }

        private static string GetMsgEntryName_SkillDesc(int skillID2, CUS.CUS_File.SkillType skillType)
        {
            string name;
            switch (skillType)
            {
                case CUS.CUS_File.SkillType.Super:
                    name = String.Format("spe_skill_eff_{0}", skillID2.ToString("D4"));
                    break;
                case CUS.CUS_File.SkillType.Ultimate:
                    name = String.Format("ult_eff_{0}", skillID2.ToString("D4"));
                    break;
                case CUS.CUS_File.SkillType.Evasive:
                    name = String.Format("avoid_skill_eff_{0}", skillID2.ToString("D4"));
                    break;
                case CUS.CUS_File.SkillType.Blast:
                    throw new InvalidDataException("GetSkillDesc: Blast was passed in as skillType, but these skills dont have descriptions.");
                case CUS.CUS_File.SkillType.Awoken:
                    name = String.Format("met_skill_eff_{0}", skillID2.ToString("D4"));
                    break;
                default:
                    throw new InvalidDataException("GetSkillDesc: Unknown skillType = " + skillType);
            }

            return name;
        }
        #endregion

        #region Set
        public void SetSkillName(string name, int skillID2, CUS.CUS_File.SkillType skillType)
        {
            string msgEntryName = GetMsgEntryName_SkillName(skillID2, skillType);

            var entry = MSG_Entries.FirstOrDefault(x => x.Name == msgEntryName);

            if(entry != null)
            {
                entry.Msg_Content[0].Text = name;
            }
            else
            {
                AddEntry(msgEntryName, name);
            }

        }

        public void SetSkillDesc(string desc, int skillID2, CUS.CUS_File.SkillType skillType)
        {
            string msgEntryName = GetMsgEntryName_SkillDesc(skillID2, skillType);

            var entry = MSG_Entries.FirstOrDefault(x => x.Name == msgEntryName);

            if (entry != null)
            {
                entry.Msg_Content[0].Text = desc;
            }
            else
            {
                AddEntry(msgEntryName, desc);
            }

        }

        public void SetCharacterName(string name, string shortName)
        {
            string tempName = String.Format("chara_{0}_000", shortName);

            var entry = MSG_Entries.FirstOrDefault(x => x.Name == tempName);

            if (entry != null)
            {
                entry.Msg_Content[0].Text = name;
            }
            else
            {
                AddEntry(tempName, name);
            }
        }

        public void SetAwokenStageName(string name, int skillID2, int stage)
        {
            string msgEntryName = String.Format("BHD_MET_{0}_{1}", skillID2.ToString("D4"), stage);

            var entry = MSG_Entries.FirstOrDefault(x => x.Name == msgEntryName);

            if (entry != null)
            {
                entry.Msg_Content[0].Text = name;
            }
            else
            {
                AddEntry(msgEntryName, name);
            }
        }
        #endregion

    }

    [YAXSerializeAs("MsgEntry")]
    public class MSG_Entry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //int32
        [YAXSerializeAs("name")]
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int I_12 { get; set; }

        [BindingSubList]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Line")]
        public List<Msg_Line> Msg_Content { get; set; }

        public string DecodedString(int line = 0)
        {
            if (Msg_Content == null) throw new Exception("Msg_Content was null.");
            if (Msg_Content.Count == 0) throw new Exception("Msg_Content.Count was 0.");

            return WebUtility.HtmlDecode(Msg_Content[line].Text);
        }
    }

    [YAXSerializeAs("Line")]
    public class Msg_Line
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Text")]
        public string Text { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("NumVars")]
        public int I_12 { get; set; }
    }

    [YAXSerializeAs("MsgComponent")]
    public class Msg_Component
    {
        public enum MsgComponentType
        {
            Name,
            Info,
            LimitBurst,
            LimitBurstBattle
        }

        [YAXSerializeAs("Type")]
        [YAXAttributeForClass]
        public MsgComponentType MsgType { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "MsgEntry")]
        public List<MSG_Entry> MsgEntries { get; set; } // Size 1 or 12 (if 1, then duplicate until 12)
    }

}
