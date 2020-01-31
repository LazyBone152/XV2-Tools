using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.EMA
{
    [Serializable]
    public class Skeleton
    {
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
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
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; }
        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("value")]
        public ushort I_54 { get; set; }
        [YAXAttributeFor("F_56")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("F_60")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_60 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        public List<Bone> Bones { get; set; }

        [YAXDontSerializeIfNull]
        public List<IKEntry> IKEntries { get; set; }
        
        [YAXDontSerializeIfNull]
        public TransformMatrix MatrixData { get; set; }

        public static Skeleton Parse(byte[] rawBytes, List<byte> bytes, int skeletonOffset)
        {
            Skeleton skeleton = new Skeleton();
            skeleton.Bones = new List<Bone>();

            //Header
            skeleton.I_02 = BitConverter.ToUInt16(rawBytes, skeletonOffset + 2);
            skeleton.I_06 = BitConverter.ToUInt16(rawBytes, skeletonOffset + 6);
            skeleton.I_16 = BitConverter.ToInt32(rawBytes, skeletonOffset + 16);
            skeleton.I_20 = BitConverter.ToInt32(rawBytes, skeletonOffset + 20);
            skeleton.I_36 = BitConverter.ToInt32(rawBytes, skeletonOffset + 36);
            skeleton.I_40 = BitConverter.ToInt32(rawBytes, skeletonOffset + 40);
            skeleton.I_44 = BitConverter.ToInt32(rawBytes, skeletonOffset + 44);
            skeleton.I_48 = BitConverter.ToInt32(rawBytes, skeletonOffset + 48);
            skeleton.I_52 = BitConverter.ToUInt16(rawBytes, skeletonOffset + 52);
            skeleton.I_54 = BitConverter.ToUInt16(rawBytes, skeletonOffset + 54);
            skeleton.F_56 = BitConverter.ToSingle(rawBytes, skeletonOffset + 56);
            skeleton.F_60 = BitConverter.ToSingle(rawBytes, skeletonOffset + 60);

            int boneCount = BitConverter.ToUInt16(rawBytes, skeletonOffset + 0);
            int ikCount = BitConverter.ToUInt16(rawBytes, skeletonOffset + 4);
            int boneOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 8);
            int namesOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 12);
            int unkDataOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 24);
            int matrixDataOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 28);
            int ikOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 32);

            //Bones
            for(int i = 0; i < boneCount; i++)
            {
                skeleton.Bones.Add(Bone.Read(rawBytes, boneOffset + skeletonOffset, (unkDataOffset != 0) ? unkDataOffset + skeletonOffset : 0, i));
                boneOffset += 80;
                if (unkDataOffset > 0) unkDataOffset += 8;
            }

            //Names
            for(int i = 0; i < boneCount; i++)
            {
                int directNameOffset = BitConverter.ToInt32(rawBytes, namesOffset + skeletonOffset) + skeletonOffset;
                skeleton.Bones[i].Name = Utils.GetString(bytes, directNameOffset);
                namesOffset += 4;
            }

            //Matrix Data
            if(matrixDataOffset > 0)
            {
                Console.WriteLine("MatrixData present");
                Console.Read();
                skeleton.MatrixData = TransformMatrix.Read(rawBytes, matrixDataOffset + skeletonOffset);
            }

            if(ikCount > 0)
            {
                if (ikCount > 1) throw new Exception("ikCount > 1. Load failed.");
                skeleton.IKEntries = new List<IKEntry>();

                skeleton.IKEntries.Add(IKEntry.Read(rawBytes, ikOffset + skeletonOffset));
            }

            return skeleton;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            int boneCount = (Bones != null) ? Bones.Count : 0;
            int ikCount = (IKEntries != null) ? IKEntries.Count : 0;
            bool useUnkSkeletonData = false;

            //Header
            bytes.AddRange(BitConverter.GetBytes((ushort)boneCount)); //Bone count
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes((ushort)ikCount));//IK count
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(new byte[4]);//Bone offset
            bytes.AddRange(new byte[4]);//Names offset
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(new byte[4]);//UnkSkeletonData offset
            bytes.AddRange(new byte[4]);//MatrixData offset
            bytes.AddRange(new byte[4]);//IkData offset
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_52));
            bytes.AddRange(BitConverter.GetBytes(I_54));
            bytes.AddRange(BitConverter.GetBytes(F_56));
            bytes.AddRange(BitConverter.GetBytes(F_60));

            //Bones
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 8);

            for (int i = 0; i < boneCount; i++)
            {
                if (Bones[i].UnknownValues != null) useUnkSkeletonData = true;
                bytes.AddRange(Bones[i].Write());
            }

            //UnknownSkeletonData
            if (useUnkSkeletonData)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 24);

                for(int i = 0; i < boneCount; i++)
                {
                    if(Bones[i].UnknownValues == null)
                    {
                        Bones[i].UnknownValues = UnkSkeletonData.GetDefault();
                    }
                    bytes.AddRange(Bones[i].UnknownValues.Write());
                }
            }

            //Names Offsets
            int namesOffsetStart = bytes.Count;
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(namesOffsetStart), 12);

            for (int i = 0; i < boneCount; i++)
            {
                bytes.AddRange(new byte[4]);
            }

            //Names
            for (int i = 0; i < boneCount; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), namesOffsetStart + (i * 4));
                bytes.AddRange(Encoding.ASCII.GetBytes(Bones[i].Name));
                bytes.Add(0);
            }

            //Padding
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

            return bytes;
        }

    }

    [Serializable]
    public class Bone
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public ushort Index { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ParentIndex")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ChildIndex")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("SiblingIndex")]
        public ushort I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("EmgIndex")]
        public ushort I_06 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("UnkIndex")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }

        [YAXSerializeAs("TransformMatrix")]
        public TransformMatrix Matrix { get; set; }

        [YAXDontSerializeIfNull]
        public UnkSkeletonData UnknownValues { get; set; }

        public static Bone Read(byte[] rawBytes, int offset, int unkDataOffset, int index)
        {
            Bone bone = new Bone();

            bone.Index = (ushort)index;
            bone.I_00 = BitConverter.ToUInt16(rawBytes, offset + 0);
            bone.I_02 = BitConverter.ToUInt16(rawBytes, offset + 2);
            bone.I_04 = BitConverter.ToUInt16(rawBytes, offset + 4);
            bone.I_06 = BitConverter.ToUInt16(rawBytes, offset + 6);
            bone.I_08 = BitConverter.ToUInt16(rawBytes, offset + 8);
            bone.I_10 = BitConverter.ToUInt16(rawBytes, offset + 10);
            bone.I_12 = BitConverter.ToUInt16(rawBytes, offset + 12);
            bone.I_14 = BitConverter.ToUInt16(rawBytes, offset + 14);
            bone.Matrix = TransformMatrix.Read(rawBytes, offset + 16);

            if(unkDataOffset > 0)
            {
                bone.UnknownValues = UnkSkeletonData.Read(rawBytes, unkDataOffset);
            }

            return bone;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_14));
            bytes.AddRange(Matrix.Write());

            if (bytes.Count != 80) throw new InvalidDataException("EMA.Bone must be 80 bytes.");
            return bytes;
        }
        
    }

    [Serializable]
    public class TransformMatrix
    {
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_60 { get; set; }

        public static TransformMatrix Read(byte[] rawBytes, int offset)
        {
            return new TransformMatrix()
            {
                F_00 = BitConverter.ToSingle(rawBytes, offset + 0),
                F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                F_56 = BitConverter.ToSingle(rawBytes, offset + 56),
                F_60 = BitConverter.ToSingle(rawBytes, offset + 60)
            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(F_00));
            bytes.AddRange(BitConverter.GetBytes(F_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(F_36));
            bytes.AddRange(BitConverter.GetBytes(F_40));
            bytes.AddRange(BitConverter.GetBytes(F_44));
            bytes.AddRange(BitConverter.GetBytes(F_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));
            bytes.AddRange(BitConverter.GetBytes(F_56));
            bytes.AddRange(BitConverter.GetBytes(F_60));

            if (bytes.Count != 64) throw new InvalidDataException("Skeleton.TransformMatrix must be 64 bytes."); 
            return bytes;
        }
    }

    [Serializable]
    public class UnkSkeletonData
    {
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] Values { get; set; } //Size 4

        public static UnkSkeletonData Read(byte[] rawBytes, int offset)
        {
            UnkSkeletonData data = new UnkSkeletonData();
            data.Values = BitConverter_Ex.ToUInt16Array(rawBytes, offset + 0, 4);
            return data;
        }

        public List<byte> Write()
        {
            if (Values.Length != 4) throw new InvalidDataException("UnkSkeletonData > Values must have 4 values.");

            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter_Ex.GetBytes(Values));
            return bytes;
        }

        public static UnkSkeletonData GetDefault()
        {
            return new UnkSkeletonData() { Values = new ushort[4] { 0, 0, ushort.MaxValue, 0 } };
        }
    }

    [Serializable]
    public class IKEntry
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public byte I_04 { get; set; }
        [YAXAttributeFor("I_05")]
        [YAXSerializeAs("value")]
        public byte I_05 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }

        public static IKEntry Read(byte[] rawBytes, int offset)
        {
            IKEntry ikEntry = new IKEntry();

            ikEntry.I_00 = BitConverter.ToUInt16(rawBytes, offset + 0);
            ikEntry.I_02 = BitConverter.ToUInt16(rawBytes, offset + 2);
            ikEntry.I_04 = rawBytes[offset + 4];
            ikEntry.I_04 = rawBytes[offset + 4];
            ikEntry.I_06 = BitConverter.ToUInt16(rawBytes, offset + 6);
            ikEntry.I_08 = BitConverter.ToUInt16(rawBytes, offset + 8);
            ikEntry.I_10 = BitConverter.ToUInt16(rawBytes, offset + 10);
            ikEntry.I_12 = BitConverter.ToUInt16(rawBytes, offset + 12);
            ikEntry.I_14 = BitConverter.ToUInt16(rawBytes, offset + 14);
            ikEntry.I_16 = BitConverter.ToInt32(rawBytes, offset + 16);
            ikEntry.I_20 = BitConverter.ToInt32(rawBytes, offset + 20);

            return ikEntry;
        }
    }

}
