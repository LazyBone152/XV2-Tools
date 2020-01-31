using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BDM
{
    //BDM Damage Types(XV1 > XV2) :
    //5 = 5
    //6 = 7 (+1)
    //7 = 9 (+2)
    //8 = 10 (+2)
    //12 = 14 (+2)
    //14 = 16 (+2)
    //15 = 17 (+2)
    //16 = 18 (+2)

    public enum BDM_Type
    {
        XV2_0,
        XV2_1,
        XV1
    }

    [YAXSerializeAs("BDM")]
    public class BDM_File : ISorting, IIsNull
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public BDM_Type BDM_Type { get; set; } = BDM_Type.XV2_0;

        [YAXComment("[XV2 Types only] Each BDM SubEntry is for a different activation condition, all known ones are as follows (by Index):" +
            "\n0 = default struck" +
            "\n1 = Unknown" +
            "\n2 = struck while in floating hit animation" +
            "\n3 = struck in back" +
            "\n4 = struck while in rolling hit animation" +
            "\n5 = struck whilst guarding or just guarding" +
            "\n6 = struck while in standard hit animation" +
            "\n7 = Unknown" +
            "\n8 = struck while in heavy stun animation" +
            "\n9 = struck while lying on ground")]

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("BDM_Entries")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BDM_Entry")]
        public List<BDM_Entry> BDM_Entries { get; set; } = new List<BDM_Entry>();

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            BDM_Entries.Sort((x, y) => x.SortID - y.SortID);
        }

        public static BDM_File Load(byte[] rawBytes, bool convertToType0 = false)
        {
            var bdm = new Parser(rawBytes).bdmFile;

            if (convertToType0 && bdm.BDM_Type == BDM_Type.XV2_1)
            {
                bdm.ConvertToXv2_0();
            }

            return bdm;
        }


        public static BDM_File Load(string path, bool convertToType0 = false)
        {
            var bdm = new Parser(path, false).bdmFile;

            if (convertToType0 && bdm.BDM_Type == BDM_Type.XV2_1)
            {
                bdm.ConvertToXv2_0();
            }

            return bdm;
        }

        public void Save(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public void AddEntry(int id, BDM_Entry entry)
        {
            for (int i = 0; i < BDM_Entries.Count; i++)
            {
                if (BDM_Entries[i].I_00 == id)
                {
                    BDM_Entries[i] = entry;
                    return;
                }
            }

            BDM_Entries.Add(entry);
        }

        public void ConvertToXv2()
        {
            if (BDM_Type == BDM_Type.XV1)
                ConvertXv1ToXv2_0();

            if (BDM_Type == BDM_Type.XV2_1)
                ConvertToXv2_0();
        }

        /// <summary>
        /// Converts a TYPE XV2_1 BDM file into a TYPE XV2_0 BDM file.
        /// </summary>
        public void ConvertToXv2_0()
        {
            if(BDM_Type == BDM_Type.XV2_1)
            {

                List<BDM_Entry> newEntries = new List<BDM_Entry>();

                foreach(var entry in BDM_Entries)
                {
                    int idx = newEntries.Count;
                    newEntries.Add(new BDM_Entry() { Type0Entries = new List<Type0SubEntry>(), I_00 = entry.I_00 });

                    foreach(var subEntry in entry.Type1Entries)
                    {
                        newEntries[idx].Type0Entries.Add(new Type0SubEntry()
                        {
                            Index = subEntry.Index,
                            I_00 = subEntry.I_00,
                            I_102 = subEntry.I_94,
                            I_04 = subEntry.I_04,
                            I_94 = subEntry.I_86,
                            I_12 = subEntry.I_12,
                            I_14 = subEntry.I_14,
                            I_16 = subEntry.I_16,
                            I_18 = subEntry.I_18,
                            I_20 = subEntry.I_20,
                            I_22 = subEntry.I_22,
                            I_24 = subEntry.I_24,
                            I_26 = subEntry.I_26,
                            I_28 = subEntry.I_28,
                            I_32 = -1,
                            F_40 = subEntry.F_32,
                            F_44 = subEntry.F_36,
                            I_48 = subEntry.I_40,
                            I_50 = subEntry.I_42,
                            I_52 = subEntry.I_44,
                            I_56 = subEntry.I_48,
                            I_54 = subEntry.I_46,
                            I_78 = subEntry.I_70,
                            F_60 = subEntry.F_52,
                            F_64 = subEntry.F_56,
                            F_68 = subEntry.F_60,
                            F_72 = subEntry.F_64,
                            I_80 = subEntry.I_72,
                            I_86 = subEntry.I_78,
                            I_104 = subEntry.I_96,
                            I_106 = subEntry.I_98,
                            I_108 = subEntry.I_100,
                            I_110 = subEntry.I_102,
                            I_112 = -1,
                            I_84 = subEntry.I_76,
                            I_100 = subEntry.I_92,
                            I_58 = subEntry.I_50,
                            I_76 = subEntry.I_68,
                            I_02 = subEntry.I_02,
                            I_06 = subEntry.I_06,
                            F_08 = subEntry.F_08,
                            I_82 = subEntry.I_74,
                            I_88 = subEntry.I_80,
                            I_96 = subEntry.I_88
                        });
                    }
                }


                BDM_Type = BDM_Type.XV2_0;
                BDM_Entries = newEntries;
            }
            else
            {
                throw new InvalidOperationException(String.Format("Cannot convert BDM Type {0} into BDM Type XV2_0.", BDM_Type));
            }
        }

        public void ConvertXv1ToXv2_0()
        {
            if (BDM_Type == BDM_Type.XV1)
            {

                List<BDM_Entry> newEntries = new List<BDM_Entry>();

                foreach (var entry in BDM_Entries)
                {
                    int idx = newEntries.Count;
                    newEntries.Add(new BDM_Entry() { Type0Entries = new List<Type0SubEntry>(), I_00 = entry.I_00 });
                    
                    foreach (var subEntry in entry.Type1Entries)
                    {
                        newEntries[idx].Type0Entries.Add(new Type0SubEntry()
                        {
                            Index = subEntry.Index,
                            I_00 = DamageTypeXv1ToXv2(subEntry.I_00),
                            I_102 = subEntry.I_94,
                            I_04 = subEntry.I_04,
                            I_94 = subEntry.I_86,
                            I_12 = subEntry.I_12,
                            I_14 = subEntry.I_14,
                            I_16 = subEntry.I_16,
                            I_18 = subEntry.I_18,
                            I_20 = subEntry.I_20,
                            I_22 = subEntry.I_22,
                            I_24 = subEntry.I_24,
                            I_26 = subEntry.I_26,
                            I_28 = subEntry.I_28,
                            I_32 = -1,
                            F_40 = subEntry.F_32,
                            F_44 = subEntry.F_36,
                            I_48 = subEntry.I_40,
                            I_50 = subEntry.I_42,
                            I_52 = subEntry.I_44,
                            I_56 = subEntry.I_48,
                            I_54 = subEntry.I_46,
                            I_78 = subEntry.I_70,
                            F_60 = subEntry.F_52,
                            F_64 = subEntry.F_56,
                            F_68 = subEntry.F_60,
                            F_72 = subEntry.F_64,
                            I_80 = subEntry.I_72,
                            I_86 = subEntry.I_78,
                            I_104 = subEntry.I_96,
                            I_106 = subEntry.I_98,
                            I_108 = subEntry.I_100,
                            I_110 = subEntry.I_102,
                            I_112 = -1,
                            I_84 = subEntry.I_76,
                            I_100 = subEntry.I_92,
                            I_58 = subEntry.I_50,
                            I_76 = subEntry.I_68,
                            I_02 = subEntry.I_02,
                            I_06 = subEntry.I_06,
                            F_08 = subEntry.F_08,
                            I_82 = subEntry.I_74,
                            I_88 = subEntry.I_80,
                            I_96 = subEntry.I_88
                        });
                    }

                    //Adding the XV2 specific subentries
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[0].Clone(7));
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[0].Clone(8));
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[6].Clone(9));
                    newEntries[idx].Type0Entries[6] = newEntries[idx].Type0Entries[0].Clone(6);
                    
                }


                BDM_Type = BDM_Type.XV2_0;
                BDM_Entries = newEntries;
            }
            else
            {
                throw new InvalidOperationException(String.Format("Cannot convert BDM Type {0} into BDM Type XV2_0.", BDM_Type));
            }
        }

        public void ConvertXv1ToXv2_0(int skillID)
        {
            if (BDM_Type == BDM_Type.XV1)
            {

                List<BDM_Entry> newEntries = new List<BDM_Entry>();

                foreach (var entry in BDM_Entries)
                {
                    int idx = newEntries.Count;
                    newEntries.Add(new BDM_Entry() { Type0Entries = new List<Type0SubEntry>(), I_00 = entry.I_00 });

                    foreach (var subEntry in entry.Type1Entries)
                    {
                        int skillID1 = subEntry.I_16;
                        int skillID2 = subEntry.I_24;
                        if(skillID != -1)
                        {
                            if (subEntry.I_20 >= 5 && subEntry.I_20 < 10)
                            {
                                skillID1 = skillID;
                            }
                            if (subEntry.I_28 >= 5 && subEntry.I_28 < 10)
                            {
                                skillID2 = skillID;
                            }
                        }
                        
                        
                        newEntries[idx].Type0Entries.Add(new Type0SubEntry()
                        {
                            Index = subEntry.Index,
                            I_00 = subEntry.I_00,
                            I_102 = subEntry.I_94,
                            I_04 = subEntry.I_04,
                            I_94 = subEntry.I_86,
                            I_12 = subEntry.I_12,
                            I_14 = subEntry.I_14,
                            I_16 = (short)skillID1,
                            I_18 = subEntry.I_18,
                            I_20 = subEntry.I_20,
                            I_22 = subEntry.I_22,
                            I_24 = (short)skillID2,
                            I_26 = subEntry.I_26,
                            I_28 = subEntry.I_28,
                            I_32 = -1,
                            F_40 = subEntry.F_32,
                            F_44 = subEntry.F_36,
                            I_48 = subEntry.I_40,
                            I_50 = subEntry.I_42,
                            I_52 = subEntry.I_44,
                            I_56 = subEntry.I_48,
                            I_54 = subEntry.I_46,
                            I_78 = subEntry.I_70,
                            F_60 = subEntry.F_52,
                            F_64 = subEntry.F_56,
                            F_68 = subEntry.F_60,
                            F_72 = subEntry.F_64,
                            I_80 = subEntry.I_72,
                            I_86 = subEntry.I_78,
                            I_104 = subEntry.I_96,
                            I_106 = subEntry.I_98,
                            I_108 = subEntry.I_100,
                            I_110 = subEntry.I_102,
                            I_112 = -1,
                            I_84 = subEntry.I_76,
                            I_100 = subEntry.I_92,
                            I_58 = subEntry.I_50,
                            I_76 = subEntry.I_68,
                            I_02 = subEntry.I_02,
                            I_06 = subEntry.I_06,
                            F_08 = subEntry.F_08,
                            I_82 = subEntry.I_74,
                            I_88 = subEntry.I_80,
                            I_96 = subEntry.I_88
                        });
                    }

                    //Adding the XV2 specific subentries
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[0].Clone(7));
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[0].Clone(8));
                    newEntries[idx].Type0Entries.Add(newEntries[idx].Type0Entries[6].Clone(9));
                    newEntries[idx].Type0Entries[6] = newEntries[idx].Type0Entries[0].Clone(6);

                }


                BDM_Type = BDM_Type.XV2_0;
                BDM_Entries = newEntries;
            }
            else
            {
                throw new Exception("BDM file was not in Xenoverse 1 format.");
            }
        }
        
        public int IndexOf(int ID)
        {
            if(BDM_Entries != null)
            {
                for(int i = 0; i < BDM_Entries.Count; i++)
                {
                    if(BDM_Entries[i].I_00 == ID)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static UInt16 DamageTypeXv1ToXv2(UInt16 damageType)
        {
            int _type = damageType;

            if (damageType == 6) _type += 1;
            if (damageType >= 7) _type += 2;

            return (ushort)_type;
        }
        
        public BDM_Entry GetEntryClone(int id)
        {
            if (BDM_Entries == null) throw new Exception("BDM_Entries was null.");

            foreach(var entry in BDM_Entries)
            {
                if (entry.I_00 == id) return entry.CloneType0();
            }

            throw new Exception("Could not find the BDM_Entry with ID " + id);
        }

        public int NextID(int minID = 500)
        {
            int id = minID;

            while(IndexOf(id) != -1)
            {
                id++;
            }

            return id;
        }
        
        public BDM_Entry GetEntry(int id)
        {
            int idx = IndexOf(id);
            if (idx == -1) throw new Exception("Could not find a BDM_Entry the id " + id);
            return BDM_Entries[idx];
        }

        public bool IsNull()
        {
            return (BDM_Entries.Count == 0);
        }
    }

    public class BDM_Entry : IInstallable, INotifyPropertyChanged
    {
        #region INotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region WrapperProperties
        [YAXDontSerialize]
        public int SortID
        {
            get
            {
                return int.Parse(Index);
            }
        }
        [YAXDontSerialize]
        public int ID { get { return int.Parse(Index); } set { Index = value.ToString(); NotifyPropertyChanged("ID"); } }

        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //Wrapper for I_00

        [YAXDontSerialize]
        public int I_00
        {
            get
            {
                int value;
                if(int.TryParse(Index, out value))
                {
                    return value;
                }
                else
                {
                    throw new FormatException("BDM_Entry.Index is not a numeric value. Get failed.");
                }
            }
            set
            {
                Index = value.ToString();
            }
        }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("BDM_Type0_Entries")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "T0_SubEntry")]
        public List<Type0SubEntry> Type0Entries { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("BDM_Type1_Entries")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "T1_SubEntry")]
        public List<Type1SubEntry> Type1Entries { get; set; }

        public BDM_Entry CloneType0()
        {
            if (Type0Entries == null) throw new Exception("Cannot clone BDM Entry as it is not Type0.");
            List<Type0SubEntry> subs = new List<Type0SubEntry>();

            foreach(var sub in Type0Entries)
            {
                subs.Add(sub.Clone());
            }

            return new BDM_Entry()
            {
                I_00 = I_00,
                Type0Entries = subs
            };
        }
    }

    [YAXSerializeAs("BDM_Type0")]
    public class Type0SubEntry
    {

        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public int Index { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Type")]
        public UInt16 I_00 { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Secondary_Type")]
        public string I_102 { get; set; } //uint16
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Amount")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Special")]
        public UInt16 I_94 { get; set; }
        [YAXAttributeFor("Sound")]
        [YAXSerializeAs("Type")]
        public UInt16 I_12 { get; set; }
        [YAXAttributeFor("Sound")]
        [YAXSerializeAs("CueID")]
        public short I_14 { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("ID")]
        public short I_16 { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("Skill_ID")]
        public short I_18 { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("Skill_Type")]
        public short I_20 { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("ID")]
        public short I_24 { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("Skill_ID")]
        public short I_26 { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("Skill_Type")]
        public short I_28 { get; set; }
        [YAXAttributeFor("Effect_3")]
        [YAXSerializeAs("ID")]
        public short I_32 { get; set; }
        [YAXAttributeFor("Effect_3")]
        [YAXSerializeAs("Skill_ID")]
        public short I_34 { get; set; }
        [YAXAttributeFor("Effect_3")]
        [YAXSerializeAs("Skill_Type")]
        public short I_36 { get; set; }
        


        [YAXAttributeFor("Pushback")]
        [YAXSerializeAs("Strength")]
        [YAXFormat("0.0###########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Pushback")]
        [YAXSerializeAs("Acceleration")]
        [YAXFormat("0.0###########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("User_Stun")]
        [YAXSerializeAs("value")]
        public UInt16 I_48 { get; set; }
        [YAXAttributeFor("Victim_Stun")]
        [YAXSerializeAs("value")]
        public UInt16 I_50 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Duration")]
        public UInt16 I_52 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Ground_Impact")]
        public UInt16 I_56 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Recovery_After_Impact")]
        public UInt16 I_54 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Gravity")]
        public UInt16 I_78 { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("Knockback_Drag")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_72 { get; set; }


        [YAXAttributeFor("Victim_Invincibility")]
        [YAXSerializeAs("Time")]
        public short I_80 { get; set; }
        [YAXAttributeFor("Aliment_Type")]
        [YAXSerializeAs("value")]
        public short I_86 { get; set; }
        [YAXAttributeFor("Camera_Shake")]
        [YAXSerializeAs("Type")]
        public SByte I_104 { get; set; } //Flag?
        [YAXAttributeFor("Camera_Shake")]
        [YAXSerializeAs("Time")]
        public UInt16 I_106 { get; set; }
        [YAXAttributeFor("User Screen Flash Transperancy")]
        [YAXSerializeAs("value")]
        public short I_108 { get; set; }
        [YAXAttributeFor("Victim Screen Flash Transparency")]
        [YAXSerializeAs("value")]
        public short I_110 { get; set; }
        [YAXAttributeFor("Stamina Broken BDM ID Override")]
        [YAXSerializeAs("value")]
        public short I_112 { get; set; }
        [YAXAttributeFor("Time Before Z-Vanish Enabled")]
        [YAXSerializeAs("value")]
        public UInt16 I_114 { get; set; }
        [YAXAttributeFor("User Animation")]
        [YAXSerializeAs("Time")]
        public UInt16 I_116 { get; set; }
        [YAXAttributeFor("Victim Animation")]
        [YAXSerializeAs("Time")]
        public UInt16 I_118 { get; set; }
        [YAXAttributeFor("User Animation")]
        [YAXSerializeAs("Speed")]
        [YAXFormat("0.0###########")]
        public float F_120 { get; set; }
        [YAXAttributeFor("Victim Animation")]
        [YAXSerializeAs("Speed")]
        [YAXFormat("0.0###########")]
        public float F_124 { get; set; }
        [YAXAttributeFor("Transformation")]
        [YAXSerializeAs("value")]
        public UInt16 I_84 { get; set; }
        [YAXAttributeFor("Stumble")]
        [YAXSerializeAs("value")]
        public UInt16 I_100 { get; set; }


        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public UInt16 I_02 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public UInt16 I_06 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public UInt16 I_22 { get; set; }
        [YAXAttributeFor("I_30")]
        [YAXSerializeAs("value")]
        public UInt16 I_30 { get; set; }
        [YAXAttributeFor("I_38")]
        [YAXSerializeAs("value")]
        public UInt16 I_38 { get; set; }
        [YAXAttributeFor("I_58")]
        [YAXSerializeAs("value")]
        public UInt16 I_58 { get; set; }


        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public UInt16 I_76 { get; set; }
        [YAXAttributeFor("I_82")]
        [YAXSerializeAs("value")]
        public UInt16 I_82 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("int16")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public UInt16[] I_88 { get; set; } //Size 3
        [YAXAttributeFor("I_96")]
        [YAXSerializeAs("int16")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public UInt16[] I_96 { get; set; } //size 2

        public Type0SubEntry Clone(int index = -1)
        {
            if(index == -1)
            {
                index = Index;
            }

            return new Type0SubEntry()
            {
                Index = index,
                I_00 = I_00,
                I_02 = I_02,
                I_04 = I_04,
                I_06 = I_06,
                I_100 = I_100,
                I_102 = I_102,
                I_104 = I_104,
                I_12 = I_12,
                I_14 = I_14,
                I_16 = I_16,
                I_18 = I_18,
                I_20 = I_20,
                I_22 = I_22,
                I_24 = I_24,
                I_26 = I_26,
                I_28 = I_28,
                I_30 = I_30,
                I_50 = I_50,
                I_76 = I_76,
                I_78 = I_78,
                I_80 = I_80,
                I_86 = I_86,
                I_88 = I_88,
                I_94 = I_94,
                I_96 = I_96,
                F_08 = F_08,
                F_60 = F_60,
                F_64 = F_64,
                I_106 = I_106,
                I_108 = I_108,
                I_110 = I_110,
                I_112 = I_112,
                I_114 = I_114,
                I_116 = I_116,
                I_118 = I_118,
                I_32 = I_32,
                I_34 = I_34,
                I_36 = I_36,
                I_38 = I_38,
                I_52 = I_52,
                I_54 = I_54,
                I_56 = I_56,
                I_58 = I_58,
                I_82 = I_82,
                I_84 = I_84,
                F_120 = F_120,
                F_124 = F_124,
                F_40 = F_40,
                F_44 = F_44,
                I_48 = I_48,
                F_68 = F_68,
                F_72 = F_72
            };
        }

    }

    [YAXSerializeAs("BDM_Type1")]
    public class Type1SubEntry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public int Index { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Type")]
        public UInt16 I_00 { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Secondary_Type")]
        public string I_94 { get; set; } //uint16
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Amount")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeFor("Damage")]
        [YAXSerializeAs("Special")]
        public UInt16 I_86 { get; set; }
        [YAXAttributeFor("Sound")]
        [YAXSerializeAs("Type")]
        public UInt16 I_12 { get; set; }
        [YAXAttributeFor("Sound")]
        [YAXSerializeAs("CueID")]
        public short I_14 { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("ID")]
        public short I_16 { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("Skill_ID")]
        public short I_18 { get; set; }
        [YAXAttributeFor("Effect_1")]
        [YAXSerializeAs("Skill_Type")]
        public short I_20 { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("ID")]
        public short I_24 { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("Skill_ID")]
        public short I_26 { get; set; }
        [YAXAttributeFor("Effect_2")]
        [YAXSerializeAs("Skill_Type")]
        public short I_28 { get; set; }


        [YAXAttributeFor("Pushback")]
        [YAXSerializeAs("Strength")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Pushback")]
        [YAXSerializeAs("Acceleration")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("User_Stun")]
        [YAXSerializeAs("value")]
        public ushort I_40 { get; set; }
        [YAXAttributeFor("Victim_Stun")]
        [YAXSerializeAs("value")]
        public ushort I_42 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Duration")]
        public ushort I_44 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Ground_Impact")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Recovery_After_Impact")]
        public ushort I_46 { get; set; }
        [YAXAttributeFor("Knockback_Time")]
        [YAXSerializeAs("Gravity")]
        public ushort I_70 { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Knockback_Strength")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("Knockback_Drag")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("Victim_Invincibility")]
        [YAXSerializeAs("Time")]
        public short I_72 { get; set; }
        [YAXAttributeFor("Aliment_Type")]
        [YAXSerializeAs("value")]
        public short I_78 { get; set; }
        [YAXAttributeFor("Camera_Shake_Type")]
        [YAXSerializeAs("value")]
        public SByte I_96 { get; set; } //Flag?
        [YAXAttributeFor("Camera_Shake_Time")]
        [YAXSerializeAs("value")]
        public ushort I_98 { get; set; }
        [YAXAttributeFor("User Screen Flash Transperancy")]
        [YAXSerializeAs("value")]
        public short I_100 { get; set; }
        [YAXAttributeFor("Victim Screen Flash Transparency")]
        [YAXSerializeAs("value")]
        public short I_102 { get; set; }
        [YAXAttributeFor("Transformation")]
        [YAXSerializeAs("value")]
        public ushort I_76 { get; set; }
        [YAXAttributeFor("Stumble")]
        [YAXSerializeAs("value")]
        public ushort I_92 { get; set; }


        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public UInt16 I_22 { get; set; }
        [YAXAttributeFor("I_30")]
        [YAXSerializeAs("value")]
        public UInt16 I_30 { get; set; }
        [YAXAttributeFor("I_50")]
        [YAXSerializeAs("value")]
        public UInt16 I_50 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public UInt16 I_68 { get; set; }
        [YAXAttributeFor("I_74")]
        [YAXSerializeAs("value")]
        public ushort I_74 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("int16")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_80 { get; set; } //Size 3
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("int16")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_88 { get; set; } //size 2
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        public int I_104 { get; set; }

        public Type1SubEntry Clone(int index = -1)
        {
            if(index == -1)
            {
                index = Index;
            }

            return new Type1SubEntry()
            {
                Index = index,
                I_00 = I_00,
                I_02 = I_02,
                I_04 = I_04,
                I_06 = I_06,
                I_100 = I_100,
                I_102 = I_102,
                I_104 = I_104,
                I_12 = I_12,
                I_14 = I_14,
                I_16 = I_16,
                I_18 = I_18,
                I_20 = I_20,
                I_22 = I_22,
                I_24 = I_24,
                I_26 = I_26,
                I_28 = I_28,
                I_30 = I_30,
                I_40 = I_40,
                I_42 = I_42,
                I_44 = I_44,
                I_46 =I_46,
                I_48 = I_48,
                I_50 = I_50,
                I_68 = I_68,
                I_70 = I_70,
                I_72 = I_72,
                I_74 = I_74,
                I_76 = I_76,
                I_78 = I_78,
                I_80 = I_80,
                I_86 = I_86,
                I_88 = I_88,
                I_92 = I_92,
                I_94 = I_94,
                I_96 = I_96,
                I_98 = I_98,
                F_08 = F_08,
                F_32 = F_32,
                F_36 = F_36,
                F_52 = F_52,
                F_56 = F_56,
                F_60 = F_60,
                F_64 = F_64
            };
        }
    }

}
