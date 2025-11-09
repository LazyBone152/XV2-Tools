using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.EMA
{
    //Used in EMO and EMA

    [Serializable]
    public class Skeleton
    {
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
        [YAXAttributeForClass]
        public ushort Flag { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("UseExtraValues")]
        public bool UseExtraValues { get; set; }
        [YAXAttributeForClass]
        public ulong SkeletonID { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        public List<Bone> Bones { get; set; } = new List<Bone>();

        [YAXDontSerializeIfNull]
        public List<IKRelation> IKRelations { get; set; }

        #region LoadSave
        public static Skeleton Parse(byte[] rawBytes, int skeletonOffset)
        {
            Skeleton skeleton = new Skeleton();
            skeleton.Bones = new List<Bone>();

            if (BitConverter.ToUInt16(rawBytes, skeletonOffset + 2) != 0)
            {
                throw new InvalidDataException("Skeleton.Parse: IK2 data is not supported.");
            }

            //Header
            skeleton.I_36 = BitConverter.ToInt32(rawBytes, skeletonOffset + 36);
            skeleton.I_40 = BitConverter.ToInt32(rawBytes, skeletonOffset + 40);
            skeleton.I_44 = BitConverter.ToInt32(rawBytes, skeletonOffset + 44);
            skeleton.I_48 = BitConverter.ToInt32(rawBytes, skeletonOffset + 48);
            skeleton.I_52 = BitConverter.ToUInt16(rawBytes, skeletonOffset + 52);
            skeleton.Flag = BitConverter.ToUInt16(rawBytes, skeletonOffset + 54);
            skeleton.SkeletonID = BitConverter.ToUInt64(rawBytes, skeletonOffset + 56);

            int boneCount = BitConverter.ToUInt16(rawBytes, skeletonOffset + 0);
            int ikCount2 = BitConverter.ToUInt16(rawBytes, skeletonOffset + 2);
            int ikCount = BitConverter.ToInt32(rawBytes, skeletonOffset + 4);
            int boneOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 8);
            int namesOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 12);
            int extraValuesOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 24);
            int absMatrixDataOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 28);
            int ikOffset = BitConverter.ToInt32(rawBytes, skeletonOffset + 32);

            if(ikCount2 > 0)
            {
                Console.WriteLine("WARNING: This ema has ik2 data which is not supported. It will be skipped.");
                Console.ReadLine();
            }

            skeleton.UseExtraValues = extraValuesOffset > 0;

            //Bones
            for (int i = 0; i < boneCount; i++)
            {
                skeleton.Bones.Add(Bone.Read(rawBytes, boneOffset + skeletonOffset, extraValuesOffset, absMatrixDataOffset, skeletonOffset, i));
                boneOffset += 80;

                if(extraValuesOffset > 0)
                    extraValuesOffset += 8;

                if(absMatrixDataOffset > 0)
                    absMatrixDataOffset += 64;
            }

            //Names
            for (int i = 0; i < boneCount; i++)
            {
                int directNameOffset = BitConverter.ToInt32(rawBytes, namesOffset + skeletonOffset) + skeletonOffset;
                skeleton.Bones[i].Name = StringEx.GetString(rawBytes, directNameOffset, false, StringEx.EncodingType.UTF8);
                namesOffset += 4;
            }

            if (ikCount > 0)
            {
                skeleton.IKRelations = IKRelation.ReadAll(rawBytes, ikOffset + skeletonOffset, ikCount, skeleton.Bones);
            }

            return skeleton;
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            int boneCount = (Bones != null) ? Bones.Count : 0;
            int ikCount = (IKRelations != null) ? IKRelation.GetValidRelationCount(IKRelations, Bones) : 0;
            bool useAbsMatrix = false;

            //Header
            bytes.AddRange(BitConverter.GetBytes((ushort)boneCount)); //Bone count (0)
            bytes.AddRange(BitConverter.GetBytes((ushort)0)); //IK2 count (2)
            bytes.AddRange(BitConverter.GetBytes(ikCount));//IK count (4)
            bytes.AddRange(new byte[4]);//Bone offset (8)
            bytes.AddRange(new byte[4]);//Names offset (12)
            bytes.AddRange(BitConverter.GetBytes(0)); //IK2 offset (16)
            bytes.AddRange(BitConverter.GetBytes(0)); //IK2 names (20)
            bytes.AddRange(new byte[4]);//UnkSkeletonData offset (24)
            bytes.AddRange(new byte[4]);//MatrixData offset (28)
            bytes.AddRange(new byte[4]);//IkData offset (32)
            bytes.AddRange(BitConverter.GetBytes(I_36));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(I_48));
            bytes.AddRange(BitConverter.GetBytes(I_52));
            bytes.AddRange(BitConverter.GetBytes(Flag));
            bytes.AddRange(BitConverter.GetBytes(SkeletonID));

            //Bones
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 8);

            for (int i = 0; i < boneCount; i++)
            {
                if (Bones[i].AbsoluteMatrix != null) useAbsMatrix = true;
                bytes.AddRange(Bones[i].Write());
            }

            //UnknownSkeletonData
            if (UseExtraValues)
            {
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 24);

                for (int i = 0; i < boneCount; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(Bones[i].ExtraValue_1));
                    bytes.AddRange(BitConverter.GetBytes(Bones[i].ExtraValue_2));
                    bytes.AddRange(BitConverter.GetBytes(Bones[i].ExtraValue_3));
                    bytes.AddRange(BitConverter.GetBytes(Bones[i].ExtraValue_4));
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

            //AbsoluteMatrix
            if (useAbsMatrix)
            {
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 28);

                for (int i = 0; i < boneCount; i++)
                {
                    if (Bones[i].AbsoluteMatrix == null)
                    {
                        bytes.AddRange(new SkeletonMatrix().Write());
                    }
                    else
                    {
                        bytes.AddRange(Bones[i].AbsoluteMatrix.Write());
                    }
                }
            }

            //Padding
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

            //IK
            if (ikCount > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 32);
                IKRelation.WriteAll(bytes, IKRelations, Bones);
            }

            return bytes;
        }

        public static Skeleton Convert(ESK.ESK_Skeleton eskSkeleton)
        {
            Skeleton skeleton = new Skeleton();
            skeleton.SkeletonID = eskSkeleton.SkeletonID;
            skeleton.IKRelations = IKRelation.CloneAll(eskSkeleton.IKRelations);
            skeleton.UseExtraValues = eskSkeleton.UseExtraValues;
            skeleton.Flag = (ushort)eskSkeleton.I_02; //The "Flag" in ESK header. Not sure if this has a counterpart in EMO land, try putting it on this unused (?) value to preserve it when converting

            foreach (ESK.ESK_Bone bone in eskSkeleton.NonRecursiveBones)
            {
                skeleton.Bones.Add(Bone.Convert(bone));
            }

            return skeleton;
        }

        public ESK.ESK_File Convert()
        {
            ESK.ESK_Skeleton skeleton = new ESK.ESK_Skeleton();
            skeleton.SkeletonID = SkeletonID;
            skeleton.IKRelations = IKRelation.CloneAll(IKRelations);
            skeleton.UseExtraValues = UseExtraValues;
            skeleton.I_02 = (short)Flag;

            foreach (Bone bone in Bones)
            {
                string parent = bone.ParentIndex != ushort.MaxValue ? Bones[bone.ParentIndex].Name : "";
                skeleton.AddBone(parent, bone.ConvertToEsk(), false);
            }

            return new ESK.ESK_File()
            {
                Skeleton = skeleton
            };
        }

        #endregion

        public string[] GetBoneNames()
        {
            string[] bones = new string[Bones.Count];

            for(int i = 0; i < Bones.Count; i++)
            {
                bones[i] = Bones[i].Name;
            }

            return bones;
        }

        public int GetBoneIndex(string boneName)
        {
            for (int i = 0; i < Bones.Count; i++)
                if (Bones[i].Name == boneName)
                    return i;

            return -1;
        }

        public Bone GetBone(string boneName)
        {
            for (int i = 0; i < Bones.Count; i++)
                if (Bones[i].Name == boneName)
                    return Bones[i];

            return null;
        }

    }

    [Serializable]
    public class Bone : IName
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
        public ushort EmoPartIndex { get; set; } = ushort.MaxValue; //"EmgIndex" in LibXenoverse, but its actually the emo part index
        [YAXAttributeForClass]
        [YAXSerializeAs("UnkIndex")]
        public ushort I_08 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("IK_Flag")]
        [YAXSerializeAs("value")]
        public ushort IKFlag { get; set; }
        [CustomSerialize]
        public float F_12 { get; set; }

        public SkeletonMatrix RelativeMatrix { get; set; }
        [YAXDontSerialize]
        public ESK.ESK_RelativeTransform EskRelativeTransform { get; set; } //Conversion of RelativeMatrix. This is used for EMA animations since its more convenient to have it as a ESK-format transform.

        //Optional Data:
        [YAXDontSerializeIfNull]
        public SkeletonMatrix AbsoluteMatrix { get; set; }

        [YAXAttributeFor("Extra")]
        [YAXSerializeAs("Value1")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (ushort)0)]
        public ushort ExtraValue_1 { get; set; }
        [YAXAttributeFor("Extra")]
        [YAXSerializeAs("Value2")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (ushort)0)]
        public ushort ExtraValue_2 { get; set; }
        [YAXAttributeFor("Extra")]
        [YAXSerializeAs("Value3")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = ushort.MaxValue)]
        public ushort ExtraValue_3 { get; set; } = ushort.MaxValue;
        [YAXAttributeFor("Extra")]
        [YAXSerializeAs("Value4")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (ushort)0)]
        public ushort ExtraValue_4 { get; set; }

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
            bone.F_12 = BitConverter.ToSingle(rawBytes, offset + 12);
            bone.RelativeMatrix = SkeletonMatrix.Read(rawBytes, offset + 16);
            bone.EskRelativeTransform = bone.RelativeMatrix.ConvertToEskRelativeTransform();

            if (absMatrixOffset > 0)
            {
                bone.AbsoluteMatrix = SkeletonMatrix.Read(rawBytes, absMatrixOffset + skeletonOffset);
            }

            if (unkDataOffset > 0)
            {
                bone.ExtraValue_1 = BitConverter.ToUInt16(rawBytes, unkDataOffset + skeletonOffset + 0);
                bone.ExtraValue_1 = BitConverter.ToUInt16(rawBytes, unkDataOffset + skeletonOffset + 2);
                bone.ExtraValue_1 = BitConverter.ToUInt16(rawBytes, unkDataOffset + skeletonOffset + 4);
                bone.ExtraValue_1 = BitConverter.ToUInt16(rawBytes, unkDataOffset + skeletonOffset + 6);
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
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(RelativeMatrix.Write());

            if (bytes.Count != 80) throw new InvalidDataException("EMA.Bone must be 80 bytes.");
            return bytes;
        }

        public static Bone Convert(ESK.ESK_Bone eskBone)
        {
            Bone bone = new Bone();
            bone.Name = eskBone.Name;
            bone.Index = (ushort)eskBone.Index;
            bone.ParentIndex = (ushort)eskBone.Index1;
            bone.ChildIndex = (ushort)eskBone.Index2;
            bone.SiblingIndex = (ushort)eskBone.Index3;
            bone.IKFlag = (ushort)eskBone.IKFlag;
            bone.ExtraValue_1 = eskBone.ExtraValue_1;
            bone.ExtraValue_2 = eskBone.ExtraValue_2;
            bone.ExtraValue_3 = eskBone.ExtraValue_3;
            bone.ExtraValue_4 = eskBone.ExtraValue_4;

            if (eskBone.RelativeTransform != null)
            {
                bone.RelativeMatrix = SkeletonMatrix.FromEskRelativeTransform(eskBone.RelativeTransform);

                /*
                //Convert RelativeTransform to RelativeMatrix

                Vector3 scale = new Vector3(eskBone.RelativeTransform.ScaleX, eskBone.RelativeTransform.ScaleY, eskBone.RelativeTransform.ScaleZ) * eskBone.RelativeTransform.ScaleW;
                Quaternion rot = new Quaternion(eskBone.RelativeTransform.RotationX, eskBone.RelativeTransform.RotationY, eskBone.RelativeTransform.RotationZ, eskBone.RelativeTransform.RotationW);
                Vector3 pos = new Vector3(eskBone.RelativeTransform.PositionX, eskBone.RelativeTransform.PositionY, eskBone.RelativeTransform.PositionZ) * eskBone.RelativeTransform.PositionW;

                Matrix4x4 matrix = Matrix4x4.Identity;
                matrix *= Matrix4x4.CreateScale(scale);
                matrix *= Matrix4x4.CreateFromQuaternion(rot);
                matrix *= Matrix4x4.CreateTranslation(pos);

                bone.RelativeMatrix = SkeletonMatrix.FromMatrix(matrix);
                */
            }

            if (eskBone.AbsoluteTransform != null)
            {
                bone.AbsoluteMatrix = SkeletonMatrix.FromEskMatrix(eskBone.AbsoluteTransform);
            }

            return bone;
        }

        public ESK.ESK_Bone ConvertToEsk()
        {
            ESK.ESK_Bone bone = new ESK.ESK_Bone();
            bone.Name = Name;
            bone.Index = (short)Index;
            bone.Index1 = (short)ParentIndex;
            bone.Index2 = (short)ChildIndex;
            bone.Index3 = (short)SiblingIndex;
            bone.IKFlag = (short)IKFlag;
            bone.ExtraValue_1 = ExtraValue_1;
            bone.ExtraValue_2 = ExtraValue_2;
            bone.ExtraValue_3 = ExtraValue_3;
            bone.ExtraValue_4 = ExtraValue_4;

            if (RelativeMatrix != null)
            {
                bone.RelativeTransform = RelativeMatrix.ConvertToEskRelativeTransform();
            }

            if(AbsoluteMatrix != null)
            {
                bone.AbsoluteTransform = AbsoluteMatrix.ConvertToEskAbsoluteTransform();
            }

            return bone;
        }
    }

    [Serializable]
    public class SkeletonMatrix
    {
        [CustomSerialize("Row1", "M11", IsFloat = true)]
        public float M11 { get; set; } = 1f;
        [CustomSerialize("Row1", "M12", IsFloat = true)]
        public float M12 { get; set; }
        [CustomSerialize("Row1", "M13", IsFloat = true)]
        public float M13 { get; set; }
        [CustomSerialize("Row1", "M14", IsFloat = true)]
        public float M14 { get; set; }
        [CustomSerialize("Row2", "M21", IsFloat = true)]
        public float M21 { get; set; }
        [CustomSerialize("Row2", "M22", IsFloat = true)]
        public float M22 { get; set; } = 1f;
        [CustomSerialize("Row2", "M23", IsFloat = true)]
        public float M23 { get; set; }
        [CustomSerialize("Row2", "M24", IsFloat = true)]
        public float M24 { get; set; }
        [CustomSerialize("Row3", "M31", IsFloat = true)]
        public float M31 { get; set; }
        [CustomSerialize("Row3", "M32", IsFloat = true)]
        public float M32 { get; set; }
        [CustomSerialize("Row3", "M33", IsFloat = true)]
        public float M33 { get; set; } = 1f;
        [CustomSerialize("Row3", "M34", IsFloat = true)]
        public float M34 { get; set; }
        [CustomSerialize("Row4", "M41", IsFloat = true)]
        public float M41 { get; set; }
        [CustomSerialize("Row4", "M42", IsFloat = true)]
        public float M42 { get; set; }
        [CustomSerialize("Row4", "M43", IsFloat = true)]
        public float M43 { get; set; }
        [CustomSerialize("Row4", "M44", IsFloat = true)]
        public float M44 { get; set; } = 1f;

        public static SkeletonMatrix Read(byte[] rawBytes, int offset)
        {
            return new SkeletonMatrix()
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

        /// <summary>
        /// Creates a new <see cref="SkeletonMatrix"/> with all values set to zero.
        /// </summary>
        public static SkeletonMatrix ZeroMatrix()
        {
            return new SkeletonMatrix()
            {
                M11 = 0,
                M22 = 0,
                M33 = 0,
                M44 = 0
            };
        }

        #region Conversions
        //ESK -> EMO
        public static SkeletonMatrix FromMatrix(Matrix4x4 matrix)
        {
            SkeletonMatrix transformMatrix = new SkeletonMatrix();

            transformMatrix.M11 = matrix.M11;
            transformMatrix.M12 = matrix.M12;
            transformMatrix.M13 = matrix.M13;
            transformMatrix.M14 = matrix.M14;
            transformMatrix.M21 = matrix.M21;
            transformMatrix.M22 = matrix.M22;
            transformMatrix.M23 = matrix.M23;
            transformMatrix.M24 = matrix.M24;
            transformMatrix.M31 = matrix.M31;
            transformMatrix.M32 = matrix.M32;
            transformMatrix.M33 = matrix.M33;
            transformMatrix.M34 = matrix.M34;
            transformMatrix.M41 = matrix.M41;
            transformMatrix.M42 = matrix.M42;
            transformMatrix.M43 = matrix.M43;
            transformMatrix.M44 = matrix.M44;

            return transformMatrix;
        }

        public static SkeletonMatrix FromEskMatrix(ESK.ESK_AbsoluteTransform matrix)
        {
            SkeletonMatrix transformMatrix = new SkeletonMatrix();

            transformMatrix.M11 = matrix.F_00;
            transformMatrix.M12 = matrix.F_04;
            transformMatrix.M13 = matrix.F_08;
            transformMatrix.M14 = matrix.F_12;
            transformMatrix.M21 = matrix.F_16;
            transformMatrix.M22 = matrix.F_20;
            transformMatrix.M23 = matrix.F_24;
            transformMatrix.M24 = matrix.F_28;
            transformMatrix.M31 = matrix.F_32;
            transformMatrix.M32 = matrix.F_36;
            transformMatrix.M33 = matrix.F_40;
            transformMatrix.M34 = matrix.F_44;
            transformMatrix.M41 = matrix.F_48;
            transformMatrix.M42 = matrix.F_52;
            transformMatrix.M43 = matrix.F_56;
            transformMatrix.M44 = matrix.F_60;

            return transformMatrix;
        }

        public static SkeletonMatrix FromEskRelativeTransform(ESK.ESK_RelativeTransform relativeTransform)
        {
            //From LibXenoverse:
            //relativeTransform is 3x4, orient is a quaternion informations, SkeletonMatrix is 4x4
            // Ordering:
            //    1. Scale
            //    2. Rotate
            //    3. Translate

            //Matrix3 rot3x3;
            //orientation.ToRotationMatrix(rot3x3);
            double fTx = relativeTransform.RotationX + relativeTransform.RotationX;     // x + x
            double fTy = relativeTransform.RotationY + relativeTransform.RotationY;     //y + y
            double fTz = relativeTransform.RotationZ + relativeTransform.RotationZ;     //z + z
            double fTwx = fTx * relativeTransform.RotationW;        // * w
            double fTwy = fTy * relativeTransform.RotationW;
            double fTwz = fTz * relativeTransform.RotationW;
            double fTxx = fTx * relativeTransform.RotationX;        // * x
            double fTxy = fTy * relativeTransform.RotationX;
            double fTxz = fTz * relativeTransform.RotationX;
            double fTyy = fTy * relativeTransform.RotationY;        // * y
            double fTyz = fTz * relativeTransform.RotationY;
            double fTzz = fTz * relativeTransform.RotationZ;        // * z
            double rot3x3_00 = 1.0f - (fTyy + fTzz);
            double rot3x3_01 = fTxy - fTwz;
            double rot3x3_02 = fTxz + fTwy;
            double rot3x3_10 = fTxy + fTwz;
            double rot3x3_11 = 1.0f - (fTxx + fTzz);
            double rot3x3_12 = fTyz - fTwx;
            double rot3x3_20 = fTxz - fTwy;
            double rot3x3_21 = fTyz + fTwx;
            double rot3x3_22 = 1.0f - (fTxx + fTyy);

            SkeletonMatrix matrix = new SkeletonMatrix();

            // Set up final matrix with scale, rotation and translation
            matrix.M11 = (float)(relativeTransform.ScaleX * rot3x3_00); //m00 = scale.x * 
            matrix.M12 = (float)(relativeTransform.ScaleY * rot3x3_01); //m01 = scale.y *
            matrix.M13 = (float)(relativeTransform.ScaleZ * rot3x3_02);    //m02 = scale.z * 
            matrix.M14 = relativeTransform.PositionX;                 //m03 = pos.x

            matrix.M21 = (float)(relativeTransform.ScaleX * rot3x3_10);
            matrix.M22 = (float)(relativeTransform.ScaleY * rot3x3_11);
            matrix.M23 = (float)(relativeTransform.ScaleZ * rot3x3_12);
            matrix.M24 = relativeTransform.PositionY;                 //m13 = pos.y

            matrix.M31 = (float)(relativeTransform.ScaleX * rot3x3_20);
            matrix.M32 = (float)(relativeTransform.ScaleY * rot3x3_21);
            matrix.M33 = (float)(relativeTransform.ScaleZ * rot3x3_22);
            matrix.M34 = relativeTransform.PositionZ;                //m23 = pos.z

            // No projection term
            matrix.M44 = relativeTransform.PositionW;                //m33 = pos.w

            return matrix.Transpose4x4();
        }

        public Matrix4x4 ToMatrix()
        {
            return new Matrix4x4(M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44);
        }

        private SkeletonMatrix Transpose4x4()
        {
            float m11 = M11, m12 = M12, m13 = M13, m14 = M14;
            float m21 = M21, m22 = M22, m23 = M23, m24 = M24;
            float m31 = M31, m32 = M32, m33 = M33, m34 = M34;
            float m41 = M41, m42 = M42, m43 = M43, m44 = M44;

            SkeletonMatrix matrix = new SkeletonMatrix();

            matrix.M11 = m11;
            matrix.M12 = m21;
            matrix.M13 = m31;
            matrix.M14 = m41;

            matrix.M21 = m12;
            matrix.M22 = m22;
            matrix.M23 = m32;
            matrix.M24 = m42;

            matrix.M31 = m13;
            matrix.M32 = m23;
            matrix.M33 = m33;
            matrix.M34 = m43;

            matrix.M41 = m14;
            matrix.M42 = m24;
            matrix.M43 = m34;
            matrix.M44 = m44;

            return matrix;
        }

        //EMO -> ESK
        public ESK.ESK_RelativeTransform ConvertToEskRelativeTransform()
        {
            SkeletonMatrix transformMatrix = Transpose4x4();

            double[] resultPosOrientScaleMatrix = new double[12];

            //From LibXenoverse:
            //posOrientScaleMatrix is 3x4, orient is a quaternion informations, TransformMatrix is 4x4
            double m11 = transformMatrix.M11, m12 = transformMatrix.M12, m13 = transformMatrix.M13, m14 = transformMatrix.M14;
            double m21 = transformMatrix.M21, m22 = transformMatrix.M22, m23 = transformMatrix.M23, m24 = transformMatrix.M24;
            double m31 = transformMatrix.M31, m32 = transformMatrix.M32, m33 = transformMatrix.M33, m34 = transformMatrix.M34;
            double m41 = transformMatrix.M41, m42 = transformMatrix.M42, m43 = transformMatrix.M43, m44 = transformMatrix.M44;

            //position
            resultPosOrientScaleMatrix[0] = transformMatrix.M14;
            resultPosOrientScaleMatrix[1] = transformMatrix.M24;
            resultPosOrientScaleMatrix[2] = transformMatrix.M34;
            resultPosOrientScaleMatrix[3] = transformMatrix.M44;

            // Factor M = QR = QDU where Q is orthogonal, D is diagonal,
            // and U is upper triangular with ones on its diagonal.  Algorithm uses
            // Gram-Schmidt orthogonalization (the QR algorithm).
            //
            // If M = [ m0 | m1 | m2 ] and Q = [ q0 | q1 | q2 ], then
            //
            //   q0 = m0/|m0|
            //   q1 = (m1-(q0*m1)q0)/|m1-(q0*m1)q0|
            //   q2 = (m2-(q0*m2)q0-(q1*m2)q1)/|m2-(q0*m2)q0-(q1*m2)q1|
            //
            // where |V| indicates length of vector V and A*B indicates dot
            // product of vectors A and B.  The matrix R has entries
            //
            //   r00 = q0*m0  r01 = q0*m1  r02 = q0*m2
            //   r10 = 0      r11 = q1*m1  r12 = q1*m2
            //   r20 = 0      r21 = 0      r22 = q2*m2
            //
            // so D = diag(r00,r11,r22) and U has entries u01 = r01/r00,
            // u02 = r02/r00, and u12 = r12/r11.

            // Q = rotation
            // D = scaling
            // U = shear

            // D stores the three diagonal entries r00, r11, r22
            // U stores the entries U[0] = u01, U[1] = u02, U[2] = u12


            // build orthogonal matrix Q
            double fInvLength = 1.0 / Math.Sqrt(m11 * m11 + m21 * m21 + m31 * m31);
            double kQ_00 = m11 * fInvLength;
            double kQ_10 = m21 * fInvLength;
            double kQ_20 = m31 * fInvLength;

            double fDot = kQ_00 * m12 + kQ_10 * m22 + kQ_20 * m32;
            double kQ_01 = m12 - fDot * kQ_00;
            double kQ_11 = m22 - fDot * kQ_10;
            double kQ_21 = m32 - fDot * kQ_20;
            fInvLength = 1.0 / Math.Sqrt(kQ_01 * kQ_01 + kQ_11 * kQ_11 + kQ_21 * kQ_21);
            kQ_01 *= fInvLength;
            kQ_11 *= fInvLength;
            kQ_21 *= fInvLength;

            fDot = kQ_00 * m13 + kQ_10 * m23 + kQ_20 * m33;
            double kQ_02 = m13 - fDot * kQ_00;
            double kQ_12 = m23 - fDot * kQ_10;
            double kQ_22 = m33 - fDot * kQ_20;
            fDot = kQ_01 * m13 + kQ_11 * m23 + kQ_21 * m33;
            kQ_02 -= fDot * kQ_01;
            kQ_12 -= fDot * kQ_11;
            kQ_22 -= fDot * kQ_21;
            fInvLength = 1.0 / Math.Sqrt(kQ_02 * kQ_02 + kQ_12 * kQ_12 + kQ_22 * kQ_22);
            kQ_02 *= fInvLength;
            kQ_12 *= fInvLength;
            kQ_22 *= fInvLength;

            // guarantee that orthogonal matrix has determinant 1 (no reflections)
            double fDet = kQ_00 * kQ_11 * kQ_22 + kQ_01 * kQ_12 * kQ_20 + kQ_02 * kQ_10 * kQ_21 - kQ_02 * kQ_11 * kQ_20 - kQ_01 * kQ_10 * kQ_22 - kQ_00 * kQ_12 * kQ_21;

            if (fDet < 0.0)
            {
                kQ_00 = -kQ_00;
                kQ_01 = -kQ_01;
                kQ_02 = -kQ_02;
                kQ_10 = -kQ_10;
                kQ_11 = -kQ_11;
                kQ_12 = -kQ_12;
                kQ_20 = -kQ_20;
                kQ_21 = -kQ_21;
                kQ_22 = -kQ_22;
            }

            // build "right" matrix R
            double kR_00 = kQ_00 * m11 + kQ_10 * m21 + kQ_20 * m31;
            double kR_01 = kQ_00 * m12 + kQ_10 * m22 + kQ_20 * m32;
            double kR_11 = kQ_01 * m12 + kQ_11 * m22 + kQ_21 * m32;
            double kR_02 = kQ_00 * m13 + kQ_10 * m23 + kQ_20 * m33;
            double kR_12 = kQ_01 * m13 + kQ_11 * m23 + kQ_21 * m33;
            double kR_22 = kQ_02 * m13 + kQ_12 * m23 + kQ_22 * m33;

            // the scaling component
            double kD_0 = kR_00;
            double kD_1 = kR_11;
            double kD_2 = kR_22;

            // the shear component
            double fInvD0 = 1.0 / kD_0;
            double kU_0 = kR_01 * fInvD0;
            double kU_1 = kR_02 * fInvD0;
            double kU_2 = kR_12 / kD_1;



            resultPosOrientScaleMatrix[8] = kD_0;
            resultPosOrientScaleMatrix[9] = kD_1;
            resultPosOrientScaleMatrix[10] = kD_2;
            resultPosOrientScaleMatrix[11] = 1.0;




            //orientation = Quaternion(matQ);		//this->FromRotationMatrix(rot);
            // Algorithm in Ken Shoemake's article in 1987 SIGGRAPH course notes
            // article "Quaternion Calculus and Fast Animation".

            double fTrace = kQ_00 + kQ_11 + kQ_22;
            double fRoot;

            if (fTrace > 0.0)
            {
                // |w| > 1/2, may as well choose w > 1/2
                fRoot = Math.Sqrt(fTrace + 1.0f);  // 2w
                resultPosOrientScaleMatrix[7] = 0.5f * fRoot;                   //w
                fRoot = 0.5f / fRoot;  // 1/(4w)
                resultPosOrientScaleMatrix[4] = (kQ_21 - kQ_12) * fRoot;
                resultPosOrientScaleMatrix[5] = (kQ_02 - kQ_20) * fRoot;
                resultPosOrientScaleMatrix[6] = (kQ_10 - kQ_01) * fRoot;
            }
            else
            {
                if (kQ_11 > kQ_00)
                {
                    if (kQ_22 > kQ_11)
                    {
                        fRoot = Math.Sqrt(kQ_22 - kQ_00 - kQ_11 + 1.0);

                        resultPosOrientScaleMatrix[6] = 0.5f * fRoot;                   //z
                        fRoot = 0.5f / fRoot;
                        resultPosOrientScaleMatrix[7] = (kQ_10 - kQ_01) * fRoot;        //w
                        resultPosOrientScaleMatrix[4] = (kQ_02 + kQ_20) * fRoot;        //x
                        resultPosOrientScaleMatrix[5] = (kQ_12 + kQ_21) * fRoot;        //y
                    }
                    else
                    {
                        fRoot = Math.Sqrt(kQ_11 - kQ_22 - kQ_00 + 1.0);
                        resultPosOrientScaleMatrix[5] = 0.5f * fRoot;                           //y
                        fRoot = 0.5f / fRoot;
                        resultPosOrientScaleMatrix[7] = (kQ_02 - kQ_20) * fRoot;        //w
                        resultPosOrientScaleMatrix[6] = (kQ_21 + kQ_12) * fRoot;        //z
                        resultPosOrientScaleMatrix[4] = (kQ_01 + kQ_10) * fRoot;        //x

                    }
                }
                else
                {

                    if (kQ_22 > kQ_00)
                    {
                        fRoot = Math.Sqrt(kQ_22 - kQ_00 - kQ_11 + 1.0);

                        resultPosOrientScaleMatrix[6] = 0.5f * fRoot;                   //z
                        fRoot = 0.5f / fRoot;
                        resultPosOrientScaleMatrix[7] = (kQ_10 - kQ_01) * fRoot;        //w
                        resultPosOrientScaleMatrix[4] = (kQ_02 + kQ_20) * fRoot;        //x
                        resultPosOrientScaleMatrix[5] = (kQ_12 + kQ_21) * fRoot;        //y
                    }
                    else
                    {
                        fRoot = Math.Sqrt(kQ_00 - kQ_11 - kQ_22 + 1.0f);

                        resultPosOrientScaleMatrix[4] = 0.5f * fRoot;                   //x
                        fRoot = 0.5f / fRoot;
                        resultPosOrientScaleMatrix[7] = (kQ_21 - kQ_12) * fRoot;        //w
                        resultPosOrientScaleMatrix[5] = (kQ_10 + kQ_01) * fRoot;    //y
                        resultPosOrientScaleMatrix[6] = (kQ_20 + kQ_02) * fRoot;    //z
                    }

                }
            }

            return new ESK.ESK_RelativeTransform
            {
                PositionX = (float)resultPosOrientScaleMatrix[0],
                PositionY = (float)resultPosOrientScaleMatrix[1],
                PositionZ = (float)resultPosOrientScaleMatrix[2],
                PositionW = (float)resultPosOrientScaleMatrix[3],
                RotationX = (float)resultPosOrientScaleMatrix[4],
                RotationY = (float)resultPosOrientScaleMatrix[5],
                RotationZ = (float)resultPosOrientScaleMatrix[6],
                RotationW = (float)resultPosOrientScaleMatrix[7],
                ScaleX = (float)resultPosOrientScaleMatrix[8],
                ScaleY = (float)resultPosOrientScaleMatrix[9],
                ScaleZ = (float)resultPosOrientScaleMatrix[10],
                ScaleW = (float)resultPosOrientScaleMatrix[11]
            };
        }

        public ESK.ESK_AbsoluteTransform ConvertToEskAbsoluteTransform()
        {
            return new ESK.ESK_AbsoluteTransform()
            {
                F_00 = M11,
                F_04 = M12,
                F_08 = M13,
                F_12 = M14,
                F_16 = M21,
                F_20 = M22,
                F_24 = M23,
                F_28 = M24,
                F_32 = M31,
                F_36 = M32,
                F_40 = M33,
                F_44 = M34,
                F_48 = M41,
                F_52 = M42,
                F_56 = M43,
                F_60 = M44
            };
        }
        #endregion
    }

    [Serializable]
    public class IKRelation
    {
        [YAXAttributeForClass]
        public byte I_04 { get; set; }


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "IKBone")]
        public List<IKBone> IKBones { get; set; } = new List<IKBone>();

        public static List<IKRelation> ReadAll<T>(byte[] bytes, int offset, int count, IList<T> bones) where T : IName
        {
            List<IKRelation> ikRelations = new List<IKRelation>();

            for(int i = 0; i < count; i++)
            {
                IKRelation ikRelation = Read(bytes, offset, bones, out int size);

                if (ikRelation == null) return null;

                ikRelations.Add(ikRelation);
                offset += size;
            }

            return ikRelations;
        }

        private static IKRelation Read<T>(byte[] bytes, int offset, IList<T> bones, out int size) where T : IName
        {
            IKRelation relation = new IKRelation();

            ushort type = BitConverter.ToUInt16(bytes, offset);

            if (type != 1)
            {
                Console.WriteLine($"IKRelation: unknown/unsupported type ({type}). IK bones will be skipped in this file.");
                size = 0;
                return null;
                //throw new Exception($"IKRelation: unknown/unsupported type ({type}). Parse failed.");
            }

            size = BitConverter.ToUInt16(bytes, offset + 2);
            relation.I_04 = bytes[offset + 4];
            byte boneCount = bytes[offset + 5];

            offset += 6;

            ushort[] boneIndices = BitConverter_Ex.ToUInt16Array(bytes, offset, boneCount + 1);
            offset += sizeof(ushort) * (boneCount + 1);
            offset += Utils.CalculatePadding(offset, 4);
            float[] weights = BitConverter_Ex.ToFloat32Array(bytes, offset, boneCount + 1);

            for(int i = 0; i <= boneCount; i++)
            {
                if (boneIndices[i] < 0 || boneIndices[i] >= bones.Count)
                {
                    //IK Bone index is out of range. This could happen on older EANs where bones were removed, but IK bones were not adjusted (they weren't parsed in older tools)
                    break;
                    //throw new ArgumentOutOfRangeException("IKRelation.Read: IK Bone is an invalid index.");
                }

                relation.IKBones.Add(new IKBone()
                {
                    Bone = bones[boneIndices[i]].Name,
                    Weight = weights[i]
                });
            }

            return relation;
        }

        public static void WriteAll<T>(List<byte> bytes, List<IKRelation> ikRelations, IList<T> bones) where T : IName
        {
            for(int i = 0; i < ikRelations.Count; i++)
            {
                if (ikRelations[i].IsValid(bones))
                    bytes.AddRange(ikRelations[i].Write(bytes.Count, bones));
            }
        }

        private byte[] Write<T>(int startOffset, IList<T> bones) where T : IName
        {
            int size = CalculateSize(startOffset);
            int count = IKBones?.Count ?? 0;

            if (count == 0)
                throw new Exception("An IKRelation cannot have zero IKBones");

            List<byte> bytes = new List<byte>(size);

            bytes.AddRange(BitConverter.GetBytes((ushort)1)); //Default type value. Only type 1 is used in DBXV2, so no need to worry about any other value
            bytes.AddRange(BitConverter.GetBytes((ushort)size));
            bytes.Add(I_04);
            bytes.Add((byte)(count - 1));

            for(int i = 0; i < count; i++)
            {
                int boneIdx = bones.IndexOf(bones.FirstOrDefault(x => x.Name == IKBones[i].Bone));
                if (boneIdx == -1)
                    throw new Exception($"IKRelation: cannot find referenced bone ({IKBones[i].Bone}) in the skeleton.");

                bytes.AddRange(BitConverter.GetBytes((ushort)boneIdx));
            }

            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count + startOffset, 4)]);

            for (int i = 0; i < count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(IKBones[i].Weight));
            }

            if (bytes.Count != size)
            throw new Exception("IKRelation: size mismatch");

            return bytes.ToArray();
        }

        internal static int GetValidRelationCount<T>(IList<IKRelation> relations, IList<T> bones) where T : IName
        {
            if (bones == null || relations == null) return 0;
            int count = 0;

            for(int i = 0; i < relations.Count; i++)
            {
                if (relations[i].IsValid(bones))
                    count++;
            }

            return count;
        }

        private bool IsValid<T>(IList<T> bones) where T : IName
        {
            if (IKBones.Count == 0) return false;

            foreach(var ikBone in IKBones)
            {
                int boneIdx = bones.IndexOf(bones.FirstOrDefault(x => x.Name == ikBone.Bone));

                if (boneIdx == -1)
                    return false;
            }

            return true;
        }

        internal int CalculateSize(int startOffset)
        {
            int offset = startOffset;
            offset += 4;
            offset += 2; //unk and boneCount
            offset += (IKBones.Count * 2);
            offset += Utils.CalculatePadding(startOffset, 4);
            offset += (IKBones.Count * 4);

            return offset - startOffset;
        }
    
        public static List<IKRelation> CloneAll(IList<IKRelation> relations)
        {
            List<IKRelation> newRelations = new List<IKRelation>();
            if (relations == null) return newRelations;

            foreach(var relation in relations)
            {
                newRelations.Add(relation.Clone());
            }

            return newRelations;
        }

        public IKRelation Clone()
        {
            IKRelation relation = new IKRelation();
            relation.I_04 = I_04;
            relation.IKBones = new List<IKBone>();

            foreach(var bone in IKBones)
            {
                relation.IKBones.Add(bone.Clone());
            }

            return relation;
        }
    }

    [Serializable]
    public class IKBone
    {
        [YAXAttributeForClass]
        public string Bone { get; set; }
        [YAXAttributeForClass]
        [YAXFormat("0.0#########")]
        public float Weight { get; set; }

        public IKBone Clone()
        {
            return new IKBone()
            {
                Bone = Bone,
                Weight = Weight
            };
        }
    }
}
