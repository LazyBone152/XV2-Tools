using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BAC_XV1
{
    [YAXSerializeAs("BAC")]
    public class BAC_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("values")]
        public int[] I_20 { get; set; } // size 3
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("values")]
        [YAXFormat("0.0#############")]
        public float[] F_32 { get; set; } // size 12
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("values")]
        public int[] I_80 { get; set; } // size 4

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BacEntry")]
        public List<BAC_Entry> BacEntries { get; set; }


        public BAC.BAC_File ConvertToXv2(int skillID)
        {
            List<BAC.BAC_Entry> xv2BacEntries = new List<BAC.BAC_Entry>();

            for(int i = 0; i < BacEntries.Count; i++)
            {
                xv2BacEntries.Add(BacEntries[i].ConvertToXv2(skillID));
            }

            return new BAC.BAC_File()
            {
                I_20 = I_20,
                I_80 = I_80,
                F_32 = F_32,
                BacEntries = new System.Collections.ObjectModel.ObservableCollection<BAC.BAC_Entry>(xv2BacEntries)
            };

        }

    }

    public class BAC_Entry
    {
        [YAXAttributeForClass]
        public string Index { get; set; }
        [YAXAttributeForClass]
        [YAXFormat("X")]
        [YAXSerializeAs("Flag")]
        public string FlagStr
        {
            get
            {
                return HexConverter.GetHexString((int)Flag);
            }
            set
            {
                Flag = (BAC.BAC_Entry.Flags)HexConverter.ToInt32(value);
            }
        }

        [YAXDontSerialize]
        public BAC.BAC_Entry.Flags Flag { get; set; }


        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation")]
        public List<BAC.BAC_Type0> Type0 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Hitbox")]
        public List<BAC.BAC_Type1> Type1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Movement")]
        public List<BAC.BAC_Type2> Type2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Invulnerability")]
        public List<BAC.BAC_Type3> Type3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TimeScale")]
        public List<BAC.BAC_Type4> Type4 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Tracking")]
        public List<BAC.BAC_Type5> Type5 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ChargeControl")]
        public List<BAC.BAC_Type6> Type6 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BcmCallback")]
        public List<BAC.BAC_Type7> Type7 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Effect")]
        public List<BAC.BAC_Type8> Type8 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Projectile")]
        public List<BAC.BAC_Type9> Type9 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Camera")]
        public List<BAC_Type10> Type10 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Sound")]
        public List<BAC.BAC_Type11> Type11 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BAC_Type12")]
        public List<BAC.BAC_Type12> Type12 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BcsPartSetInvisibility")]
        public List<BAC.BAC_Type13> Type13 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BoneRotation")]
        public List<BAC.BAC_Type14> Type14 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "GeneralControl")]
        public List<BAC_Type15> Type15 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ScreenEffect")]
        public List<BAC.BAC_Type16> Type16 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ThrowHandler")]
        public List<BAC.BAC_Type17> Type17 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BAC_Type18")]
        public List<BAC.BAC_Type18> Type18 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Aura")]
        public List<BAC.BAC_Type19> Type19 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "HomingMovement")]
        public List<BAC.BAC_Type20> Type20 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BAC_Type21")]
        public List<BAC.BAC_Type21> Type21 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BAC_Type22")]
        public List<BAC.BAC_Type22> Type22 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "TransparencyEffect")]
        public List<BAC.BAC_Type23> Type23 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BAC_Type24")]
        public List<BAC.BAC_Type24> Type24 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("Types")]
        [YAXAttributeFor("HasDummy")]
        public List<int> TypeDummy { get; set; }

        public BAC.BAC_Entry ConvertToXv2(int skillID)
        {
            if(skillID != -1)
            {
                Type8 = BAC.BAC_Type8.ChangeSkillId(Type8, skillID);
                Type9 = BAC.BAC_Type9.ChangeSkillId(Type9, skillID);
            }

            return new BAC.BAC_Entry()
            {
                Index = Index,
                Flag = Flag,
                Type0 = Type0,
                Type1 = Type1,
                Type2 = Type2,
                Type3 = Type3,
                Type4 = Type4,
                Type5 = Type5,
                Type6 = Type6,
                Type7 = Type7,
                Type8 = Type8,
                Type9 = Type9,
                Type10 = BAC_Type10.ConvertToXv2(Type10),
                Type11 = Type11,
                Type12 = Type12,
                Type13 = Type13,
                Type14 = Type14,
                Type15 = BAC_Type15.ConvertToXv2(Type15),
                Type16 = Type16,
                Type17 = Type17,
                Type18 = Type18,
                Type19 = Type19,
                Type20 = Type20,
                Type21 = Type21,
                Type22 = Type22,
                Type23 = Type23,
                Type24 = Type24,
            };
        }

    }

    [YAXSerializeAs("Camera")]
    public class BAC_Type10
    {

        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("value")]
        public short I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("FLAGS")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("EAN_TO_USE")]
        [YAXSerializeAs("value")]
        public BAC.BAC_Type10.EanType I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("EAN_Index")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("StartFrame")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }

        

        public static List<BAC_Type10> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type10> Type10 = new List<BAC_Type10>();

            for (int i = 0; i < count; i++)
            {
                Type10.Add(new BAC_Type10()
                {
                    I_00 = BitConverter.ToInt16(rawBytes, offset + 0),
                    I_02 = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToUInt16(rawBytes, offset + 4),
                    I_06 = BitConverter.ToUInt16(rawBytes, offset + 6),
                    I_08 = (BAC.BAC_Type10.EanType)BitConverter.ToInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                    I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                    I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                    I_16 = BitConverter.ToUInt16(rawBytes, offset + 16),
                    I_18 = BitConverter.ToUInt16(rawBytes, offset + 18)
                });

                offset += 20;
            }

            return Type10;
        }

        public static List<byte> Write(List<BAC_Type10> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.I_00));
                bytes.AddRange(BitConverter.GetBytes(type.I_02));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.I_06));
                bytes.AddRange(BitConverter.GetBytes((ushort)type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.I_12));
                bytes.AddRange(BitConverter.GetBytes(type.I_14));
                bytes.AddRange(BitConverter.GetBytes(type.I_16));
                bytes.AddRange(BitConverter.GetBytes(type.I_18));
            }

            return bytes;
        }

        public static List<BAC.BAC_Type10> ConvertToXv2(List<BAC_Type10> type10s)
        {
            List<BAC.BAC_Type10> xv2Type10s = new List<BAC.BAC_Type10>();

            if (type10s == null) return null;

            foreach(var type in type10s)
            {
                xv2Type10s.Add(new BAC.BAC_Type10()
                {
                    StartTime = type.I_00,
                    Duration = type.I_02,
                    I_04 = (short)type.I_04,
                    Flags = (short)type.I_06,
                    I_08 = type.I_08,
                    I_10 = type.I_10,
                    EanIndex = type.I_12,
                    I_14 = type.I_14,
                    I_16 = type.I_16,
                    I_18 = type.I_18
                });

            }

            return xv2Type10s;
        }

    }

    [YAXSerializeAs("GeneralControl")]
    public class BAC_Type15
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("value")]
        public short I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public short I_04 { get; set; }
        [YAXAttributeFor("FLAGS")]
        [YAXSerializeAs("value")]
        public short I_06 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Type")]
        public short I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public short I_10 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("Parameter")]
        [YAXFormat("0.0########")]
        public float F_12 { get; set; }

        public static List<BAC_Type15> Read(byte[] rawBytes, List<byte> bytes, int offset, int count)
        {
            List<BAC_Type15> Type15 = new List<BAC_Type15>();

            for (int i = 0; i < count; i++)
            {
                Type15.Add(new BAC_Type15()
                {
                    I_00 = BitConverter.ToInt16(rawBytes, offset + 0),
                    I_02 = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    I_06 = BitConverter.ToInt16(rawBytes, offset + 6),
                    I_08 = BitConverter.ToInt16(rawBytes, offset + 8),
                    I_10 = BitConverter.ToInt16(rawBytes, offset + 10),
                    F_12 = BitConverter.ToSingle(rawBytes, offset + 12)
                });

                offset += 16;
            }

            return Type15;
        }

        public static List<byte> Write(List<BAC_Type15> types)
        {
            List<byte> bytes = new List<byte>();

            foreach (var type in types)
            {
                bytes.AddRange(BitConverter.GetBytes(type.I_00));
                bytes.AddRange(BitConverter.GetBytes(type.I_02));
                bytes.AddRange(BitConverter.GetBytes(type.I_04));
                bytes.AddRange(BitConverter.GetBytes(type.I_06));
                bytes.AddRange(BitConverter.GetBytes(type.I_08));
                bytes.AddRange(BitConverter.GetBytes(type.I_10));
                bytes.AddRange(BitConverter.GetBytes(type.F_12));
            }

            return bytes;
        }

        public static List<BAC.BAC_Type15> ConvertToXv2(List<BAC_Type15> type15s)
        {
            List<BAC.BAC_Type15> xv2Type15s = new List<BAC.BAC_Type15>();

            if (type15s == null) return null;

            foreach (var type in type15s)
            {
                xv2Type15s.Add(new BAC.BAC_Type15()
                {
                    StartTime = type.I_00,
                    Duration = type.I_02,
                    I_10 = (ushort)type.I_10,
                    I_08 = (ushort)type.I_08,
                    I_04 = type.I_04,
                    Flags = type.I_06,
                    F_12 = type.F_12,
                    F_16 = type.F_12
                });

            }

            return xv2Type15s;
        }

    }

}
