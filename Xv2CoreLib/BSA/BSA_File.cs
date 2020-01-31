using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.BSA
{
    public enum EepkType
    {
        Common = 0,
        StageBG = 1,
        Character = 2,
        AwokenSkill = 3,
        SuperSkill = 5,
        UltimateSkill = 6,
        EvasiveSkill = 7,
        KiBlastSkill = 9,
        Stage = 11
    }

    public enum AcbType
    {
        Common_SE = 0,
        Skill_SE = 3
    }
    
    public enum Switch
    {
        On = 0,
        Off = 1
    }

    [YAXSerializeAs("BSA")]
    public class BSA_File : ISorting, IIsNull
    {
        [YAXAttributeForClass]
        public Int64 I_08 = 0;
        [YAXAttributeForClass]
        public Int16 I_16 = 0;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Entry")]
        public List<BSA_Entry> BSA_Entries { get; set; } = new List<BSA_Entry>();

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static BSA_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetBsaFile();
        }

        public static BSA_File Load(string path)
        {
            return new Parser(path, false).GetBsaFile();
        }

        public void Save(string path)
        {
            new Deserializer(this, path);
        }

        public void SortEntries()
        {
            BSA_Entries.Sort((x, y) => x.SortID - y.SortID);
        }

        public void AddEntry(int id, BSA_Entry entry)
        {
            for(int i = 0; i < BSA_Entries.Count; i++)
            {
                if(int.Parse(BSA_Entries[i].Index) == id)
                {
                    BSA_Entries[i] = entry;
                    return;
                }
            }

            BSA_Entries.Add(entry);
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public bool IsNull()
        {
            return (BSA_Entries.Count == 0);
        }
    }

    [YAXSerializeAs("BSA_Entry")]
    public class BSA_Entry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } set { Index = value.ToString(); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //int32

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("ImpactPropeties")]
        [YAXSerializeAs("a")]
        public byte I_16_a { get; set; }
        [YAXAttributeFor("ImpactPropeties")]
        [YAXSerializeAs("b")]
        public byte I_16_b { get; set; }
        [YAXAttributeFor("I_17")]
        [YAXSerializeAs("value")]
        public byte I_17 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public int I_18 { get; set; }
        [YAXAttributeFor("Lifetime")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public ushort I_24 { get; set; }
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("Expires")]
        public string I_26 { get; set; } //short
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("ImpactProjectile")]
        public string I_28 { get; set; } //short
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("ImpactEnemy")]
        public string I_30 { get; set; } //short
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("ImpactGround")]
        public string I_32 { get; set; } //short
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_40 { get; set; } // size 3

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("AfterEffects")]
        [BindingSubList]
        public BSA_SubEntries SubEntries { get; set; }

        //Types
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BsaEntryPassing")]
        [BindingSubList]
        public List<BSA_Type0> Type0 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Movement")]
        [BindingSubList]
        public List<BSA_Type1> Type1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Type2")]
        [BindingSubList]
        public List<BSA_Type2> Type2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Hitbox")]
        [BindingSubList]
        public List<BSA_Type3> Type3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Deflection")]
        [BindingSubList]
        public List<BSA_Type4> Type4 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Effect")]
        [BindingSubList]
        public List<BSA_Type6> Type6 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Sound")]
        [BindingSubList]
        public List<BSA_Type7> Type7 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Type8")]
        [BindingSubList]
        public List<BSA_Type8> Type8 { get; set; }


        //Properties
        [YAXDontSerialize]
        public ushort Expires { get { return (I_26 == "-1") ? ushort.MaxValue : ushort.Parse(I_26); } set { I_26 = value.ToString(); } }
        [YAXDontSerialize]
        public ushort ImpactProjectile { get { return (I_28 == "-1") ? ushort.MaxValue : ushort.Parse(I_28); } set { I_28 = value.ToString(); } }
        [YAXDontSerialize]
        public ushort ImpactEnemy { get { return (I_30 == "-1") ? ushort.MaxValue : ushort.Parse(I_30); } set { I_30 = value.ToString(); } }
        [YAXDontSerialize]
        public ushort ImpactGround { get { return (I_32 == "-1") ? ushort.MaxValue : ushort.Parse(I_32); } set { I_32 = value.ToString(); } }
    }

    [YAXSerializeAs("AfterEffects")]
    public class BSA_SubEntries
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Collision")]
        [BindingSubList]
        public List<Unk1> Unk1 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Expiration")]
        [BindingSubList]
        public List<Unk2> Unk2 { get; set; }
    }

    [YAXSerializeAs("Collision")]
    [BindingSubClass]
    public class Unk1
    {
        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("Type")]
        public EepkType I_00 { get; set; } //int16
        [YAXAttributeFor("Skill_ID")]
        [YAXSerializeAs("value")]
        public string I_02 { get; set; } //ushort
        [YAXAttributeFor("Effect_ID")]
        [YAXSerializeAs("value")]
        public string I_04 { get; set; } //int
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }

        //Propeties
        [YAXDontSerialize]
        public ushort SkillID { get { return  ushort.Parse(I_02); } set { I_02 = value.ToString(); } }
        [YAXDontSerialize]
        public int EffectID { get { return int.Parse(I_04); } set { I_04 = value.ToString(); } }

        public static List<Unk1> ChangeSkillId(List<Unk1> types, int skillID)
        {
            if (types == null) return null;

            for (int i = 0; i < types.Count; i++)
            {
                switch (types[i].I_00)
                {
                    case EepkType.AwokenSkill:
                    case EepkType.SuperSkill:
                    case EepkType.UltimateSkill:
                    case EepkType.EvasiveSkill:
                    case EepkType.KiBlastSkill:
                        types[i].SkillID = (ushort)skillID;
                        break;
                }
            }

            return types;
        }
        
    }

    [YAXSerializeAs("Expiration")]
    public class Unk2
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public UInt16 I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public UInt16 I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public UInt16 I_06 { get; set; }
    }
    
    //Types
    [YAXSerializeAs("BsaEntryPassing")]
    [BindingSubClass]
    public class BSA_Type0
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public short StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public short Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAttributeFor("Main_Condition")]
        [YAXSerializeAs("value")]
        public string I_02 { get; set; }
        [YAXAttributeFor("BSA_Entry")]
        [YAXSerializeAs("ID")]
        public string I_04 { get; set; } //ushort
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public short I_06 { get; set; }
        [YAXAttributeFor("Bac_Condition")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }

        //Props
        [YAXDontSerialize]
        public ushort BSA_EntryID { get { return (I_04 == "-1") ? ushort.MaxValue : ushort.Parse(I_04); } set { I_04 = value.ToString(); } }

    }

    [YAXSerializeAs("Movement")]
    public class BSA_Type1
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public short StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public short Duration { get; set; }
        [YAXAttributeFor("Motion_Flags")]
        [YAXSerializeAs("value")]
        public string I_00 { get; set; }
        [YAXAttributeFor("Speed")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#######")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Speed")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#######")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Speed")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#######")]
        public float F_04 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Acceleration")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#######")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Acceleration")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#######")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Acceleration")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#######")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Falloff Strength")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Spread Direction")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#######")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Spread Direction")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#######")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Spread Direction")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#######")]
        public float F_44 { get; set; }
    }

    [YAXSerializeAs("BSA_Type2")]
    public class BSA_Type2
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public short StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public short Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public short I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public short I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public short I_06 { get; set; }
    }

    [YAXSerializeAs("Hitbox")]
    [BindingSubClass]
    public class BSA_Type3
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public short StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public short Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public UInt16 I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public UInt16 I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("a")]
        public byte I_06_a { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("b")]
        public byte I_06_b { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("c")]
        public byte I_06_c { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("d")]
        public byte I_06_d { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#######")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Hitbox_Scale")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Hit_Amount")]
        [YAXSerializeAs("value")]
        public UInt16 I_48 { get; set; }
        [YAXAttributeFor("Hitbox_Lifetime")]
        [YAXSerializeAs("value")]
        public UInt16 I_50 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public UInt16 I_52 { get; set; }
        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("value")]
        public UInt16 I_54 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public UInt16 I_56 { get; set; }
        [YAXAttributeFor("BDM_ID")]
        [YAXSerializeAs("FirstHit")]
        public string I_58 { get; set; } //short
        [YAXAttributeFor("BDM_ID")]
        [YAXSerializeAs("MultipleHits")]
        public string I_60 { get; set; } //short
        [YAXAttributeFor("BDM_ID")]
        [YAXSerializeAs("LastHit")]
        public string I_62 { get; set; } //short

        //Props
        [YAXDontSerialize]
        public short FirstHit { get { return short.Parse(I_58); } set { I_58 = value.ToString(); } }
        [YAXDontSerialize]
        public short MultipleHits { get { return short.Parse(I_60); } set { I_60 = value.ToString(); } }
        [YAXDontSerialize]
        public short LastHit { get { return short.Parse(I_62); } set { I_62 = value.ToString(); } }
    }

    [YAXSerializeAs("Deflection")]
    public class BSA_Type4
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public short StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public short Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }

        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }

        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public UInt16 I_48 { get; set; }
        [YAXAttributeFor("I_50")]
        [YAXSerializeAs("value")]
        public UInt16 I_50 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public UInt16 I_52 { get; set; }
        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("value")]
        public UInt16 I_54 { get; set; }
    }

    [YAXSerializeAs("Effect")]
    [BindingSubClass]
    public class BSA_Type6
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public short StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public short Duration { get; set; }
        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("Type")]
        public EepkType I_00 { get; set; } //Int16
        [YAXAttributeFor("Skill ID")]
        [YAXSerializeAs("value")]
        public string I_02 { get; set; } //ushort
        [YAXAttributeFor("Effect")]
        [YAXSerializeAs("ID")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public UInt16 I_06 { get; set; }
        [YAXAttributeFor("Effect")]
        [YAXSerializeAs("Switch")]
        public Switch I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public UInt16 I_10 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }

        //Props
        [YAXDontSerialize]
        public ushort SkillID { get { return ushort.Parse(I_02); } set { I_02 = value.ToString(); } }

        public bool IsSkillEepk()
        {
            switch (I_00)
            {
                case EepkType.SuperSkill:
                case EepkType.UltimateSkill:
                case EepkType.EvasiveSkill:
                case EepkType.AwokenSkill:
                case EepkType.KiBlastSkill:
                    return true;
            }
            return false;
        }

        public static List<BSA_Type6> ChangeSkillId(List<BSA_Type6> types, int skillID)
        {
            if (types == null) return null;

            for (int i = 0; i < types.Count; i++)
            {
                switch (types[i].I_00)
                {
                    case EepkType.SuperSkill:
                    case EepkType.UltimateSkill:
                    case EepkType.EvasiveSkill:
                    case EepkType.AwokenSkill:
                    case EepkType.KiBlastSkill:
                        types[i].SkillID = (ushort)skillID;
                        break;
                }
            }

            return types;
        }
        
    }

    [YAXSerializeAs("Sound")]
    public class BSA_Type7
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public short StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public short Duration { get; set; }
        [YAXAttributeFor("ACB_File")]
        [YAXSerializeAs("value")]
        public AcbType I_00 { get; set; } //int16
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public UInt16 I_02 { get; set; }
        [YAXAttributeFor("Cue ID")]
        [YAXSerializeAs("value")]
        public short I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public UInt16 I_06 { get; set; }
        
        

    }

    [YAXSerializeAs("BSA_Type8")]
    public class BSA_Type8
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("frames")]
        public short StartTime { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("frames")]
        public short Duration { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public UInt16 I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public UInt16 I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
    }

}
