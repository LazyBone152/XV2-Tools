using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.ESK;
using YAXLib;

namespace Xv2CoreLib.FPF
{
    [YAXSerializeAs("FPF")]
    public class FPF_File
    {
        [YAXDontSerialize]
        public const int FPF_SIGNATURE = 1179665955;
        [YAXDontSerialize]
        public const int BoneIndexListOffset = 112;
        [YAXDontSerialize]
        public const int BoneIndexListCount = 60;
        [YAXDontSerialize]
        public const int EntryPointerListOffset = 352;
        [YAXDontSerialize]
        public const int EntryPointerListEntryCount = 70;
        [YAXDontSerialize]
        public const int EntryPointerListEntrySize = 8;
        [YAXDontSerialize]
        public const int MainSkeletonEntryId = 0;
        [YAXDontSerialize]
        public const int FpfBonePoseSize = 320;
        [YAXDontSerialize]
        public const int MatrixCountPerBonePose = 5;

        [YAXAttributeForClass]
        [YAXSerializeAs("Version")]
        public ushort Version { get; set; }
        [YAXAttributeFor("CharacterID")]
        [YAXSerializeAs("value")]
        public int CharacterID { get; set; }
        [YAXAttributeFor("Costume")]
        [YAXSerializeAs("value")]
        public int Costume { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("FigurePositionX")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float FigurePositionX { get; set; }
        [YAXAttributeFor("FigurePositionY")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float FigurePositionY { get; set; }
        [YAXAttributeFor("FigurePositionZ")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float FigurePositionZ { get; set; }
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
        [YAXAttributeFor("F_96")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("I_100")]
        [YAXSerializeAs("value")]
        public int I_100 { get; set; }
        [YAXAttributeFor("F_104")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        public int I_108 { get; set; }

        [YAXComment("Fixed 60-entry bone index table. Values are skeleton bone indexes, -1 means unused.")]
        public FPF_BoneIndexList BoneIndexes { get; set; }
        [YAXDontSerializeIfNull]
        public List<FPF_Entry> Entries { get; set; }

        public static FPF_File Parse(string path, bool writeXml)
        {
            FPF_File fpfFile = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(FPF_File));
                serializer.SerializeToFile(fpfFile, path + ".xml");
            }

            return fpfFile;
        }

        public static FPF_File Parse(byte[] rawBytes)
        {
            if (rawBytes == null) throw new ArgumentNullException(nameof(rawBytes));
            if (rawBytes.Length < EntryPointerListOffset + EntryPointerListEntryCount * EntryPointerListEntrySize)
                throw new InvalidDataException("FPF file is too small.");
            if (BitConverter.ToInt32(rawBytes, 0) != FPF_SIGNATURE)
                throw new InvalidDataException("FPF_SIGNATURE not found at offset 0x0. Parse failed.");

            FPF_File fpfFile = new FPF_File
            {
                Version = BitConverter.ToUInt16(rawBytes, 6),
                CharacterID = BitConverter.ToInt32(rawBytes, 8),
                Costume = BitConverter.ToInt32(rawBytes, 12),
                F_16 = BitConverter.ToSingle(rawBytes, 16),
                F_20 = BitConverter.ToSingle(rawBytes, 20),
                F_24 = BitConverter.ToSingle(rawBytes, 24),
                F_28 = BitConverter.ToSingle(rawBytes, 28),
                F_32 = BitConverter.ToSingle(rawBytes, 32),
                F_36 = BitConverter.ToSingle(rawBytes, 36),
                I_40 = BitConverter.ToInt32(rawBytes, 40),
                I_44 = BitConverter.ToInt32(rawBytes, 44),
                FigurePositionX = BitConverter.ToSingle(rawBytes, 48),
                FigurePositionY = BitConverter.ToSingle(rawBytes, 52),
                FigurePositionZ = BitConverter.ToSingle(rawBytes, 56),
                I_60 = BitConverter.ToInt32(rawBytes, 60),
                I_64 = BitConverter.ToInt32(rawBytes, 64),
                I_68 = BitConverter.ToInt32(rawBytes, 68),
                I_72 = BitConverter.ToInt32(rawBytes, 72),
                I_76 = BitConverter.ToInt32(rawBytes, 76),
                I_80 = BitConverter.ToInt32(rawBytes, 80),
                I_84 = BitConverter.ToInt32(rawBytes, 84),
                I_88 = BitConverter.ToInt32(rawBytes, 88),
                I_92 = BitConverter.ToInt32(rawBytes, 92),
                F_96 = BitConverter.ToSingle(rawBytes, 96),
                I_100 = BitConverter.ToInt32(rawBytes, 100),
                F_104 = BitConverter.ToSingle(rawBytes, 104),
                I_108 = BitConverter.ToInt32(rawBytes, 108),
                BoneIndexes = FPF_BoneIndexList.Read(rawBytes, BoneIndexListOffset),
                Entries = new List<FPF_Entry>()
            };

            for (int i = 0; i < EntryPointerListEntryCount; i++)
            {
                int offset = EntryPointerListOffset + i * EntryPointerListEntrySize;
                int entryOffset = BitConverter.ToInt32(rawBytes, offset);

                if (entryOffset != 0)
                {
                    if (entryOffset < EntryPointerListOffset || entryOffset >= rawBytes.Length)
                        throw new InvalidDataException(String.Format("FPF entry {0} has an invalid offset.", i));

                    fpfFile.Entries.Add(FPF_Entry.Read(rawBytes, entryOffset, i));
                }
            }

            return fpfFile;
        }

        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(FPF_File), YAXSerializationOptions.DontSerializeNullObjects);
            FPF_File fpfFile = (FPF_File)serializer.DeserializeFromFile(xmlPath);
            List<byte> bytes = fpfFile.Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public FPF_Entry GetEntry(int id)
        {
            return Entries?.FirstOrDefault(entry => entry.ID == id);
        }

        public FPF_Entry GetMainSkeletonEntry()
        {
            return GetEntry(MainSkeletonEntryId);
        }

        public void ValidateMainSkeleton(ESK_File eskFile)
        {
            if (eskFile?.Skeleton == null) throw new ArgumentNullException(nameof(eskFile));
            ValidateMainSkeleton(eskFile.Skeleton.GetBoneList());
        }

        public void ValidateMainSkeleton(IList<string> boneNames)
        {
            if (boneNames == null) throw new ArgumentNullException(nameof(boneNames));

            FPF_Entry mainEntry = GetMainSkeletonEntry();
            if (mainEntry == null)
                throw new InvalidDataException("FPF file does not contain the main skeleton entry.");

            int bonePoseCount = mainEntry.BonePoses?.Count ?? 0;
            if (bonePoseCount != boneNames.Count)
                throw new InvalidDataException(String.Format("Main FPF entry has {0} bone transforms, but the skeleton has {1} bones.", bonePoseCount, boneNames.Count));

            BoneIndexes.ValidateAgainstBoneCount(boneNames.Count);
        }

        public void RemapMainSkeleton(ESK_File sourceSkeleton, ESK_File targetSkeleton)
        {
            if (sourceSkeleton?.Skeleton == null) throw new ArgumentNullException(nameof(sourceSkeleton));
            if (targetSkeleton?.Skeleton == null) throw new ArgumentNullException(nameof(targetSkeleton));

            RemapMainSkeleton(sourceSkeleton.Skeleton.NonRecursiveBones, targetSkeleton.Skeleton.NonRecursiveBones);
        }

        public void RemapMainSkeleton(IList<ESK_Bone> sourceBones, IList<ESK_Bone> targetBones)
        {
            if (sourceBones == null) throw new ArgumentNullException(nameof(sourceBones));
            if (targetBones == null) throw new ArgumentNullException(nameof(targetBones));

            FPF_Entry mainEntry = GetMainSkeletonEntry();
            if (mainEntry == null)
                throw new InvalidDataException("FPF file does not contain the main skeleton entry.");
            if (mainEntry.BonePoses == null || mainEntry.BonePoses.Count != sourceBones.Count)
                throw new InvalidDataException(String.Format("Main FPF entry has {0} bone transforms, but the source skeleton has {1} bones.", mainEntry.BonePoses?.Count ?? 0, sourceBones.Count));

            Dictionary<string, FPF_BonePose> sourceEntries = new Dictionary<string, FPF_BonePose>();
            Dictionary<string, int> targetBoneIndexes = new Dictionary<string, int>();

            for (int i = 0; i < sourceBones.Count; i++)
                sourceEntries.Add(sourceBones[i].Name, mainEntry.BonePoses[i]);

            for (int i = 0; i < targetBones.Count; i++)
                targetBoneIndexes.Add(targetBones[i].Name, i);

            List<FPF_BonePose> remappedBonePoses = new List<FPF_BonePose>();

            for (int i = 0; i < targetBones.Count; i++)
            {
                FPF_BonePose sourceEntry;

                if (sourceEntries.TryGetValue(targetBones[i].Name, out sourceEntry))
                {
                    remappedBonePoses.Add(sourceEntry.Copy(i));
                }
                else
                {
                    remappedBonePoses.Add(FPF_BonePose.CreateForNewBone(targetBones[i], remappedBonePoses));
                }
            }

            mainEntry.BonePoses = remappedBonePoses;
            BoneIndexes.Remap(sourceBones.Select(bone => bone.Name).ToList(), targetBoneIndexes);
            ValidateMainSkeleton(targetBones.Select(bone => bone.Name).ToList());
        }

        public List<byte> Write()
        {
            if (BoneIndexes == null) throw new InvalidDataException("BoneIndexes is required.");

            List<FPF_Entry> entries = Entries?.OrderBy(entry => entry.ID).ToList() ?? new List<FPF_Entry>();
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(FPF_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(CharacterID));
            bytes.AddRange(BitConverter.GetBytes(Costume));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(F_36));
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));
            bytes.AddRange(BitConverter.GetBytes(FigurePositionX));
            bytes.AddRange(BitConverter.GetBytes(FigurePositionY));
            bytes.AddRange(BitConverter.GetBytes(FigurePositionZ));
            bytes.AddRange(BitConverter.GetBytes(I_60));
            bytes.AddRange(BitConverter.GetBytes(I_64));
            bytes.AddRange(BitConverter.GetBytes(I_68));
            bytes.AddRange(BitConverter.GetBytes(I_72));
            bytes.AddRange(BitConverter.GetBytes(I_76));
            bytes.AddRange(BitConverter.GetBytes(I_80));
            bytes.AddRange(BitConverter.GetBytes(I_84));
            bytes.AddRange(BitConverter.GetBytes(I_88));
            bytes.AddRange(BitConverter.GetBytes(I_92));
            bytes.AddRange(BitConverter.GetBytes(F_96));
            bytes.AddRange(BitConverter.GetBytes(I_100));
            bytes.AddRange(BitConverter.GetBytes(F_104));
            bytes.AddRange(BitConverter.GetBytes(I_108));
            bytes.AddRange(BoneIndexes.Write());

            if (bytes.Count != EntryPointerListOffset)
                throw new InvalidDataException("FPF header is an invalid size.");

            bytes.AddRange(new byte[EntryPointerListEntryCount * EntryPointerListEntrySize]);

            foreach (FPF_Entry entry in entries)
            {
                if (entry.ID < 0 || entry.ID >= EntryPointerListEntryCount)
                    throw new InvalidDataException(String.Format("FPF entry ID {0} is out of range.", entry.ID));

                int pointerOffset = EntryPointerListOffset + entry.ID * EntryPointerListEntrySize;
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), pointerOffset);
                bytes.AddRange(entry.Write());
            }

            return bytes;
        }
    }

    public class FPF_BoneIndexList
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BoneIndex")]
        public List<int> Indexes { get; set; }

        public static FPF_BoneIndexList Read(byte[] rawBytes, int offset)
        {
            return new FPF_BoneIndexList
            {
                Indexes = BitConverter_Ex.ToInt32Array(rawBytes, offset, FPF_File.BoneIndexListCount).ToList()
            };
        }

        public void ValidateAgainstBoneCount(int boneCount)
        {
            if (Indexes == null || Indexes.Count != FPF_File.BoneIndexListCount)
                throw new InvalidDataException(String.Format("BoneIndexes must contain exactly {0} entries.", FPF_File.BoneIndexListCount));

            for (int i = 0; i < Indexes.Count; i++)
            {
                if (Indexes[i] < -1 || Indexes[i] >= boneCount)
                    throw new InvalidDataException(String.Format("BoneIndexes entry {0} points to invalid bone index {1}.", i, Indexes[i]));
            }
        }

        public void Remap(IList<string> sourceBoneNames, IDictionary<string, int> targetBoneIndexes)
        {
            if (sourceBoneNames == null) throw new ArgumentNullException(nameof(sourceBoneNames));
            if (targetBoneIndexes == null) throw new ArgumentNullException(nameof(targetBoneIndexes));
            if (Indexes == null || Indexes.Count != FPF_File.BoneIndexListCount)
                throw new InvalidDataException(String.Format("BoneIndexes must contain exactly {0} entries.", FPF_File.BoneIndexListCount));

            for (int i = 0; i < Indexes.Count; i++)
            {
                int oldIndex = Indexes[i];

                if (oldIndex >= 0)
                {
                    if (oldIndex >= sourceBoneNames.Count)
                        throw new InvalidDataException(String.Format("BoneIndexes entry {0} points to invalid source bone index {1}.", i, oldIndex));

                    string boneName = sourceBoneNames[oldIndex];
                    int newIndex;

                    if (!targetBoneIndexes.TryGetValue(boneName, out newIndex))
                        throw new InvalidDataException(String.Format("Target skeleton is missing bone \"{0}\".", boneName));

                    Indexes[i] = newIndex;
                }
            }
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            if (Indexes == null || Indexes.Count != FPF_File.BoneIndexListCount)
                throw new InvalidDataException(String.Format("BoneIndexes must contain exactly {0} entries.", FPF_File.BoneIndexListCount));

            bytes.AddRange(BitConverter_Ex.GetBytes(Indexes.ToArray()));

            if (bytes.Count != FPF_File.BoneIndexListCount * 4)
                throw new InvalidDataException("BoneIndexes is an invalid size.");

            return bytes;
        }
    }

    public class FPF_Entry
    {
        [YAXAttributeForClass]
        public int ID { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("EntryType")]
        public int EntryType { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BonePose")]
        public List<FPF_BonePose> BonePoses { get; set; }

        public static FPF_Entry Read(byte[] rawBytes, int offset, int index)
        {
            int bonePoseCount = BitConverter.ToInt32(rawBytes, offset + 4);
            int bonePoseOffset = offset + 16;
            int endOffset = bonePoseOffset + bonePoseCount * FPF_File.FpfBonePoseSize;

            if (bonePoseCount < 0 || endOffset > rawBytes.Length)
                throw new InvalidDataException(String.Format("FPF entry {0} has an invalid bone pose count.", index));

            return new FPF_Entry
            {
                ID = index,
                EntryType = BitConverter.ToInt32(rawBytes, offset + 0),
                BonePoses = FPF_BonePose.ReadAll(rawBytes, bonePoseOffset, bonePoseCount)
            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();
            int bonePoseCount = BonePoses != null ? BonePoses.Count : 0;

            bytes.AddRange(BitConverter.GetBytes(EntryType));
            bytes.AddRange(BitConverter.GetBytes(bonePoseCount));
            bytes.AddRange(new byte[8]);

            for (int i = 0; i < bonePoseCount; i++)
                bytes.AddRange(BonePoses[i].Write());

            if (bytes.Count != FPF_File.FpfBonePoseSize * bonePoseCount + 16)
                throw new InvalidDataException("FPF_Entry is an invalid size.");

            return bytes;
        }
    }

    [YAXSerializeAs("BonePose")]
    public class FPF_BonePose
    {
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXComment("Local bind transform. Matches the skeleton relative matrix.")]
        public TransformMatrix4x4 RelativeTransform { get; set; }
        [YAXComment("Local baked pose transform. This affects the intro pose path.")]
        public TransformMatrix4x4 LocalPoseTransform { get; set; }
        [YAXComment("Absolute baked pose transform.")]
        public TransformMatrix4x4 AbsolutePoseTransform { get; set; }
        [YAXComment("Absolute baked pose transform used for attachments and scene placement.")]
        public TransformMatrix4x4 AttachmentPoseTransform { get; set; }
        [YAXComment("Formation skinning matrix. Stored as transpose(inverseBind * absolutePose).")]
        public TransformMatrix4x4 FormationSkinningTransform { get; set; }

        public static List<FPF_BonePose> ReadAll(byte[] rawBytes, int offset, int count)
        {
            List<FPF_BonePose> entries = new List<FPF_BonePose>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(rawBytes, offset, i));
                offset += FPF_File.FpfBonePoseSize;
            }

            return entries;
        }

        public static FPF_BonePose Read(byte[] rawBytes, int offset, int index)
        {
            return new FPF_BonePose
            {
                Index = index,
                RelativeTransform = TransformMatrix4x4.Read(rawBytes, offset + 0),
                LocalPoseTransform = TransformMatrix4x4.Read(rawBytes, offset + 64),
                AbsolutePoseTransform = TransformMatrix4x4.Read(rawBytes, offset + 128),
                AttachmentPoseTransform = TransformMatrix4x4.Read(rawBytes, offset + 192),
                FormationSkinningTransform = TransformMatrix4x4.Read(rawBytes, offset + 256)
            };
        }

        public static FPF_BonePose CreateForNewBone(ESK_Bone bone, IList<FPF_BonePose> targetEntries)
        {
            TransformMatrix4x4 localTransform = TransformMatrix4x4.FromRelativeTransform(bone.RelativeTransform);
            FPF_BonePose parentEntry = bone.Index1 >= 0 && bone.Index1 < targetEntries.Count ? targetEntries[bone.Index1] : null;
            TransformMatrix4x4 identity = TransformMatrix4x4.Identity();

            return new FPF_BonePose
            {
                Index = targetEntries.Count,
                RelativeTransform = localTransform.Copy(),
                LocalPoseTransform = localTransform.Copy(),
                AbsolutePoseTransform = parentEntry?.AbsolutePoseTransform.Copy() ?? identity.Copy(),
                AttachmentPoseTransform = parentEntry?.AttachmentPoseTransform.Copy() ?? identity.Copy(),
                FormationSkinningTransform = parentEntry?.FormationSkinningTransform.Copy() ?? identity.Copy()
            };
        }

        public FPF_BonePose Copy(int index)
        {
            return new FPF_BonePose
            {
                Index = index,
                RelativeTransform = RelativeTransform.Copy(),
                LocalPoseTransform = LocalPoseTransform.Copy(),
                AbsolutePoseTransform = AbsolutePoseTransform.Copy(),
                AttachmentPoseTransform = AttachmentPoseTransform.Copy(),
                FormationSkinningTransform = FormationSkinningTransform.Copy()
            };
        }

        public List<byte> Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(RelativeTransform.Write());
            bytes.AddRange(LocalPoseTransform.Write());
            bytes.AddRange(AbsolutePoseTransform.Write());
            bytes.AddRange(AttachmentPoseTransform.Write());
            bytes.AddRange(FormationSkinningTransform.Write());

            if (bytes.Count != FPF_File.FpfBonePoseSize)
                throw new InvalidDataException("FPF_BonePose is an invalid size.");

            return bytes;
        }
    }

    public class TransformMatrix4x4
    {
        [YAXAttributeFor("M11")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M11 { get; set; }
        [YAXAttributeFor("M12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M12 { get; set; }
        [YAXAttributeFor("M13")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M13 { get; set; }
        [YAXAttributeFor("M14")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M14 { get; set; }
        [YAXAttributeFor("M21")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M21 { get; set; }
        [YAXAttributeFor("M22")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M22 { get; set; }
        [YAXAttributeFor("M23")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M23 { get; set; }
        [YAXAttributeFor("M24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M24 { get; set; }
        [YAXAttributeFor("M31")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M31 { get; set; }
        [YAXAttributeFor("M32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M32 { get; set; }
        [YAXAttributeFor("M33")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M33 { get; set; }
        [YAXAttributeFor("M34")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M34 { get; set; }
        [YAXAttributeFor("M41")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M41 { get; set; }
        [YAXAttributeFor("M42")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M42 { get; set; }
        [YAXAttributeFor("M43")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M43 { get; set; }
        [YAXAttributeFor("M44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float M44 { get; set; }

        public static TransformMatrix4x4 Identity()
        {
            return new TransformMatrix4x4
            {
                M11 = 1f,
                M22 = 1f,
                M33 = 1f,
                M44 = 1f
            };
        }

        public static TransformMatrix4x4 FromRelativeTransform(ESK_RelativeTransform transform)
        {
            float positionX = transform.PositionX * transform.PositionW;
            float positionY = transform.PositionY * transform.PositionW;
            float positionZ = transform.PositionZ * transform.PositionW;
            float rotationX = transform.RotationX;
            float rotationY = transform.RotationY;
            float rotationZ = transform.RotationZ;
            float rotationW = transform.RotationW;
            float scaleX = transform.ScaleX * transform.ScaleW;
            float scaleY = transform.ScaleY * transform.ScaleW;
            float scaleZ = transform.ScaleZ * transform.ScaleW;
            float rotationLength = (float)Math.Sqrt(rotationX * rotationX + rotationY * rotationY + rotationZ * rotationZ + rotationW * rotationW);

            if (rotationLength > 0f)
            {
                rotationX /= rotationLength;
                rotationY /= rotationLength;
                rotationZ /= rotationLength;
                rotationW /= rotationLength;
            }

            float xx = rotationX * rotationX;
            float yy = rotationY * rotationY;
            float zz = rotationZ * rotationZ;
            float xy = rotationX * rotationY;
            float xz = rotationX * rotationZ;
            float yz = rotationY * rotationZ;
            float wx = rotationW * rotationX;
            float wy = rotationW * rotationY;
            float wz = rotationW * rotationZ;

            return new TransformMatrix4x4
            {
                M11 = (1f - 2f * (yy + zz)) * scaleX,
                M12 = 2f * (xy + wz) * scaleX,
                M13 = 2f * (xz - wy) * scaleX,
                M14 = 0f,
                M21 = 2f * (xy - wz) * scaleY,
                M22 = (1f - 2f * (xx + zz)) * scaleY,
                M23 = 2f * (yz + wx) * scaleY,
                M24 = 0f,
                M31 = 2f * (xz + wy) * scaleZ,
                M32 = 2f * (yz - wx) * scaleZ,
                M33 = (1f - 2f * (xx + yy)) * scaleZ,
                M34 = 0f,
                M41 = positionX,
                M42 = positionY,
                M43 = positionZ,
                M44 = 1f
            };
        }

        public static TransformMatrix4x4 Read(byte[] rawBytes, int offset)
        {
            return new TransformMatrix4x4
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

        public TransformMatrix4x4 Copy()
        {
            return new TransformMatrix4x4
            {
                M11 = M11,
                M12 = M12,
                M13 = M13,
                M14 = M14,
                M21 = M21,
                M22 = M22,
                M23 = M23,
                M24 = M24,
                M31 = M31,
                M32 = M32,
                M33 = M33,
                M34 = M34,
                M41 = M41,
                M42 = M42,
                M43 = M43,
                M44 = M44
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

            if (bytes.Count != 64)
                throw new InvalidDataException("TransformMatrix4x4 is an invalid size.");

            return bytes;
        }
    }
}
