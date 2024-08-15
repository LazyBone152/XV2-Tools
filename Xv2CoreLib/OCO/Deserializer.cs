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
        OCO_File ocoFile;
        public List<byte> bytes = new List<byte>() { 35, 79, 67, 79, 254, 255 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(OCO_File), YAXSerializationOptions.DontSerializeNullObjects);
            ocoFile = (OCO_File)serializer.DeserializeFromFile(location);
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(OCO_File ocoFile)
        {
            this.ocoFile = ocoFile;
            Write();
        }

        private void Write()
        {
            // Header
            VersionCheck();
            bytes.AddRange(BitConverter.GetBytes(ocoFile.Version));
            int count = (ocoFile.Partners != null) ? ocoFile.Partners.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(new byte[4]);

            //Tabel
            int dataOffset = count * 16;
            int currentIndex = 0;

            for(int i = 0; i < count; i++)
            {
                int subDataCount = (ocoFile.Partners[i].SubEntries != null) ? ocoFile.Partners[i].SubEntries.Count() : 0;
                bytes.AddRange(BitConverter.GetBytes(subDataCount));
                bytes.AddRange(BitConverter.GetBytes(dataOffset));
                bytes.AddRange(BitConverter.GetBytes(currentIndex));
                bytes.AddRange(BitConverter.GetBytes(ocoFile.Partners[i].PartnerID));
                currentIndex += subDataCount;
            }

            //Table Entries
            for (int i = 0; i < count; i++)
            {
                int subDataCount = (ocoFile.Partners[i].SubEntries != null) ? ocoFile.Partners[i].SubEntries.Count() : 0;

                for(int a = 0; a < subDataCount; a++)
                {
                    bytes.AddRange(BitConverter.GetBytes(ocoFile.Partners[i].PartnerID));
                    bytes.AddRange(BitConverter.GetBytes(ocoFile.Partners[i].SubEntries[a].I_04));
                    bytes.AddRange(BitConverter.GetBytes(ocoFile.Partners[i].SubEntries[a].I_08));
                    bytes.AddRange(BitConverter.GetBytes(ocoFile.Partners[i].SubEntries[a].I_12));

                    if (ocoFile.Version >= 20)
                    {
                        bytes.AddRange(BitConverter.GetBytes(ocoFile.Partners[i].SubEntries[a].NEW_I_16));
                    }

                    bytes.AddRange(BitConverter.GetBytes(ocoFile.Partners[i].SubEntries[a].I_16));
                }

            }

        }

        private void VersionCheck()
        {
            switch (ocoFile.Version)
            {
                case 16:
                case 20:
                    return;
                default:
                    throw new Exception("Unknown OCO version: " + ocoFile.Version);
            }

        }
    }
}
