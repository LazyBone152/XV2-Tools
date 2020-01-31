using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.ESK
{
    public class Deserializer
    {
        string saveLocation;
        ESK_File eskFile;
        public List<byte> bytes = new List<byte>() { 35, 69, 83, 75, 254, 255, 28, 0, 192, 146,0,0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(ESK_File), YAXSerializationOptions.DontSerializeNullObjects);
            eskFile = (ESK_File)serializer.DeserializeFromFile(location);
            
            WriteFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(ESK_File _eskFile)
        {
            eskFile = _eskFile;
            WriteFile();
        }

        public Deserializer(ESK_File _eskFile, string path)
        {
            eskFile = _eskFile;
            WriteFile();
            File.WriteAllBytes(path, bytes.ToArray());
        }

        private void WriteFile()
        {
            //Header
            bytes.AddRange(BitConverter.GetBytes(eskFile.I_12));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(eskFile.I_20));
            bytes.AddRange(BitConverter.GetBytes(eskFile.I_24));
            bytes.AddRange(new byte[4]);

            //Skeleton
            WriteSkeleton(eskFile.Skeleton, 16);
        }

        private void WriteSkeleton(ESK_Skeleton skeleton, int offsetToReplace)
        {
            List<ESK_BoneNonHierarchal> bones = eskFile.Skeleton.GetNonHierarchalBoneList();

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), offsetToReplace);

            int startOffset = bytes.Count();
            int count = (bones != null) ? bones.Count() : 0;

            bytes.AddRange(BitConverter.GetBytes((short)count));
            bytes.AddRange(BitConverter.GetBytes(skeleton.I_02));
            bytes.AddRange(new byte[24]);
            bytes.AddRange(BitConverter_Ex.GetBytes(skeleton.I_28));

            if (count > 0)
            {
                //Writing Index List
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 4);

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(bones[i].Index1));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].Index2));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].Index3));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].Index4));
                }

                //Writing Name Table and List
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 8);
                List<StringWriter.StringInfo> stringInfo = new List<StringWriter.StringInfo>();

                for (int i = 0; i < count; i++)
                {
                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        StringToWrite = bones[i].Name,
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
                StartNewLine();
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 12);

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_00));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_04));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_08));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_12));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_16));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_20));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_24));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_28));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_32));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_36));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_40));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].RelativeTransform.F_44));
                }

                //Writing AbsoluteTransform (esk only)
                StartNewLine();
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 16);

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_00));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_04));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_08));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_12));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_16));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_20));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_24));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_28));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_32));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_36));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_40));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_44));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_48));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_52));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_56));
                    bytes.AddRange(BitConverter.GetBytes(bones[i].AbsoluteTransform.F_60));
                }

                //Writing Unk1
                if (skeleton.Unk1 != null)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 20);
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_00));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_04));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_08));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_12));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_16));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_20));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_24));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_28));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_32));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_36));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_40));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_44));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_48));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_52));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_56));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_60));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_64));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_68));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_72));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_76));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_80));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_84));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_88));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_92));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_96));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_100));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_104));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_108));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_112));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_116));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Unk1.I_120));
                }

                //Writing Unk2
                if (skeleton.UseUnk2 == true && count > 0)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - startOffset), startOffset + 24);

                    for (int i = 0; i < count; i++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(281470681743360));
                    }
                }

            }


        }


        //Utility

        private void StartNewLine()
        {
            while (Convert.ToSingle(bytes.Count()) / 16 != Math.Floor(Convert.ToSingle(bytes.Count()) / 16))
            {
                bytes.Add(0);
            }
        }


    }
}
