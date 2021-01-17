using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.TNN
{

    [YAXSerializeAs("TNN")]
    public class TNN_File
    {

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TNN_Entry")]
        public List<TNN_Entry> Entries { get; set; } = new List<TNN_Entry>();

        public List<TNN_String> Strings { get; set; } = new List<TNN_String>();

        #region LoadSave
        public static TNN_File Parse(string path, bool writeXml)
        {
            TNN_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(TNN_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static TNN_File Parse(byte[] bytes)
        {
            TNN_File tnn = new TNN_File();

            int numEntries = BitConverter.ToInt32(bytes, 0);
            int offset = 4;
            int totalSub = 0;

            for(int i = 0; i < numEntries; i++)
            {
                TNN_Entry entry = new TNN_Entry();

                entry.I_00 = BitConverter.ToInt32(bytes, offset + 0);
                entry.I_04 = BitConverter.ToInt32(bytes, offset + 4);
                int numSubEntires = BitConverter.ToInt32(bytes, offset + 8);

                offset += 12;

                for(int a = 0; a < numSubEntires; a++)
                {
                    TNN_SubEntry subEntry = new TNN_SubEntry();

                    subEntry.I_00 = BitConverter.ToInt32(bytes, offset + 0);
                    subEntry.I_04 = BitConverter.ToInt32(bytes, offset + 4);
                    subEntry.I_08 = BitConverter.ToInt32(bytes, offset + 8);

                    entry.SubEntries.Add(subEntry);

                    offset += 12;
                    totalSub++;
                }

                tnn.Entries.Add(entry);
            }

            //strings
            int strId = 0;
            while(offset < bytes.Length)
            {
                string str = StringEx.GetString(bytes, offset, false, StringEx.EncodingType.Unicode);

                int estSize = str.Length * 2;

                try
                {
                    while (bytes[estSize + offset] == 0)
                        estSize++;
                }
                catch (IndexOutOfRangeException ex)
                {

                }

                offset += estSize;
                tnn.Strings.Add(new TNN_String() { ID = strId, StringValue = str });
                strId++;
            }

            //Check
            //if (tnn.Strings.Count != (totalSub * 9))
            //  throw new Exception("nope");

            return tnn;
        }
    

        
        #endregion
    }

    public class TNN_Entry
    {
        [YAXAttributeForClass]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        public int I_04 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TNN_SubEntry")]
        public List<TNN_SubEntry> SubEntries { get; set; } = new List<TNN_SubEntry>();
    }

    public class TNN_SubEntry
    {
        [YAXAttributeForClass]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        public int I_04 { get; set; }
        [YAXAttributeForClass]
        public int I_08 { get; set; }
    }


    public class TNN_String
    {
        [YAXAttributeForClass]
        public int ID { get; set; }
        [YAXAttributeForClass]
        public string StringValue { get; set; }
    }
}
