using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCP
{
    public class Deserializer
    {
        string saveLocation;
        OCP_File ocpFile;
        public List<byte> bytes = new List<byte>() { 35, 79, 67, 80, 254, 255, 16, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(OCP_File), YAXSerializationOptions.DontSerializeNullObjects);
            ocpFile = (OCP_File)serializer.DeserializeFromFile(location);
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(OCP_File ocpFile)
        {
            this.ocpFile = ocpFile;
            Write();
        }

        private void Write()
        {
            int count = (ocpFile.TableEntries != null) ? ocpFile.TableEntries.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(new byte[4]);

            //Tabel
            int dataOffset = count * 16;
            int currentIndex = 0;

            for(int i = 0; i < count; i++)
            {
                int subDataCount = (ocpFile.TableEntries[i].SubEntries != null) ? ocpFile.TableEntries[i].SubEntries.Count() : 0;
                bytes.AddRange(BitConverter.GetBytes(subDataCount));
                bytes.AddRange(BitConverter.GetBytes(dataOffset));
                bytes.AddRange(BitConverter.GetBytes(currentIndex));
                bytes.AddRange(BitConverter.GetBytes(ocpFile.TableEntries[i].PartnerID));
                currentIndex += subDataCount;
            }

            //Table Entries
            for (int i = 0; i < count; i++)
            {
                int subDataCount = (ocpFile.TableEntries[i].SubEntries != null) ? ocpFile.TableEntries[i].SubEntries.Count() : 0;

                for(int a = 0; a < subDataCount; a++)
                {
                    bytes.AddRange(BitConverter.GetBytes(ocpFile.TableEntries[i].PartnerID));
                    bytes.AddRange(BitConverter.GetBytes(ocpFile.TableEntries[i].SubEntries[a].I_04));
                    bytes.AddRange(BitConverter.GetBytes(ocpFile.TableEntries[i].SubEntries[a].I_08));
                    bytes.AddRange(BitConverter.GetBytes(ocpFile.TableEntries[i].SubEntries[a].I_12));
                    bytes.AddRange(BitConverter.GetBytes(ocpFile.TableEntries[i].SubEntries[a].I_16));
                }

            }

        }
    }
}
