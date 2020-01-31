using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.ERS
{


    [YAXSerializeAs("ERS")]
    public class ERS_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Entry")]
        public List <ERS_MainTable> Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static ERS_File Load(byte[] bytes)
        {
            return new Parser(bytes).ersFile;
        }

        public bool EntryExists(int tableId, int entryId)
        {
            foreach(var table in Entries)
            {
                if(ushort.Parse(table.Index) == tableId)
                {
                    foreach(var entry in table.SubEntries)
                    {
                        if(int.Parse(entry.Index) == entryId)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void AddEntry(int tableId, ERS_MainTableEntry entry)
        {
            bool tableExists = false;

            for (int i = 0;i < Entries.Count; i++)
            {
                if(ushort.Parse(Entries[i].Index) == tableId)
                {
                    if (Entries[i].SubEntries == null) Entries[i].SubEntries = new List<ERS_MainTableEntry>();
                    Entries[i].Dummy = null;

                    for (int a = 0; a < Entries[i].SubEntries.Count; a++)
                    {
                        if(Entries[i].SubEntries[a].Index == entry.Index)
                        {
                            Entries[i].SubEntries[a] = entry;
                            return;
                        }
                    }
                    tableExists = true;
                }
            }

            if (tableExists)
            {
                //Entry didnt exist but the table did
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (ushort.Parse(Entries[i].Index) == tableId)
                    {
                        Entries[i].SubEntries.Add(entry);
                        return;
                    }
                }
            }
            else
            {
                //Table did not exist
                Entries.Add(new ERS_MainTable() { Index = tableId.ToString(), SubEntries = new List<ERS_MainTableEntry>() { entry } });
            }
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public List<ERS_MainTableEntry> GetSubentryList(int tableId)
        {
            foreach(var table in Entries)
            {
                if(int.Parse(table.Index) == tableId)
                {
                    return table.SubEntries;
                }
            }

            return null;
        }

        public ERS_MainTable GetMainEntry(string id)
        {
            var entry = Entries.Find(e => e.Index == id);

            if (entry != null) return entry;

            entry = new ERS_MainTable() { Index = id, SubEntries = new List<ERS_MainTableEntry>() };
            Entries.Add(entry);

            return entry;
        }

        public ERS_MainTableEntry GetEntry(int tableId, int entryId)
        {
            var subEntries = GetSubentryList(tableId);
            return (subEntries != null) ? subEntries.FirstOrDefault(x => x.ID == entryId) : null;
        }
    }

    [YAXSerializeAs("Entry")]
    public class ERS_MainTable
    {
        #region WrappedProperties
        [YAXDontSerialize]
        public ushort ID { get { return ushort.Parse(Index); } set { Index = value.ToString(); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public string Index { get; set; } //short
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public List<string> Dummy { get; set; }
        //Count/size will be list.count
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SubEntry")]
        public List<ERS_MainTableEntry> SubEntries {get; set;}

        public int offset;

        public void AddEntry(ERS_MainTableEntry entry)
        {
            if (SubEntries == null) SubEntries = new List<ERS_MainTableEntry>();

            int existingIndex = SubEntries.FindIndex(e => e.Index == entry.Index);

            if(existingIndex != -1)
            {
                SubEntries[existingIndex] = entry;
            }
            else
            {
                SubEntries.Add(entry);
            }
        }
    }

    [YAXSerializeAs("SubEntry")]
    public class ERS_MainTableEntry : IInstallable
    {
        #region WrappedProperties
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }
        [YAXDontSerialize]
        public int ID { get { return int.Parse(Index); } set { Index = value.ToString(); } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } // I_00
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Str_04 { get; set; }
        [YAXAttributeFor("File")]
        [YAXSerializeAs("string")]
        public string FILE_PATH { get; set; }

        public int offset;
        public int offsetToString;
    }
}
