using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using YAXLib;

namespace Xv2CoreLib.Iggy
{
    //NOT A COMPLETE PARSER. JUST EXISTS FOR TESTING/RESEARCH PURPOSES RIGHT NOW!

    [YAXSerializeAs("Iggy")]
    public class IggyFile
    {
        public const uint SIGNATURE = 0xED0A6749;
        public const int HEADER_SIZE = 32;

        [YAXAttributeForClass]
        public int Version { get; set; }
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public byte[] Platform { get; set; }
        [YAXAttributeForClass]
        public int I_12 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "IggySubFile")]
        public List<IggySubFile> SubFiles { get; set; } = new List<IggySubFile>();

        #region LoadSave
        public static void CreateXml(string path)
        {
            IggyFile file = Load(File.ReadAllBytes(path));

            YAXSerializer serializer = new YAXSerializer(typeof(IggyFile));
            serializer.SerializeToFile(file, path + ".xml");
        }

        public static IggyFile Load(byte[] bytes)
        {
            IggyFile iggy = new IggyFile();

            if (BitConverter.ToUInt32(bytes, 0) != SIGNATURE)
                return null; //throw new InvalidDataException("IggyFile.Load: This is not a valid iggy file (signature not found).");

            //Header
            iggy.Version = BitConverter.ToInt32(bytes, 4);
            iggy.Platform = bytes.GetRange(8, 4);
            iggy.I_12 = BitConverter.ToInt32(bytes, 12);

            if (iggy.Version != 0x900)
                throw new InvalidDataException($"IggyFile.Load: Unsupported version ({HexConverter.GetHexString(iggy.Version)})");

            if (iggy.Platform[1] != 0x40)
                throw new InvalidDataException($"IggyFile.Load: Unsupported bit size ({HexConverter.GetHexString(iggy.Platform[1])})");

            int subFilesCount = BitConverter.ToInt32(bytes, 28);

            //Sub Files
            for(int i = 0; i < subFilesCount; i++)
            {
                int offset = HEADER_SIZE + (IggySubFile.SIZE * i);

                IggySubFile subFile = new IggySubFile();
                subFile.ID = BitConverter.ToInt32(bytes, offset);
                int size = BitConverter.ToInt32(bytes, offset + 4);
                int size2 = BitConverter.ToInt32(bytes, offset + 8);
                int flashHeaderOffset = BitConverter.ToInt32(bytes, offset + 12); //Absolute

                if (size != size2)
                {
                    throw new Exception("Size1 and Size2 mismatch");
                }

                //"ID" seems to be type. 1 contains a flash file, 0 is something else...
                if(subFile.ID == 1)
                {
                    //Flash Header
                    if (!IsNullPointer(flashHeaderOffset))
                    {
                        //The offsets are technically 64 bit values here (in 64 bit iggy), but since we are loading it via a byte array we are limited to int32 anyway (byte[] indexer is only 32 bits)
                        int flashObjectsOffset = BitConverter.ToInt32(bytes, flashHeaderOffset) + flashHeaderOffset;
                        int flashObjectCount = BitConverter.ToInt32(bytes, flashHeaderOffset + 64);

                        for (int a = 0; a < flashObjectCount; a++)
                        {
                            int currentPos = flashObjectsOffset + (8 * a);
                            int flashObjectOffset = BitConverter.ToInt32(bytes, currentPos);

                            if (!IsNullPointer(flashObjectOffset))
                            {
                                flashObjectOffset += currentPos;

                                IggyObject flashObject = new IggyObject();

                                flashObject.Type = bytes[flashObjectOffset];
                                flashObject.I_01 = bytes[flashObjectOffset + 1];
                                flashObject.ID = BitConverter.ToInt16(bytes, flashObjectOffset + 2);
                                flashObject.Offset = flashObjectOffset;

                                switch (flashObject.Type)
                                {
                                    case 4:
                                        flashObject.Type4 = IggyType4.Read(bytes, flashObjectOffset + 32);
                                        break;
                                    case 6:
                                        flashObject.Type6 = IggyType6.Read(bytes, flashObjectOffset + 32);
                                        break;
                                }

                                /*
                                if(flashObject.Type == 1)
                                {
                                    int subCount = BitConverter.ToInt32(bytes, flashObjectOffset + 32 + 24);

                                    if(subCount > 3)
                                    {
                                        Console.WriteLine(flashObjectOffset);
                                        Console.ReadLine();
                                    }
                                }
                                */

                                subFile.IggyObjects.Add(flashObject);
                            }
                        }

                    }

                    //Deubg: Set sizes for flash objects
                    for (int a = 0; a < subFile.IggyObjects.Count; a++)
                    {
                        if (a != subFile.IggyObjects.Count - 1)
                        {
                            subFile.IggyObjects[a].Size = subFile.IggyObjects[a + 1].Offset - subFile.IggyObjects[a].Offset;

                            if(subFile.IggyObjects[a].Type == 4 && subFile.IggyObjects[a].Size != 88)
                            {
                                throw new Exception("...");
                            }
                        }
                    }
                }


                iggy.SubFiles.Add(subFile);
            }


            return iggy;
        }

        private static bool IsNullPointer(int pointer)
        {
            return pointer == 0 || pointer == 1;
        }
        #endregion
    }

    public class IggySubFile
    {
        public const int SIZE = 16;

        [YAXAttributeForClass]
        public int ID { get; set; }

        public List<IggyObject> IggyObjects { get; set; } = new List<IggyObject>();
    }

    public class IggyObject
    {
        public const int SIZE_32 = 128;
        public const int SIZE_64 = 184;

        [YAXAttributeForClass]
        public byte Type { get; set; }
        [YAXAttributeForClass]
        public byte I_01 { get; set; }
        [YAXAttributeForClass]
        public int ID { get; set; }

        [YAXAttributeForClass]
        public int Offset { get; set; }
        [YAXAttributeForClass]
        public int Size { get; set; }


        [YAXDontSerializeIfNull]
        public IggyType4 Type4 { get; set; }
        [YAXDontSerializeIfNull]
        public IggyType6 Type6 { get; set; }
    }

    public class IggyType4
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }

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

        public static IggyType4 Read(byte[] bytes, int offset)
        {
            IggyType4 type = new IggyType4();

            type.I_00 = BitConverter.ToUInt32(bytes, offset + 0);
            type.I_04 = BitConverter.ToUInt32(bytes, offset + 4);
            type.I_08 = BitConverter.ToInt32(bytes, offset + 8);
            type.I_12 = BitConverter.ToInt32(bytes, offset + 12);
            type.F_16 = BitConverter.ToSingle(bytes, offset + 16);
            type.F_20 = BitConverter.ToSingle(bytes, offset + 20);
            type.I_32 = BitConverter.ToInt32(bytes, offset + 32);
            type.I_36 = BitConverter.ToInt32(bytes, offset + 36);
            type.I_40 = BitConverter.ToInt32(bytes, offset + 40);
            type.I_44 = BitConverter.ToInt32(bytes, offset + 44);
            type.I_48 = BitConverter.ToInt32(bytes, offset + 48);
            type.I_52 = BitConverter.ToInt32(bytes, offset + 52);


            return type;
        }
    }

    public class IggyType6
    {
        [YAXAttributeFor("F_00")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_12 { get; set; }


        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
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
        public ushort I_44 { get; set; }
        [YAXAttributeFor("I_46")]
        [YAXSerializeAs("value")]
        public ushort I_46 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("I_50")]
        [YAXSerializeAs("value")]
        public ushort I_50 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public ushort I_52 { get; set; }
        [YAXAttributeFor("I_54")]
        [YAXSerializeAs("value")]
        public ushort I_54 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public ushort I_56 { get; set; }
        [YAXAttributeFor("I_58")]
        [YAXSerializeAs("value")]
        public ushort I_58 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public ushort I_60 { get; set; }
        [YAXAttributeFor("I_62")]
        [YAXSerializeAs("value")]
        public ushort I_62 { get; set; }

        public string EmbeddedElement { get; set; }
        [YAXAttributeForClass]
        public string Font { get; set; }

        public static IggyType6 Read(byte[] bytes, int offset)
        {
            IggyType6 type = new IggyType6();

            type.F_00 = BitConverter.ToSingle(bytes, offset + 0);
            type.F_04 = BitConverter.ToSingle(bytes, offset + 4);
            type.F_08 = BitConverter.ToSingle(bytes, offset + 8);
            type.F_12 = BitConverter.ToSingle(bytes, offset + 12);
            type.I_16 = BitConverter.ToInt32(bytes, offset + 16);
            type.I_20 = BitConverter.ToInt32(bytes, offset + 20);
            type.I_32 = BitConverter.ToInt32(bytes, offset + 32);
            type.I_36 = BitConverter.ToInt32(bytes, offset + 36);
            type.I_40 = BitConverter.ToInt32(bytes, offset + 40);
            type.I_44 = BitConverter.ToUInt16(bytes, offset + 44);
            type.I_46 = BitConverter.ToUInt16(bytes, offset + 46);
            type.I_48 = BitConverter.ToUInt16(bytes, offset + 48);
            type.I_50 = BitConverter.ToUInt16(bytes, offset + 50);
            type.I_52 = BitConverter.ToUInt16(bytes, offset + 52);
            type.I_54 = BitConverter.ToUInt16(bytes, offset + 54);
            type.I_56 = BitConverter.ToUInt16(bytes, offset + 56);
            type.I_58 = BitConverter.ToUInt16(bytes, offset + 58);
            type.I_60 = BitConverter.ToUInt16(bytes, offset + 60);
            type.I_62 = BitConverter.ToUInt16(bytes, offset + 62);

            int fontOffset = BitConverter.ToInt32(bytes, offset + 24);
            int xmlOffset = BitConverter.ToInt32(bytes, offset + 64);

            if(fontOffset != 0)
            {
                type.Font = StringEx.GetString(bytes, offset + 24 + fontOffset, false, StringEx.EncodingType.Unicode);
            }

            if (xmlOffset != 0)
            {
                type.EmbeddedElement = StringEx.GetString(bytes, offset + 64 + xmlOffset, false, StringEx.EncodingType.Unicode);
            }

            return type;
        }
    }
}
