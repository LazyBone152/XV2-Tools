using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.EMA
{
    //Used in EMO and EMA

    [Serializable]
    public class Skeleton
    {
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
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

        public static Skeleton Parse(byte[] rawBytes, int skeletonOffset)
        {
            Skeleton skeleton = new Skeleton();
            skeleton.Bones = new List<Bone>();

            if(BitConverter.ToUInt16(rawBytes, skeletonOffset + 2) != 0)
            {
                throw new InvalidDataException("Skeleton.Parse: IK2 data is not supported.");
            }

            //Header
            skeleton.I_06 = BitConverter.ToUInt16(rawBytes, skeletonOffset + 6);
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
                skeleton.Bones.Add(Bone.Read(rawBytes, boneOffset + skeletonOffset, unkDataOffset, matrixDataOffset, skeletonOffset, i));
                boneOffset += 80;
                if (unkDataOffset > 0) unkDataOffset += 8;
            }

            //Names
            for(int i = 0; i < boneCount; i++)
            {
                int directNameOffset = BitConverter.ToInt32(rawBytes, namesOffset + skeletonOffset) + skeletonOffset;
                skeleton.Bones[i].Name = StringEx.GetString(rawBytes, directNameOffset, false, StringEx.EncodingType.UTF8);
                namesOffset += 4;
            }

            if(ikCount > 0)
            {
                //if (ikCount > 1) throw new Exception("ikCount > 1. Load failed.");
                skeleton.IKEntries = new List<IKEntry>();

                for(int i = 0; i < ikCount; i++)
                {
                    skeleton.IKEntries.Add(IKEntry.Read(rawBytes, ikOffset + skeletonOffset + (24 * i)));
                }
            }

            return skeleton;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            int boneCount = (Bones != null) ? Bones.Count : 0;
            int ikCount = (IKEntries != null) ? IKEntries.Count : 0;
            bool useUnkSkeletonData = false;
            bool useAbsMatrix = false;

            //Header
            bytes.AddRange(BitConverter.GetBytes((ushort)boneCount)); //Bone count
            bytes.AddRange(BitConverter.GetBytes((ushort)0)); //IK2 count
            bytes.AddRange(BitConverter.GetBytes((ushort)ikCount));//IK count
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(new byte[4]);//Bone offset
            bytes.AddRange(new byte[4]);//Names offset
            bytes.AddRange(BitConverter.GetBytes(0)); //IK2 offset
            bytes.AddRange(BitConverter.GetBytes(0)); //IK2 names
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
                if (Bones[i].AbsoluteMatrix != null) useAbsMatrix = true;
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

            //AbsoluteMatrix
            if (useAbsMatrix)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 28);

                for (int i = 0; i < boneCount; i++)
                {
                    if (Bones[i].AbsoluteMatrix == null)
                    {
                        bytes.AddRange(new TransformMatrix().Write());
                    }
                    else
                    {
                        bytes.AddRange(Bones[i].AbsoluteMatrix.Write());
                    }
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

            //IK
            if(ikCount > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 32);

                for(int i = 0; i < ikCount; i++)
                {
                    bytes.AddRange(IKEntries[i].Write());
                }
            }

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
        public ushort ParentIndex { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ChildIndex")]
        public ushort ChildIndex { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("SiblingIndex")]
        public ushort SiblingIndex { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("EmoPartIndex")]
        public ushort EmoPartIndex { get; set; } //"EmgIndex" in LibXenoverse, but its actually the emo part index
        [YAXAttributeForClass]
        [YAXSerializeAs("UnkIndex")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("IK_Flag")]
        [YAXSerializeAs("value")]
        public ushort IKFlag { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }

        public TransformMatrix RelativeMatrix { get; set; }

        //Optional Data:
        [YAXDontSerializeIfNull]
        public TransformMatrix AbsoluteMatrix { get; set; }
        [YAXDontSerializeIfNull]
        public UnkSkeletonData UnknownValues { get; set; }

        public static Bone Read(byte[] rawBytes, int offset, int unkDataOffset, int absMatrixOffset, int skeletonOffset, int index)
        {
            Bone bone = new Bone();

            bone.Index = (ushort)index;
            bone.ParentIndex = BitConverter.ToUInt16(rawBytes, offset + 0);
            bone.ChildIndex = BitConverter.ToUInt16(rawBytes, offset + 2);
            bone.SiblingIndex = BitConverter.ToUInt16(rawBytes, offset + 4);
            bone.EmoPartIndex = BitConverter.ToUInt16(rawBytes, offset + 6);
            bone.I_08 = BitConverter.ToUInt16(rawBytes, offset + 8);
            bone.IKFlag = BitConverter.ToUInt16(rawBytes, offset + 10);
            bone.I_12 = BitConverter.ToUInt16(rawBytes, offset + 12);
            bone.I_14 = BitConverter.ToUInt16(rawBytes, offset + 14);
            bone.RelativeMatrix = TransformMatrix.Read(rawBytes, offset + 16);

            if (absMatrixOffset > 0)
            {
                bone.AbsoluteMatrix = TransformMatrix.Read(rawBytes, absMatrixOffset + skeletonOffset);
            }

            if (unkDataOffset > 0)
            {
                bone.UnknownValues = UnkSkeletonData.Read(rawBytes, unkDataOffset + skeletonOffset);
            }

            return bone;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(ParentIndex));
            bytes.AddRange(BitConverter.GetBytes(ChildIndex));
            bytes.AddRange(BitConverter.GetBytes(SiblingIndex));
            bytes.AddRange(BitConverter.GetBytes(EmoPartIndex));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(IKFlag));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_14));
            bytes.AddRange(RelativeMatrix.Write());

            if (bytes.Count != 80) throw new InvalidDataException("EMA.Bone must be 80 bytes.");
            return bytes;
        }
        
    }

    [Serializable]
    public class TransformMatrix
    {
        [YAXAttributeFor("Row1")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float M11 { get; set; } = 1f;
        [YAXAttributeFor("Row1")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float M12 { get; set; }
        [YAXAttributeFor("Row1")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float M13 { get; set; }
        [YAXAttributeFor("Row1")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float M14 { get; set; }
        [YAXAttributeFor("Row2")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float M21 { get; set; }
        [YAXAttributeFor("Row2")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float M22 { get; set; } = 1f;
        [YAXAttributeFor("Row2")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float M23 { get; set; }
        [YAXAttributeFor("Row2")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float M24 { get; set; }
        [YAXAttributeFor("Row3")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float M31 { get; set; }
        [YAXAttributeFor("Row3")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float M32 { get; set; }
        [YAXAttributeFor("Row3")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float M33 { get; set; } = 1f;
        [YAXAttributeFor("Row3")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float M34 { get; set; }
        [YAXAttributeFor("Row4")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float M41 { get; set; }
        [YAXAttributeFor("Row4")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float M42 { get; set; }
        [YAXAttributeFor("Row4")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float M43 { get; set; }
        [YAXAttributeFor("Row4")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float M44 { get; set; } = 1f;

        public static TransformMatrix Read(byte[] rawBytes, int offset)
        {
            return new TransformMatrix()
            {
                M11 = BitConverter.ToSingle(rawBytes, offset + 0),
                M12 = BitConverter.ToSingle(rawBytes, offset + 4),
                M13 = BitConverter.ToSingle(rawBytes, offset + 8),
                M14 = BitConverter.ToSingle(rawBytes, offset + 12),
                M21 = BitConverter.ToSingle(rawBytes, offset + 16),
                M22 = BitConverter.ToSingle(rawBytes, offset + 20),
                M23 = BitConverter.ToSingle(rawBytes, offset + 24),
                M24 = BitConverter.ToSingle(rawBytes, offset + 28),
                M31 = BitConverter.ToSingle(rawBytes, offset + 32),
                M32 = BitConverter.ToSingle(rawBytes, offset + 36),
                M33 = BitConverter.ToSingle(rawBytes, offset + 40),
                M34 = BitConverter.ToSingle(rawBytes, offset + 44),
                M41 = BitConverter.ToSingle(rawBytes, offset + 48),
                M42 = BitConverter.ToSingle(rawBytes, offset + 52),
                M43 = BitConverter.ToSingle(rawBytes, offset + 56),
                M44 = BitConverter.ToSingle(rawBytes, offset + 60)
            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(M11));
            bytes.AddRange(BitConverter.GetBytes(M12));
            bytes.AddRange(BitConverter.GetBytes(M13));
            bytes.AddRange(BitConverter.GetBytes(M14));
            bytes.AddRange(BitConverter.GetBytes(M21));
            bytes.AddRange(BitConverter.GetBytes(M22));
            bytes.AddRange(BitConverter.GetBytes(M23));
            bytes.AddRange(BitConverter.GetBytes(M24));
            bytes.AddRange(BitConverter.GetBytes(M31));
            bytes.AddRange(BitConverter.GetBytes(M32));
            bytes.AddRange(BitConverter.GetBytes(M33));
            bytes.AddRange(BitConverter.GetBytes(M34));
            bytes.AddRange(BitConverter.GetBytes(M41));
            bytes.AddRange(BitConverter.GetBytes(M42));
            bytes.AddRange(BitConverter.GetBytes(M43));
            bytes.AddRange(BitConverter.GetBytes(M44));

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
            ikEntry.I_05 = rawBytes[offset + 5];
            ikEntry.I_06 = BitConverter.ToUInt16(rawBytes, offset + 6);
            ikEntry.I_08 = BitConverter.ToUInt16(rawBytes, offset + 8);
            ikEntry.I_10 = BitConverter.ToUInt16(rawBytes, offset + 10);
            ikEntry.I_12 = BitConverter.ToUInt16(rawBytes, offset + 12);
            ikEntry.I_14 = BitConverter.ToUInt16(rawBytes, offset + 14);
            ikEntry.I_16 = BitConverter.ToInt32(rawBytes, offset + 16);
            ikEntry.I_20 = BitConverter.ToInt32(rawBytes, offset + 20);

            return ikEntry;
        }
    
        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.Add(I_04);
            bytes.Add(I_05);
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_14));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));

            return bytes;
        }
    }

}
