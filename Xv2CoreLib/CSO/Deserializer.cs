using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CSO
{
    public class Deserializer
    {
        string saveLocation;
        CSO_File csoFile { get; set; }
        public List<byte> bytes = new List<byte>() { 35, 67, 83, 79, 254, 255, 16, 0 };

        //String Writer info
        List<StringWriter.StringInfo> StringInfo = new List<StringWriter.StringInfo>();

        public Deserializer(string fileLocation)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(fileLocation), Path.GetFileNameWithoutExtension(fileLocation));
            YAXSerializer serializer = new YAXSerializer(typeof(CSO_File), YAXSerializationOptions.DontSerializeNullObjects);
            csoFile = (CSO_File)serializer.DeserializeFromFile(fileLocation);
            WriteFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(CSO_File _csoFile, string _saveLocation)
        {
            saveLocation = _saveLocation;
            csoFile = _csoFile;
            WriteFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(CSO_File _csoFile)
        {
            csoFile = _csoFile;
            WriteFile();
        }

        private void WriteFile()
        {
            int count = (csoFile.CsoEntries != null) ? csoFile.CsoEntries.Count() : 0;
            int offset = (csoFile.CsoEntries != null) ? 16 : 0;
            bytes.AddRange(BitConverter.GetBytes((uint)count));
            bytes.AddRange(BitConverter.GetBytes((uint)offset));

            //Writing Cso Entries
            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(uint.Parse(csoFile.CsoEntries[i].I_00)));
                bytes.AddRange(BitConverter.GetBytes(csoFile.CsoEntries[i].I_04));

                StringInfo.Add(new StringWriter.StringInfo() { Offset = bytes.Count(), RelativeOffset = 0, StringToWrite = csoFile.CsoEntries[i].Str_08 });
                bytes.AddRange(new byte[4]);
                StringInfo.Add(new StringWriter.StringInfo() { Offset = bytes.Count(), RelativeOffset = 0, StringToWrite = csoFile.CsoEntries[i].Str_12 });
                bytes.AddRange(new byte[4]);
                StringInfo.Add(new StringWriter.StringInfo() { Offset = bytes.Count(), RelativeOffset = 0, StringToWrite = csoFile.CsoEntries[i].Str_16 });
                bytes.AddRange(new byte[4]);
                StringInfo.Add(new StringWriter.StringInfo() { Offset = bytes.Count(), RelativeOffset = 0, StringToWrite = csoFile.CsoEntries[i].Str_20 });
                bytes.AddRange(new byte[4]);

                /*
                if (!String.IsNullOrWhiteSpace(csoFile.CsoEntries[i].Str_08))
                {
                    StringInfo.Add(new StringWriter.StringInfo() { Offset = bytes.Count(), RelativeOffset = 0, StringToWrite = csoFile.CsoEntries[i].Str_08 });
                }
                bytes.AddRange(new byte[4]);
                if (!String.IsNullOrWhiteSpace(csoFile.CsoEntries[i].Str_08))
                {
                    StringInfo.Add(new StringWriter.StringInfo() { Offset = bytes.Count(), RelativeOffset = 0, StringToWrite = csoFile.CsoEntries[i].Str_12 });
                }
                bytes.AddRange(new byte[4]);
                if (!String.IsNullOrWhiteSpace(csoFile.CsoEntries[i].Str_08))
                {
                    StringInfo.Add(new StringWriter.StringInfo() { Offset = bytes.Count(), RelativeOffset = 0, StringToWrite = csoFile.CsoEntries[i].Str_16 });
                }
                bytes.AddRange(new byte[4]);
                if (!String.IsNullOrWhiteSpace(csoFile.CsoEntries[i].Str_08))
                {
                    StringInfo.Add(new StringWriter.StringInfo() { Offset = bytes.Count(), RelativeOffset = 0, StringToWrite = csoFile.CsoEntries[i].Str_20 });
                }
                bytes.AddRange(new byte[4]);
                */

                bytes.AddRange(new byte[8]);
            }

            //Writing strings

            bytes = StringWriter.WritePointerStrings(StringInfo, bytes);

        }
    }
}
