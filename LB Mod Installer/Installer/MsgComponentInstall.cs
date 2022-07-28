using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xv2CoreLib.CUS;
using Xv2CoreLib.IDB;
using Xv2CoreLib.MSG;

namespace LB_Mod_Installer.Installer
{
    public class MsgComponentInstall
    {
        public enum ComponentMode
        {
            IDB_LB_HUD,
            IDB,
            CMS
        }

        public enum SkillMode
        {
            Name,
            Info,
            BtlHud
        }

        public Install install = null;

        public MsgComponentInstall(Install _install)
        {
            install = _install;
        }

        /// <summary>
        /// Writes msg entries contained in Msg_Component.
        /// </summary>
        /// <param name="msgComponent"></param>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <param name="args"></param>
        /// <returns>MSG ID for Mode==CMS, MSG Index for Mode==IDB</returns>
        public int WriteMsgEntries(Msg_Component msgComponent, string path, ComponentMode mode, string args = null, IDB_Entry idbEntry = null)
        {
            MSG_File[] msgFiles = LoadMsgFiles(path);

            //Check if files are in sync and handle that.
            ValidateMsgFiles(msgFiles, path);

            //First, ensure that a msg entry exists for each language.
            if (msgComponent.MsgEntries.Count < GeneralInfo.LanguageSuffix.Length)
            {
                while (msgComponent.MsgEntries.Count < GeneralInfo.LanguageSuffix.Length)
                {
                    msgComponent.MsgEntries.Add(msgComponent.MsgEntries[0]);
                }
            }

            //Now determine the ID/Index
            //We need to assign a new ID and add it to the tracker
            //If mode == IDB, then id is the MSG Index
            int msgId = msgFiles[0].NextID();
            int id = (mode == ComponentMode.IDB) ? msgFiles[0].MSG_Entries.Count : msgId;
            GeneralInfo.Tracker.AddMsgID(path, Sections.MSG_Entries, msgId);
            
            //Get the name
            string name = null;

            if (mode == ComponentMode.CMS || mode == ComponentMode.IDB_LB_HUD)
            {
                name = GetMsgName(path, args, 0, msgFiles[0]);
            }
            else if (mode == ComponentMode.IDB)
            {
                name = GetMsgName(path, null, id, msgFiles[0], idbEntry);
            }

            //Process MsgComponent and add name and id
            for (int i = 0; i < msgComponent.MsgEntries.Count; i++)
            {
                msgComponent.MsgEntries[i].Name = name;
                msgComponent.MsgEntries[i].Index = ((mode == ComponentMode.IDB) ? msgFiles[i].NextID() : id).ToString();
            }

            //Remove duplicate entries (in non-index mode only! we dont want to mess up the index!)
            if(mode == ComponentMode.IDB_LB_HUD || mode == ComponentMode.CMS)
            {
                foreach(var msgFile in msgFiles)
                {
                    msgFile.RemoveDuplicateEntries(name, id);
                }
            }

            //Add entry
            if(mode == ComponentMode.IDB)
            {
                for(int i = 0; i < msgFiles.Length; i++)
                {
                    if (msgFiles[i].MSG_Entries.Count == id)
                    {
                        msgFiles[i].MSG_Entries.Add(msgComponent.MsgEntries[i]);
                    }
                    else
                    {
                        msgFiles[i].MSG_Entries[id] = msgComponent.MsgEntries[i];
                    }
                }
            }
            else if (mode == ComponentMode.CMS || mode == ComponentMode.IDB_LB_HUD)
            {
                for(int i = 0; i < msgFiles.Length; i++)
                {
                    msgFiles[i].AddEntryAtId(msgComponent.MsgEntries[i], id);
                }
            }

            return id;
        }

        public int WriteSkillMsgEntries(string[] msg, int skillID2, CUS_File.SkillType skillType, SkillMode msgType)
        {
            string path = null;

            switch (msgType) 
            {
                case SkillMode.Name:
                    path = IDB_File.SkillNameMsgFile(skillType);
                    break;
                case SkillMode.Info:
                    path = IDB_File.SkillInfoMsgFile(skillType);
                    break;
                case SkillMode.BtlHud:
                    path = "quest_btlhud_";
                    break;
            }

            MSG_File[] msgFiles = LoadMsgFiles(path);
            ValidateMsgFiles(msgFiles, path);

            //Assign ID
            int id = msgFiles[0].NextID();
            GeneralInfo.Tracker.AddID(path, Sections.MSG_Entries, id.ToString());

            for (int i = 0; i < msgFiles.Length; i++)
            {
                string msgName = GetMsgName(path, "0", skillID2, msgFiles[i], isAwoken: true);
                msgFiles[i].AddEntry(msgName, msg[i], id);
            }

            return id;
        }

        public MSG_File[] LoadMsgFiles(string path)
        {
            MSG_File[] files = new MSG_File[(int)Xv2CoreLib.Xenoverse2.Language.NumLanguages];

            for (int i = 0; i < Xv2CoreLib.Xenoverse2.LanguageSuffix.Length; i++)
            {
                string msgPath = string.Format("{0}{1}", path, Xv2CoreLib.Xenoverse2.LanguageSuffix[i]);

                files[i] = (MSG_File)install.GetParsedFile<MSG_File>(msgPath);
            }

            return files;
        }
        
        private string GetMsgName(string path, string args, int numArgs, MSG_File msgFile, IDB_Entry idbEntry = null, bool isAwoken = false)
        {

            switch (Path.GetFileName(path))
            {
                case "proper_noun_skill_met_name_":
                    return $"met_skill_{numArgs.ToString("D4")}";
                case "proper_noun_skill_met_info_":
                    return $"met_skill_eff_{numArgs.ToString("D4")}";
                case "proper_noun_talisman_info_":
                    return String.Format("talisman_eff_{0}", numArgs.ToString("D3"));
                case "proper_noun_talisman_name_":
                    return String.Format("talisman_{0}", numArgs.ToString("D3"));
                case "proper_noun_costume_name_":
                    {
                        string name = MSG_Entry.ChooseCostumeEntryName(idbEntry);
                        int id = 300;

                        while (msgFile.MSG_Entries.Any(x => x.Name == String.Format("{0}{1}", name, id.ToString("D3"))))
                        {
                            id++;

                            if (id >= ushort.MaxValue)
                                throw new Exception("MsgComponentInstall.GetMsgName: Overflow, an MSG ID could not be assigned to the costume.");
                        }

                        return String.Format("{0}{1}", name, id.ToString("D3"));
                    }
                case "proper_noun_costume_info_":
                    {
                        string name = MSG_Entry.ChooseCostumeEntryName(idbEntry) + "eff_";
                        int id = 300;

                        while (msgFile.MSG_Entries.Any(x => x.Name == String.Format("{0}{1}", name, id.ToString("D3"))))
                        {
                            id++;

                            if (id >= ushort.MaxValue)
                                throw new Exception("MsgComponentInstall.GetMsgName: Overflow, an MSG ID could not be assigned to the costume.");
                        }

                        return String.Format("{0}{1}", name, id.ToString("D3"));
                    }
                case "proper_noun_accessory_name_":
                    return String.Format("accessory_{0}", numArgs.ToString("D3"));
                case "proper_noun_accessory_info_":
                    return String.Format("accessory_eff_{0}", numArgs.ToString("D3"));
                case "proper_noun_material_name_":
                    return String.Format("material_item_{0}", numArgs.ToString("D3"));
                case "proper_noun_material_info_":
                    return String.Format("material_item_eff_{0}", numArgs.ToString("D3"));
                case "proper_noun_battle_name_":
                    return String.Format("battle_item_{0}", numArgs.ToString("D3"));
                case "proper_noun_battle_info_":
                    return String.Format("battle_item_eff_{0}", numArgs.ToString("D3"));
                case "proper_noun_extra_name_":
                    return String.Format("extra_item_{0}", numArgs.ToString("D3"));
                case "proper_noun_extra_info_":
                    return String.Format("extra_item_eff_{0}", numArgs.ToString("D3"));
                case "proper_noun_talisman_info_olt_":
                    return String.Format("talisman_olt_{0}", numArgs.ToString("D3"));
                case "proper_noun_gallery_illust_name_":
                    return String.Format("gallery_illust_{0}", numArgs.ToString("D4"));
                case "proper_noun_gallery_illust_info_":
                    return String.Format("gallery_illust_eff_{0}", numArgs.ToString("D3"));
                case "proper_noun_float_pet_name_":
                    return String.Format("float_pet_{0}", numArgs.ToString("D4"));
                case "proper_noun_float_pet_info_":
                    return String.Format("float_pet_eff_{0}", numArgs.ToString("D3"));
                case "quest_btlhud_": 
                    return isAwoken ? String.Format("BHD_MET_{0}_{1}", numArgs.ToString("D4"), args) : String.Format("BHD_OLT_000_{0}", int.Parse(args).ToString("D2"));
                case "proper_noun_character_name_":
                    return String.Format("chara_{0}_000", args);
            }

            throw new InvalidOperationException("Could not determine the name for the msg entry.");
        }

        private void ValidateMsgFiles(MSG_File[] MsgFiles, string path)
        {
            foreach (var msgFile in MsgFiles)
            {
                if (msgFile.MSG_Entries.Count != MsgFiles[0].MSG_Entries.Count)
                {
                    //Entries out of sync betwen languages. 
                    MSG_File.SynchronizeMsgFiles(MsgFiles);
                }
            }
        }

    }
}
