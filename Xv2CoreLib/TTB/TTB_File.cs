using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.TTB
{
    public enum EventType
    {
        versus = 0,
        ally_talk = 1,
        death = 2
    }

    [YAXSerializeAs("TTB")]
    public class TTB_File
    {
        private const uint TTB_SIGNATURE = 0x42545423;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Entry")]
        public List<TTB_Entry> Entries { get; set; } = new List<TTB_Entry>();

        #region LoadSave
        public static TTB_File Parse(string path, bool writeXml)
        {
            TTB_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(TTB_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static TTB_File Parse(byte[] bytes)
        {
            if (BitConverter.ToUInt32(bytes, 0) != TTB_SIGNATURE) throw new InvalidDataException("TTB signature not found!");

            TTB_File ttb = new TTB_File();
            
            //Headers
            int stringSize = BitConverter.ToInt32(bytes, 12);
            int numEntries = BitConverter.ToInt32(bytes, stringSize + 16);
            int dataStart = BitConverter.ToInt32(bytes, stringSize + 20) + stringSize + 16; //Relative

            //Data
            for(int i = 0; i < numEntries; i++)
            {
                TTB_Entry entry = new TTB_Entry();
                entry.CmsID = BitConverter.ToInt32(bytes, dataStart + (16 * i) + 12);

                int numEvents = BitConverter.ToInt32(bytes, dataStart + (16 * i));
                int eventDataStart = BitConverter.ToInt32(bytes, dataStart + (16 * i) + 4) + stringSize + 16; //Relative to header 2 (after strings)
                int startIndex = BitConverter.ToInt32(bytes, dataStart + (16 * i) + 8);
                eventDataStart += 116 * startIndex;

                for(int a = 0; a < numEvents; a++)
                {
                    TTB_Event _event = new TTB_Event();

                    _event.Index = BitConverter.ToInt32(bytes, eventDataStart + 0).ToString();
                    _event.Cms_Id1 = BitConverter.ToInt32(bytes, eventDataStart + 4);
                    _event.Costume = BitConverter.ToInt32(bytes, eventDataStart + 8);
                    _event.Transformation = BitConverter.ToInt32(bytes, eventDataStart + 12);
                    _event.Cms_Id2 = BitConverter.ToInt32(bytes, eventDataStart + 16);
                    _event.Costume2 = BitConverter.ToInt32(bytes, eventDataStart + 20);
                    _event.Transformation2 = BitConverter.ToInt32(bytes, eventDataStart + 24);
                    _event.Cms_Id3 = BitConverter.ToInt32(bytes, eventDataStart + 28);
                    _event.Costume3 = BitConverter.ToInt32(bytes, eventDataStart + 32);
                    _event.Transformation3 = BitConverter.ToInt32(bytes, eventDataStart + 36);
                    _event.Cms_Id4 = BitConverter.ToInt32(bytes, eventDataStart + 40);
                    _event.Costume4 = BitConverter.ToInt32(bytes, eventDataStart + 44);
                    _event.Transformation4 = BitConverter.ToInt32(bytes, eventDataStart + 48);
                    _event.Cms_Id5 = BitConverter.ToInt32(bytes, eventDataStart + 52);
                    _event.Costume5 = BitConverter.ToInt32(bytes, eventDataStart + 56);
                    _event.Transformation5 = BitConverter.ToInt32(bytes, eventDataStart + 60);
                    _event.Type = (EventType)BitConverter.ToInt32(bytes, eventDataStart + 64);
                    _event.I_68 = BitConverter.ToUInt32(bytes, eventDataStart + 68);
                    _event.I_72 = BitConverter.ToUInt32(bytes, eventDataStart + 72);

                    _event.Order1 = BitConverter.ToInt32(bytes, eventDataStart + 76);
                    _event.Order2 = BitConverter.ToInt32(bytes, eventDataStart + 84);
                    _event.Order3 = BitConverter.ToInt32(bytes, eventDataStart + 92);
                    _event.Order4 = BitConverter.ToInt32(bytes, eventDataStart + 100);
                    _event.Order5 = BitConverter.ToInt32(bytes, eventDataStart + 108);

                    //Names
                    int name1 = BitConverter.ToInt32(bytes, eventDataStart + 80);
                    int name2 = BitConverter.ToInt32(bytes, eventDataStart + 88);
                    int name3 = BitConverter.ToInt32(bytes, eventDataStart + 96);
                    int name4 = BitConverter.ToInt32(bytes, eventDataStart + 104);
                    int name5 = BitConverter.ToInt32(bytes, eventDataStart + 112);
                    
                    if (name1 != -1 && name1 < stringSize)
                        _event.Voice1 = StringEx.GetString(bytes, name1 + 16, false);

                    if (name2 != -1 && name2 < stringSize)
                        _event.Voice2 = StringEx.GetString(bytes, name2 + 16, false);

                    if (name3 != -1 && name3 < stringSize)
                        _event.Voice3 = StringEx.GetString(bytes, name3 + 16, false);

                    if (name4 != -1 && name4 < stringSize)
                        _event.Voice4 = StringEx.GetString(bytes, name4 + 16, false);

                    if (name5 != -1 && name5 < stringSize)
                        _event.Voice5 = StringEx.GetString(bytes, name5 + 16, false);

                    entry.SubEntries.Add(_event);
                    eventDataStart += 116;
                }

                ttb.Entries.Add(entry);
            }

            return ttb;
        }
        
        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .ttb file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(TTB_File), YAXSerializationOptions.DontSerializeNullObjects);
            var oblFile = (TTB_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, oblFile.Write());
        }

        /// <summary>
        /// Save the TTB_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            if (Entries == null) Entries = new List<TTB_Entry>();

            List<byte> bytes = new List<byte>();
            List<byte> ttbEntryBytes = new List<byte>();
            List<string> strings = new List<string>();

            //Header
            bytes.AddRange(BitConverter.GetBytes((uint)TTB_SIGNATURE)); //signature
            bytes.AddRange(BitConverter.GetBytes((ushort)65534)); //endianness
            bytes.AddRange(BitConverter.GetBytes((ushort)64)); //Unknown
            bytes.AddRange(BitConverter.GetBytes((uint)1)); //Unknown
            bytes.AddRange(BitConverter.GetBytes((uint)0)); //string size / offset to header 2

            //Header 2 (16 bytes)
            ttbEntryBytes.AddRange(BitConverter.GetBytes(Entries.Count));
            ttbEntryBytes.AddRange(BitConverter.GetBytes((uint)16)); //Offset to entries, relative to header 2
            ttbEntryBytes.AddRange(new byte[8]); //padding

            //Entries
            int dataStartOffsetCalc = (16 * Entries.Count) + 16; //Relative to header 2
            int currentIdx = 0;

            foreach (var entry in Entries)
            {
                if (entry.SubEntries == null) entry.SubEntries = new List<TTB_Event>();

                ttbEntryBytes.AddRange(BitConverter.GetBytes(entry.SubEntries.Count));
                ttbEntryBytes.AddRange(BitConverter.GetBytes(dataStartOffsetCalc));
                ttbEntryBytes.AddRange(BitConverter.GetBytes(currentIdx));
                ttbEntryBytes.AddRange(BitConverter.GetBytes(entry.CmsID));

                currentIdx += entry.SubEntries.Count;
            }

            //Events
            foreach(var entry in Entries)
            {
                entry.SubEntries.Sort((x, y) => x.SortID - y.SortID);

                foreach (var _event in entry.SubEntries)
                {
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.SortID));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Cms_Id1));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Costume));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Transformation));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Cms_Id2));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Costume2));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Transformation2));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Cms_Id3));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Costume3));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Transformation3));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Cms_Id4));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Costume4));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Transformation4));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Cms_Id5));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Costume5));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Transformation5));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes((int)_event.Type));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.I_68));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.I_72));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Order1));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(AddString(strings, _event.Voice1)));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Order2));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(AddString(strings, _event.Voice2)));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Order3));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(AddString(strings, _event.Voice3)));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Order4));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(AddString(strings, _event.Voice4)));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(_event.Order5));
                    ttbEntryBytes.AddRange(BitConverter.GetBytes(AddString(strings, _event.Voice5)));

                }
            }

            //Strings
            foreach (var str in strings)
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(str));
                bytes.Add(0);
            }

            //finalize
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - 16), 12);
            bytes.AddRange(ttbEntryBytes);

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }

        //util
        private static int AddString(List<string> strList, string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return -1;

            //Add string
            if (!strList.Contains(str))
                strList.Add(str);

            //Calc offset
            int idx = strList.IndexOf(str);
            int totalPrecedingLength = 0;

            for (int i = 0; i < idx; i++)
                totalPrecedingLength += strList[i].Length + 1;

            return totalPrecedingLength;
        }

        #endregion

    }

    [YAXSerializeAs("Entry")]
    public class TTB_Entry : IInstallable_2<TTB_Event>, IInstallable
    {
        #region NonSerialized
        [YAXDontSerialize]
        public int CmsID { set { I_12 = value.ToString(); } get { return int.Parse(I_12); } }

        //interface
        [YAXDontSerialize]
        public int SortID { get { return CmsID; } }
        [YAXDontSerialize]
        public string Index { get { return I_12; } set { I_12 = value; } }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Cms_Id")]
        public string I_12 { get; set; } //12

        [BindingSubList]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Event")]
        public List<TTB_Event> SubEntries { get; set; } = new List<TTB_Event>();
    }

    [YAXSerializeAs("Event")]
    public class TTB_Event : IInstallable
    {
        #region NonSerialized
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } set { Index = value.ToString(); } }

        [YAXDontSerialize]
        public int Cms_Id1 { get { return int.Parse(I_04); } set { I_04 = value.ToString(); } }
        [YAXDontSerialize]
        public int Cms_Id2 { get { return int.Parse(I_16); } set { I_16 = value.ToString(); } }
        [YAXDontSerialize]
        public int Cms_Id3 { get { return int.Parse(I_28); } set { I_28 = value.ToString(); } }
        [YAXDontSerialize]
        public int Cms_Id4 { get { return int.Parse(I_40); } set { I_40 = value.ToString(); } }
        [YAXDontSerialize]
        public int Cms_Id5 { get { return int.Parse(I_52); } set { I_52 = value.ToString(); } }
        #endregion

        [BindingAutoId]
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public string Index { get; set; } //0 (ID)
       

        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public EventType Type { get; set; } //64


        [YAXAttributeFor("Actor1")]
        [YAXSerializeAs("Cms_Id")]
        public string I_04 { get; set; } //4
        [YAXAttributeFor("Actor1")]
        [YAXSerializeAs("Costume")]
        public int Costume { get; set; } //8
        [YAXAttributeFor("Actor1")]
        [YAXSerializeAs("Transformation")]
        public int Transformation { get; set; } //12
        [YAXAttributeFor("Actor1")]
        [YAXSerializeAs("Order")]
        public int Order1 { get; set; } //76
        [YAXAttributeFor("Actor1")]
        [YAXSerializeAs("Voice")]
        public string Voice1 { get; set; } //80

        [YAXAttributeFor("Actor2")]
        [YAXSerializeAs("Cms_Id")]
        public string I_16 { get; set; } //16
        [YAXAttributeFor("Actor2")]
        [YAXSerializeAs("Costume")]
        public int Costume2 { get; set; } //20
        [YAXAttributeFor("Actor2")]
        [YAXSerializeAs("Transformation")]
        public int Transformation2 { get; set; } //24
        [YAXAttributeFor("Actor2")]
        [YAXSerializeAs("Order")]
        public int Order2 { get; set; } //84
        [YAXAttributeFor("Actor2")]
        [YAXSerializeAs("Voice")]
        public string Voice2 { get; set; } //88

        [YAXAttributeFor("Actor3")]
        [YAXSerializeAs("Cms_Id")]
        public string I_28 { get; set; } //28
        [YAXAttributeFor("Actor3")]
        [YAXSerializeAs("Costume")]
        public int Costume3 { get; set; } //32
        [YAXAttributeFor("Actor3")]
        [YAXSerializeAs("Transformation")]
        public int Transformation3 { get; set; } //36
        [YAXAttributeFor("Actor3")]
        [YAXSerializeAs("Order")]
        public int Order3 { get; set; } //92
        [YAXAttributeFor("Actor3")]
        [YAXSerializeAs("Voice")]
        public string Voice3 { get; set; } //96

        [YAXAttributeFor("Actor4")]
        [YAXSerializeAs("Cms_Id")]
        public string I_40 { get; set; } //40
        [YAXAttributeFor("Actor4")]
        [YAXSerializeAs("Costume")]
        public int Costume4 { get; set; } //44
        [YAXAttributeFor("Actor4")]
        [YAXSerializeAs("Transformation")]
        public int Transformation4 { get; set; } //48
        [YAXAttributeFor("Actor4")]
        [YAXSerializeAs("Order")]
        public int Order4 { get; set; } //100
        [YAXAttributeFor("Actor4")]
        [YAXSerializeAs("Voice")]
        public string Voice4 { get; set; } //104

        [YAXAttributeFor("Actor5")]
        [YAXSerializeAs("Cms_Id")]
        public string I_52 { get; set; } //52
        [YAXAttributeFor("Actor5")]
        [YAXSerializeAs("Costume")]
        public int Costume5 { get; set; } //56
        [YAXAttributeFor("Actor5")]
        [YAXSerializeAs("Transformation")]
        public int Transformation5 { get; set; } //60
        [YAXAttributeFor("Actor5")]
        [YAXSerializeAs("Order")]
        public int Order5 { get; set; } //108
        [YAXAttributeFor("Actor5")]
        [YAXSerializeAs("Voice")]
        public string Voice5 { get; set; } //112


        [YAXAttributeFor("U_44")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint I_68 { get; set; } //68
        [YAXAttributeFor("U_48")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint I_72 { get; set; } //72
    }
}
