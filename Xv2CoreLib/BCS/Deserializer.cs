using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BCS
{
    public class Deserializer
    {
        string saveLocation;
        BCS_File bcsFile;
        public List<byte> bytes { get; private set; } = new List<byte>() { 35, 66, 67, 83, 254, 255, 76, 0, 0, 0, 0, 0 };

        //Counts
        int PartSetCount { get; set; }
        int PartColorCount { get; set; }
        int BodyCount { get; set; }

        //Offset Lists
        List<int> PartSetTable = new List<int>();
        List<int> PartColorTable = new List<int>();
        List<int> BodyTable = new List<int>();
        int Skeleton2Table = 0;
        int Skeleton1Table = 0;

        //Strings
        List<StringWriter.StringInfo> stringInfo = new List<StringWriter.StringInfo>();

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(BCS_File), YAXSerializationOptions.DontSerializeNullObjects);
            bcsFile = (BCS_File)serializer.DeserializeFromFile(location);
            bcsFile.SortEntries();

            PartSetCount = (bcsFile.PartSets != null) ? int.Parse(bcsFile.PartSets[bcsFile.PartSets.Count() - 1].Index) + 1 : 0;
            PartColorCount = (bcsFile.Part_Colors != null) ? int.Parse(bcsFile.Part_Colors[bcsFile.Part_Colors.Count() - 1].Index) + 1 : 0;
            BodyCount = (bcsFile.Bodies != null) ? int.Parse(bcsFile.Bodies[bcsFile.Bodies.Count() - 1].Index) + 1 : 0;

            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BCS_File _bcsFile, string location)
        {
            saveLocation = location;
            bcsFile = _bcsFile;
            _bcsFile.SortEntries();

            PartSetCount = (bcsFile.PartSets != null) ? int.Parse(bcsFile.PartSets[bcsFile.PartSets.Count() - 1].Index) + 1 : 0;
            PartColorCount = (bcsFile.Part_Colors != null) ? int.Parse(bcsFile.Part_Colors[bcsFile.Part_Colors.Count() - 1].Index) + 1 : 0;
            BodyCount = (bcsFile.Bodies != null) ? int.Parse(bcsFile.Bodies[bcsFile.Bodies.Count() - 1].Index) + 1 : 0;

            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(BCS_File _bcsFile)
        {
            bcsFile = _bcsFile;
            _bcsFile.SortEntries();

            PartSetCount = (bcsFile.PartSets != null) ? int.Parse(bcsFile.PartSets[bcsFile.PartSets.Count() - 1].Index) + 1 : 0;
            PartColorCount = (bcsFile.Part_Colors != null) ? int.Parse(bcsFile.Part_Colors[bcsFile.Part_Colors.Count() - 1].Index) + 1 : 0;
            BodyCount = (bcsFile.Bodies != null) ? int.Parse(bcsFile.Bodies[bcsFile.Bodies.Count() - 1].Index) + 1 : 0;

            Write();
        }

        private void Write()
        {
            
            //Header
            int I_18 = (bcsFile.SkeletonData2 != null) ? 1 : 0;
            bytes.AddRange(BitConverter.GetBytes((short)PartSetCount));
            bytes.AddRange(BitConverter.GetBytes((short)PartColorCount));
            bytes.AddRange(BitConverter.GetBytes((short)BodyCount));
            bytes.AddRange(BitConverter.GetBytes((short)I_18));
            bytes.AddRange(new byte[24]);
            bytes.AddRange(BitConverter.GetBytes(bcsFile.I_44));
            Assertion.AssertArraySize(bcsFile.F_48, 7, "BCS", "F_48");
            bytes.AddRange(BitConverter_Ex.GetBytes(bcsFile.F_48));


            //PartSet Table
            if(bcsFile.PartSets != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 24);
                for(int i = 0; i < PartSetCount; i++)
                {
                    for(int a = 0; a < bcsFile.PartSets.Count(); a++)
                    {
                        if(int.Parse(bcsFile.PartSets[a].Index) == i)
                        {
                            PartSetTable.Add(bytes.Count());
                            bytes.AddRange(new byte[4]);
                            break;
                        }
                        else if (a == bcsFile.PartSets.Count() - 1)
                        {
                            //Null entry
                            bytes.AddRange(new byte[4]);
                            break;
                        }
                    }

                }
            }

            //PartColor Table
            if (bcsFile.Part_Colors != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 28);
                for (int i = 0; i < PartColorCount; i++)
                {
                    for (int a = 0; a < bcsFile.Part_Colors.Count(); a++)
                    {
                        if (int.Parse(bcsFile.Part_Colors[a].Index) == i)
                        {
                            PartColorTable.Add(bytes.Count());
                            bytes.AddRange(new byte[4]);
                            break;
                        }
                        else if (a == bcsFile.Part_Colors.Count() - 1)
                        {
                            //Null entry
                            bytes.AddRange(new byte[4]);
                            break;
                        }
                    }

                }
            }

            //Body Table
            if (bcsFile.Bodies != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 32);
                for (int i = 0; i < BodyCount; i++)
                {
                    for (int a = 0; a < bcsFile.Bodies.Count(); a++)
                    {
                        if (int.Parse(bcsFile.Bodies[a].Index) == i)
                        {
                            BodyTable.Add(bytes.Count());
                            bytes.AddRange(new byte[4]);
                            break;
                        }
                        else if (a == bcsFile.Bodies.Count() - 1)
                        {
                            //Null entry
                            bytes.AddRange(new byte[4]);
                            break;
                        }
                    }

                }
            }
            
            //Skeleton1 Table
            if (bcsFile.SkeletonData1 != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 40);
                Skeleton1Table = bytes.Count();
                bytes.AddRange(new byte[4]);
            }

            //Skeleton2 Table
            if (bcsFile.SkeletonData2 != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 36);
                Skeleton2Table = bytes.Count();
                bytes.AddRange(new byte[4]);
            }

            //PartSets
            if (bcsFile.PartSets != null)
            {
                for(int i = 0; i < bcsFile.PartSets.Count(); i++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), PartSetTable[i]);
                    int PartSetStart = bytes.Count();

                    bytes.AddRange(new byte[20]);
                    bytes.AddRange(BitConverter.GetBytes(10));
                    bytes.AddRange(BitConverter.GetBytes(32));
                    bytes.AddRange(new byte[4]);
                    int[] PartSetSubTable = new int[10] { bytes.Count() + 0, bytes.Count() + 4, bytes.Count() + 8, bytes.Count() + 12, bytes.Count() + 16, bytes.Count() + 20, bytes.Count() + 24, bytes.Count() + 28, bytes.Count() + 32, bytes.Count() + 36 };
                    bytes.AddRange(new byte[40]);

                    //Parts
                    WritePart(bcsFile.PartSets[i].FaceBase, PartSetSubTable[0], PartSetStart);
                    WritePart(bcsFile.PartSets[i].FaceForehead, PartSetSubTable[1], PartSetStart);
                    WritePart(bcsFile.PartSets[i].FaceEye, PartSetSubTable[2], PartSetStart);
                    WritePart(bcsFile.PartSets[i].FaceNose, PartSetSubTable[3], PartSetStart);
                    WritePart(bcsFile.PartSets[i].FaceEar, PartSetSubTable[4], PartSetStart);
                    WritePart(bcsFile.PartSets[i].Hair, PartSetSubTable[5], PartSetStart);
                    WritePart(bcsFile.PartSets[i].Bust, PartSetSubTable[6], PartSetStart);
                    WritePart(bcsFile.PartSets[i].Pants, PartSetSubTable[7], PartSetStart);
                    WritePart(bcsFile.PartSets[i].Rist, PartSetSubTable[8], PartSetStart);
                    WritePart(bcsFile.PartSets[i].Boots, PartSetSubTable[9], PartSetStart);

                }
            }

            //PartColors
            if(bcsFile.Part_Colors != null)
            {
                List<int> PartColorOffsets = new List<int>();

                for (int i = 0; i < bcsFile.Part_Colors.Count(); i++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), PartColorTable[i]);
                    int PartColorStart = bytes.Count();
                    int colorCount = (bcsFile.Part_Colors[i]._Colors != null) ? bcsFile.Part_Colors[i]._Colors.Count() : 0;

                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        Offset = bytes.Count(),
                        RelativeOffset = PartColorStart,
                        StringToWrite = bcsFile.Part_Colors[i].Str_00
                    });
                    bytes.AddRange(new byte[10]);
                    bytes.AddRange(BitConverter.GetBytes((short)colorCount));
                    PartColorOffsets.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                }

                for(int i = 0; i < bcsFile.Part_Colors.Count(); i++)
                {
                    if(bcsFile.Part_Colors[i]._Colors != null)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - PartColorOffsets[i] + 12), PartColorOffsets[i]);
                        for (int a = 0; a < bcsFile.Part_Colors[i]._Colors.Count(); a++)
                        {
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_00));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_04));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_08));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_12));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_16));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_20));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_24));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_28));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_32));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_36));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_40));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_44));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_48));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_52));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_56));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_60));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_64));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_68));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_72));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Part_Colors[i]._Colors[a].F_76));
                        }
                    }
                }

            }

            //Bodies
            if (bcsFile.Bodies != null)
            {
                List<int> BodyOffsets = new List<int>();

                for (int i = 0; i < bcsFile.Bodies.Count(); i++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), BodyTable[i]);

                    int bodyScalesCount = (bcsFile.Bodies[i].BodyScales != null) ? bcsFile.Bodies[i].BodyScales.Count() : 0;
                    bytes.AddRange(new byte[2]);
                    bytes.AddRange(BitConverter.GetBytes((short)bodyScalesCount));
                    BodyOffsets.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);
                }

                for (int i = 0; i < bcsFile.Bodies.Count(); i++)
                {
                    if (bcsFile.Bodies[i].BodyScales != null)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - BodyOffsets[i] + 4), BodyOffsets[i]);
                        
                        for(int a = 0; a < bcsFile.Bodies[i].BodyScales.Count(); a++)
                        {
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Bodies[i].BodyScales[a].F_00));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Bodies[i].BodyScales[a].F_04));
                            bytes.AddRange(BitConverter.GetBytes(bcsFile.Bodies[i].BodyScales[a].F_08)); stringInfo.Add(new StringWriter.StringInfo()
                            {
                                Offset = bytes.Count(),
                                RelativeOffset = bytes.Count() - 12,
                                StringToWrite = bcsFile.Bodies[i].BodyScales[a].Str_12
                            });
                            bytes.AddRange(new byte[4]);
                        }

                    }
                }

            }

            //Skeleton1
            WriteSkeleton(bcsFile.SkeletonData1, Skeleton1Table);

            //Skeleton2
            WriteSkeleton(bcsFile.SkeletonData2, Skeleton2Table);

            //Strings
            bytes = StringWriter.WritePointerStrings(stringInfo, bytes);
            


        }

        //Part Writers
        private void WritePart(Part part, int offsetToFill, int offsetRelativeTo)
        {
            if(part != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetRelativeTo), offsetToFill);

                int partStartOffset = bytes.Count();
                int ColorSelectorsCount = (part.Color_Selectors != null) ? part.Color_Selectors.Count() : 0;
                int PhysicsObjectCount = (part.Physics_Objects != null) ? part.Physics_Objects.Count() : 0;
                int Unk3Count = (part.Unk_3 != null) ? part.Unk_3.Count() : 0;

                bytes.AddRange(BitConverter.GetBytes(part.I_00));
                bytes.AddRange(BitConverter.GetBytes(part.I_02));
                bytes.AddRange(BitConverter.GetBytes(part.I_04));
                bytes.AddRange(new byte[10]);
                bytes.AddRange(BitConverter.GetBytes(part.I_16));
                bytes.AddRange(BitConverter.GetBytes((short)ColorSelectorsCount));
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(part.I_24));
                bytes.AddRange(BitConverter.GetBytes(part.I_28));
                bytes.AddRange(BitConverter.GetBytes(part.I_32));
                bytes.AddRange(BitConverter.GetBytes(part.F_36));
                bytes.AddRange(BitConverter.GetBytes(part.F_40));
                bytes.AddRange(new byte[8]);
                if (part.Str_52.Length > 4)
                {
                    Console.WriteLine(String.Format("\"{0}\" exceeds the maximum allowed length of 4 for the paramater \"Name\"", part.Str_52));
                    Utils.WaitForInputThenQuit();
                }
                bytes.AddRange(Encoding.ASCII.GetBytes(part.Str_52));
                bytes.AddRange(new byte[4 - part.Str_52.Length]);
                stringInfo.Add(new StringWriter.StringInfo()
                {
                    Offset = bytes.Count(),
                    RelativeOffset = partStartOffset,
                    StringToWrite = part.Str_56
                });
                bytes.AddRange(new byte[4]);
                stringInfo.Add(new StringWriter.StringInfo()
                {
                    Offset = bytes.Count(),
                    RelativeOffset = partStartOffset,
                    StringToWrite = part.Str_60
                });
                bytes.AddRange(new byte[4]);
                stringInfo.Add(new StringWriter.StringInfo()
                {
                    Offset = bytes.Count(),
                    RelativeOffset = partStartOffset,
                    StringToWrite = part.Str_64
                });
                bytes.AddRange(new byte[4]);
                stringInfo.Add(new StringWriter.StringInfo()
                {
                    Offset = bytes.Count(),
                    RelativeOffset = partStartOffset,
                    StringToWrite = part.Str_68
                });
                bytes.AddRange(new byte[6]);
                bytes.AddRange(BitConverter.GetBytes((short)PhysicsObjectCount));
                bytes.AddRange(new byte[6]);
                bytes.AddRange(BitConverter.GetBytes((short)Unk3Count));
                bytes.AddRange(new byte[4]);

                //Extended data
                WriteColorSelectors(part.Color_Selectors, partStartOffset + 20, partStartOffset);
                WritePhysicsObjects(part.Physics_Objects, partStartOffset + 76, partStartOffset);
                WriteUnk3(part.Unk_3, partStartOffset + 84, partStartOffset);
            }
        }

        private void WriteColorSelectors(List<ColorSelector> colorSelectors, int offsetToFill, int relativeOffset)
        {
            if(colorSelectors != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - relativeOffset), offsetToFill);

                for(int i = 0; i < colorSelectors.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(colorSelectors[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(colorSelectors[i].I_02));
                }
            }
        }

        private void WritePhysicsObjects(List<PhysicsObject> physicsObjects, int offsetToFill, int relativeOffset)
        {
            if (physicsObjects != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - relativeOffset), offsetToFill);

                for (int i = 0; i < physicsObjects.Count(); i++)
                {
                    int physicsObjectStartOffset = bytes.Count();

                    bytes.AddRange(BitConverter.GetBytes(physicsObjects[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(physicsObjects[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(physicsObjects[i].I_04));
                    bytes.AddRange(new byte[18]);
                    bytes.AddRange(BitConverter.GetBytes(physicsObjects[i].I_24));
                    bytes.AddRange(BitConverter.GetBytes(physicsObjects[i].I_28));
                    bytes.AddRange(BitConverter.GetBytes(physicsObjects[i].I_32));
                    if (physicsObjects[i].Str_36.Length > 4)
                    {
                        Console.WriteLine(String.Format("\"{0}\" exceeds the maximum allowed length of 4 for the paramater \"Name\"", physicsObjects[i].Str_36));
                        Utils.WaitForInputThenQuit();
                    }
                    bytes.AddRange(Encoding.ASCII.GetBytes(physicsObjects[i].Str_36));
                    bytes.AddRange(new byte[4 - physicsObjects[i].Str_36.Length]);
                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        Offset = bytes.Count(),
                        RelativeOffset = physicsObjectStartOffset,
                        StringToWrite = physicsObjects[i].Str_40[0]
                    });
                    bytes.AddRange(new byte[4]);
                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        Offset = bytes.Count(),
                        RelativeOffset = physicsObjectStartOffset,
                        StringToWrite = physicsObjects[i].Str_40[1]
                    });
                    bytes.AddRange(new byte[4]);
                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        Offset = bytes.Count(),
                        RelativeOffset = physicsObjectStartOffset,
                        StringToWrite = physicsObjects[i].Str_40[2]
                    });
                    bytes.AddRange(new byte[4]);
                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        Offset = bytes.Count(),
                        RelativeOffset = physicsObjectStartOffset,
                        StringToWrite = physicsObjects[i].Str_40[3]
                    });
                    bytes.AddRange(new byte[4]);
                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        Offset = bytes.Count(),
                        RelativeOffset = physicsObjectStartOffset,
                        StringToWrite = physicsObjects[i].Str_40[4]
                    });
                    bytes.AddRange(new byte[4]);
                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        Offset = bytes.Count(),
                        RelativeOffset = physicsObjectStartOffset,
                        StringToWrite = physicsObjects[i].Str_40[5]
                    });
                    bytes.AddRange(new byte[12]);

                }
            }
        }

        private void WriteUnk3(List<Unk3> unk3, int offsetToFill, int relativeOffset)
        {
            if (unk3 != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - relativeOffset), offsetToFill);

                for (int i = 0; i < unk3.Count(); i++)
                {
                    Assertion.AssertArraySize(unk3[i].I_00, 6, "Unk3", "values");
                    bytes.AddRange(BitConverter_Ex.GetBytes(unk3[i].I_00));
                }
            }
        }

        //Skeleton Writers
        private void WriteSkeleton(SkeletonData skeleton, int offsetToFill)
        {
            if(skeleton != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), offsetToFill);
                int boneCount = (skeleton.Bones != null) ? skeleton.Bones.Count() : 0;
                int boneOffset = (skeleton.Bones != null) ? 8 : 0;

                bytes.AddRange(BitConverter.GetBytes(skeleton.I_00));
                bytes.AddRange(BitConverter.GetBytes((short)boneCount));
                bytes.AddRange(BitConverter.GetBytes(boneOffset));
                
                for(int i = 0; i < boneCount; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].I_04));
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].F_12));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].F_16));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].F_20));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].F_24));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].F_28));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].F_32));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].F_36));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].F_40));
                    bytes.AddRange(BitConverter.GetBytes(skeleton.Bones[i].F_44));
                    stringInfo.Add(new StringWriter.StringInfo()
                    {
                        Offset = bytes.Count(),
                        RelativeOffset = bytes.Count() - 48,
                        StringToWrite = skeleton.Bones[i].Str_48
                    });
                    bytes.AddRange(new byte[4]);
                }

            }
        }

    }
}
