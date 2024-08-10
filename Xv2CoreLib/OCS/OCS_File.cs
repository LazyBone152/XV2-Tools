using System;
using System.Collections.Generic;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.OCS
{
    [YAXSerializeAs("OCS")]
    [Serializable]
    public class OCS_File
    {
        [YAXAttributeForClass]
        public ushort Version { get; set; } // 0x6: 16 = pre 1.13, 20 = 1.13 or later, 28 = 1.22 or later

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Partner")]
        public List<OCS_Partner> Partners { get; set; }

        public int CalculateDataOffset()
        {
            int offset = (int)(Partners.Count * 16);

            foreach(var tableEntry in Partners)
            {
                foreach(var secondEntry in tableEntry.SkillTypes)
                {
                    offset += 16;
                }
            }

            return offset;
        }

        #region LoadSave
        public static OCS_File Load(byte[] bytes)
        {
            return new Parser(bytes).ocsFile;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).GetByteArray();
        }
        #endregion

        #region Install
        public List<string> InstallEntries(List<OCS_Partner> entries)
        {
            List<string> ids = new List<string>();

            if(entries != null)
            {
                foreach(var entry in entries)
                {
                    OCS_Partner destEntry = Partners.FirstOrDefault(x => x.Index == entry.Index);

                    //If no entry exists for this Partner ID, then create it.
                    if(destEntry == null)
                    {
                        destEntry = new OCS_Partner(entry.Index);
                        Partners.Add(destEntry);
                    }

                    foreach (var skillType in entry.SkillTypes)
                    {
                        OCS_SkillTypeGroup destSkillType = destEntry.SkillTypes.FirstOrDefault(x => x.Skill_Type == skillType.Skill_Type);

                        //Create the sub entry if required.
                        if(destSkillType == null)
                        {
                            destSkillType = new OCS_SkillTypeGroup(skillType.Skill_Type);
                            destEntry.SkillTypes.Add(destSkillType);
                        }

                        foreach (var skill in skillType.Skills)
                        {
                            string installID = GetInstallId(entry, skillType, skill);
                            ids.Add(installID);

                            OCS_Skill destSkill = destSkillType.Skills.FirstOrDefault(x => x.SkillID2 == skill.SkillID2);

                            skill.EntryID = GetSkillEntryId(destSkillType.Skills);

                            if(destSkill == null)
                            {
                                destSkillType.Skills.Add(skill);
                            }
                            else
                            {
                                destSkillType.Skills[destSkillType.Skills.IndexOf(destSkill)] = skill;
                            }
                        }

                    }

                    //Sort skill type list
                    destEntry.SkillTypes.Sort((x, y) => (int)x.Skill_Type - (int)y.Skill_Type);
                }

                //Sort partner list
                Partners.Sort((x, y) => x.Index - y.Index);

            }

            return ids;
        }

        public void UninstallEntries(List<string> installIds, OCS_File originalFile)
        {
            //Uninstall skill entries
            foreach(var partner in Partners)
            {
                foreach(var skillType in partner.SkillTypes)
                {
                    for (int i = skillType.Skills.Count - 1; i >= 0; i--)
                    {
                        string installId = GetInstallId(partner, skillType, skillType.Skills[i]);
                        OCS_Skill originalSkill = (originalFile != null) ? originalFile.GetSkill(partner.Index, skillType.Skill_Type, skillType.Skills[i].SkillID2) : null;

                        if (installIds.Contains(installId))
                        {
                            if(originalSkill != null)
                            {
                                skillType.Skills[i] = originalSkill.Copy();
                                skillType.Skills[i].EntryID = GetSkillEntryId(skillType.Skills);
                            }
                            else
                            {
                                skillType.Skills.RemoveAt(i);
                            }
                        }
                    }
                }

            }

            //Remove empty entries
            foreach(var partner in Partners)
            {
                partner.SkillTypes.RemoveAll(x => x.Skills.Count == 0);
            }

            Partners.RemoveAll(x => x.SkillTypes.Count == 0);
        }

        private string GetInstallId(OCS_Partner main, OCS_SkillTypeGroup skillType, OCS_Skill skill)
        {
            return $"{main.Index}_{skillType.Skill_Type}_{skill.SkillID2}";
        }

        private int GetSkillEntryId(List<OCS_Skill> skills)
        {
            return skills.Count == 0 ? 0 : skills.Max(x => x.EntryID) + 1;
        }
        
        private OCS_Skill GetSkill(int partnerID, OCS_SkillTypeGroup.SkillType skillType, int skillID2)
        {
            var partner = Partners.FirstOrDefault(x => x.Index == partnerID);
            if (partner == null) return null;

            var skillTypeGroup = partner.SkillTypes.FirstOrDefault(x => x.Skill_Type == skillType);
            if (skillTypeGroup == null) return null;

            return skillTypeGroup.Skills.FirstOrDefault(x => x.SkillID2 == skillID2);
        }
        #endregion
    }

    [YAXSerializeAs("Partner")]
    [Serializable]
    public class OCS_Partner
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Partner_ID")]
        public int Index { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SkillList")]
        public List<OCS_SkillTypeGroup> SkillTypes { get; set; } = new List<OCS_SkillTypeGroup>();

        public OCS_Partner() 
        {
        }

        public OCS_Partner(int id)
        {
            Index = id;
        }
        

    }

    [YAXSerializeAs("SkillList")]
    [Serializable]
    public class OCS_SkillTypeGroup
    {
        public enum SkillType
        {
            Super = 0,
            Ultimate = 1,
            Evasive = 2,
            Awoken = 3
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public SkillType Skill_Type { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Skill")]
        public List<OCS_Skill> Skills { get; set; } = new List<OCS_Skill>();

        public OCS_SkillTypeGroup()
        {
        }

        public OCS_SkillTypeGroup(SkillType skillType)
        {
            Skill_Type = skillType;
        }

    }

    [YAXSerializeAs("Skill")]
    [Serializable]
    public class OCS_Skill
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("EntryID")]
        public int EntryID { get; set; } //Some kind of entry or sorting ID?
        [YAXAttributeForClass]
        [YAXSerializeAs("TP_Cost_Toggle")]
        public int TP_Cost_Toggle { get; set; } // uint32
        [YAXAttributeForClass]
        [YAXSerializeAs("TP_Cost")]
        public int TP_Cost { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("STP_Cost")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int STP_Cost { get; set; } // Added in 1.22
        [YAXAttributeForClass]
        [YAXSerializeAs("ID2")]
        public int SkillID2 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("DLC_Flag")]
        public int DLC_Flag { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("NEW_I_32")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int NEW_I_32 { get; set; } // Added in 1.22
    }
}
