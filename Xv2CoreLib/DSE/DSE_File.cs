using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.DSE
{
    [YAXSerializeAs("DSE")]
    public class DSE_File
    {
        public const int DSE_SIGNATURE = 1163084835;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "DemoSoundEffect")]
        public List<DSE_Entry> DSE_Entries { get; set; }

        public static void ParseFile(string filePath)
        {
            var dseFile = Load(filePath);
            YAXSerializer serializer = new YAXSerializer(typeof(DSE_File));
            serializer.SerializeToFile(dseFile, filePath + ".xml");
        }

        public static void LoadXmlAndSave(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(DSE_File), YAXSerializationOptions.DontSerializeNullObjects);
            DSE_File dseFile = (DSE_File)serializer.DeserializeFromFile(xmlPath);
            byte[] bytes = dseFile.GetBytes();
            File.WriteAllBytes(saveLocation, bytes);
        }

        public static DSE_File Load(string path)
        {
            //Init
            DSE_File dseFile = new DSE_File() { DSE_Entries = new List<DSE_Entry>() };
            byte[] rawBytes = File.ReadAllBytes(path);

            //Header
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);
            
            //Entries
            for(int i = 0; i < count; i++)
            {
                dseFile.DSE_Entries.Add(new DSE_Entry()
                {
                    I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                    I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                });

                offset += 16;
            }

            return dseFile;
            
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            int count = (DSE_Entries != null) ? DSE_Entries.Count : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(DSE_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)16));
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(16));

            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(DSE_Entries[i].I_00));
                bytes.AddRange(BitConverter.GetBytes(DSE_Entries[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(DSE_Entries[i].I_08));
                bytes.AddRange(BitConverter.GetBytes(DSE_Entries[i].I_12));
            }

            return bytes.ToArray();
        }
    }

    [YAXSerializeAs("DemoSoundEffect")]
    public class DSE_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Time")]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_04")]
        public int I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Cue_ID")]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_12")]
        public int I_12 { get; set; }
    }
}
