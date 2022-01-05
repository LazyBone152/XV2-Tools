using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMG;
using YAXLib;

namespace Xv2CoreLib.EMO
{
    [YAXSerializeAs("EMO")]
    [Serializable]
    public class EMO_File
    {
        public const int EMO_SIGNATURE = 0x4F4D4523;

        [YAXAttributeFor("MaterialsCount")]
        [YAXSerializeAs("value")]
        public ushort MaterialsCount { get; set; }
        [YAXAttributeFor("Version")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int Version { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ulong I_24 { get; set; }


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Part")]
        public List<EMO_Part> Parts { get; set; } = new List<EMO_Part>();
        public Skeleton Skeleton { get; set; }

        #region XmlLoadSave
        public static void CreateXml(string path)
        {
            var file = Load(path);

            YAXSerializer serializer = new YAXSerializer(typeof(EMO_File));
            serializer.SerializeToFile(file, path + ".xml");
        }

        public static void ConvertFromXml(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));

            YAXSerializer serializer = new YAXSerializer(typeof(EMO_File), YAXSerializationOptions.DontSerializeNullObjects);
            var file = (EMO_File)serializer.DeserializeFromFile(xmlPath);

            file.SaveFile(saveLocation);
        }
        #endregion

        #region LoadSave
        public static EMO_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static EMO_File Load(byte[] bytes)
        {
            if (BitConverter.ToInt32(bytes, 0) != EMO_SIGNATURE)
                throw new InvalidDataException("EMO_File.Read: \"#EMO\" signature not found!");

            EMO_File emoFile = new EMO_File();

            //Header:
            emoFile.Version = BitConverter.ToInt32(bytes, 8);
            emoFile.I_24 = BitConverter.ToUInt64(bytes, 24);

            int partsHeaderOffset = BitConverter.ToInt32(bytes, 12);
            int skeletonOffset = BitConverter.ToInt32(bytes, 16);
            //int verticesOffset = BitConverter.ToInt32(bytes, 20); //Not needed for loading. Vertices will be loaded with the EMG.

            //Parts:
            emoFile.MaterialsCount = BitConverter.ToUInt16(bytes, partsHeaderOffset + 2);
            ushort partCount = BitConverter.ToUInt16(bytes, partsHeaderOffset + 0);
            int namesOffset = BitConverter.ToInt32(bytes, partsHeaderOffset + 4) + partsHeaderOffset;
            int partsOffset = partsHeaderOffset + 8;

            for (int i = 0; i < partCount; i++)
            {
                int partOffset = BitConverter.ToInt32(bytes, partsOffset + (4 * i)) + partsHeaderOffset;
                int nameOffset = BitConverter.ToInt32(bytes, namesOffset + (4 * i)) + partsHeaderOffset;

                emoFile.Parts.Add(EMO_Part.Read(bytes, partOffset, nameOffset));
            }

            //Skeleton:
            emoFile.Skeleton = Skeleton.Parse(bytes, skeletonOffset);

            return emoFile;
        }


        public void SaveFile(string path)
        {
            byte[] bytes = SaveToBytes();
            File.WriteAllBytes(path, bytes);
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int partsCount = Parts != null ? Parts.Count : 0;

            //Header:
            bytes.AddRange(BitConverter.GetBytes(EMO_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)32));
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(32)); //Parts offset (12). WIll always be 32 as it comes directly after the header.
            bytes.AddRange(BitConverter.GetBytes(0)); //Skeleton offset (16)
            bytes.AddRange(BitConverter.GetBytes(0)); //Vertices offset (20)
            bytes.AddRange(BitConverter.GetBytes(I_24));

            //PartsHeader:
            int partsHeaderStart = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes((ushort)partsCount));
            bytes.AddRange(BitConverter.GetBytes(MaterialsCount));
            bytes.AddRange(BitConverter.GetBytes(0)); //name pointers offset (36)

            int pointerList = bytes.Count;
            bytes.AddRange(new byte[partsCount * 4]);

            //Add padding to keep alignment
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

            for (int i = 0; i < partsCount; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - partsHeaderStart), pointerList + (4 * i));
                bytes.AddRange(Parts[i].Write(bytes.Count));
            }

            //Write part names
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - partsHeaderStart), partsHeaderStart + 4);
            pointerList = bytes.Count;
            bytes.AddRange(new byte[partsCount * 4]);

            //Add padding to keep alignment
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

            for (int i = 0; i < partsCount; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - partsHeaderStart), pointerList + (4 * i));
                bytes.AddRange(Encoding.UTF8.GetBytes(Parts[i].Name));
                bytes.Add(0);
            }


            //Skeleton:
            if (Skeleton != null)
            {
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 16);

                bytes.AddRange(Skeleton.Write());
            }

            //Vertices:
            if (partsCount > 0)
            {
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 20);

                foreach (var part in Parts)
                {
                    foreach (var emg in part.EmgFiles)
                    {
                        foreach (var mesh in emg.Mesh)
                        {
                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - mesh.StartOffset), mesh.VertexOffset);
                            bytes.AddRange(EMD_Vertex.GetBytes(mesh.Vertices, mesh.VertexFlags));
                        }
                    }
                }
            }

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }
        #endregion

    }

    [YAXSerializeAs("Part")]
    [Serializable]
    public class EMO_Part
    {
        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EMG")]
        public List<EMG_File> EmgFiles { get; set; } = new List<EMG_File>();

        public static EMO_Part Read(byte[] bytes, int offset, int nameOffset)
        {
            EMO_Part part = new EMO_Part();
            part.Name = StringEx.GetString(bytes, nameOffset, false, StringEx.EncodingType.UTF8);
            int count = BitConverter.ToInt32(bytes, offset);

            for(int i = 0; i < count; i++)
            {
                int emgOffset = BitConverter.ToInt32(bytes, offset + 4 + (4 * i)) + offset;
                part.EmgFiles.Add(EMG_File.Read(bytes, emgOffset));
            }

            return part;
        }
    
        public List<byte> Write(int absOffset)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(EmgFiles.Count));

            //Emg file offsets
            bytes.AddRange(new byte[4 * EmgFiles.Count]);

            //Padding
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

            for(int i = 0; i < EmgFiles.Count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 4 + (4 * i));
                bytes.AddRange(EmgFiles[i].Write(false, absOffset + bytes.Count));
            }

            return bytes;
        }
    }
}
