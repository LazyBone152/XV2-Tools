using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.AMK
{
    public class AMK_File
    {
        public const int AMK_SIGNATURE = 1263354147;

        [YAXDontSerialize]
        public int NumEntries
        {
            get
            {
                SortEntries();
                if (Animations == null) return 0;
                if (Animations.Count == 0) return 0;
                return Animations[Animations.Count - 1].ID + 1;
            }
        }

        [YAXAttributeForClass]
        [YAXHexValue]
        public ushort I_06 { get; set; }
        [YAXAttributeForClass]
        [YAXHexValue]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        [YAXHexValue]
        public int I_12 { get; set; }
        [YAXAttributeForClass]
        [YAXHexValue]
        public int I_16 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AMK_Animation")]
        public List<AMK_Animation> Animations { get; set; }
        
        public void SortEntries()
        {
            if (Animations != null)
                Animations.Sort((x, y) => x.ID - y.ID);
        }

        public static AMK_File Read(string path, bool writeXml)
        {
            var amkFile = Read(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(AMK_File));
                serializer.SerializeToFile(amkFile, path + ".xml");
            }

            return amkFile;
        }

        public static AMK_File Read(byte[] bytes)
        {
            AMK_File amkFile = new AMK_File();

            //Header
            amkFile.I_06 = BitConverter.ToUInt16(bytes, 6);
            amkFile.I_08 = BitConverter.ToInt32(bytes, 8);
            amkFile.I_12 = BitConverter.ToInt32(bytes, 12);
            amkFile.I_16 = BitConverter.ToInt32(bytes, 16);

            int entryCount = BitConverter.ToInt32(bytes, 20);
            int entryOffset = BitConverter.ToInt32(bytes, 24);
            int nameOffset = BitConverter.ToInt32(bytes, 28);

            if(entryCount > 0)
            {
                amkFile.Animations = new List<AMK_Animation>();

                for(int i = 0; i < entryCount; i++)
                {
                    var anim = AMK_Animation.Read(bytes, i, entryOffset, nameOffset);

                    if(anim != null)
                        amkFile.Animations.Add(anim);

                    entryOffset += 16;
                    nameOffset += 32;
                }
            }

            return amkFile;
        }

        public static void SaveXml(string xmlPath)
        {
            string path = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(AMK_File), YAXSerializationOptions.DontSerializeNullObjects);
            AMK_File amkFile = (AMK_File)serializer.DeserializeFromFile(xmlPath);
            amkFile.Save(path);
        }

        public void Save(string path)
        {
            byte[] bytes = Write();
            File.WriteAllBytes(path, bytes);
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int numEntries = NumEntries;

            //Header (32 bytes)
            bytes.AddRange(BitConverter.GetBytes(AMK_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)32));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(numEntries));
            bytes.AddRange(BitConverter.GetBytes(32)); //Entry offset
            bytes.AddRange(new byte[4]); //Names offset

            List<int> entryOffsets = new List<int>();

            SortEntries();

            //Anim header
            for(int i = 0; i < numEntries; i++)
            {
                AMK_Animation anim = Animations.Find(a => a.ID == i);

                if(anim != null)
                {
                    //Anim exists
                    int keyframeCount = (anim.Keyframes != null) ? anim.Keyframes.Count : 0;

                    bytes.AddRange(BitConverter.GetBytes(anim.I_00));
                    bytes.AddRange(BitConverter.GetBytes(keyframeCount));
                    bytes.AddRange(BitConverter.GetBytes(anim.I_08));
                    entryOffsets.Add(bytes.Count);
                    bytes.AddRange(new byte[4]);
                }
                else
                {
                    //Anim is null, write dummy data
                    bytes.AddRange(new byte[16]);
                }
            }

            //Anim data
            int index = 0;
            for (int i = 0; i < numEntries; i++)
            {
                AMK_Animation anim = Animations.Find(a => a.ID == i);

                if (anim != null)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), entryOffsets[index]);

                    int keyframeCount = (anim.Keyframes != null) ? anim.Keyframes.Count : 0;

                    for (int a = 0; a < keyframeCount; a++)
                    {
                        bytes.AddRange(anim.Keyframes[a].Write());
                    }

                    index++;
                }
            }

            //Names
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 28);
            for (int i = 0; i < numEntries; i++)
            {
                AMK_Animation anim = Animations.Find(a => a.ID == i);

                if (anim != null)
                {
                    bytes.AddRange(Utils.GetStringBytes(anim.Name, 32));
                }
                else
                {
                    //Anim is null, write dummy data
                    bytes.AddRange(new byte[32]);
                }
            }

            return bytes.ToArray();
        }

        public static AMK_File Load(byte[] bytes)
        {
            return Read(bytes);
        }
    }

    public class AMK_Animation
    {
        [YAXAttributeForClass]
        public int ID { get; set; }
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXHexValue]
        public int I_08 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AMK_Keyframe")]
        public List<AMK_Keyframe> Keyframes { get; set; }

        public static AMK_Animation Read(byte[] bytes, int index, int offset, int nameOffset)
        {
            int dataCount = BitConverter.ToInt32(bytes, offset + 4);
            int dataOffset = BitConverter.ToInt32(bytes, offset + 12);
            if (dataOffset == 0) return null;

            AMK_Animation anim = new AMK_Animation();
            anim.ID = index;
            anim.Keyframes = new List<AMK_Keyframe>();
            anim.Name = StringEx.GetString(bytes, nameOffset, false, StringEx.EncodingType.ASCII, 32, true);
            anim.I_00 = BitConverter.ToInt32(bytes, offset + 0);
            anim.I_08 = BitConverter.ToInt32(bytes, offset + 8);

            for (int i = 0; i < dataCount; i++)
            {
                anim.Keyframes.Add(AMK_Keyframe.Read(bytes, dataOffset + (4 * i)));
            }

            return anim;
        }
        
    }

    public class AMK_Keyframe
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Frame")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_02")]
        public byte I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_03")]
        public byte I_03 { get; set; }

        public static AMK_Keyframe Read(byte[] bytes, int offset)
        {
            return new AMK_Keyframe()
            {
                I_00 = BitConverter.ToUInt16(bytes, offset + 0),
                I_02 = bytes[offset + 2],
                I_03 = bytes[offset + 3],
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.Add(I_02);
            bytes.Add(I_03);

            return bytes.ToArray();
        }
    }
}
