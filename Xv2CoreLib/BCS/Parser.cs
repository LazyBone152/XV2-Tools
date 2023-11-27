using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.BCS
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        public BCS_File bcsFile { get; private set; } = new BCS_File();


        public Parser(string location, bool _writeXml = false)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            Parse();
            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(BCS_File));
                serializer.SerializeToFile(bcsFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            Parse();
        }

        public BCS_File GetBcsFile()
        {
            return bcsFile;
        }

        private void Parse()
        {
            switch(BitConverter.ToInt16(rawBytes, 6))
            {
                case 72:
                    bcsFile.Version = Version.XV1;
                    break;
                case 0: //Some Xv2 BCS files dont have a signature (first 8 bytes all null)
                case 76:
                    bcsFile.Version = Version.XV2;
                    break;
                default:
                    throw new InvalidDataException("BCS: Unknown BCS version!");
            }

            //counts
            int partsetCount = BitConverter.ToInt16(rawBytes, 12);
            int partcolorsCount = BitConverter.ToInt16(rawBytes, 14);
            int bodyCount = BitConverter.ToInt16(rawBytes, 16);

            //offsets
            int partsetOffset;
            int partcolorsOffset;
            int bodyOffset;
            int skeleton2Offset;
            int skeleton1Offset;

            if(bcsFile.Version == Version.XV1)
            {
                partsetOffset = BitConverter.ToInt32(rawBytes, 20);
                partcolorsOffset = BitConverter.ToInt32(rawBytes, 24);
                bodyOffset = BitConverter.ToInt32(rawBytes, 28);
                skeleton2Offset = 0;
                skeleton1Offset = 64; //XV1 BCS files have a skeleton instance directly in the header
                bcsFile.F_48 = BitConverter_Ex.ToFloat32Array(rawBytes, 36, 7);

                byte[] _I_44 = rawBytes.GetRange(32, 4);
                bcsFile.Race = (Race)_I_44[0];
                bcsFile.Gender = (Gender)_I_44[1];
            }
            else
            {
                partsetOffset = BitConverter.ToInt32(rawBytes, 24);
                partcolorsOffset = BitConverter.ToInt32(rawBytes, 28);
                bodyOffset = BitConverter.ToInt32(rawBytes, 32);
                skeleton2Offset = BitConverter.ToInt32(rawBytes, 36);
                skeleton1Offset = BitConverter.ToInt32(rawBytes, 40);
                bcsFile.F_48 = BitConverter_Ex.ToFloat32Array(rawBytes, 48, 7);

                byte[] _I_44 = rawBytes.GetRange(44, 4);
                bcsFile.Race = (Race)_I_44[0];
                bcsFile.Gender = (Gender)_I_44[1];
            }

            //PartSets
            int actualIndex = 0;

            if(partsetCount > 0)
            {
                for (int i = 0; i < partsetCount; i++)
                {
                    int thisPartsetOffset = BitConverter.ToInt32(rawBytes, partsetOffset);

                    if(thisPartsetOffset != 0)
                    {
                        bcsFile.PartSets.Add(new PartSet());
                        bcsFile.PartSets[actualIndex].ID = i;

                        if (BitConverter.ToInt32(rawBytes, thisPartsetOffset + 20) != 10)
                        {
                            throw new Exception(string.Format("Part count mismatch on PartSet {0} (Expected 10, but found {1})\nThis BCS file cannot be parsed.", i, BitConverter.ToInt32(rawBytes, thisPartsetOffset + 20)));
                        }

                        int tableOffset = thisPartsetOffset + BitConverter.ToInt32(rawBytes, thisPartsetOffset + 24);
                        
                        bcsFile.PartSets[actualIndex].FaceBase = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 0), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].FaceForehead = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 4), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].FaceEye = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 8), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].FaceNose = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 12), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].FaceEar = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 16), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Hair = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 20), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Bust = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 24), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Pants = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 28), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Rist = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 32), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Boots = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 36), thisPartsetOffset);

                        actualIndex++;
                    }

                    partsetOffset += 4;
                }
            }

            //PartColors
            if(partcolorsCount > 0)
            {
                for(int i = 0; i < partcolorsCount; i++)
                {
                    int thisPartColorOffset = BitConverter.ToInt32(rawBytes, partcolorsOffset);

                    if(thisPartColorOffset != 0)
                    {
                        bcsFile.PartColors.Add(new PartColor()
                        {
                            Index = i.ToString(),
                            Name = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, thisPartColorOffset + 0) + thisPartColorOffset, false),
                            ColorsList = ParseColors(BitConverter.ToInt32(rawBytes, thisPartColorOffset + 12) + thisPartColorOffset, BitConverter.ToInt16(rawBytes, thisPartColorOffset + 10))
                        });
                    }

                    partcolorsOffset += 4;
                }
            }

            //BodyScales
            if(bodyCount > 0)
            {
                for(int i = 0; i < bodyCount; i++)
                {
                    int thisBodyScaleOffset = BitConverter.ToInt32(rawBytes, bodyOffset);

                    if(thisBodyScaleOffset != 0)
                    {
                        Body body = ParseBody(thisBodyScaleOffset, i);

                        if(body?.BodyScales?.Count > 0)
                            bcsFile.Bodies.Add(body);
                    }

                    bodyOffset += 4;
                }

            }

            if(skeleton1Offset != 0)
            {
                bcsFile.SkeletonData1 = ParseSkeleton(bcsFile.Version == Version.XV1 ? skeleton1Offset : BitConverter.ToInt32(rawBytes, skeleton1Offset));
            }

            if(skeleton2Offset != 0)
            {
                bcsFile.SkeletonData2 = ParseSkeleton(BitConverter.ToInt32(rawBytes, skeleton2Offset));
            }
        }


        //Part Parsers
        private Part ParsePart(int offset, int partOffset)
        {
            if(offset != 0)
            {
                offset += partOffset;
                
                //validation
                int i_28 = BitConverter.ToInt32(rawBytes, offset + 28);
                int i_32 = BitConverter.ToInt32(rawBytes, offset + 32);

                if (i_28 > 0x3FF)
                    throw new InvalidDataException($"Unexpected I_28 value: {i_28}.");

                if (i_32 > 0x3FF)
                    throw new InvalidDataException($"Unexpected I_32 value: {i_32}.");

                var part = new Part()
                {
                    Model = BitConverter.ToInt16(rawBytes, offset + 0),
                    Model2 = BitConverter.ToInt16(rawBytes, offset + 2),
                    Texture = BitConverter.ToInt16(rawBytes, offset + 4),
                    Shader = BitConverter.ToInt16(rawBytes, offset + 16),
                    Flags = (Part.PartFlags)BitConverter.ToUInt32(rawBytes, offset + 24),
                    HideFlags = (PartTypeFlags)BitConverter.ToInt32(rawBytes, offset + 28),
                    HideMatFlags = (PartTypeFlags)BitConverter.ToInt32(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                    I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                    CharaCode = StringEx.GetString(rawBytes, offset + 52, false, StringEx.EncodingType.ASCII, 4),
                    EmdPath = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 56), offset),
                    EmmPath = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 60), offset),
                    EmbPath = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 64), offset),
                    EanPath = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 68), offset),
                    ColorSelectors = ParseColorSelector(BitConverter.ToInt32(rawBytes, offset + 20) + offset, BitConverter.ToInt16(rawBytes, offset + 18)),
                    PhysicsParts = ParsePhysicsObject(BitConverter.ToInt32(rawBytes, offset + 76) + offset, BitConverter.ToInt16(rawBytes, offset + 74))
                };

                if(bcsFile.Version == Version.XV2)
                {
                    part.Unk_3 = ParseUnk3(BitConverter.ToInt32(rawBytes, offset + 84) + offset, BitConverter.ToInt16(rawBytes, offset + 82));
                }

                return part;

            } 
            else
            {
                return null;
            }
            
        }

        private AsyncObservableCollection<ColorSelector> ParseColorSelector(int offset, int count)
        {
            AsyncObservableCollection<ColorSelector> colorSelectors = new AsyncObservableCollection<ColorSelector>();

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    colorSelectors.Add(new ColorSelector()
                    {
                        PartColorGroup = BitConverter.ToUInt16(rawBytes, offset + 0),
                        ColorIndex = BitConverter.ToUInt16(rawBytes, offset + 2)
                    });
                    offset += 4;
                }
            }

            return colorSelectors;
        }

        private AsyncObservableCollection<PhysicsPart> ParsePhysicsObject(int offset, int count)
        {
            AsyncObservableCollection<PhysicsPart> physicsObjects = new AsyncObservableCollection<PhysicsPart>();

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    //validation
                    int i_28 = BitConverter.ToInt32(rawBytes, offset + 28);
                    int i_32 = BitConverter.ToInt32(rawBytes, offset + 32);

                    if (i_28 > 0x200)
                        throw new InvalidDataException($"Unexpected I_28 value: {i_28}.");

                    if (i_32 > 0x200)
                        throw new InvalidDataException($"Unexpected I_32 value: {i_32}.");

                    physicsObjects.Add(new PhysicsPart()
                    {
                        Model1 = BitConverter.ToInt16(rawBytes, offset + 0),
                        Model2 = BitConverter.ToInt16(rawBytes, offset + 2),
                        Texture = BitConverter.ToInt16(rawBytes, offset + 4),
                        Flags = (Part.PartFlags)BitConverter.ToInt32(rawBytes, offset + 24),
                        HideFlags = (PartTypeFlags)BitConverter.ToInt32(rawBytes, offset + 28),
                        HideMatFlags = (PartTypeFlags)BitConverter.ToInt32(rawBytes, offset + 32),
                        CharaCode = StringEx.GetString(rawBytes, offset + 36, false, StringEx.EncodingType.ASCII, 4),
                        EmdPath = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 40), offset),
                        EmmPath = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 44), offset),
                        EmbPath = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 48), offset),
                        EanPath = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 52), offset),
                        BoneToAttach = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 56), offset),
                        ScdPath = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 60), offset)
                    });
                    offset += 72;
                }
            }

            return physicsObjects;
        }
        
        private List<Unk3> ParseUnk3(int offset, int count)
        {
            if(count > 0)
            {
                List<Unk3> unk3 = new List<Unk3>();

                for(int i = 0; i < count; i++)
                {
                    unk3.Add(new Unk3()
                    {
                        I_00 = BitConverter_Ex.ToInt16Array(rawBytes, offset, 6)
                    });
                    offset += 12;
                }

                return unk3;

            }
            else
            {
                return null;
            }
        }

        //Color Parsers
        private AsyncObservableCollection<Colors> ParseColors (int offset, int count)
        {
            AsyncObservableCollection<Colors> colors = new AsyncObservableCollection<Colors>();

            if (count > 0)
            {
                for(int i = 0; i < count; i++)
                {
                    Colors entry = new Colors()
                    {
                        ID = i,
                        Color1 = new LB_Common.Numbers.CustomColor(BitConverter.ToSingle(rawBytes, offset + 0),
                                                                   BitConverter.ToSingle(rawBytes, offset + 4),
                                                                   BitConverter.ToSingle(rawBytes, offset + 8),
                                                                   BitConverter.ToSingle(rawBytes, offset + 12)),
                        Color2 = new LB_Common.Numbers.CustomColor(BitConverter.ToSingle(rawBytes, offset + 16),
                                                                   BitConverter.ToSingle(rawBytes, offset + 20),
                                                                   BitConverter.ToSingle(rawBytes, offset + 24),
                                                                   BitConverter.ToSingle(rawBytes, offset + 28)),
                        Color3 = new LB_Common.Numbers.CustomColor(BitConverter.ToSingle(rawBytes, offset + 32),
                                                                   BitConverter.ToSingle(rawBytes, offset + 36),
                                                                   BitConverter.ToSingle(rawBytes, offset + 40),
                                                                   BitConverter.ToSingle(rawBytes, offset + 44)),
                        Color4 = new LB_Common.Numbers.CustomColor(BitConverter.ToSingle(rawBytes, offset + 48),
                                                                   BitConverter.ToSingle(rawBytes, offset + 52),
                                                                   BitConverter.ToSingle(rawBytes, offset + 56),
                                                                   BitConverter.ToSingle(rawBytes, offset + 60))
                    };

                    if(!entry.IsNull())
                        colors.Add(entry);

                    offset += 80;
                }
            }

            return colors;
        }
        
        //Body Parsers
        private Body ParseBody(int offset, int _index)
        {
            Body body = new Body();
            body.ID = _index;

            int bodyCount = BitConverter.ToInt16(rawBytes, offset + 2);
            int bodyOffset = BitConverter.ToInt32(rawBytes, offset + 4) + offset;
            
            for(int i = 0; i < bodyCount; i++)
            {
                body.BodyScales.Add(new BoneScale()
                {
                    ScaleX = BitConverter.ToSingle(rawBytes, bodyOffset + 0),
                    ScaleY = BitConverter.ToSingle(rawBytes, bodyOffset + 4),
                    ScaleZ = BitConverter.ToSingle(rawBytes, bodyOffset + 8),
                    BoneName = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, bodyOffset + 12) + bodyOffset, false)
                });
                bodyOffset += 16;
            }

            return body;
        }

        //Skeleton Parsers
        private SkeletonData ParseSkeleton (int offset)
        {
            SkeletonData skeleton = new SkeletonData();

            if (offset != 0)
            {
                int relativeTo = bcsFile.Version == Version.XV1 ? 32 : offset;

                skeleton.I_00 = BitConverter.ToInt16(rawBytes, offset);
                int boneCount = BitConverter.ToInt16(rawBytes, offset + 2);
                int boneOffset = BitConverter.ToInt32(rawBytes, offset + 4) + relativeTo;

                if (boneCount > 0)
                {
                    skeleton.Bones = new AsyncObservableCollection<Bone>();

                    for (int i = 0; i < boneCount; i++)
                    {
                        if(bcsFile.Version == Version.XV1)
                        {
                            skeleton.Bones.Add(new Bone()
                            {
                                I_00 = BitConverter.ToInt32(rawBytes, boneOffset + 0),
                                I_04 = BitConverter.ToInt32(rawBytes, boneOffset + 4),
                                F_12 = BitConverter.ToSingle(rawBytes, boneOffset + 16),
                                F_16 = BitConverter.ToSingle(rawBytes, boneOffset + 20),
                                F_20 = BitConverter.ToSingle(rawBytes, boneOffset + 24),
                                F_24 = BitConverter.ToSingle(rawBytes, boneOffset + 28),
                                F_28 = BitConverter.ToSingle(rawBytes, boneOffset + 32),
                                F_32 = BitConverter.ToSingle(rawBytes, boneOffset + 36),
                                F_36 = BitConverter.ToSingle(rawBytes, boneOffset + 40),
                                F_40 = BitConverter.ToSingle(rawBytes, boneOffset + 44),
                                F_44 = BitConverter.ToSingle(rawBytes, boneOffset + 48),
                                BoneName = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, boneOffset + 12) + boneOffset, false)
                            });
                        }
                        else
                        {
                            skeleton.Bones.Add(new Bone()
                            {
                                I_00 = BitConverter.ToInt32(rawBytes, boneOffset + 0),
                                I_04 = BitConverter.ToInt32(rawBytes, boneOffset + 4),
                                F_12 = BitConverter.ToSingle(rawBytes, boneOffset + 12),
                                F_16 = BitConverter.ToSingle(rawBytes, boneOffset + 16),
                                F_20 = BitConverter.ToSingle(rawBytes, boneOffset + 20),
                                F_24 = BitConverter.ToSingle(rawBytes, boneOffset + 24),
                                F_28 = BitConverter.ToSingle(rawBytes, boneOffset + 28),
                                F_32 = BitConverter.ToSingle(rawBytes, boneOffset + 32),
                                F_36 = BitConverter.ToSingle(rawBytes, boneOffset + 36),
                                F_40 = BitConverter.ToSingle(rawBytes, boneOffset + 40),
                                F_44 = BitConverter.ToSingle(rawBytes, boneOffset + 44),
                                BoneName = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, boneOffset + 48) + boneOffset, false)
                            });
                        }

                        boneOffset += 52;
                    }
                }
            }

            return skeleton;
        }

        //Utility
        
        private string GetStringWrapper(int relativeOffset, int mainOffset)
        {
            if(relativeOffset != 0)
            {
                return StringEx.GetString(rawBytes, relativeOffset + mainOffset, false);
            }
            else
            {
                return string.Empty;
            }
        }

    }
}
