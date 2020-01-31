using System;
using System.Collections.Generic;
using Xv2CoreLib.BSA;
using YAXLib;

namespace Xv2CoreLib.BSA_XV1
{
    [YAXSerializeAs("BSA")]
    public class BSA_File
    {
        [YAXAttributeForClass]
        public Int64 I_08 = 0;
        [YAXAttributeForClass]
        public Int16 I_16 = 0;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Entry")]
        public List<BSA_Entry> BSA_Entries { get; set; }

        public BSA.BSA_File ConvertToXv2(int skillID)
        {
            List<BSA.BSA_Entry> xv2BsaEntries = new List<BSA.BSA_Entry>();

            foreach(var entry in BSA_Entries)
            {
                xv2BsaEntries.Add(entry.ConvertToXv2(skillID));
            }

            return new BSA.BSA_File()
            {
                I_08 = I_08,
                I_16 = I_16,
                BSA_Entries = xv2BsaEntries
            };
        }
    }

    [YAXSerializeAs("BSA_Entry")]
    public class BSA_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int Index { get; set; }

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("a")]
        public byte I_16_a { get; set; }
        [YAXAttributeFor("I_16")]
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
        public short I_26 { get; set; }
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("ImpactProjectile")]
        public short I_28 { get; set; }
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("ImpactEnemy")]
        public short I_30 { get; set; }
        [YAXAttributeFor("EntryPassOn_When")]
        [YAXSerializeAs("ImpactGround")]
        public short I_32 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("AfterEffects")]
        public BSA_SubEntries SubEntries { get; set; }

        //Types
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BsaEntryPassing")]
        public List<BSA_Type0> Type0 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Movement")]
        public List<BSA_Type1> Type1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Type2")]
        public List<BSA_Type2> Type2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Hitbox")]
        public List<BSA_Type3> Type3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Type4")]
        public List<BSA_Type4> Type4 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Effect")]
        public List<BSA_Type6> Type6 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Sound")]
        public List<BSA_Type7> Type7 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BSA_Type8")]
        public List<BSA_Type8> Type8 { get; set; }

        public BSA.BSA_Entry ConvertToXv2(int skillID)
        {
            BSA.BSA_SubEntries Xv2SubEntries = null;
            if(SubEntries != null)
            {
                Xv2SubEntries = SubEntries.ConvertToXv2(skillID);
            }
            if(skillID != -1)
            {
                Type6 = BSA.BSA_Type6.ChangeSkillId(Type6, skillID);
            }

            return new BSA.BSA_Entry()
            {
                Index = Index.ToString(),
                I_00 = I_00,
                I_16_a = I_16_a,
                I_16_b = I_16_b,
                I_17 = I_17,
                I_18 = I_18,
                I_22 = I_22,
                I_24 = I_24,
                I_26 = I_26.ToString(),
                I_28 = "-1",
                I_30 = "-1",
                I_32 = "-1",
                I_40 = new int[3],
                SubEntries = Xv2SubEntries,
                Type0 = Type0,
                Type1 = BSA_Type1.ConvertToXv2(Type1),
                Type2 = Type2,
                Type3 = Type3,
                Type4 = Type4,
                Type6 = Type6,
                Type7 = Type7,
                Type8 = Type8
            };
        }
    }

    [YAXSerializeAs("AfterEffects")]
    public class BSA_SubEntries
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Collision")]
        public List<BSA.Unk1> Unk1 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Expiration")]
        public List<BSA.Unk2> Unk2 { get; set; }

        public BSA.BSA_SubEntries ConvertToXv2(int skillID)
        {
            if(skillID != -1)
            {
                Unk1 = BSA.Unk1.ChangeSkillId(Unk1, skillID);
            }

            return new BSA.BSA_SubEntries()
            {
                Unk1 = Unk1,
                Unk2 = Unk2
            };
        }
    }
    
    //Types
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

        public static List<BSA.BSA_Type1> ConvertToXv2(List<BSA_Type1> types)
        {
            if (types == null) return null;
            List<BSA.BSA_Type1> xv2Types = new List<BSA.BSA_Type1>();

            foreach(var type in types)
            {
                xv2Types.Add(new BSA.BSA_Type1()
                {
                    I_00 = type.I_00,
                    Duration = type.Duration,
                    F_04 = type.F_04,
                    F_08 = type.F_08,
                    F_12 = type.F_12,
                    F_16 = type.F_16,
                    F_20 = type.F_20,
                    F_24 = type.F_24,
                    F_28 = type.F_28,
                    StartTime = type.StartTime
                });
            }
            return xv2Types;
        }

    }
    
}
