using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;


namespace Xv2CoreLib.SEV
{
    [YAXSerializeAs("SEV")]
    public class SEV_File
    {
        private const uint SEV_SIGNATURE = 1447383843;
        private const int SEV_ENTRY_SIZE = 20;
        private const int SEV_CHARA_EVENT_SIZE = 16;
        private const int SEV_EVENT_SIZE = 12;
        private const int SEV_EVENT_ENTRY_SIZE = 36;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SevEntry")]
        public List<SEV_Entry> Entries { get; set; } = new List<SEV_Entry>();

        #region LoadSave
        public void SortEntries()
        {
            if (Entries != null)
            {
                Entries.Sort((x, y) => x.SortID - y.SortID);

                foreach(var entry in Entries.Where(x => x.SubEntries != null))
                    entry.SubEntries.Sort((x, y) => x.SortID - y.SortID);
            }
        }

        public static SEV_File Parse(string path, bool writeXml)
        {
            SEV_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(SEV_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static SEV_File Parse(byte[] bytes)
        {
            if (BitConverter.ToUInt32(bytes, 0) != SEV_SIGNATURE) throw new InvalidDataException("SEV signature not found!");

            SEV_File sev = new SEV_File();
            int count = BitConverter.ToInt32(bytes, 8);

            //Find offsets to each section
            int sevEntryStart = 16;
            int charEventStart = sevEntryStart + (SEV_ENTRY_SIZE * count);

            int totalCharEvents = 0;
            int totalEvents = 0;

            for (int i = 0; i < count; i++)
                totalCharEvents += BitConverter.ToInt32(bytes, sevEntryStart + (SEV_ENTRY_SIZE * i) + 12);
            
            for (int i = 0; i < totalCharEvents; i++)
                totalEvents += BitConverter.ToInt32(bytes, charEventStart + (SEV_CHARA_EVENT_SIZE * i) + 8);

            int eventStart = charEventStart + (SEV_CHARA_EVENT_SIZE * totalCharEvents);
            int eventEntryStart = eventStart + (SEV_EVENT_SIZE * totalEvents);


            //Parse the data
            for (int i = 0; i < count; i++)
            {
                SEV_Entry sevEntry = new SEV_Entry();
                sevEntry.CharaID = BitConverter.ToInt32(bytes, sevEntryStart + (SEV_ENTRY_SIZE * i) + 0);
                sevEntry.CostumeID = BitConverter.ToInt32(bytes, sevEntryStart + (SEV_ENTRY_SIZE * i) + 4);
                sevEntry.I_08 = BitConverter.ToUInt32(bytes, sevEntryStart + (SEV_ENTRY_SIZE * i) + 8);
                int charEventCount = BitConverter.ToInt32(bytes, sevEntryStart + (SEV_ENTRY_SIZE * i) + 12);
                
                for(int a = 0; a < charEventCount; a++)
                {
                    SEV_CharEvent charEvent = new SEV_CharEvent();
                    charEvent.CharaID = BitConverter.ToInt32(bytes, charEventStart + 0);
                    charEvent.CostumeID = BitConverter.ToInt32(bytes, charEventStart + 4);
                    int eventCount = BitConverter.ToInt32(bytes, charEventStart + 8);

                    for(int b = 0; b < eventCount; b++)
                    {
                        SEV_Event _event = new SEV_Event();
                        _event.Type = BitConverter.ToInt32(bytes, eventStart + 0);
                        int eventEntryCount = BitConverter.ToInt32(bytes, eventStart + 4);

                        for(int t = 0; t < eventEntryCount; t++)
                        {
                            SEV_EventEntry eventEntry = new SEV_EventEntry();
                            eventEntry.I_00 = BitConverter.ToInt32(bytes, eventEntryStart + 0);
                            eventEntry.CueID = BitConverter.ToInt32(bytes, eventEntryStart + 4);
                            eventEntry.FileID = BitConverter.ToInt32(bytes, eventEntryStart + 8);
                            eventEntry.I_12 = BitConverter.ToInt32(bytes, eventEntryStart + 12);
                            eventEntry.ResponseCueID = BitConverter.ToInt32(bytes, eventEntryStart + 16);
                            eventEntry.ResponseFileID = BitConverter.ToInt32(bytes, eventEntryStart + 20);
                            eventEntry.I_24 = BitConverter.ToInt32(bytes, eventEntryStart + 24);
                            eventEntry.I_28 = BitConverter.ToInt32(bytes, eventEntryStart + 28);
                            eventEntry.I_32 = BitConverter.ToInt32(bytes, eventEntryStart + 32);

                            eventEntryStart += SEV_EVENT_ENTRY_SIZE;
                            _event.EventEntries.Add(eventEntry);
                        }

                        eventStart += SEV_EVENT_SIZE;
                        charEvent.Events.Add(_event);
                    }

                    charEventStart += SEV_CHARA_EVENT_SIZE;
                    sevEntry.SubEntries.Add(charEvent);
                }

                sev.Entries.Add(sevEntry);
            }

            return sev;
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .sev file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(SEV_File), YAXSerializationOptions.DontSerializeNullObjects);
            var oblFile = (SEV_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, oblFile.Write());
        }

        /// <summary>
        /// Save the SEV_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }


        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();
            List<byte> charEventBytes = new List<byte>();
            List<byte> eventBytes = new List<byte>();
            List<byte> eventEntryBytes = new List<byte>();

            //Header
            bytes.AddRange(BitConverter.GetBytes((uint)SEV_SIGNATURE)); //signature
            bytes.AddRange(BitConverter.GetBytes((ushort)65534)); //endianness
            bytes.AddRange(BitConverter.GetBytes((ushort)16)); //header size
            bytes.AddRange(BitConverter.GetBytes((uint)Entries.Count));
            bytes.AddRange(BitConverter.GetBytes((uint)0));

            //Entries
            foreach(var sevEntry in Entries)
            {
                if (sevEntry.SubEntries == null) sevEntry.SubEntries = new List<SEV_CharEvent>();

                bytes.AddRange(BitConverter.GetBytes(sevEntry.CharaID));
                bytes.AddRange(BitConverter.GetBytes(sevEntry.CostumeID));
                bytes.AddRange(BitConverter.GetBytes(sevEntry.I_08));
                bytes.AddRange(BitConverter.GetBytes(sevEntry.SubEntries.Count));
                bytes.AddRange(BitConverter.GetBytes((uint)0));

                foreach(var charEvent in sevEntry.SubEntries)
                {
                    if (charEvent.Events == null) charEvent.Events = new List<SEV_Event>();

                    charEventBytes.AddRange(BitConverter.GetBytes(charEvent.CharaID));
                    charEventBytes.AddRange(BitConverter.GetBytes(charEvent.CostumeID));
                    charEventBytes.AddRange(BitConverter.GetBytes(charEvent.Events.Count));
                    charEventBytes.AddRange(BitConverter.GetBytes((uint)0));

                    foreach(var _event in charEvent.Events)
                    {
                        if (_event.EventEntries == null) _event.EventEntries = new List<SEV_EventEntry>();

                        eventBytes.AddRange(BitConverter.GetBytes(_event.Type));
                        eventBytes.AddRange(BitConverter.GetBytes(_event.EventEntries.Count));
                        eventBytes.AddRange(BitConverter.GetBytes((uint)0));

                        foreach(var eventEntry in _event.EventEntries)
                        {
                            eventEntryBytes.AddRange(BitConverter.GetBytes(eventEntry.I_00));
                            eventEntryBytes.AddRange(BitConverter.GetBytes(eventEntry.CueID));
                            eventEntryBytes.AddRange(BitConverter.GetBytes(eventEntry.FileID));
                            eventEntryBytes.AddRange(BitConverter.GetBytes(eventEntry.I_12));
                            eventEntryBytes.AddRange(BitConverter.GetBytes(eventEntry.ResponseCueID));
                            eventEntryBytes.AddRange(BitConverter.GetBytes(eventEntry.ResponseFileID));
                            eventEntryBytes.AddRange(BitConverter.GetBytes(eventEntry.I_24));
                            eventEntryBytes.AddRange(BitConverter.GetBytes(eventEntry.I_28));
                            eventEntryBytes.AddRange(BitConverter.GetBytes(eventEntry.I_32));
                        }
                    }
                }
            }

            bytes.AddRange(charEventBytes);
            bytes.AddRange(eventBytes);
            bytes.AddRange(eventEntryBytes);

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }
        #endregion
    }

    [YAXSerializeAs("SevEntry")]
    public class SEV_Entry : IInstallable_2<SEV_CharEvent>, IInstallable
    {
        #region NonSerialized
        [YAXDontSerialize]
        public int CharaID { set { I_00 = value.ToString(); } get { return int.Parse(I_00); } }
        [YAXDontSerialize]
        public int CostumeID { set { I_04 = value.ToString(); } get { return int.Parse(I_04); } }

        //interface
        [YAXDontSerialize]
        public int SortID { get { return CharaID; } }
        [YAXDontSerialize]
        public string Index
        {
            get
            {
                return $"{I_00}_{I_04}";
            }
            set
            {
                string[] split = value.Split('_');

                if(split.Length == 2)
                {
                    I_00 = split[0];
                    I_04 = split[1];
                }
            }
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("CharaID")]
        public string I_00 { get; set; } //0 (int32)
        [YAXAttributeForClass]
        [YAXSerializeAs("CostumeID")]
        public string I_04 { get; set; } //4 (int32)
        [YAXAttributeForClass]
        [YAXSerializeAs("I_08")]
        [YAXHexValue]
        public uint I_08 { get; set; } //8 (int32)

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "CharEvent")]
        [BindingSubList]
        public List<SEV_CharEvent> SubEntries { get; set; } = new List<SEV_CharEvent>();
        
    }

    [YAXSerializeAs("CharEvent")]
    public class SEV_CharEvent : IInstallable
    {

        #region NonSerialized
        [YAXDontSerialize]
        public int CharaID { set { I_00 = value.ToString(); } get { return int.Parse(I_00); } }
        [YAXDontSerialize]
        public int CostumeID { set { I_04 = value.ToString(); } get { return int.Parse(I_04); } }

        //interface
        [YAXDontSerialize]
        public int SortID { get { return CharaID; } }
        [YAXDontSerialize]
        public string Index
        {
            get
            {
                return $"{I_00}_{I_04}";
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 2)
                {
                    I_00 = split[0];
                    I_04 = split[1];
                }
            }
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("CharaID")]
        public string I_00 { get; set; } //0 (int32)
        [YAXAttributeForClass]
        [YAXSerializeAs("CostumeID")]
        public string I_04 { get; set; } //4 (int32)

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Event")]
        public List<SEV_Event> Events { get; set; } = new List<SEV_Event>();
    }

    [YAXSerializeAs("Event")]
    public class SEV_Event
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public int Type { get; set; } //0

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EventEntry")]
        public List<SEV_EventEntry> EventEntries { get; set; } = new List<SEV_EventEntry>();
    }

    [YAXSerializeAs("EventEntry")]
    public class SEV_EventEntry
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; } //0
        [YAXAttributeFor("CueID")]
        [YAXSerializeAs("value")]
        public int CueID { get; set; } //4
        [YAXAttributeFor("FileID")]
        [YAXSerializeAs("value")]
        public int FileID { get; set; } //8
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; } //12
        [YAXAttributeFor("ResponseCueID")]
        [YAXSerializeAs("value")]
        public int ResponseCueID { get; set; } //16
        [YAXAttributeFor("ResponseFileID")]
        [YAXSerializeAs("value")]
        public int ResponseFileID { get; set; } //20
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; } //24
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; } //28
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; } //32

    }
}
