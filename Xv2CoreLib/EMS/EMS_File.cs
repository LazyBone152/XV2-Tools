using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.EMS
{
    [YAXSerializeAs("EMS")]
    public class EMS_File
    {
        public const int SIGNATURE = 1397572899;
        public const int HEADER_SIZE = 48;

        [YAXAttributeForClass]
        public int Version { get; set; } = 37568;
        [YAXAttributeForClass]
        public int I_40 { get; set; }
        [YAXAttributeForClass]
        public int I_44 { get; set; }

        public List<EMS_Node> Nodes { get; set; } = new List<EMS_Node>();
        public List<EMS_Sprite> Sprites { get; set; } = new List<EMS_Sprite>();

        #region SaveLoad
        public static void CreateXml(string path)
        {
            EMS_File file = Load(File.ReadAllBytes(path));

            YAXSerializer serializer = new YAXSerializer(typeof(EMS_File));
            serializer.SerializeToFile(file, path + ".xml");
        }

        public static EMS_File Load(byte[] bytes)
        {
            if (BitConverter.ToInt32(bytes, 0) != SIGNATURE)
                throw new InvalidDataException("#EMS signature not found. This is not a valid .ems file.");

            EMS_File emsFile = new EMS_File();

            //Header
            emsFile.Version = BitConverter.ToInt32(bytes, 8);
            emsFile.I_40 = BitConverter.ToInt32(bytes, 40);
            emsFile.I_44 = BitConverter.ToInt32(bytes, 44);

            int nodeCount = BitConverter.ToInt32(bytes, 12);
            int rootNodes = BitConverter.ToInt32(bytes, 16);
            int nodeOffset = BitConverter.ToInt32(bytes, 20);

            int spriteCount = BitConverter.ToInt32(bytes, 24);
            int sprite_Offset1 = BitConverter.ToInt32(bytes, 28);
            int sprite_Offset2 = BitConverter.ToInt32(bytes, 32);
            int sprite_Offset3 = BitConverter.ToInt32(bytes, 36);

            //Create index lists
            List<int> nodeIndexList = new List<int>();
            List<int> section2IndexList = new List<int>();
            List<int> section2IndexList2 = new List<int>();
            List<int> section2IndexList3 = new List<int>();

            for (int i = 0; i < nodeCount; i++)
            {
                nodeIndexList.Add(BitConverter.ToInt32(bytes, nodeOffset + (4 * i)));
            }

            for (int i = 0; i < spriteCount; i++)
            {
                section2IndexList.Add(BitConverter.ToInt32(bytes, sprite_Offset1 + (4 * i)));
                section2IndexList2.Add(BitConverter.ToInt32(bytes, sprite_Offset2 + (4 * i)));
                section2IndexList3.Add(BitConverter.ToInt32(bytes, sprite_Offset3 + (4 * i)));
            }

            //Read nodes
            for (int i = 0; i < nodeCount; i++)
            {
                int offset = nodeIndexList[i];

                int childOffset = BitConverter.ToInt32(bytes, offset + 4);
                int siblingOffset = BitConverter.ToInt32(bytes, offset + 8);
                int prevOffset = BitConverter.ToInt32(bytes, offset + 12);

                //todo: remove after test
                if (BitConverter.ToInt32(bytes, offset + 20) != i)
                {
                    throw new Exception("Node index mismatch");
                }

                emsFile.Nodes.Add(new EMS_Node()
                {
                    Name = StringEx.GetString(bytes, offset + 32, false, StringEx.EncodingType.UTF8, 32),
                    Index = nodeIndexList.IndexOf(offset),
                    ChildIndex = nodeIndexList.IndexOf(childOffset),
                    SiblingIndex = nodeIndexList.IndexOf(siblingOffset),
                    PreviousNodeIndex = nodeIndexList.IndexOf(prevOffset),
                    I_00 = BitConverter.ToInt32(bytes, offset + 0),
                    SpriteIndex = section2IndexList.IndexOf(BitConverter.ToInt32(bytes, offset + 16))
                });
            }

            emsFile.Nodes = EMS_Node.CreateRecursiveNodes(emsFile.Nodes, rootNodes);

            //Read sprites
            for (int i = 0; i < spriteCount; i++)
            {
                int offset = section2IndexList[i];
                EMS_Sprite sprite = new EMS_Sprite();

                int animatedPart1 = BitConverter.ToInt32(bytes, offset + 4);
                int animatedPart2 = BitConverter.ToInt32(bytes, offset + 8);
                int animatedPart3 = BitConverter.ToInt32(bytes, offset + 12);

                sprite.I_00 = BitConverter.ToUInt16(bytes, offset + 0);
                sprite.I_02 = BitConverter.ToUInt16(bytes, offset + 2);
                sprite.I_16 = BitConverter.ToInt32(bytes, offset + 16);
                sprite.I_20 = BitConverter.ToInt32(bytes, offset + 20);
                sprite.Index = BitConverter.ToInt32(bytes, offset + 24);
                sprite.I_28 = BitConverter.ToInt32(bytes, offset + 28);
                sprite.I_32 = BitConverter.ToInt32(bytes, offset + 32);
                sprite.F_36 = BitConverter.ToSingle(bytes, offset + 36);
                sprite.F_40 = BitConverter.ToSingle(bytes, offset + 40);
                sprite.F_44 = BitConverter.ToSingle(bytes, offset + 44);
                sprite.F_48 = BitConverter.ToSingle(bytes, offset + 48);
                sprite.Name = StringEx.GetString(bytes, offset + 52, false, StringEx.EncodingType.UTF8, 32);

                if (BitConverter.ToInt64(bytes, offset + 84) != 0 || BitConverter.ToInt32(bytes, offset + 92) != 0)
                    throw new Exception("Sprite: offset > 84 not padding.");

                //Read AnimationPart1
                if(animatedPart1 != 0)
                {
                    sprite.InstructionPart1 = new InstructionPart1();
                    sprite.InstructionPart1.I_00 = BitConverter.ToInt32(bytes, animatedPart1 + 0);
                    sprite.InstructionPart1.Component1_InstructionCount = BitConverter.ToUInt16(bytes, animatedPart1 + 6);
                    sprite.InstructionPart1.Component2_InstructionCount = BitConverter.ToUInt16(bytes, animatedPart1 + 10);

                    ushort component1_Count = BitConverter.ToUInt16(bytes, animatedPart1 + 4);
                    ushort component2_Count = BitConverter.ToUInt16(bytes, animatedPart1 + 8);
                    int component1_IndexOffset = BitConverter.ToInt32(bytes, animatedPart1 + 16);
                    int component1_DataOffset = BitConverter.ToInt32(bytes, animatedPart1 + 20);
                    int component2_IndexOffset = BitConverter.ToInt32(bytes, animatedPart1 + 24);
                    int component2_DataOffset = BitConverter.ToInt32(bytes, animatedPart1 + 28);

                    if(component1_Count > 0)
                    {
                        sprite.InstructionPart1.Component1_Keyframes = new List<EMS_AnimPart1_Component1>();

                        for (int a = 0; a < component1_Count; a++)
                        {
                            sprite.InstructionPart1.Component1_Keyframes.Add(EMS_AnimPart1_Component1.Read(bytes, component1_DataOffset + (32 * a), component1_IndexOffset + (4 * a)));
                        }
                    }

                    if (component2_Count > 0)
                    {
                        sprite.InstructionPart1.Component2_Keyframes = new List<EMS_AnimPart1_Component2>();

                        for (int a = 0; a < component2_Count; a++)
                        {
                            sprite.InstructionPart1.Component2_Keyframes.Add(EMS_AnimPart1_Component2.Read(bytes, component2_DataOffset + (32 * a), component2_IndexOffset + (4 * a)));
                        }
                    }

                }

                //Read AnimationPart2
                if (animatedPart2 != 0)
                {
                    sprite.InstructionPart2 = new InstructionPart2();
                    sprite.InstructionPart2.I_00 = BitConverter.ToInt32(bytes, animatedPart2 + 0);
                    sprite.InstructionPart2.Component1_InstructionCount = BitConverter.ToUInt16(bytes, animatedPart2 + 6);
                    sprite.InstructionPart2.Component2_InstructionCount = BitConverter.ToUInt16(bytes, animatedPart2 + 10);

                    ushort component1_Count = BitConverter.ToUInt16(bytes, animatedPart2 + 4);
                    ushort component2_Count = BitConverter.ToUInt16(bytes, animatedPart2 + 8);
                    int component1_IndexOffset = BitConverter.ToInt32(bytes, animatedPart2 + 16);
                    int component1_DataOffset = BitConverter.ToInt32(bytes, animatedPart2 + 20);
                    int component2_IndexOffset = BitConverter.ToInt32(bytes, animatedPart2 + 24);
                    int component2_DataOffset = BitConverter.ToInt32(bytes, animatedPart2 + 28);

                    if (component1_Count > 0)
                    {
                        sprite.InstructionPart2.Component1_Keyframes = new List<EMS_AnimPart2_Component1>();

                        for (int a = 0; a < component1_Count; a++)
                        {
                            sprite.InstructionPart2.Component1_Keyframes.Add(EMS_AnimPart2_Component1.Read(bytes, component1_DataOffset + (32 * a), component1_IndexOffset + (4 * a)));
                        }
                    }

                    if (component2_Count > 0)
                    {
                        sprite.InstructionPart2.Component2_Keyframes = new List<EMS_AnimPart2_Component2>();

                        for (int a = 0; a < component2_Count; a++)
                        {
                            int extendedDataOffset = component2_DataOffset + (component2_Count * 32) + (64 * a);
                            sprite.InstructionPart2.Component2_Keyframes.Add(EMS_AnimPart2_Component2.Read(bytes, component2_DataOffset + (32 * a), component2_IndexOffset + (4 * a), extendedDataOffset));
                        }
                    }

                }

                //Read AnimationPart3
                if (animatedPart3 != 0)
                {
                    sprite.InstructionPart3 = new InstructionPart3();
                    sprite.InstructionPart3.I_00 = BitConverter.ToInt32(bytes, animatedPart3 + 0);
                    sprite.InstructionPart3.Component1_InstructionCount = BitConverter.ToUInt16(bytes, animatedPart3 + 6);
                    sprite.InstructionPart3.Component2_InstructionCount = BitConverter.ToUInt16(bytes, animatedPart3 + 10);
                    sprite.InstructionPart3.Component3_InstructionCount = BitConverter.ToUInt16(bytes, animatedPart3 + 14);
                    sprite.InstructionPart3.Component1_DummyCount = BitConverter.ToUInt16(bytes, animatedPart3 + 4);
                    sprite.InstructionPart3.Component2_DummyCount = BitConverter.ToUInt16(bytes, animatedPart3 + 8);
                    sprite.InstructionPart3.Component3_DummyCount = BitConverter.ToUInt16(bytes, animatedPart3 + 12);

                    ushort component1_Count = BitConverter.ToUInt16(bytes, animatedPart3 + 4);
                    ushort component2_Count = BitConverter.ToUInt16(bytes, animatedPart3 + 12);
                    int component1_IndexOffset = BitConverter.ToInt32(bytes, animatedPart3 + 24);
                    int component1_DataOffset = BitConverter.ToInt32(bytes, animatedPart3 + 28);
                    int component2_IndexOffset = BitConverter.ToInt32(bytes, animatedPart3 + 40);
                    int component2_DataOffset = BitConverter.ToInt32(bytes, animatedPart3 + 44);

                    if(BitConverter.ToInt32(bytes, animatedPart3 + 32) != 0 || BitConverter.ToInt32(bytes, animatedPart3 + 36) != 0)
                    {
                        throw new InvalidDataException("This EMS has a Component2 on its AnimatedPart3, which is not supported on the current version!");
                    }

                    if (component1_DataOffset > 0)
                    {
                        sprite.InstructionPart3.Component1_Keyframes = new List<EMS_AnimPart1_Component1>();

                        for (int a = 0; a < component1_Count; a++)
                        {
                            sprite.InstructionPart3.Component1_Keyframes.Add(EMS_AnimPart1_Component1.Read(bytes, component1_DataOffset + (32 * a), component1_IndexOffset + (4 * a)));
                        }
                    }

                    if (component2_DataOffset > 0)
                    {
                        sprite.InstructionPart3.Component3_Keyframes = new List<EMS_AnimPart2_Component2>();

                        for (int a = 0; a < component2_Count; a++)
                        {
                            int extendedDataOffset = component2_DataOffset + (component2_Count * 32) + (64 * a);
                            sprite.InstructionPart3.Component3_Keyframes.Add(EMS_AnimPart2_Component2.Read(bytes, component2_DataOffset + (32 * a), component2_IndexOffset + (4 * a), extendedDataOffset));
                        }
                    }
                }

                emsFile.Sprites.Add(sprite);
            }

            return emsFile;
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            List<EMS_Node> nodes = EMS_Node.CreateNonRecursiveNodes(Nodes);

            int nodeCount = nodes.Count;
            int rootNodeCount = Nodes != null ? Nodes.Count : 0;
            int spriteCount = Sprites != null ? Sprites.Count : 0;

            //Offsets get set even when count is zero

            //Header: (48 bytes)
            bytes.AddRange(BitConverter.GetBytes(SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)HEADER_SIZE));
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(nodeCount));
            bytes.AddRange(BitConverter.GetBytes(rootNodeCount));
            bytes.AddRange(BitConverter.GetBytes(HEADER_SIZE)); //20 = node offset
            bytes.AddRange(BitConverter.GetBytes(spriteCount));
            bytes.AddRange(BitConverter.GetBytes(0)); //28 = Sprite offset
            bytes.AddRange(BitConverter.GetBytes(0)); //32 = AnimatedPart1 index list offset
            bytes.AddRange(BitConverter.GetBytes(0)); //36 = AnimatedPart2 index list offset
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter.GetBytes(I_44));

            int spriteStart = HEADER_SIZE;
            int nodePointerListSize = (nodeCount * 4) + Utils.CalculatePadding(nodeCount * 4, 16);
            int spritePointerListSize = (spriteCount * 4) + Utils.CalculatePadding(spriteCount * 4, 16);

            int[] spriteReferenceOffset = new int[nodeCount];
            int[] spriteOffsets = new int[spriteCount];

            //Nodes
            if (nodeCount > 0)
            {
                //Create node pointer list (4 bytes for each node, plus padding to align it to 16 bytes)
                bytes.AddRange(new byte[nodePointerListSize]);
                int nodeStart = bytes.Count;

                spriteStart = nodeStart + (nodeCount * 64);

                for(int i = 0; i < nodeCount; i++)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), HEADER_SIZE + (i * 4));

                    int childNode = nodes[i].ChildIndex != -1 ? nodeStart + (nodes[i].ChildIndex * 64) : 0;
                    int siblingNode = nodes[i].SiblingIndex != -1 ? nodeStart + (nodes[i].SiblingIndex * 64) : 0;
                    int prevLogicNode = nodes[i].PreviousNodeIndex != -1 ? nodeStart + (nodes[i].PreviousNodeIndex * 64) : 0;

                    bytes.AddRange(BitConverter.GetBytes(nodes[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(childNode));
                    bytes.AddRange(BitConverter.GetBytes(siblingNode));
                    bytes.AddRange(BitConverter.GetBytes(prevLogicNode));

                    //Sprite offset. Save this so it can be filled in later when writing the sprites.
                    spriteReferenceOffset[i] = bytes.Count;
                    bytes.AddRange(BitConverter.GetBytes(0));

                    bytes.AddRange(BitConverter.GetBytes(i));
                    bytes.AddRange(BitConverter.GetBytes(0)); //padding
                    bytes.AddRange(BitConverter.GetBytes(0)); //padding

                    if (nodes[i].Name.Length > 32)
                        throw new Exception($"EMS_Node: Name cannot be greater than 32 characters ({nodes[i].Name})");

                    bytes.AddRange(StringEx.WriteFixedSizeString(nodes[i].Name, 32));
                }
            }

            //Just for validation... remove after testing
            if (bytes.Count != spriteStart)
                throw new Exception("EMS: invalid spriteStart");

            //Sprites
            if(spriteCount > 0)
            {
                int animPart1Start = spriteStart + spritePointerListSize;
                int animPart2Start = spriteStart + (spritePointerListSize * 2);

                //Set the offsets in the header
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(spriteStart), 28);
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(animPart1Start), 32);
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(animPart2Start), 36);

                //Create pointer lists for Sprites, AnimatedPart1 and AnimatedPart2 (AnimatedPart3 isn't indexed)
                bytes.AddRange(new byte[spritePointerListSize * 3]);

                for(int i = 0; i < spriteCount; i++)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), spriteStart + (4 * i));
                    int offset = bytes.Count;
                    spriteOffsets[i] = offset;

                    //Sprite Block
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(0)); //AnimatedPart1 offset
                    bytes.AddRange(BitConverter.GetBytes(0)); //AnimatedPart2 offset
                    bytes.AddRange(BitConverter.GetBytes(0)); //AnimatedPart3 offset
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].I_20));
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].Index));
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].I_28));
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].I_32));
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].F_36));
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].F_40));
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].F_44));
                    bytes.AddRange(BitConverter.GetBytes(Sprites[i].F_48));
                    bytes.AddRange(StringEx.WriteFixedSizeString(Sprites[i].Name, 32));

                    //Padding
                    bytes.AddRange(BitConverter.GetBytes(0));
                    bytes.AddRange(BitConverter.GetBytes(0));
                    bytes.AddRange(BitConverter.GetBytes(0));

                    //Write AnimationPart1
                    if(Sprites[i].InstructionPart1 != null)
                    {
                        int animPart1_Offset = bytes.Count;
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(animPart1_Offset), animPart1Start + (4 * i));
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(animPart1_Offset), offset + 4);

                        int component1_Count = Sprites[i].InstructionPart1.Component1_Keyframes != null ? Sprites[i].InstructionPart1.Component1_Keyframes.Count : 0;
                        int component2_Count = Sprites[i].InstructionPart1.Component2_Keyframes != null ? Sprites[i].InstructionPart1.Component2_Keyframes.Count : 0;

                        //AnimatedPart block
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart1.I_00));
                        bytes.AddRange(BitConverter.GetBytes((ushort)component1_Count));
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart1.Component1_InstructionCount));
                        bytes.AddRange(BitConverter.GetBytes((ushort)component2_Count));
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart1.Component2_InstructionCount));
                        bytes.AddRange(BitConverter.GetBytes(0)); //Padding
                        bytes.AddRange(BitConverter.GetBytes(0)); //16 = component 1 index list offset
                        bytes.AddRange(BitConverter.GetBytes(0)); //20 = component 1 data offset
                        bytes.AddRange(BitConverter.GetBytes(0)); //24 = component 2 index list offset
                        bytes.AddRange(BitConverter.GetBytes(0)); //28 = component 2 data offset

                        if(component1_Count > 0)
                        {
                            //Index list offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart1_Offset + 16);

                            //Frame Index List
                            foreach(var animPart in Sprites[i].InstructionPart1.Component1_Keyframes)
                            {
                                bytes.AddRange(BitConverter.GetBytes(animPart.Time));
                                bytes.AddRange(BitConverter.GetBytes(animPart.I_02));
                            }

                            bytes.AddRange(new byte[Utils.CalculatePadding(4 * component1_Count, 16)]);

                            //Data offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart1_Offset + 20);

                            //Data
                            foreach (var animPart in Sprites[i].InstructionPart1.Component1_Keyframes)
                            {
                                bytes.AddRange(animPart.Write());
                            }
                        }

                        if(component2_Count > 0)
                        {
                            //Index list offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart1_Offset + 24);

                            //Frame Index List
                            foreach (var animPart in Sprites[i].InstructionPart1.Component2_Keyframes)
                            {
                                bytes.AddRange(BitConverter.GetBytes(animPart.Time));
                                bytes.AddRange(BitConverter.GetBytes(animPart.I_02));
                            }

                            bytes.AddRange(new byte[Utils.CalculatePadding(4 * component2_Count, 16)]);

                            //Data offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart1_Offset + 28);

                            //Data
                            foreach (var animPart in Sprites[i].InstructionPart1.Component2_Keyframes)
                            {
                                bytes.AddRange(animPart.Write());
                            }
                        }
                    }

                    //Write AnimationPart2
                    if (Sprites[i].InstructionPart2 != null)
                    {
                        int animPart2_Offset = bytes.Count;
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(animPart2_Offset), animPart2Start + (4 * i));
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(animPart2_Offset), offset + 8);

                        int component1_Count = Sprites[i].InstructionPart2.Component1_Keyframes != null ? Sprites[i].InstructionPart2.Component1_Keyframes.Count : 0;
                        int component2_Count = Sprites[i].InstructionPart2.Component2_Keyframes != null ? Sprites[i].InstructionPart2.Component2_Keyframes.Count : 0;

                        //AnimatedPart block
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart2.I_00));
                        bytes.AddRange(BitConverter.GetBytes((ushort)component1_Count));
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart2.Component1_InstructionCount));
                        bytes.AddRange(BitConverter.GetBytes((ushort)component2_Count));
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart2.Component2_InstructionCount));
                        bytes.AddRange(BitConverter.GetBytes(0)); //Padding
                        bytes.AddRange(BitConverter.GetBytes(0)); //16 = component 1 index list offset
                        bytes.AddRange(BitConverter.GetBytes(0)); //20 = component 1 data offset
                        bytes.AddRange(BitConverter.GetBytes(0)); //24 = component 2 index list offset
                        bytes.AddRange(BitConverter.GetBytes(0)); //28 = component 2 data offset

                        if (component1_Count > 0)
                        {
                            //Index list offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart2_Offset + 16);

                            //Frame Index List
                            foreach (var animPart in Sprites[i].InstructionPart2.Component1_Keyframes)
                            {
                                bytes.AddRange(BitConverter.GetBytes(animPart.Time));
                                bytes.AddRange(BitConverter.GetBytes(animPart.I_02));
                            }

                            bytes.AddRange(new byte[Utils.CalculatePadding(4 * component1_Count, 16)]);

                            //Data offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart2_Offset + 20);

                            //Data
                            foreach (var animPart in Sprites[i].InstructionPart2.Component1_Keyframes)
                            {
                                bytes.AddRange(animPart.Write());
                            }
                        }

                        if (component2_Count > 0)
                        {
                            //Index list offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart2_Offset + 24);

                            //Frame Index List
                            foreach (var animPart in Sprites[i].InstructionPart2.Component2_Keyframes)
                            {
                                bytes.AddRange(BitConverter.GetBytes(animPart.Time));
                                bytes.AddRange(BitConverter.GetBytes(animPart.I_02));
                            }

                            bytes.AddRange(new byte[Utils.CalculatePadding(4 * component2_Count, 16)]);

                            //Data offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart2_Offset + 28);

                            //Data
                            foreach (var animPart in Sprites[i].InstructionPart2.Component2_Keyframes)
                            {
                                bytes.AddRange(animPart.Write());
                            }

                            //Second data section
                            foreach (var animPart in Sprites[i].InstructionPart2.Component2_Keyframes)
                            {
                                bytes.AddRange(animPart.ExtendedData.Write());
                            }

                        }
                    }

                    //Write AnimationPart3
                    if (Sprites[i].InstructionPart3 != null)
                    {
                        int animPart3_Offset = bytes.Count;
                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(animPart3_Offset), offset + 12);

                        int component1_Count = Sprites[i].InstructionPart3.Component1_Keyframes != null ? Sprites[i].InstructionPart3.Component1_Keyframes.Count : Sprites[i].InstructionPart3.Component1_DummyCount;
                        int component3_Count = Sprites[i].InstructionPart3.Component3_Keyframes != null ? Sprites[i].InstructionPart3.Component3_Keyframes.Count : Sprites[i].InstructionPart3.Component3_DummyCount;

                        //AnimatedPart block
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart3.I_00));
                        bytes.AddRange(BitConverter.GetBytes((ushort)component1_Count));
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart3.Component1_InstructionCount));
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart3.Component2_DummyCount));
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart3.Component2_InstructionCount));
                        bytes.AddRange(BitConverter.GetBytes((ushort)component3_Count));
                        bytes.AddRange(BitConverter.GetBytes(Sprites[i].InstructionPart3.Component3_InstructionCount));
                        bytes.AddRange(BitConverter.GetBytes((long)0)); //Padding
                        bytes.AddRange(BitConverter.GetBytes(0)); //24 = component 1 index list offset
                        bytes.AddRange(BitConverter.GetBytes(0)); //28 = component 1 data offset
                        bytes.AddRange(BitConverter.GetBytes(0)); //Unused. Suspected to be for Component2 index list
                        bytes.AddRange(BitConverter.GetBytes(0)); //Unused. Suspected to be for Component2 data
                        bytes.AddRange(BitConverter.GetBytes(0)); //40 = component 3 index list offset
                        bytes.AddRange(BitConverter.GetBytes(0)); //44 = component 3 data offset

                        if (Sprites[i].InstructionPart3.Component1_Keyframes?.Count > 0)
                        {
                            //Index list offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart3_Offset + 24);

                            //Frame Index List
                            foreach (var animPart in Sprites[i].InstructionPart3.Component1_Keyframes)
                            {
                                bytes.AddRange(BitConverter.GetBytes(animPart.Time));
                                bytes.AddRange(BitConverter.GetBytes(animPart.I_02));
                            }

                            bytes.AddRange(new byte[Utils.CalculatePadding(4 * component1_Count, 16)]);

                            //Data offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart3_Offset + 28);

                            //Data
                            foreach (var animPart in Sprites[i].InstructionPart3.Component1_Keyframes)
                            {
                                bytes.AddRange(animPart.Write());
                            }
                        }

                        if (Sprites[i].InstructionPart3.Component3_Keyframes?.Count > 0)
                        {
                            //Index list offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart3_Offset + 40);

                            //Frame Index List
                            foreach (var animPart in Sprites[i].InstructionPart3.Component3_Keyframes)
                            {
                                bytes.AddRange(BitConverter.GetBytes(animPart.Time));
                                bytes.AddRange(BitConverter.GetBytes(animPart.I_02));
                            }

                            bytes.AddRange(new byte[Utils.CalculatePadding(4 * component3_Count, 16)]);

                            //Data offset
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), animPart3_Offset + 44);

                            //Data
                            foreach (var animPart in Sprites[i].InstructionPart3.Component3_Keyframes)
                            {
                                bytes.AddRange(animPart.Write());
                            }

                            //Second data section
                            foreach (var animPart in Sprites[i].InstructionPart3.Component3_Keyframes)
                            {
                                bytes.AddRange(animPart.ExtendedData.Write());
                            }

                        }
                    }

                }
            }

            //Fill in references to sprites on nodes
            if(nodeCount > 0)
            {
                for(int i = 0; i < nodeCount; i++)
                {
                    if(nodes[i].SpriteIndex >= 0)
                    {
                        if (nodes[i].SpriteIndex >= spriteOffsets.Length)
                            throw new ArgumentException($"SpriteIndex on Node {i} references a sprite which does not exist!");

                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(spriteOffsets[nodes[i].SpriteIndex]), spriteReferenceOffset[i]);
                    }
                }
            }

            return bytes.ToArray();
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public static void SaveXml(string xmlPath)
        {
            string path = string.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(EMS_File), YAXSerializationOptions.DontSerializeNullObjects);
            EMS_File amkFile = (EMS_File)serializer.DeserializeFromFile(xmlPath);
            amkFile.Save(path);
        }
        #endregion
    }

    [YAXSerializeAs("Node")]
    [Serializable]
    public class EMS_Node
    {
        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXAttributeForClass]
        public int SpriteIndex { get; set; }
        [YAXAttributeForClass]
        public int I_00 { get; set; }

        //Index links. Used when loading and saving, otherwise irrelevant.
        internal int Index;
        internal int ChildIndex;
        internal int SiblingIndex;
        internal int PreviousNodeIndex;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Node")]
        public List<EMS_Node> Nodes { get; set; } = new List<EMS_Node>();

        public static List<EMS_Node> CreateRecursiveNodes(List<EMS_Node> nodes, int rootNodeCount)
        {
            if (nodes.Count < rootNodeCount)
                throw new ArgumentException("EMS_Node.CreateRecursiveNodes: rootNodeCount is more than the total amount of nodes.");

            List<EMS_Node> newNodes = new List<EMS_Node>();

            for(int i = 0; i < rootNodeCount; i++)
            {
                EMS_Node node = nodes[i].Copy();

                if (node.ChildIndex != -1)
                {
                    node.Nodes.AddRange(CreateRecursiveNode_Rec(nodes, node.ChildIndex));
                }

                newNodes.Add(node);
            }

            return newNodes;
        }

        private static List<EMS_Node> CreateRecursiveNode_Rec(List<EMS_Node> originalNodes, int childIndex)
        {
            List<EMS_Node> nodes = new List<EMS_Node>();

            int siblingIdx = childIndex;

            while(siblingIdx != -1)
            {
                EMS_Node node = originalNodes.FirstOrDefault(x => x.Index == siblingIdx);

                if(node != null)
                {
                    node = node.Copy();
                    nodes.Add(node);

                    if(node.ChildIndex != -1)
                    {
                        node.Nodes.AddRange(CreateRecursiveNode_Rec(originalNodes, node.ChildIndex));
                    }

                    siblingIdx = node.SiblingIndex;
                }
                else
                {
                    break;
                }
            }

            return nodes;
        }

        public static List<EMS_Node> CreateNonRecursiveNodes(List<EMS_Node> nodes)
        {
            List<EMS_Node> newNodes = new List<EMS_Node>();

            if (nodes == null)
                return newNodes;

            //Create root level nodes first. These have to be at the start of the list as that is how the game reads the file.
            for(int i = 0; i < nodes.Count; i++)
            {
                EMS_Node node = nodes[i].Copy();
                node.Index = i;
                node.SiblingIndex = -1; //Root level nodes dont have any sibling connection
                node.PreviousNodeIndex = i - 1;

                newNodes.Add(node);
            }

            //Now write the children nodes
            for(int i = 0; i < nodes.Count; i++)
            {
                if(newNodes[i].Nodes?.Count > 0)
                {
                    newNodes[i].ChildIndex = CreateNonRecursiveNodes_Rec(newNodes, newNodes[i].Nodes, i);
                    newNodes[i].Nodes = null;
                }
                else
                {
                    newNodes[i].ChildIndex = -1;
                }
            }

            return newNodes;
        }

        private static int CreateNonRecursiveNodes_Rec(List<EMS_Node> newNodes, List<EMS_Node> childrenNodes, int parentIndex)
        {
            int[] newNodeIndex = new int[childrenNodes.Count];

            for(int i = 0; i < childrenNodes.Count; i++)
            {
                EMS_Node node = childrenNodes[i].Copy();
                node.Index = newNodes.Count;
                newNodeIndex[i] = node.Index;

                newNodes.Add(node);

                if(node.Nodes?.Count > 0)
                {
                    node.ChildIndex = CreateNonRecursiveNodes_Rec(newNodes, node.Nodes, node.Index);
                    node.Nodes = null;
                }
                else
                {
                    node.ChildIndex = -1;
                }
            }

            //Create sibling and previous links
            for(int i = 0; i < newNodeIndex.Length; i++)
            {
                EMS_Node node = newNodes[newNodeIndex[i]];
                node.SiblingIndex = i < newNodeIndex.Length - 1 ? newNodeIndex[i + 1] : -1;
                node.PreviousNodeIndex = i > 0 ? newNodeIndex[i - 1] : parentIndex;
            }

            return newNodeIndex[0];
        }
    }

    [YAXSerializeAs("Sprite")]
    public class EMS_Sprite
    {
        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public ushort I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public ushort I_02 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("InstructionTime")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }

        [YAXAttributeFor("BoundsMin")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("BoundsMin")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("BoundsMax")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("BoundsMax")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#########")]
        public float F_48 { get; set; }

        [YAXDontSerializeIfNull]
        public InstructionPart1 InstructionPart1 { get; set; }
        [YAXDontSerializeIfNull]
        public InstructionPart2 InstructionPart2 { get; set; }
        [YAXDontSerializeIfNull]
        public InstructionPart3 InstructionPart3 { get; set; }
    }

    public class InstructionPart1
    {
        [YAXAttributeForClass]
        public ushort Component1_InstructionCount { get; set; }
        [YAXAttributeForClass]
        public ushort Component2_InstructionCount { get; set; }
        [YAXAttributeForClass]
        public int I_00 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Instruction")]
        [YAXSerializeAs("Component1")]
        public List<EMS_AnimPart1_Component1> Component1_Keyframes { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Instruction")]
        [YAXSerializeAs("Component2")]
        public List<EMS_AnimPart1_Component2> Component2_Keyframes { get; set; }

    }

    public class InstructionPart2
    {
        [YAXAttributeForClass]
        public ushort Component1_InstructionCount { get; set; }
        [YAXAttributeForClass]
        public ushort Component2_InstructionCount { get; set; }
        [YAXAttributeForClass]
        public int I_00 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Instruction")]
        [YAXSerializeAs("Component1")]
        public List<EMS_AnimPart2_Component1> Component1_Keyframes { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Instruction")]
        [YAXSerializeAs("Component2")]
        public List<EMS_AnimPart2_Component2> Component2_Keyframes { get; set; }
    }

    public class InstructionPart3
    {
        [YAXAttributeForClass]
        public ushort Component1_InstructionCount { get; set; }
        [YAXAttributeForClass]
        public ushort Component2_InstructionCount { get; set; }
        [YAXAttributeForClass]
        public ushort Component3_InstructionCount { get; set; }
        [YAXAttributeForClass]
        public int I_00 { get; set; }

        [YAXComment("These values will only be written to the file if there are NO instructions.")]
        [YAXAttributeFor("DummyEntryCounts")]
        [YAXSerializeAs("Component1")]
        public ushort Component1_DummyCount { get; set; }
        [YAXAttributeFor("DummyEntryCounts")]
        [YAXSerializeAs("Component2")]
        public ushort Component2_DummyCount { get; set; }
        [YAXAttributeFor("DummyEntryCounts")]
        [YAXSerializeAs("Component3")]
        public ushort Component3_DummyCount { get; set; }

        [YAXComment("Named values for Component1 and Component2 are probably wrong (reused code).")]

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Instruction")]
        [YAXSerializeAs("Component1")]
        public List<EMS_AnimPart1_Component1> Component1_Keyframes { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Instruction")]
        [YAXSerializeAs("Component3")]
        public List<EMS_AnimPart2_Component2> Component3_Keyframes { get; set; }
    }

    public class EMS_AnimPart1_Component1
    {

        [YAXAttributeForClass]
        public ushort Time { get; set; }
        [YAXAttributeForClass]
        public ushort I_02 { get; set; }


        [YAXAttributeFor("F_00")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Width")]
        [YAXFormat("0.0#########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Height")]
        [YAXFormat("0.0#########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_28 { get; set; }

        public static EMS_AnimPart1_Component1 Read(byte[] bytes, int dataOffset, int keyframeIndexOffset)
        {
            return new EMS_AnimPart1_Component1()
            {
                Time = BitConverter.ToUInt16(bytes, keyframeIndexOffset),
                I_02 = BitConverter.ToUInt16(bytes, keyframeIndexOffset + 2),
                F_00 = BitConverter.ToSingle(bytes, dataOffset),
                F_04 = BitConverter.ToSingle(bytes, dataOffset + 4),
                F_08 = BitConverter.ToSingle(bytes, dataOffset + 8),
                F_12 = BitConverter.ToSingle(bytes, dataOffset + 12),
                F_16 = BitConverter.ToSingle(bytes, dataOffset + 16),
                F_20 = BitConverter.ToSingle(bytes, dataOffset + 20),
                F_24 = BitConverter.ToSingle(bytes, dataOffset + 24),
                F_28 = BitConverter.ToSingle(bytes, dataOffset + 28)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(32);

            bytes.AddRange(BitConverter.GetBytes(F_00));
            bytes.AddRange(BitConverter.GetBytes(F_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));

            if (bytes.Count != 32) throw new Exception("EMS_AnimPart1_Component1 invalid size!");

            return bytes.ToArray();
        }
    }

    public class EMS_AnimPart1_Component2
    {

        [YAXAttributeForClass]
        public ushort Time { get; set; }
        [YAXAttributeForClass]
        public ushort I_02 { get; set; }


        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
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
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_28 { get; set; }

        public static EMS_AnimPart1_Component2 Read(byte[] bytes, int dataOffset, int keyframeIndexOffset)
        {
            return new EMS_AnimPart1_Component2()
            {
                Time = BitConverter.ToUInt16(bytes, keyframeIndexOffset),
                I_02 = BitConverter.ToUInt16(bytes, keyframeIndexOffset + 2),
                F_00 = BitConverter.ToSingle(bytes, dataOffset),
                F_04 = BitConverter.ToSingle(bytes, dataOffset + 4),
                F_08 = BitConverter.ToSingle(bytes, dataOffset + 8),
                F_12 = BitConverter.ToSingle(bytes, dataOffset + 12),
                F_16 = BitConverter.ToSingle(bytes, dataOffset + 16),
                F_20 = BitConverter.ToSingle(bytes, dataOffset + 20),
                F_24 = BitConverter.ToSingle(bytes, dataOffset + 24),
                F_28 = BitConverter.ToSingle(bytes, dataOffset + 28)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(32);

            bytes.AddRange(BitConverter.GetBytes(F_00));
            bytes.AddRange(BitConverter.GetBytes(F_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));

            if (bytes.Count != 32) throw new Exception("EMS_AnimPart1_Component2 invalid size!");

            return bytes.ToArray();
        }
    }

    public class EMS_AnimPart2_Component1
    {
        [YAXAttributeForClass]
        public ushort Time { get; set; }
        [YAXAttributeForClass]
        public ushort I_02 { get; set; }


        [YAXAttributeFor("EMB_Index")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("MinWidth")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("MinHeight")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("MaxWidth")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("MaxHeight")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_28 { get; set; }

        public static EMS_AnimPart2_Component1 Read(byte[] bytes, int dataOffset, int keyframeIndexOffset)
        {
            return new EMS_AnimPart2_Component1()
            {
                Time = BitConverter.ToUInt16(bytes, keyframeIndexOffset),
                I_02 = BitConverter.ToUInt16(bytes, keyframeIndexOffset + 2),
                I_00 = BitConverter.ToInt32(bytes, dataOffset),
                I_04 = BitConverter.ToInt32(bytes, dataOffset + 4),
                F_08 = BitConverter.ToSingle(bytes, dataOffset + 8),
                I_12 = BitConverter.ToInt32(bytes, dataOffset + 12),
                F_16 = BitConverter.ToSingle(bytes, dataOffset + 16),
                F_20 = BitConverter.ToSingle(bytes, dataOffset + 20),
                F_24 = BitConverter.ToSingle(bytes, dataOffset + 24),
                F_28 = BitConverter.ToSingle(bytes, dataOffset + 28)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(32);

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(F_16));
            bytes.AddRange(BitConverter.GetBytes(F_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));

            if (bytes.Count != 32) throw new Exception("EMS_AnimPart2_Component1 invalid size!");

            return bytes.ToArray();
        }
    }

    public class EMS_AnimPart2_Component2
    {
        [YAXAttributeForClass]
        public ushort Time { get; set; }
        [YAXAttributeForClass]
        public ushort I_02 { get; set; }


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
        [YAXHexValue]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_28 { get; set; }

        [YAXErrorIfMissed(YAXExceptionTypes.Error)]
        public EMS_ExtendedData ExtendedData { get; set; }

        public static EMS_AnimPart2_Component2 Read(byte[] bytes, int dataOffset, int keyframeIndexOffset, int extendedDataOffset)
        {
            return new EMS_AnimPart2_Component2()
            {
                Time = BitConverter.ToUInt16(bytes, keyframeIndexOffset),
                I_02 = BitConverter.ToUInt16(bytes, keyframeIndexOffset + 2),
                F_00 = BitConverter.ToSingle(bytes, dataOffset),
                F_04 = BitConverter.ToSingle(bytes, dataOffset + 4),
                F_08 = BitConverter.ToSingle(bytes, dataOffset + 8),
                F_12 = BitConverter.ToSingle(bytes, dataOffset + 12),
                I_16 = BitConverter.ToInt32(bytes, dataOffset + 16),
                I_20 = BitConverter.ToInt32(bytes, dataOffset + 20),
                F_24 = BitConverter.ToSingle(bytes, dataOffset + 24),
                F_28 = BitConverter.ToSingle(bytes, dataOffset + 28),
                ExtendedData = EMS_ExtendedData.Read(bytes, extendedDataOffset)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(32);

            bytes.AddRange(BitConverter.GetBytes(F_00));
            bytes.AddRange(BitConverter.GetBytes(F_04));
            bytes.AddRange(BitConverter.GetBytes(F_08));
            bytes.AddRange(BitConverter.GetBytes(F_12));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(I_20));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(F_28));

            if (bytes.Count != 32) throw new Exception("EMS_AnimPart2_Component2 invalid size!");

            return bytes.ToArray();
        }
    }

    public class EMS_ExtendedData
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
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("F_48")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("F_52")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("F_56")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("F_60")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_60 { get; set; }

        public static EMS_ExtendedData Read(byte[] bytes, int offset)
        {
            return new EMS_ExtendedData()
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
                F_60 = BitConverter.ToSingle(bytes, offset + 60)
            };
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(64);

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

            if (bytes.Count != 64) throw new Exception("EMS_ExtendedData: Invalid size!");

            return bytes.ToArray();
        }

    }

}
