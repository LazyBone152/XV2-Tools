using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EMA;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.ESK
{
    [Serializable]
    public class ESK_File
    {
        public const int SIGNATURE = 1263748387;
        public const string BaseBone = "b_C_Base";
        public const string PelvisBone = "b_C_Pelvis";
        public const string LeftEyeIrisBone = "f_L_EyeIris";
        public const string RightEyeIrisBone = "f_R_EyeIris";
        public const string LeftHandBone = "b_L_Hand";
        public const string RightHandBone = "b_R_Hand";

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (ushort)37568)]
        public ushort Version { get; set; } = 37568;//I_08
        [YAXAttributeForClass]
        public ushort I_10 { get; set; }
        [YAXAttributeForClass]
        public int I_12 { get; set; }
        [YAXAttributeForClass]
        public int I_24 { get; set; }

        public ESK_Skeleton Skeleton { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static ESK_File Load(byte[] bytes)
        {
            return new Parser(bytes).eskFile;
        }

        public static ESK_File Load(string path)
        {
            return new Parser(path, false).eskFile;
        }

        public void Save(string path)
        {
            new Deserializer(this, path);
        }

        public static ESK_File MergeSkeletons(ESK_File baseSkeleton, ESK_File scdSkeleton)
        {
            ESK_File mergedEsk = baseSkeleton.Copy();

            for(int i = 0; i < scdSkeleton.Skeleton.NonRecursiveBones.Count; i++)
            {
                //Skip bones that already exist
                if (mergedEsk.Skeleton.NonRecursiveBones.FirstOrDefault(x => x.Name == scdSkeleton.Skeleton.NonRecursiveBones[i].Name) != null) continue;

                if (scdSkeleton.Skeleton.NonRecursiveBones[i].Parent != null)
                {
                    mergedEsk.Skeleton.AddBone(scdSkeleton.Skeleton.NonRecursiveBones[i].Parent.Name, scdSkeleton.Skeleton.NonRecursiveBones[i]);
                }
            }

            //Reload bone list
            mergedEsk.Skeleton.CreateNonRecursiveBoneList();

            return mergedEsk;
        }
    }


    [YAXSerializeAs("Skeleton")]
    [Serializable]
    public class ESK_Skeleton
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Flag")]
        public short I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("UseUnk2")]
        public bool UseExtraValues { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = (ulong)0)]
        public ulong SkeletonID { get; set; }

        //Skeleton ID as a int array (how it was originally). Preserved here to maintain support for older EAN XMLs.
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("I_28")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_28
        {
            get => null;
            set
            {
                if (value?.Length == 2)
                {
                    byte[] bytes = BitConverter_Ex.GetBytes(value);
                    SkeletonID = BitConverter.ToUInt64(bytes, 0);
                }
            }
        }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        [YAXDontSerializeIfNull]
        public AsyncObservableCollection<ESK_Bone> ESKBones { get; set; } = new AsyncObservableCollection<ESK_Bone>();
        [YAXDontSerializeIfNull]
        public ESK_Unk1 Unk1 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "IKRelation")]
        [YAXDontSerializeIfNull]
        public List<IKRelation> IKRelations { get; set; }

        //Non-hierarchy list - Save it here for better performance when using GetBone. (mostly for XenoKit, since it needs to use this method several dozen times each frame)
        private List<ESK_Bone> _nonRecursiveBones = null;
        [YAXDontSerialize]
        public List<ESK_Bone> NonRecursiveBones
        {
            get
            {
                if (_nonRecursiveBones == null)
                    CreateNonRecursiveBoneList();

                return _nonRecursiveBones;
            }
        }

        #region Load/Save
        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            CreateNonRecursiveBoneList();

            //bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), offsetToReplace);

            int startOffset = bytes.Count();
            int count = (NonRecursiveBones != null) ? NonRecursiveBones.Count : 0;

            bytes.AddRange(BitConverter.GetBytes((short)count));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(new byte[24]);
            bytes.AddRange(BitConverter.GetBytes(SkeletonID));

            if (count > 0)
            {
                //Writing Index List
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - startOffset), startOffset + 4);

                bool writeAbsTransform = NonRecursiveBones[0].AbsoluteTransform != null;

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].Index1));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].Index2));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].Index3));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].IKFlag));
                }

                //Writing Name Table and List
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 8);
                List<StringWriter.StringInfo> stringInfo = new List<StringWriter.StringInfo>();

                for (int i = 0; i < count; i++)
                {
                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        StringToWrite = NonRecursiveBones[i].Name,
                        Offset = bytes.Count(),
                        RelativeOffset = startOffset
                    });
                    bytes.AddRange(new byte[4]);
                }

                for (int i = 0; i < count; i++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - stringInfo[i].RelativeOffset), stringInfo[i].Offset);
                    bytes.AddRange(Encoding.ASCII.GetBytes(stringInfo[i].StringToWrite));
                    bytes.Add(0);
                }

                //Writing RelativeTransform
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 12);

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.PositionX));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.PositionY));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.PositionZ));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.PositionW));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.RotationX));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.RotationY));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.RotationZ));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.RotationW));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.ScaleX));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.ScaleY));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.ScaleZ));
                    bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].RelativeTransform.ScaleW));
                }

                if (writeAbsTransform)
                {
                    //Writing AbsoluteTransform (esk only)
                    bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 16);

                    for (int i = 0; i < count; i++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_00));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_04));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_08));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_12));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_16));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_20));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_24));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_28));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_32));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_36));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_40));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_44));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_48));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_52));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_56));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].AbsoluteTransform.F_60));
                    }
                }

                //Writing IK
                if(IKRelations?.Count > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - startOffset), startOffset + 20);

                    //IK Header; count of IK relations
                    bytes.AddRange(BitConverter.GetBytes(IKRelations.Count));

                    IKRelation.WriteAll(bytes, IKRelations, NonRecursiveBones);
                }
                else if (Unk1 != null)
                {
                    //Alternative path for XMLs with the old "Unk1" section instead of IKRelations
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - startOffset), startOffset + 20);
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_00));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_04));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_08));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_12));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_16));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_20));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_24));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_28));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_32));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_36));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_40));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_44));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_48));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_52));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_56));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_60));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_64));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_68));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_72));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_76));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_80));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_84));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_88));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_92));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_96));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_100));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_104));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_108));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_112));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_116));
                    bytes.AddRange(BitConverter.GetBytes(Unk1.I_120));
                }

                //Writing Extra Values
                if (UseExtraValues && count > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 24);

                    for (int i = 0; i < count; i++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].ExtraValue_1));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].ExtraValue_2));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].ExtraValue_3));
                        bytes.AddRange(BitConverter.GetBytes(NonRecursiveBones[i].ExtraValue_4));
                    }
                }

            }

            return bytes.ToArray();
        }

        public static ESK_Skeleton Read(byte[] rawBytes, int offset, bool loadAbsTransform)
        {
            //Init
            int ikOffset = BitConverter.ToInt32(rawBytes, offset + 20); //Unk1
            int boneExtraInfoOffset = BitConverter.ToInt32(rawBytes, offset + 24) + offset; //Unk2

            //Skeleton init
            ESK_Skeleton skeleton = new ESK_Skeleton()
            {
                I_02 = BitConverter.ToInt16(rawBytes, offset + 2),
                SkeletonID = BitConverter.ToUInt64(rawBytes, offset + 28),
                //Unk1 = ikOffset > offset ? ESK_Unk1.Read(rawBytes, ikOffset) : null,
                UseExtraValues = boneExtraInfoOffset - offset != 0
            };

            short boneCount = BitConverter.ToInt16(rawBytes, offset);

            //Setting the offsets for the initial loop to use
            int[] offsets = GetBoneOffset(rawBytes, 0, offset);
            int boneIndexOffset = offsets[0];
            int nameOffset = offsets[1];
            int skinningMatrixOffset = offsets[2];
            int transformMatrixOffset = offsets[3];

            int _index = 0;

            while (true)
            {
                int idx = skeleton.ESKBones.Count;
                ESK_Bone parent = ESK_Bone.Read(rawBytes, offsets, _index, null, loadAbsTransform);
                skeleton.ESKBones.Add(parent);
                _index += 1;

                //Children
                short childIdx = BitConverter.ToInt16(rawBytes, boneIndexOffset + 2);

                //Fix for some files like "ZMG_000.esk" where the first bone doesn't reference the child.
                if (childIdx == -1 && idx == 0 && boneCount > 1)
                    childIdx = 1;

                if (childIdx != -1)
                {
                    skeleton.ESKBones[idx].ESK_Bones = ParseChildrenBones(rawBytes, childIdx, offset, parent, ref _index, loadAbsTransform);
                }

                //Loop management
                short siblingIdx = BitConverter.ToInt16(rawBytes, boneIndexOffset + 4);

                if (siblingIdx != -1)
                {
                    //There is a sibling
                    offsets = GetBoneOffset(rawBytes, siblingIdx, offset);
                    boneIndexOffset = offsets[0];
                    nameOffset = offsets[1];
                    skinningMatrixOffset = offsets[2];
                    transformMatrixOffset = offsets[3];
                }
                else
                {
                    //There is no sibling. End loop.
                    break;
                }
            }

            //Parse extra data
            skeleton.CreateNonRecursiveBoneList();

            //Update bone count to account for any skipped self-referencing bones
            boneCount = (short)skeleton.NonRecursiveBones.Count;

            for(int i = 0; i < boneCount; i++)
            {
                skeleton.NonRecursiveBones[i].ExtraValue_1 = BitConverter.ToUInt16(rawBytes, boneExtraInfoOffset + 0);
                skeleton.NonRecursiveBones[i].ExtraValue_2 = BitConverter.ToUInt16(rawBytes, boneExtraInfoOffset + 2);
                skeleton.NonRecursiveBones[i].ExtraValue_3 = BitConverter.ToUInt16(rawBytes, boneExtraInfoOffset + 4);
                skeleton.NonRecursiveBones[i].ExtraValue_4 = BitConverter.ToUInt16(rawBytes, boneExtraInfoOffset + 6);
            }

            //Read IK relations
            if (ikOffset > 0)
            {
                ikOffset += offset;

                int ikCount = BitConverter.ToInt32(rawBytes, ikOffset);
                ikOffset += 4;

                skeleton.IKRelations = IKRelation.ReadAll(rawBytes, ikOffset, ikCount, skeleton.NonRecursiveBones);

            }

            return skeleton;
        }

        //Helper
        private static AsyncObservableCollection<ESK_Bone> ParseChildrenBones(byte[] rawBytes, int indexOfFirstSibling, int offset, ESK_Bone parent, ref int _index, bool loadAbsTransform)
        {
            AsyncObservableCollection<ESK_Bone> newBones = new AsyncObservableCollection<ESK_Bone>();

            int[] offsets = GetBoneOffset(rawBytes, indexOfFirstSibling, offset);
            int boneIndexOffset = offsets[0];
            int nameOffset = offsets[1];
            int skinningMatrixOffset = offsets[2];
            int transformMatrixOffset = offsets[3];

            while (true)
            {
                int idx = newBones.Count;
                ESK_Bone bone = ESK_Bone.Read(rawBytes, offsets, _index, parent, loadAbsTransform);
                newBones.Add(bone);
                _index += 1;
                short childIdx = BitConverter.ToInt16(rawBytes, boneIndexOffset + 2);

                //SOME ESKs have bones that have themselves as a child - not sure why...
                if (childIdx != -1 && childIdx != indexOfFirstSibling)
                {
                    newBones[idx].ESK_Bones = ParseChildrenBones(rawBytes, childIdx, offset, bone, ref _index, loadAbsTransform);
                }

                //Loop management
                short siblingIdx = BitConverter.ToInt16(rawBytes, boneIndexOffset + 4);

                if (siblingIdx != -1)
                {
                    //There is a sibling
                    offsets = GetBoneOffset(rawBytes, BitConverter.ToInt16(rawBytes, boneIndexOffset + 4), offset);
                    boneIndexOffset = offsets[0];
                    nameOffset = offsets[1];
                    skinningMatrixOffset = offsets[2];
                    transformMatrixOffset = offsets[3];
                }
                else
                {
                    //There is no sibling. End loop.
                    break;
                }
            }

            return newBones;
        }

        /// <summary>
        /// Returns the offsets for the bone indexes[0], name[1], Skinning Matrix[2], and Transform Matrix[3], in that order.
        /// </summary>
        private static int[] GetBoneOffset(byte[] rawBytes, int index, int SkeletonOffset)
        {
            if (BitConverter.ToInt16(rawBytes, SkeletonOffset + 0) - 1 < index)
            {
                throw new Exception("BoneIndex is greater than BoneCount.");
            }

            int boneIndexTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 4) + SkeletonOffset;
            int nameTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 8) + SkeletonOffset;
            int skinningMatrixTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 12) + SkeletonOffset;
            int transformMatrixTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 16);

            //Calc offsets
            int boneIndex = (8 * index) + boneIndexTableOffset;
            int nameTable = BitConverter.ToInt32(rawBytes, (4 * index) + nameTableOffset) + SkeletonOffset; //Points to the actual string, not the table
            int skinningMatrix = (48 * index) + skinningMatrixTableOffset;
            int transformMatrix = (transformMatrixTableOffset != 0) ? (64 * index) + transformMatrixTableOffset + SkeletonOffset : 0;

            return new int[4] { boneIndex, nameTable, skinningMatrix, transformMatrix };
        }
        #endregion

        #region Add
        public void AddBones(string parent, List<ESK_Bone> bonesToAdd, bool reloadListBones = true)
        {
            foreach (var bone in bonesToAdd)
            {
                AddBone(parent, bone, false);
            }

            if (reloadListBones)
                CreateNonRecursiveBoneList();
        }

        public bool AddBone(string parent, ESK_Bone boneToAdd, bool reloadListBones = true)
        {
            if (string.IsNullOrWhiteSpace(parent))
            {
                ESKBones.Add(boneToAdd.Copy());

                if (reloadListBones)
                    CreateNonRecursiveBoneList();

                return true;
            }

            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == parent)
                {
                    ESKBones[i].ESK_Bones.Add(boneToAdd.Copy());

                    if (reloadListBones)
                        CreateNonRecursiveBoneList();

                    return true;
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    bool result = AddBoneRecursive(parent, boneToAdd, ESKBones[i].ESK_Bones);

                    if (result == true)
                    {
                        if (reloadListBones)
                            CreateNonRecursiveBoneList();

                        return true;
                    }
                }

            }

            return false;
        }

        private bool AddBoneRecursive(string parent, ESK_Bone boneToAdd, AsyncObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == parent)
                {
                    eskBones[i].ESK_Bones.Add(boneToAdd.Copy());
                    return true;
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    bool result = AddBoneRecursive(parent, boneToAdd, eskBones[i].ESK_Bones);

                    if (result == true) return true;
                }

            }

            return false;
        }

        #endregion

        #region Get
        public ESK_Bone GetBone(string boneName)
        {
            for (int i = 0; i < NonRecursiveBones.Count; i++)
                if (NonRecursiveBones[i].Name == boneName)
                    return NonRecursiveBones[i];

            return null;
        }

        public ESK_Bone GetBone(int boneIndex, bool checkID)
        {
            if (checkID)
            {
                return NonRecursiveBones.FirstOrDefault(x => x.Index == boneIndex);
            }
            else
            {
                if (boneIndex < 0 || boneIndex >= NonRecursiveBones.Count)
                    throw new ArgumentOutOfRangeException("ESK_Skeleton.GetBone: Index out of range");

                return NonRecursiveBones[boneIndex];
            }
        }

        public string NameOf(int index)
        {
            if (ESKBones == null) throw new Exception(String.Format("Could not get the name of the bone at index {0} because ESKBones is null.", index));
            if (index > ESKBones.Count - 1) throw new Exception(String.Format("Could not get the name of the bone at index {0} because a bone does not exist there.", index));

            if (index == -1)
            {
                return string.Empty;
            }
            else
            {
                return ESKBones[index].Name;
            }
        }

        public string GetSibling(string bone)
        {
            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == bone)
                {
                    if (i != ESKBones.Count - 1)
                    {
                        return ESKBones[i + 1].Name;
                    }
                    else
                    {
                        break;
                    }
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    string result = GetSiblingRecursive(bone, ESKBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        private string GetSiblingRecursive(string bone, AsyncObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == bone)
                {
                    if (i != eskBones.Count - 1)
                    {
                        return eskBones[i + 1].Name;
                    }
                    else
                    {
                        break;
                    }
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    string result = GetSiblingRecursive(bone, eskBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        public string GetChild(string bone)
        {
            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == bone)
                {
                    if (ESKBones[i].ESK_Bones != null)
                    {
                        if (ESKBones[i].ESK_Bones.Count > 0)
                        {
                            return ESKBones[i].ESK_Bones[0].Name;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    string result = GetChildRecursive(bone, ESKBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        private string GetChildRecursive(string bone, AsyncObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == bone)
                {
                    if (eskBones[i].ESK_Bones != null)
                    {
                        if (eskBones[i].ESK_Bones.Count > 0)
                        {
                            return eskBones[i].ESK_Bones[0].Name;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    string result = GetChildRecursive(bone, eskBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        public string GetParent(string bone)
        {
            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == bone)
                {
                    break;
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    string result = GetParentRecursive(bone, ESKBones[i].Name, ESKBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        private string GetParentRecursive(string bone, string parentBone, AsyncObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == bone)
                {
                    return parentBone;
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    string result = GetParentRecursive(bone, eskBones[i].Name, eskBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }


        public void CreateNonRecursiveBoneList()
        {
            List<ESK_Bone> CreateNonRecursiveBoneList_Rec(AsyncObservableCollection<ESK_Bone> eskBones)
            {
                List<ESK_Bone> _bones = new List<ESK_Bone>();

                foreach (var bone in eskBones)
                {
                    _bones.Add(bone);

                    if (bone.ESK_Bones != null)
                    {
                        _bones.AddRange(CreateNonRecursiveBoneList_Rec(bone.ESK_Bones));
                    }
                }

                return _bones;
            }

            List<ESK_Bone> bones = CreateNonRecursiveBoneList_Rec(ESKBones);

            //Setting index numbers
            for (int i = 0; i < bones.Count; i++)
            {
                bones[i].Index1_Name = GetParent(bones[i].Name);
                bones[i].Index2_Name = GetChild(bones[i].Name);
                bones[i].Index3_Name = GetSibling(bones[i].Name);
                bones[i].Index1 = (short)bones.IndexOf(bones.FirstOrDefault(x => x.Name == bones[i].Index1_Name));
                bones[i].Index2 = (short)bones.IndexOf(bones.FirstOrDefault(x => x.Name == bones[i].Index2_Name));
                bones[i].Index3 = (short)bones.IndexOf(bones.FirstOrDefault(x => x.Name == bones[i].Index3_Name));
            }

            _nonRecursiveBones = bones;
        }

        #endregion

        #region Helper
        public bool Exists(string boneName)
        {
            foreach (var bone in NonRecursiveBones)
            {
                if (bone.Name == boneName) return true;
            }

            return false;
        }

        public int GetBoneIndex(string boneName)
        {
            return NonRecursiveBones.IndexOf(NonRecursiveBones.FirstOrDefault(x => x.Name == boneName));
        }

        public ESK_Skeleton Clone()
        {
            AsyncObservableCollection<ESK_Bone> bones = new AsyncObservableCollection<ESK_Bone>();

            foreach (var e in ESKBones)
            {
                bones.Add(e.Clone());
            }

            return new ESK_Skeleton()
            {
                I_02 = I_02,
                SkeletonID = SkeletonID,
                IKRelations = IKRelation.CloneAll(IKRelations),
                Unk1 = Unk1,
                UseExtraValues = UseExtraValues,
                ESKBones = bones
            };
        }

        public List<string> GetBoneList()
        {
            List<string> bones = new List<string>();

            foreach (var bone in NonRecursiveBones)
                bones.Add(bone.Name);

            return bones;
        }
        #endregion

        public EMA.Skeleton ConvertToEmaSkeleton()
        {
            return EMA.Skeleton.Convert(this);
        }

        public void GenerateAbsoluteMatrices()
        {
            foreach(var bone in ESKBones)
            {
                bone.GenerateAbsoluteMatrix(Matrix4x4.Identity);
            }
        }
    }

    [YAXSerializeAs("Bone")]
    [Serializable]
    public class ESK_Bone : INotifyPropertyChanged, IName
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region XenoKit
        private bool isSelectedValue = true;
        [YAXDontSerialize]
        public bool isSelected
        {
            get
            {
                return this.isSelectedValue;
            }

            set
            {
                if (value != this.isSelectedValue)
                {
                    this.isSelectedValue = value;
                    NotifyPropertyChanged("isSelected");
                }
            }
        }
        #endregion

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public short Index { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("UnkIndex")]
        public short IKFlag { get; set; }

        //Transforms
        public ESK_RelativeTransform RelativeTransform { get; set; }
        [YAXDontSerializeIfNull]
        public ESK_AbsoluteTransform AbsoluteTransform { get; set; }

        [YAXDontSerialize]
        [field: NonSerialized]
        public Matrix4x4 GeneratedAbsoluteMatrix { get; set; }

        //Parent reference
        [YAXDontSerialize]
        public ESK_Bone Parent { get; set; }

        //For non-recursive bone list:
        [YAXDontSerialize]
        public short Index1 { get; set; } //Parent
        [YAXDontSerialize]
        public short Index2 { get; set; } //Child
        [YAXDontSerialize]
        public short Index3 { get; set; } //Sibling
        [YAXDontSerialize]
        public string Index1_Name { get; set; }
        [YAXDontSerialize]
        public string Index2_Name { get; set; }
        [YAXDontSerialize]
        public string Index3_Name { get; set; }

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


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        public AsyncObservableCollection<ESK_Bone> ESK_Bones { get; set; } = new AsyncObservableCollection<ESK_Bone>();

        public ESK_Bone Clone()
        {
            return new ESK_Bone()
            {
                Name = (string)Name.Clone(),
                IKFlag = IKFlag,
                RelativeTransform = RelativeTransform.Clone(),
                AbsoluteTransform = AbsoluteTransform?.Clone(),
                ESK_Bones = CloneChildrenRecursive(ESK_Bones),
                isSelected = true,
                Index = Index,
                Index1 = Index1,
                Index2 = Index2,
                Index3 = Index3,
                Index1_Name = Index1_Name,
                Index2_Name = Index2_Name,
                Index3_Name = Index3_Name
            };
        }

        private AsyncObservableCollection<ESK_Bone> CloneChildrenRecursive(AsyncObservableCollection<ESK_Bone> bones)
        {
            AsyncObservableCollection<ESK_Bone> newBones = new AsyncObservableCollection<ESK_Bone>();

            foreach (var bone in bones)
            {
                newBones.Add(bone.Clone());
            }

            return newBones;
        }

        public static ESK_Bone Read(byte[] bytes, int[] offsets, int idx, ESK_Bone parent = null, bool loadAbsTransform = true)
        {
            int boneIndexOffset = offsets[0];
            int nameOffset = offsets[1];
            int skinningMatrixOffset = offsets[2];
            int transformMatrixOffset = offsets[3];

            return new ESK_Bone()
            {
                IKFlag = BitConverter.ToInt16(bytes, boneIndexOffset + 6),
                Index = (short)idx,
                Name = StringEx.GetString(bytes, nameOffset, false),
                RelativeTransform = ESK_RelativeTransform.Read(bytes, skinningMatrixOffset),
                AbsoluteTransform = (loadAbsTransform && transformMatrixOffset != 0) ? ESK_AbsoluteTransform.Read(bytes, transformMatrixOffset) : null,
                Parent = parent
            };
        }

        public ESK_Bone GetBoneWithIndex(int index)
        {
            if (Index == index) return this;
            if (ESK_Bones == null) return null;

            foreach (var child in ESK_Bones)
            {
                if (child.Index == index) return child;

                ESK_Bone _children = child.GetBoneWithIndex(index);

                if (_children != null) return _children;

            }

            return null;
        }

        public ESK_Bone GetBoneWithName(string name)
        {
            if (Name == name) return this;
            if (ESK_Bones == null) return null;

            foreach (var child in ESK_Bones)
            {
                if (child.Name == name) return child;

                ESK_Bone _children = child.GetBoneWithName(name);

                if (_children != null) return _children;
            }

            return null;
        }

        public override string ToString()
        {
            return Name;
        }
    
        public void GenerateAbsoluteMatrix(Matrix4x4 parent)
        {
            Matrix4x4 relativeMatrix = RelativeTransform.ToMatrix();

            GeneratedAbsoluteMatrix = relativeMatrix * parent;

            foreach (var child in ESK_Bones)
            {
                child.GenerateAbsoluteMatrix(GeneratedAbsoluteMatrix);
            }
        }
    }

    [YAXSerializeAs("RelativeTransform")]
    [Serializable]
    public class ESK_RelativeTransform
    {
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float PositionX { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float PositionY { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float PositionZ { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float PositionW { get; set; }

        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float RotationX { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float RotationY { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float RotationZ { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float RotationW { get; set; }

        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float ScaleX { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float ScaleY { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float ScaleZ { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float ScaleW { get; set; }

        public ESK_RelativeTransform() { }

        public ESK_RelativeTransform(Vector3 pos, float posW, Quaternion rot, Vector3 scale)
        {
            PositionX = pos.X;
            PositionY = pos.Y;
            PositionZ = pos.Z;
            PositionW = posW;
            RotationX = rot.X;
            RotationY = rot.Y;
            RotationZ = rot.Z;
            RotationW = rot.W;
            ScaleX = scale.X;
            ScaleY = scale.Y;
            ScaleZ = scale.Z;
            ScaleW = 1f;
        }

        public ESK_RelativeTransform Clone()
        {
            return new ESK_RelativeTransform()
            {
                PositionX = PositionX,
                PositionY = PositionY,
                PositionZ = PositionZ,
                PositionW = PositionW,
                RotationX = RotationX,
                RotationY = RotationY,
                RotationZ = RotationZ,
                RotationW = RotationW,
                ScaleX = ScaleX,
                ScaleY = ScaleY,
                ScaleZ = ScaleZ,
                ScaleW = ScaleW
            };
        }

        public static ESK_RelativeTransform Read(byte[] bytes, int offset)
        {
            if (offset == 0) return null;

            return new ESK_RelativeTransform()
            {
                PositionX = BitConverter.ToSingle(bytes, offset + 0),
                PositionY = BitConverter.ToSingle(bytes, offset + 4),
                PositionZ = BitConverter.ToSingle(bytes, offset + 8),
                PositionW = BitConverter.ToSingle(bytes, offset + 12),
                RotationX = BitConverter.ToSingle(bytes, offset + 16),
                RotationY = BitConverter.ToSingle(bytes, offset + 20),
                RotationZ = BitConverter.ToSingle(bytes, offset + 24),
                RotationW = BitConverter.ToSingle(bytes, offset + 28),
                ScaleX = BitConverter.ToSingle(bytes, offset + 32),
                ScaleY = BitConverter.ToSingle(bytes, offset + 36),
                ScaleZ = BitConverter.ToSingle(bytes, offset + 40),
                ScaleW = BitConverter.ToSingle(bytes, offset + 44)
            };
        }

        public void SetAsIdentity()
        {
            PositionX = PositionY = PositionZ = 0;
            PositionW = 1f;
            RotationX = RotationY = RotationY = 0;
            RotationW = 1f;
            ScaleX = ScaleY = ScaleZ = ScaleW = 1f;
        }

        #region Convert
        public Vector3 PositionToVector3()
        {
            return new Vector3(PositionX, PositionY, PositionZ) * PositionW;
        }

        public Vector3 ScaleToVector3()
        {
            return new Vector3(ScaleX, ScaleY, ScaleZ) * ScaleW;
        }

        public Quaternion RotationToQuaternion()
        {
            return new Quaternion(RotationX, RotationY, RotationZ, RotationW);
        }

        public Matrix4x4 ToMatrix()
        {
            Matrix4x4 matrix = Matrix4x4.Identity;

            matrix *= Matrix4x4.CreateScale(new Vector3(ScaleX, ScaleY, ScaleZ) * ScaleW);
            matrix *= Matrix4x4.CreateFromQuaternion(new Quaternion(RotationX, RotationY, RotationZ, RotationW));
            matrix *= Matrix4x4.CreateTranslation(new Vector3(PositionX, PositionY, PositionZ) * PositionW);

            return matrix;
        }

        public EAN_Keyframe ToEanPosKeyframe(int frame = 0)
        {
            return new EAN_Keyframe((ushort)frame, PositionX, PositionY, PositionZ, PositionW);
        }

        public EAN_Keyframe ToEanRotKeyframe(int frame = 0)
        {
            return new EAN_Keyframe((ushort)frame, RotationX, RotationY, RotationZ, RotationW);
        }

        public EAN_Keyframe ToEanScaleKeyframe(int frame = 0)
        {
            return new EAN_Keyframe((ushort)frame, ScaleX, ScaleY, ScaleZ, ScaleW);
        }
        #endregion
    }

    [YAXSerializeAs("AbsoluteTransform")]
    [Serializable]
    public class ESK_AbsoluteTransform
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

        public ESK_AbsoluteTransform Clone()
        {
            return new ESK_AbsoluteTransform()
            {
                F_00 = F_00,
                F_04 = F_04,
                F_08 = F_08,
                F_12 = F_12,
                F_16 = F_16,
                F_20 = F_20,
                F_24 = F_24,
                F_28 = F_28,
                F_32 = F_32,
                F_36 = F_36,
                F_40 = F_40,
                F_44 = F_44,
                F_48 = F_48,
                F_52 = F_52,
                F_56 = F_56,
                F_60 = F_60
            };
        }

        public static ESK_AbsoluteTransform Read(byte[] bytes, int offset)
        {
            if (offset == 0) return null;

            return new ESK_AbsoluteTransform()
            {
                F_00 = BitConverter.ToSingle(bytes, offset + 0),
                F_04 = BitConverter.ToSingle(bytes, offset + 4),
                F_08 = BitConverter.ToSingle(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                F_28 = BitConverter.ToSingle(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52),
                F_56 = BitConverter.ToSingle(bytes, offset + 56),
                F_60 = BitConverter.ToSingle(bytes, offset + 60),
            };
        }

    }

    [YAXSerializeAs("Unk1")]
    [Serializable]
    public class ESK_Unk1
    {
        //All are Int32!
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
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
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public int I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public int I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public int I_76 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        public int I_80 { get; set; }
        [YAXAttributeFor("I_84")]
        [YAXSerializeAs("value")]
        public int I_84 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        public int I_88 { get; set; }
        [YAXAttributeFor("I_92")]
        [YAXSerializeAs("value")]
        public int I_92 { get; set; }
        [YAXAttributeFor("I_96")]
        [YAXSerializeAs("value")]
        public int I_96 { get; set; }
        [YAXAttributeFor("I_100")]
        [YAXSerializeAs("value")]
        public int I_100 { get; set; }
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        public int I_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        public int I_108 { get; set; }
        [YAXAttributeFor("I_112")]
        [YAXSerializeAs("value")]
        public int I_112 { get; set; }
        [YAXAttributeFor("I_116")]
        [YAXSerializeAs("value")]
        public int I_116 { get; set; }
        [YAXAttributeFor("I_120")]
        [YAXSerializeAs("value")]
        public int I_120 { get; set; }

        public static ESK_Unk1 Read(byte[] bytes, int offset)
        {
            var unk1 = new ESK_Unk1();
            try
            {
                unk1.I_00 = BitConverter.ToInt32(bytes, offset + 0);
                unk1.I_04 = BitConverter.ToInt32(bytes, offset + 4);
                unk1.I_08 = BitConverter.ToInt32(bytes, offset + 8);
                unk1.I_12 = BitConverter.ToInt32(bytes, offset + 12);
                unk1.I_16 = BitConverter.ToInt32(bytes, offset + 16);
                unk1.I_20 = BitConverter.ToInt32(bytes, offset + 20);
                unk1.I_24 = BitConverter.ToInt32(bytes, offset + 24);
                unk1.I_28 = BitConverter.ToInt32(bytes, offset + 28);
                unk1.I_32 = BitConverter.ToInt32(bytes, offset + 32);
                unk1.I_36 = BitConverter.ToInt32(bytes, offset + 36);
                unk1.I_40 = BitConverter.ToInt32(bytes, offset + 40);
                unk1.I_44 = BitConverter.ToInt32(bytes, offset + 44);
                unk1.I_48 = BitConverter.ToInt32(bytes, offset + 48);
                unk1.I_52 = BitConverter.ToInt32(bytes, offset + 52);
                unk1.I_56 = BitConverter.ToInt32(bytes, offset + 56);
                unk1.I_60 = BitConverter.ToInt32(bytes, offset + 60);
                unk1.I_64 = BitConverter.ToInt32(bytes, offset + 64);
                unk1.I_68 = BitConverter.ToInt32(bytes, offset + 68);
                unk1.I_72 = BitConverter.ToInt32(bytes, offset + 72);
                unk1.I_76 = BitConverter.ToInt32(bytes, offset + 76);
                unk1.I_80 = BitConverter.ToInt32(bytes, offset + 80);
                unk1.I_84 = BitConverter.ToInt32(bytes, offset + 84);
                unk1.I_88 = BitConverter.ToInt32(bytes, offset + 88);
                unk1.I_92 = BitConverter.ToInt32(bytes, offset + 92);
                unk1.I_96 = BitConverter.ToInt32(bytes, offset + 96);
                unk1.I_100 = BitConverter.ToInt32(bytes, offset + 100);
                unk1.I_104 = BitConverter.ToInt32(bytes, offset + 104);
                unk1.I_108 = BitConverter.ToInt32(bytes, offset + 108);
                unk1.I_112 = BitConverter.ToInt32(bytes, offset + 112);
                unk1.I_116 = BitConverter.ToInt32(bytes, offset + 116);
                unk1.I_120 = BitConverter.ToInt32(bytes, offset + 120);
                return unk1;

            }
            catch
            {
                return unk1;
            }
        }

    }

}
