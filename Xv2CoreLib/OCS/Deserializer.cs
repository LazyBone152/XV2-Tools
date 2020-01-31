using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCS
{
    public class Deserializer
    {
        string saveLocation;
        OCS_File ocsFile;
        List<byte> bytes = new List<byte>() { 35, 79, 67, 83, 254, 255 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(OCS_File), YAXSerializationOptions.DontSerializeNullObjects);
            ocsFile = (OCS_File)serializer.DeserializeFromFile(location);
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        private void Write()
        {
            //Header
            VersionCheck();
            bytes.AddRange(BitConverter.GetBytes(ocsFile.Version));
            int count = (ocsFile.TableEntries != null) ? ocsFile.TableEntries.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(new byte[4]);

            //Tabel
            int secondTableOffset = count * 16;
            int dataOffset = ocsFile.CalculateDataOffset();
            int currentIndex1 = 0;
            int currentIndex2 = 0;


            //1st Table Entries
            for (int i = 0; i < count; i++)
            {
                int subEntryCount = (ocsFile.TableEntries[i].SubEntries != null) ? ocsFile.TableEntries[i].SubEntries.Count() : 0;
                bytes.AddRange(BitConverter.GetBytes(subEntryCount));
                bytes.AddRange(BitConverter.GetBytes(secondTableOffset));
                bytes.AddRange(BitConverter.GetBytes(currentIndex1));
                bytes.AddRange(BitConverter.GetBytes(ocsFile.TableEntries[i].Index));
                currentIndex1 += subEntryCount;
            }

            //2nd Table Entries
            foreach(var tableEntry in ocsFile.TableEntries)
            {
                int subEntryCount = (tableEntry.SubEntries != null) ? tableEntry.SubEntries.Count() : 0;

                for (int i = 0; i < subEntryCount; i++)
                {
                    int subDataCount = (tableEntry.SubEntries[i].SubEntries != null) ? tableEntry.SubEntries[i].SubEntries.Count() : 0;

                    bytes.AddRange(BitConverter.GetBytes(subDataCount));
                    bytes.AddRange(BitConverter.GetBytes(dataOffset));
                    bytes.AddRange(BitConverter.GetBytes(currentIndex2));
                    bytes.AddRange(BitConverter.GetBytes((int)tableEntry.SubEntries[i].Skill_Type));
                    currentIndex2 += subDataCount;
                }
            }

            //Table Entries
            for (int i = 0; i < count; i++)
            {
                int subTableCount = (ocsFile.TableEntries[i].SubEntries != null) ? ocsFile.TableEntries[i].SubEntries.Count() : 0;

                for(int a = 0; a < subTableCount; a++)
                {
                    int subDataCount = (ocsFile.TableEntries[i].SubEntries[a].SubEntries != null) ? ocsFile.TableEntries[i].SubEntries[a].SubEntries.Count() : 0;

                    for (int s = 0; s < subDataCount; s++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.TableEntries[i].Index));
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.TableEntries[i].SubEntries[a].SubEntries[s].I_04));
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.TableEntries[i].SubEntries[a].SubEntries[s].I_08));
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.TableEntries[i].SubEntries[a].SubEntries[s].I_12));
                        bytes.AddRange(BitConverter.GetBytes((int)ocsFile.TableEntries[i].SubEntries[a].Skill_Type));
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.TableEntries[i].SubEntries[a].SubEntries[s].I_20));

                        if(ocsFile.Version == 20)
                        {
                            bytes.AddRange(BitConverter.GetBytes(ocsFile.TableEntries[i].SubEntries[a].SubEntries[s].I_24));
                        }
                    }
                }

            }

        }

        private void VersionCheck()
        {
            switch (ocsFile.Version)
            {
                case 16:
                case 20:
                    return;
                default:
                    throw new Exception("Unknown OCS version: " + ocsFile.Version);
            }

        }
    }
}
