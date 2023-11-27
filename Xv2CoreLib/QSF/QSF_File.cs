using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.QSF
{
    [YAXSerializeAs("QSF")]
    [YAXComment("For install purposes, SortedID is useless and can be left out.")]
    public class QSF_File
    {
        [YAXSerializeAs("I_12")]
        [YAXAttributeForClass]
        public int I_12 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "QuestType")]
        public List<QSF_QuestType> QuestTypes { get; set; } = new List<QSF_QuestType>();

        public static QSF_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetQsfFile();
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public QSF_QuestType GetQuestType(string type)
        {
            foreach(var entry in QuestTypes)
            {
                if (entry.Type == type) return entry;
            }
            return null;
        }

        #region Install
        public List<string> InstallEntries(List<QSF_QuestType> entries)
        {
            //InstallID = {Type}_{Index}_{QuestID}
            List<string> installIDs = new List<string>();

            foreach(var installQuestType in entries)
            {
                QSF_QuestType questType = QuestTypes.FirstOrDefault(x => x.Type == installQuestType.Type);

                if (questType == null)
                    throw new ArgumentException($"QSF_File.InstallEntries: Invalid QuestType {installQuestType.Type}.");

                foreach(var installQuestGroup in installQuestType.QuestGroups)
                {
                    QSF_QuestGroup questGroup = questType.QuestGroups.FirstOrDefault(x => x.Index == installQuestGroup.Index);

                    if(questGroup == null)
                        throw new ArgumentException($"QSF_File.InstallEntries: Invalid QuestGroup Index {installQuestGroup.Index}. No QuestGroup with this index was found.");

                    foreach(var installQuest in installQuestGroup.QuestEntries)
                    {
                        QuestEntry existingEntry = questGroup.QuestEntries.FirstOrDefault(x => x.QuestID == installQuest.QuestID);

                        if(existingEntry == null)
                            questGroup.QuestEntries.Add(installQuest);

                        installIDs.Add($"{installQuestType.Type}_{installQuestGroup.Index}_{installQuest.QuestID}");
                    }
                }
            }

            return installIDs;
        }

        public void UninstallEntries(List<string> installIDs, QSF_File cpkQsfFile)
        {
            foreach(var questType in QuestTypes)
            {
                foreach(var questGroup in questType.QuestGroups)
                {
                    if(questGroup.QuestEntries != null)
                    {
                        for (int i = questGroup.QuestEntries.Count - 1; i >= 0; i--)
                        {
                            string id = $"{questType.Type}_{questGroup.Index}_{questGroup.QuestEntries[i].QuestID}";

                            if (installIDs.Contains(id))
                            {
                                QuestEntry originalEntry = cpkQsfFile != null ? cpkQsfFile.GetEntry(id) : null;

                                if(originalEntry != null)
                                {
                                    questGroup.QuestEntries[i] = originalEntry.Copy();
                                }
                                else
                                {
                                    questGroup.QuestEntries.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
        }

        public QuestEntry GetEntry(string installID)
        {
            foreach (var questType in QuestTypes)
            {
                foreach (var questGroup in questType.QuestGroups)
                {
                    if (questGroup.QuestEntries != null)
                    {
                        foreach(var quest in questGroup.QuestEntries)
                        {
                            if ($"{questType.Type}_{questGroup.Index}_{quest.QuestID}" == installID) return quest;
                        }
                    }
                }
            }

            return null;
        }
        #endregion
    }

    [YAXSerializeAs("QuestType")]
    [Serializable]
    public class QSF_QuestType 
    {
        [YAXSerializeAs("Type")]
        [YAXAttributeForClass]
        public string Type { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_12")]
        public int I_12 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "QuestGroup")]
        public List<QSF_QuestGroup> QuestGroups { get; set; } = new List<QSF_QuestGroup>();

        

    }

    [YAXSerializeAs("QuestGroup")]
    [Serializable]
    public class QSF_QuestGroup 
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (int)0)]
        public int Index { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "QuestEntry")]
        public List<QuestEntry> QuestEntries { get; set; } = new List<QuestEntry>();
    }

    [YAXSerializeAs("QuestEntry")]
    [Serializable]
    public class QuestEntry 
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("QuestID")]
        public string QuestID { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("SortedID")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (int)0)]
        public int Alias_ID { get; set; }

    }

}
