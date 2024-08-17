using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CDT
{
    public class Deserializer
    {
        string saveLocation;
        CDT_File cdtFile;
        public List<byte> bytes = new List<byte>() { 35, 67, 68, 84 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(CDT_File), YAXSerializationOptions.DontSerializeNullObjects);
            cdtFile = (CDT_File)serializer.DeserializeFromFile(location);
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(CDT_File cdtFile)
        {
            this.cdtFile = cdtFile;
            Write();
        }

        private void Write()
        {
            // Header
            int count = (cdtFile.Entries != null) ? cdtFile.Entries.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(cdtFile.I_08));
            bytes.AddRange(BitConverter.GetBytes(cdtFile.I_12));

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(i));
                    if (cdtFile.Entries[i].TextLeft == null) cdtFile.Entries[i].TextLeft = string.Empty;
                    bytes.AddRange(BitConverter.GetBytes(cdtFile.Entries[i].TextLeft.Length * 2 + 2));
                    bytes.AddRange(Encoding.Unicode.GetBytes(cdtFile.Entries[i].TextLeft));
                    bytes.AddRange(new byte[2]);
                    if (cdtFile.Entries[i].TextRight == null) cdtFile.Entries[i].TextRight = string.Empty;
                    bytes.AddRange(BitConverter.GetBytes(cdtFile.Entries[i].TextRight.Length * 2 + 2));
                    bytes.AddRange(Encoding.Unicode.GetBytes(cdtFile.Entries[i].TextRight));
                    bytes.AddRange(new byte[2]);
                    bytes.AddRange(BitConverter.GetBytes(cdtFile.Entries[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(cdtFile.Entries[i].I_20));
                    bytes.AddRange(BitConverter.GetBytes(cdtFile.Entries[i].I_24));
                }
            }
        }
    }
}
