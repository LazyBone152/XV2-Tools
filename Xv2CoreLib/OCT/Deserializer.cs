using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCT
{
    public class Deserializer
    {
        string saveLocation;
        OCT_File octFile;
        List<byte> bytes = new List<byte>() { 35, 79, 67, 84, 254, 255, 20, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(OCT_File), YAXSerializationOptions.DontSerializeNullObjects);
            octFile = (OCT_File)serializer.DeserializeFromFile(location);
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        private void Write()
        {
            int count = (octFile.OctTableEntries != null) ? octFile.OctTableEntries.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(new byte[4]);

            //Tabel
            int dataOffset = count * 16;
            int currentIndex = 0;

            for(int i = 0; i < count; i++)
            {
                int subDataCount = (octFile.OctTableEntries[i].OctSubEntries != null) ? octFile.OctTableEntries[i].OctSubEntries.Count() : 0;
                bytes.AddRange(BitConverter.GetBytes(subDataCount));
                bytes.AddRange(BitConverter.GetBytes(dataOffset));
                bytes.AddRange(BitConverter.GetBytes(currentIndex));
                bytes.AddRange(BitConverter.GetBytes(octFile.OctTableEntries[i].Index));
                currentIndex += subDataCount;
            }

            //Table Entries
            for (int i = 0; i < count; i++)
            {
                int subDataCount = (octFile.OctTableEntries[i].OctSubEntries != null) ? octFile.OctTableEntries[i].OctSubEntries.Count() : 0;

                for(int a = 0; a < subDataCount; a++)
                {
                    bytes.AddRange(BitConverter.GetBytes(octFile.OctTableEntries[i].Index));
                    bytes.AddRange(BitConverter.GetBytes(octFile.OctTableEntries[i].OctSubEntries[a].Index));
                    bytes.AddRange(BitConverter.GetBytes(octFile.OctTableEntries[i].OctSubEntries[a].I_08));
                    bytes.AddRange(BitConverter.GetBytes(octFile.OctTableEntries[i].OctSubEntries[a].I_12));
                    bytes.AddRange(BitConverter.GetBytes(octFile.OctTableEntries[i].OctSubEntries[a].STP_Cost));
                    bytes.AddRange(BitConverter.GetBytes(octFile.OctTableEntries[i].OctSubEntries[a].I_16));
                    bytes.AddRange(BitConverter.GetBytes(octFile.OctTableEntries[i].OctSubEntries[a].I_20));
                }

            }

        }
    }
}
