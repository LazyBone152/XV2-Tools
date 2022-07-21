using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCO
{
    public class Deserializer
    {
        string saveLocation;
        OCO_File octFile;
        public List<byte> bytes = new List<byte>() { 35, 79, 67, 79, 254, 255, 16, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(OCO_File), YAXSerializationOptions.DontSerializeNullObjects);
            octFile = (OCO_File)serializer.DeserializeFromFile(location);
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(OCO_File ocoFile)
        {
            this.octFile = ocoFile;
            Write();
        }

        private void Write()
        {
            int count = (octFile.Partners != null) ? octFile.Partners.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(new byte[4]);

            //Tabel
            int dataOffset = count * 16;
            int currentIndex = 0;

            for(int i = 0; i < count; i++)
            {
                int subDataCount = (octFile.Partners[i].SubEntries != null) ? octFile.Partners[i].SubEntries.Count() : 0;
                bytes.AddRange(BitConverter.GetBytes(subDataCount));
                bytes.AddRange(BitConverter.GetBytes(dataOffset));
                bytes.AddRange(BitConverter.GetBytes(currentIndex));
                bytes.AddRange(BitConverter.GetBytes(octFile.Partners[i].PartnerID));
                currentIndex += subDataCount;
            }

            //Table Entries
            for (int i = 0; i < count; i++)
            {
                int subDataCount = (octFile.Partners[i].SubEntries != null) ? octFile.Partners[i].SubEntries.Count() : 0;

                for(int a = 0; a < subDataCount; a++)
                {
                    bytes.AddRange(BitConverter.GetBytes(octFile.Partners[i].PartnerID));
                    bytes.AddRange(BitConverter.GetBytes(octFile.Partners[i].SubEntries[a].I_04));
                    bytes.AddRange(BitConverter.GetBytes(octFile.Partners[i].SubEntries[a].I_08));
                    bytes.AddRange(BitConverter.GetBytes(octFile.Partners[i].SubEntries[a].I_12));
                    bytes.AddRange(BitConverter.GetBytes(octFile.Partners[i].SubEntries[a].I_16));
                }

            }

        }
    }
}
