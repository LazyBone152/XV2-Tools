using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.CUS
{
    public class Deserializer
    {
        string saveLocation;
        CUS_File cusFile;
        public List<byte> bytes = new List<byte>() { 35, 67, 85, 83, 254, 255, 0, 0 };
        
        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(CUS_File), YAXSerializationOptions.DontSerializeNullObjects);
            cusFile = (CUS_File)serializer.DeserializeFromFile(location);
            Validation();
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(CUS_File _cusFile, string location)
        {
            saveLocation = location;
            cusFile = _cusFile;
            Validation();
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(CUS_File _cusFile)
        {
            cusFile = _cusFile;
            Validation();
            Write();
        }

        private void Validation()
        {
            cusFile.SortEntries();
        }

        private void Write()
        {
            int skillsetCount = (cusFile.Skillsets != null) ? cusFile.Skillsets.Count() : 0;
            int superCount = (cusFile.SuperSkills != null) ? cusFile.SuperSkills.Count() : 0;
            int ultimateCount = (cusFile.UltimateSkills != null) ? cusFile.UltimateSkills.Count() : 0;
            int evasiveCount = (cusFile.EvasiveSkills != null) ? cusFile.EvasiveSkills.Count() : 0;
            int unkCount = (cusFile.UnkSkills != null) ? cusFile.UnkSkills.Count() : 0;
            int blastCount = (cusFile.BlastSkills != null) ? cusFile.BlastSkills.Count() : 0;
            int awokenCount = (cusFile.AwokenSkills != null) ? cusFile.AwokenSkills.Count() : 0;

            //Header
            bytes.AddRange(BitConverter.GetBytes(skillsetCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(superCount));
            bytes.AddRange(BitConverter.GetBytes(ultimateCount));
            bytes.AddRange(BitConverter.GetBytes(evasiveCount));
            bytes.AddRange(BitConverter.GetBytes(unkCount));
            bytes.AddRange(BitConverter.GetBytes(blastCount));
            bytes.AddRange(BitConverter.GetBytes(awokenCount));
            bytes.AddRange(new byte[32]);

            if(skillsetCount > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 12);

                for (int i = 0; i < skillsetCount; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(int.Parse(cusFile.Skillsets[i].I_00)));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_08));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_10));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_14));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_18));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_20));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_22));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_24));
                    bytes.AddRange(BitConverter.GetBytes(cusFile.Skillsets[i].I_26));
                    bytes.AddRange(new byte[4]);
                }
            }

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 40);
            List<List<int>> superStrList = WriteSkills(cusFile.SuperSkills);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 44);
            List<List<int>> ultimateStrList = WriteSkills(cusFile.UltimateSkills);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 48);
            List<List<int>> evasiveStrList = WriteSkills(cusFile.EvasiveSkills);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 52);
            List<List<int>> unkStrList = WriteSkills(cusFile.UnkSkills);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 56);
            List<List<int>> blastStrList = WriteSkills(cusFile.BlastSkills);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 60);
            List<List<int>> awokenStrList = WriteSkills(cusFile.AwokenSkills);

            WriteStr(cusFile.SuperSkills, superStrList);
            WriteStr(cusFile.UltimateSkills, ultimateStrList);
            WriteStr(cusFile.EvasiveSkills, evasiveStrList);
            WriteStr(cusFile.UnkSkills, unkStrList);
            WriteStr(cusFile.BlastSkills, blastStrList);
            WriteStr(cusFile.AwokenSkills, awokenStrList);
        }

        private List<List<int>> WriteSkills(List<Skill> skills)
        {
            List<List<int>> strOffsets = new List<List<int>>();

            if(skills != null)
            {
                for (int i = 0; i < skills.Count(); i++)
                {
                    strOffsets.Add(new List<int>());
                    if(skills[i].ShortName.Length > 4)
                    {
                        throw new Exception(String.Format("The skill ShortName: \"{0}\" exceeds the maximum length of 4!", skills[i].ShortName));
                    }
                    
                    bytes.AddRange(Encoding.ASCII.GetBytes(skills[i].ShortName));
                    bytes.AddRange(new byte[4 - skills[i].ShortName.Length]);
                    bytes.AddRange(BitConverter.GetBytes(skills[i].I_04));
                    bytes.AddRange(BitConverter.GetBytes((ushort)skills[i].ID1));
                    bytes.AddRange(BitConverter.GetBytes((ushort)skills[i].ID2));
                    bytes.Add(skills[i].I_12);
                    bytes.Add(skills[i].I_13);
                    bytes.AddRange(BitConverter.GetBytes((ushort)skills[i].FilesLoadedFlags1));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].I_18));
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    strOffsets[i].Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(skills[i].I_48));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].I_50));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].I_52));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].I_54));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].PUP));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].CusAura));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].CharaSwapId));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].I_62));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].NumTransformations));
                    bytes.AddRange(BitConverter.GetBytes(skills[i].I_66));
                }
            }

            return strOffsets;

        }

        private void WriteStr(List<Skill> skills, List<List<int>> strOffsets)
        {
            if(skills != null)
            {
                for(int i = 0; i < skills.Count(); i++)
                {
                    if(skills[i].EanPath != "NULL" && !string.IsNullOrWhiteSpace(skills[i].EanPath))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][0]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(skills[i].EanPath));
                        bytes.Add(0);
                    }

                    if (skills[i].CamEanPath != "NULL" && !string.IsNullOrWhiteSpace(skills[i].CamEanPath))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][1]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(skills[i].CamEanPath));
                        bytes.Add(0);
                    }
                    if (skills[i].EepkPath != "NULL" && !string.IsNullOrWhiteSpace(skills[i].EepkPath))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][2]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(skills[i].EepkPath));
                        bytes.Add(0);
                    }
                    if (skills[i].SePath != "NULL" && !string.IsNullOrWhiteSpace(skills[i].SePath))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][3]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(skills[i].SePath));
                        bytes.Add(0);
                    }
                    if (skills[i].VoxPath != "NULL" && !string.IsNullOrWhiteSpace(skills[i].VoxPath))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][4]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(skills[i].VoxPath));
                        bytes.Add(0);
                    }
                    if (skills[i].AfterBacPath != "NULL" && !string.IsNullOrWhiteSpace(skills[i].AfterBacPath))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][5]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(skills[i].AfterBacPath));
                        bytes.Add(0);
                    }
                    if (skills[i].AfterBcmPath != "NULL" && !string.IsNullOrWhiteSpace(skills[i].AfterBcmPath))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), strOffsets[i][6]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(skills[i].AfterBcmPath));
                        bytes.Add(0);
                    }
                }
            }
        }
    }
}
