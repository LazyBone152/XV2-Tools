using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib.AGD
{
    public class AGD_File
    {
        public List<AGD_Entry> AGD_Entries { get; set; }

        public static AGD_File LoadFile(byte[] rawBytes)
        {
            AGD_File agdFile = new AGD_File() { AGD_Entries = new List<AGD_Entry>() };

            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);

            for(int i = 0; i < count; i++)
            {
                agdFile.AGD_Entries.Add(new AGD_Entry()
                {
                    Level = BitConverter.ToInt32(rawBytes, offset + 0),
                    XpToNextLevel = BitConverter.ToInt32(rawBytes, offset + 4),
                    XpToThisLevel = BitConverter.ToInt32(rawBytes, offset + 8),
                    AttributePointsGained = BitConverter.ToInt32(rawBytes, offset + 12)
                });
                offset += 16;
            }

            return agdFile;
        }

        public int AttributePointsForLevel(int level)
        {
            int attributePoints = 0;
            
            for(int i = 0; i < AGD_Entries.Count; i++)
            {
                if(level < AGD_Entries[i].Level)
                {
                    break;
                } 
                else if(level == AGD_Entries[i].Level)
                {
                    attributePoints += AGD_Entries[i].AttributePointsGained;
                    break;
                }
                else
                {
                    attributePoints += AGD_Entries[i].AttributePointsGained;
                }
            }

            return attributePoints;
        }

        public int ExperienceForLevel(int level)
        {
            int experience = 0;

            for (int i = 0; i < AGD_Entries.Count; i++)
            {
                if (level < AGD_Entries[i].Level)
                {
                    break;
                }
                else if (level == AGD_Entries[i].Level)
                {
                    experience = AGD_Entries[i].XpToThisLevel;
                    break;
                }
                else
                {
                    experience = AGD_Entries[i].XpToThisLevel;
                }
            }

            return experience;
        }

        public int ExperienceForNextLevel(int level)
        {
            int experience = 0;

            for (int i = 0; i < AGD_Entries.Count; i++)
            {
                if (level < AGD_Entries[i].Level)
                {
                    break;
                }
                else if (level == AGD_Entries[i].Level)
                {
                    experience += AGD_Entries[i].XpToNextLevel;
                    break;
                }
                else
                {
                    experience += AGD_Entries[i].XpToNextLevel;
                }
            }

            return experience;
        }

        public bool IsMaxLevel(int level)
        {
            if (AGD_Entries.Count == 0) return false;
            if (AGD_Entries[AGD_Entries.Count - 1].Level == level) return true;
            return false;
        }

        public int CalculateExperienceRequired(int experience, int level)
        {
            int xpNeeded = ExperienceForLevel(level);
            int xpNeededForNextLevel = ExperienceForNextLevel(level);

            if(experience >= xpNeeded && experience < xpNeededForNextLevel)
            {
                return experience;
            }
            else
            {
                return xpNeeded;
            }
        }

        public int GetMaximumLevel()
        {
            return AGD_Entries.Max(x => x.Level);
        }
    }

    public class AGD_Entry
    {
        public int Level { get; set; } //0
        public int XpToNextLevel { get; set; } //4 (relative)
        public int XpToThisLevel { get; set; } //8 (absolute)
        public int AttributePointsGained { get; set; } //12
    }
}
