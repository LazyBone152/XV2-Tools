using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.OCS
{
    public class Deserializer
    {
        private string saveLocation;
        private OCS_File ocsFile;
        private List<byte> bytes = new List<byte>() { 35, 79, 67, 83, 254, 255 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(OCS_File), YAXSerializationOptions.DontSerializeNullObjects);
            ocsFile = (OCS_File)serializer.DeserializeFromFile(location);
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(OCS_File file)
        {
            ocsFile = file;
            Write();
        }

        private void Write()
        {
            //Header
            VersionCheck();
            bytes.AddRange(BitConverter.GetBytes(ocsFile.Version));
            int count = (ocsFile.Partners != null) ? ocsFile.Partners.Count() : 0;
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
                int subEntryCount = (ocsFile.Partners[i].SkillTypes != null) ? ocsFile.Partners[i].SkillTypes.Count() : 0;
                bytes.AddRange(BitConverter.GetBytes(subEntryCount));
                bytes.AddRange(BitConverter.GetBytes(secondTableOffset));
                bytes.AddRange(BitConverter.GetBytes(currentIndex1));
                bytes.AddRange(BitConverter.GetBytes(ocsFile.Partners[i].Index));
                currentIndex1 += subEntryCount;
            }

            //2nd Table Entries
            foreach(var tableEntry in ocsFile.Partners)
            {
                int subEntryCount = (tableEntry.SkillTypes != null) ? tableEntry.SkillTypes.Count() : 0;

                for (int i = 0; i < subEntryCount; i++)
                {
                    int subDataCount = (tableEntry.SkillTypes[i].Skills != null) ? tableEntry.SkillTypes[i].Skills.Count() : 0;

                    bytes.AddRange(BitConverter.GetBytes(subDataCount));
                    bytes.AddRange(BitConverter.GetBytes(dataOffset));
                    bytes.AddRange(BitConverter.GetBytes(currentIndex2));
                    bytes.AddRange(BitConverter.GetBytes((int)tableEntry.SkillTypes[i].Skill_Type));
                    currentIndex2 += subDataCount;
                }
            }

            //Table Entries
            for (int i = 0; i < count; i++)
            {
                int subTableCount = (ocsFile.Partners[i].SkillTypes != null) ? ocsFile.Partners[i].SkillTypes.Count() : 0;

                for(int a = 0; a < subTableCount; a++)
                {
                    int subDataCount = (ocsFile.Partners[i].SkillTypes[a].Skills != null) ? ocsFile.Partners[i].SkillTypes[a].Skills.Count() : 0;

                    for (int s = 0; s < subDataCount; s++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.Partners[i].Index));
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.Partners[i].SkillTypes[a].Skills[s].EntryID));
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.Partners[i].SkillTypes[a].Skills[s].TP_Cost_Toggle));
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.Partners[i].SkillTypes[a].Skills[s].TP_Cost));
                        bytes.AddRange(BitConverter.GetBytes((int)ocsFile.Partners[i].SkillTypes[a].Skill_Type));
                        bytes.AddRange(BitConverter.GetBytes(ocsFile.Partners[i].SkillTypes[a].Skills[s].SkillID2));

                        if(ocsFile.Version >= 20)
                        {
                            bytes.AddRange(BitConverter.GetBytes(ocsFile.Partners[i].SkillTypes[a].Skills[s].DLC_Flag));
                        }

                        if(ocsFile.Version >= 28)
                        {
                            bytes.AddRange(BitConverter.GetBytes(ocsFile.Partners[i].SkillTypes[a].Skills[s].STP_Cost));
                            bytes.AddRange(BitConverter.GetBytes(ocsFile.Partners[i].SkillTypes[a].Skills[s].NEW_I_32));
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
                case 28:
                    return;
                default:
                    throw new Exception("Unknown OCS version: " + ocsFile.Version);
            }

        }

        public byte[] GetByteArray()
        {
            return bytes.ToArray();
        }
    }
}
