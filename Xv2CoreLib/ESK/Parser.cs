using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.ESK
{
    public class Parser
    {
        public ESK_File eskFile { private set; get; }
        byte[] rawBytes { get; set; }
        List<byte> bytes { get; set; }
        private static int _index = 0;


        public Parser(string location, bool writeXml)
        {
            eskFile = new ESK_File();
            rawBytes = File.ReadAllBytes(location);
            bytes = rawBytes.ToList();
            Parse();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(ESK_File));
                serializer.SerializeToFile(eskFile, location + ".xml");
            }

        }

        public Parser(byte[] _bytes)
        {
            eskFile = new ESK_File();
            rawBytes = _bytes;
            bytes = rawBytes.ToList();
            Parse();
        }

        private void Parse()
        {

            //Header
            eskFile.I_12 = BitConverter.ToInt32(rawBytes, 12);
            eskFile.I_20 = BitConverter.ToInt32(rawBytes, 20);
            eskFile.I_24 = BitConverter.ToInt32(rawBytes, 24);

            //Skeleton
            ParseSkeleton(BitConverter.ToInt32(rawBytes, 16));
        }

        private void ParseSkeleton(int offset)
        {
            //Init
            int unk1Offset = BitConverter.ToInt32(rawBytes, offset + 20) + offset;
            int unk2Offset = BitConverter.ToInt32(rawBytes, offset + 24) + offset;

            //Skeleton init
            eskFile.Skeleton = new ESK_Skeleton()
            {
                I_02 = BitConverter.ToInt16(rawBytes, offset + 2),
                I_28 = BitConverter_Ex.ToInt32Array(rawBytes, offset + 28, 2),
                Unk1 = ESK_Unk1.Read(rawBytes, unk1Offset),
                UseUnk2 = (unk2Offset != 0) ? true : false,
                ESKBones = new ObservableCollection<ESK_Bone>()
            };

            //Setting the offsets for the initial loop to use
            int[] offsets = GetBoneOffset(0, offset);
            int boneIndexOffset = offsets[0];
            int nameOffset = offsets[1];
            int skinningMatrixOffset = offsets[2];
            int transformMatrixOffset = offsets[3];

            while(true)
            {
                int idx = eskFile.Skeleton.ESKBones.Count;
                ESK_Bone parent = ESK_Bone.Read(bytes, rawBytes, offsets, _index, null);
                eskFile.Skeleton.ESKBones.Add(parent);
                _index += 1;
                if (BitConverter.ToInt16(rawBytes, boneIndexOffset + 2) != -1)
                {
                    eskFile.Skeleton.ESKBones[idx].ESK_Bones = ParseChildrenBones(BitConverter.ToInt16(rawBytes, boneIndexOffset + 2), offset, parent);
                }
                //Loop management
                if (BitConverter.ToInt16(rawBytes, boneIndexOffset + 4) != -1)
                {
                    //There is a sibling
                    offsets = GetBoneOffset(BitConverter.ToInt16(rawBytes, boneIndexOffset + 4), offset);
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

        }


        private ObservableCollection<ESK_Bone> ParseChildrenBones(int indexOfFirstSibling, int offset, ESK_Bone parent)
        {
            ObservableCollection<ESK_Bone> newBones = new ObservableCollection<ESK_Bone>();

            int[] offsets = GetBoneOffset(indexOfFirstSibling, offset);
            int boneIndexOffset = offsets[0];
            int nameOffset = offsets[1];
            int skinningMatrixOffset = offsets[2];
            int transformMatrixOffset = offsets[3];

            while (true)
            {
                int idx = newBones.Count;
                ESK_Bone bone = ESK_Bone.Read(bytes, rawBytes, offsets, _index, parent);
                newBones.Add(bone);
                _index += 1;
                if (BitConverter.ToInt16(rawBytes, boneIndexOffset + 2) != -1)
                {
                    newBones[idx].ESK_Bones = ParseChildrenBones(BitConverter.ToInt16(rawBytes, boneIndexOffset + 2), offset, bone);
                }
                //Loop management
                if (BitConverter.ToInt16(rawBytes, boneIndexOffset + 4) != -1)
                {
                    //There is a sibling
                    offsets = GetBoneOffset(BitConverter.ToInt16(rawBytes, boneIndexOffset + 4), offset);
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

        //Helper methods below

        /// <summary>
        /// Returns the offsets for the bone indexes[0], name[1], Skinning Matrix[2], and Transform Matrix[3], in that order.
        /// </summary>
        private int[] GetBoneOffset(int index, int SkeletonOffset)
        {
            if(BitConverter.ToInt16(rawBytes, SkeletonOffset + 0) - 1 < index)
            {
                throw new Exception("BoneIndex is greater than BoneCount.");
            }

            int boneIndexTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 4) + SkeletonOffset;
            int nameTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 8) + SkeletonOffset;
            int skinningMatrixTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 12) + SkeletonOffset;
            int transformMatrixTableOffset = BitConverter.ToInt32(rawBytes, SkeletonOffset + 16) + SkeletonOffset;

            //Calc offsets
            int boneIndex = (8 * index) + boneIndexTableOffset;
            int nameTable = BitConverter.ToInt32(rawBytes, (4 * index) + nameTableOffset) + SkeletonOffset; //Points to the actual string, not the table
            int skinningMatrix = (48 * index) + skinningMatrixTableOffset;
            int transformMatrix = (64 * index) + transformMatrixTableOffset;

            return new int[4] { boneIndex, nameTable, skinningMatrix, transformMatrix};
        }
        
        
        private void ValidateSkeletonUnk2(int offset, int count)
        {

            for (int i = 0; i < count; i++)
            {
                if (BitConverter.ToInt64(rawBytes, offset) != 281470681743360)
                {
                    throw new Exception("Parse failed. Skeleton Unk2 Mismatch!");
                }
                offset += 8;
            }
        }

    }
}
