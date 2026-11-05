using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.EMD;
using Xv2CoreLib.ESK;
using Xv2CoreLib.Resource;
using YAXLib;
using static Xv2CoreLib.FMP.CollisionCreator;

namespace Xv2CoreLib.FMP
{
    [YAXSerializeAs("FMP")]
    public class FMP_File
    {
        private const int SIGNATURE = 1347241507;

        private bool IsOldVersion => (Version & 0xFFF00) == 0;

        [YAXAttributeForClass]
        public int Version { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = FmpFlags.UseHavok | FmpFlags.Unk2)]
        public FmpFlags Flags { get; set; }
        [YAXAttributeForClass]
        public int I_12 { get; set; }

        public FMP_Settings Settings { get; set; }
        public FMP_CollisionLayerMatrix CollisionLayerMatrix { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive)]
        public List<FMP_PhysicsMaterial> PhysicsMaterials { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive)]
        public List<FMP_MotionPreset> MotionPresets { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "ObjectGroup")]
        public List<FMP_ObjectGroup> ObjectGroups { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Object")]
        public List<FMP_Object> Objects { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "CollisionGroup")]
        public List<FMP_CollisionGroup> CollisionGroups { get; set; }

        #region SaveLoad
        public static void SerializeToXml(string path)
        {
            FMP_File fmpFile = Load(File.ReadAllBytes(path));

            YAXSerializer serializer = new YAXSerializer(typeof(FMP_File));
            serializer.SerializeToFile(fmpFile, path + ".xml");
        }

        public static void DeserializeFromXml(string path)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            YAXSerializer serializer = new YAXSerializer(typeof(FMP_File), YAXSerializationOptions.DontSerializeNullObjects);
            FMP_File fmpFile = (FMP_File)serializer.DeserializeFromFile(path);
            fmpFile.ResolveObjectReferences(); //Also add this in the installer INSTALL method when loading the xml for map

            File.WriteAllBytes(saveLocation, fmpFile.SaveToBytes());
        }

        public static FMP_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        [FileLoad]
        public static FMP_File Load(byte[] bytes)
        {
            if (BitConverter.ToInt32(bytes, 0) != SIGNATURE)
            {
                throw new InvalidDataException("#FMP magic bytes not found. This is not a .map file!");
            }

            FMP_File fmpFile = new FMP_File();

            //Header
            fmpFile.Version = BitConverter.ToInt32(bytes, 4);
            fmpFile.Flags = (FmpFlags)BitConverter.ToInt32(bytes, 8);
            fmpFile.I_12 = BitConverter.ToInt32(bytes, 12);

            if (!fmpFile.Flags.HasFlag(FmpFlags.UseHavok))
                throw new NotSupportedException($"FMP_File.Load: This file has the UseHavok flag disabled. An FMP file can not be read in this configuration.");

            if (fmpFile.Flags.HasFlag(FmpFlags.UseCompressedHavok))
                throw new NotSupportedException($"FMP_File.Load: Compressed Havok files are not supported.");

            int settings_Offset = BitConverter.ToInt32(bytes, 16);
            int collisionLayerMatrix_Offset = BitConverter.ToInt32(bytes, 20);
            int physicsMaterials_Count = BitConverter.ToInt32(bytes, 24);
            int physicsMaterials_Offset = BitConverter.ToInt32(bytes, 28);
            int motionPresets_Count = BitConverter.ToInt32(bytes, 32);
            int motionPresets_Offset = BitConverter.ToInt32(bytes, 36);
            int fragmentGroup_Count = BitConverter.ToInt32(bytes, 40);
            int fragmentGroup_Offset = BitConverter.ToInt32(bytes, 44);
            int object_Count = BitConverter.ToInt32(bytes, 48);
            int object_Offset = BitConverter.ToInt32(bytes, 52);
            int hitboxGroup_Count = BitConverter.ToInt32(bytes, 56);
            int hitboxGroup_Offset = BitConverter.ToInt32(bytes, 60);
            int depot1_Count = BitConverter.ToInt32(bytes, 64);
            int depot1_Offset = BitConverter.ToInt32(bytes, 68);
            int depot2_Count = BitConverter.ToInt32(bytes, 72);
            int depot2_Offset = BitConverter.ToInt32(bytes, 76);
            int depot3_Count = BitConverter.ToInt32(bytes, 80);
            int depot3_Offset = BitConverter.ToInt32(bytes, 84);
            int depot4_Count = BitConverter.ToInt32(bytes, 88);
            int depot4_Offset = BitConverter.ToInt32(bytes, 92);

            int embeddedSection_Count = BitConverter.ToInt32(bytes, 96);
            int embeddedSection_Offset = BitConverter.ToInt32(bytes, 100);
            int depot5_Count = BitConverter.ToInt32(bytes, 104);
            int depot5_Offset = BitConverter.ToInt32(bytes, 108);

            if (depot5_Count > 0 || depot5_Offset > 0)
                throw new NotSupportedException("FMP_File.Load: Depot 5 (0x60 / 0x64) is not implemented.");

            if (embeddedSection_Count > 0 || embeddedSection_Offset > 0)
                throw new NotSupportedException("FMP_File.Load: Embedded section (0x68 / 0x6C) is not implemented.");
            
            string[] depot1 = GetDepotStrings(bytes, depot1_Offset, depot1_Count);
            string[] depot2 = GetDepotStrings(bytes, depot2_Offset, depot2_Count);
            string[] depot3 = GetDepotStrings(bytes, depot3_Offset, depot3_Count);
            string[] depot4 = GetDepotStrings(bytes, depot4_Offset, depot4_Count);

            fmpFile.Settings = FMP_Settings.Read(bytes, settings_Offset);
            fmpFile.CollisionLayerMatrix = FMP_CollisionLayerMatrix.Read(bytes, collisionLayerMatrix_Offset);
            fmpFile.PhysicsMaterials = FMP_PhysicsMaterial.ReadAll(bytes, physicsMaterials_Offset, physicsMaterials_Count);
            fmpFile.MotionPresets = FMP_MotionPreset.ReadAll(bytes, motionPresets_Offset, motionPresets_Count);
            fmpFile.ObjectGroups = FMP_ObjectGroup.ReadAll(bytes, fragmentGroup_Offset, fragmentGroup_Count);
            fmpFile.CollisionGroups = FMP_CollisionGroup.ReadAll(bytes, hitboxGroup_Offset, hitboxGroup_Count, fmpFile.IsOldVersion);
            fmpFile.Objects = FMP_Object.ReadAll(bytes, object_Offset, object_Count, depot1, depot2, depot3, depot4, fmpFile);

            fmpFile.ResolveObjectReferences();

            return fmpFile;
        }

        [FileSave]
        public byte[] SaveToBytes()
        {
            ResolveIndexReferences();

            foreach(var hitboxGroup in CollisionGroups)
            {
                hitboxGroup.CreateUnorderedHitboxList();
            }

            List<byte> bytes = new List<byte>();

            List<string> depot1 = new List<string>();
            List<string> depot2 = new List<string>();
            List<string> depot3 = new List<string>();
            List<string> depot4 = new List<string>();
            CreateDepots(depot1, depot2, depot3, depot4);

            int physicsMaterialCount = PhysicsMaterials != null ? PhysicsMaterials.Count : 0;
            int motionPresetCount = MotionPresets != null ? MotionPresets.Count : 0;
            int fragmentCount = ObjectGroups != null ? ObjectGroups.Count : 0;
            int objectCount = Objects != null ? Objects.Count : 0;
            int hitboxGroupCount = CollisionGroups != null ? CollisionGroups.Count : 0;

            List<StringWriter.StringInfo> stringsWriter = new List<StringWriter.StringInfo>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes((int)Flags));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes((int)112)); //Offset to Settings. Comes after header, so always 112
            bytes.AddRange(BitConverter.GetBytes((int)252)); //20, offset to CollisionLayerMatrix, always 252 since it comes after Settings, which is also fixed size
            bytes.AddRange(BitConverter.GetBytes(physicsMaterialCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(motionPresetCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(fragmentCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(objectCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(hitboxGroupCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(depot1.Count));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(depot2.Count));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(depot3.Count));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(depot4.Count));
            bytes.AddRange(new byte[4]);

            bytes.AddRange(new byte[8]); //Depot 5 count / offset, always 0 since depot 5 is not supported (unused by game)
            bytes.AddRange(new byte[8]);

            if (bytes.Count != 112)
                throw new Exception("FMP_File.Write: Header size is incorrect!");

            bytes.AddRange(Settings.Write());
            bytes.AddRange(CollisionLayerMatrix.Write(bytes.Count, stringsWriter));

            if(physicsMaterialCount > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 28);
                FMP_PhysicsMaterial.WriteAll(PhysicsMaterials, bytes, stringsWriter);
            }

            if (motionPresetCount > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 36);
                FMP_MotionPreset.WriteAll(MotionPresets, bytes, stringsWriter);
            }

            if (fragmentCount > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 44);
                FMP_ObjectGroup.WriteAll(ObjectGroups, bytes, stringsWriter);
            }

            if (objectCount > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 52);
                FMP_Object.WriteAll(Objects, CollisionGroups, bytes, stringsWriter);
            }

            if (hitboxGroupCount > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 60);
                FMP_CollisionGroup.WriteAll(CollisionGroups, bytes, stringsWriter, IsOldVersion);
            }

            //Write depots
            if (depot1.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 68);
                WriteDepot(bytes, depot1, stringsWriter);
            }

            if (depot2.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 76);
                WriteDepot(bytes, depot2, stringsWriter);
            }

            if (depot3.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 84);
                WriteDepot(bytes, depot3, stringsWriter);
            }

            if (depot4.Count > 0)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 92);
                WriteDepot(bytes, depot4, stringsWriter);
            }

            //Write name strings
            StringWriter.WriteStrings(stringsWriter, bytes, true);

            return bytes.ToArray();
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, SaveToBytes());
        }
        
        private static string[] GetDepotStrings(byte[] bytes, int offset, int count)
        {
            string[] files = new string[count];

            for(int i = 0; i < count; i++)
            {
                files[i] = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset + (4 * i)));
            }

            return files;
        }
        
        private void CreateDepots(List<string> depot1,  List<string> depot2, List<string> depot3, List<string> depot4)
        {
            if (Objects == null) return;

            foreach(var obj in Objects)
            {
                if (obj.Entities == null) continue;

                foreach(var entity in obj.Entities)
                {
                    if(entity.Visual != null)
                    {
                        entity.Visual.CreateDepots(depot1, depot2, depot3, depot4);
                    }
                }
            }
        }
        
        private static void WriteDepot(List<byte> bytes, List<string> depotStrings, List<StringWriter.StringInfo> stringWriter)
        {
            foreach(string depotString in depotStrings)
            {
                if (!string.IsNullOrWhiteSpace(depotString))
                    stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = depotString });

                bytes.AddRange(new byte[4]);
            }
        }
        
        private void ResolveObjectReferences()
        {
            foreach(var fragment in ObjectGroups)
            {
                if(fragment.ObjectIndices != null)
                {
                    foreach (var index in fragment.ObjectIndices)
                    {
                        index.Object = Objects.FirstOrDefault(x => x.Index == index.Index);
                    }
                }
            }

            foreach(var obj in Objects)
            {
                if(obj.CollisionGroupInstance != null)
                {
                    obj.CollisionGroupInstance.CollisionGroup = CollisionGroups.FirstOrDefault(x => x.Index == obj.CollisionGroupInstance.CollisionGroupIndex);
                }
            }
        }

        private void ResolveIndexReferences()
        {
            foreach (var fragment in ObjectGroups)
            {
                if(fragment.ObjectIndices != null)
                {
                    for (int i = fragment.ObjectIndices.Count - 1; i >= 0; i--)
                    {
                        int idx = Objects.IndexOf(fragment.ObjectIndices[i].Object);

                        if (idx == -1)
                        {
                            fragment.ObjectIndices.RemoveAt(i);
                            continue;
                        }

                        fragment.ObjectIndices[i].Index = (ushort)idx;
                    }
                }
            }

            foreach (var obj in Objects)
            {
                if (obj.CollisionGroupInstance != null)
                {
                    int idx = CollisionGroups.IndexOf(obj.CollisionGroupInstance.CollisionGroup);

                    obj.CollisionGroupInstance.CollisionGroupIndex = (ushort)(idx == -1 ? ushort.MaxValue : idx);
                }
            }
        }
        #endregion

        public void GetCollisionCount(out int collisionGroups, out int havokCount)
        {
            collisionGroups = CollisionGroups.Count;
            havokCount = 0;

            foreach(var collisionGroup in CollisionGroups)
            {
                collisionGroup.CreateUnorderedHitboxList();

                foreach(var collider in collisionGroup.UnorderedHitboxList)
                {
                    foreach(var havok in collider.HavokColliders)
                    {
                        if(havok.HvkFile?.Length > 0)
                            havokCount++;
                    }
                }
            }
        }
    
        public void CreateCollision(FMP_Object obj, EMD_File emdModel, ESK_File eskSkeleton = null, List<MeshFlags> externalFlags = null)
        {
            CollisionCreator.CreateCollision(this, obj, emdModel, eskSkeleton, externalFlags);
        }

        public EMD_File ExportCollision(FMP_Object obj)
        {
            return CollisionCreator.ExportCollisionAsEmd(this, obj);
        }
    }

    public class FMP_Settings
    {
        [CustomSerialize(parent: "Width", serializeAs: "X", isFloat: true)]
        public float WidthX { get; set; }
        [CustomSerialize(parent: "Width", serializeAs: "Y", isFloat: true)]
        public float WidthY { get; set; }
        [CustomSerialize(parent: "Width", serializeAs: "Z", isFloat: true)]
        public float WidthZ { get; set; }
        [CustomSerialize(isFloat: true)]
        public float NearDistance { get; set; } //128
        [CustomSerialize(isFloat: true)]
        public float FarDistance { get; set; } //132
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_136/value")]
        public float DefaultFoV { get; set; } //Radians, convert to degrees on read and then back on save
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_12/value")]
        public float CenterX { get; set; } //12
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_16/value")]
        public float CenterY { get; set; } //16
        [YAXAltAliases("F_20/value")]
        [CustomSerialize(isFloat: true)]
        public float CenterZ { get; set; } //20
        [CustomSerialize(isFloat: true)]
        public float F_24 { get; set; }
        [CustomSerialize]
        public int I_28 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_32 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_36 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_40 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_44 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_48 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_52 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_56 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_60 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_64 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_68 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_72 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_76 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_80 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_84 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_88 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_92 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_96 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_100 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_104 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_108 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_112 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_116 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_120 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_124 { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(WidthX));
            bytes.AddRange(BitConverter.GetBytes(WidthY));
            bytes.AddRange(BitConverter.GetBytes(WidthZ));
            bytes.AddRange(BitConverter.GetBytes(CenterX));
            bytes.AddRange(BitConverter.GetBytes(CenterY));
            bytes.AddRange(BitConverter.GetBytes(CenterZ));
            bytes.AddRange(BitConverter.GetBytes(F_24));
            bytes.AddRange(BitConverter.GetBytes(I_28));
            bytes.AddRange(BitConverter.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(F_36));
            bytes.AddRange(BitConverter.GetBytes(F_40));
            bytes.AddRange(BitConverter.GetBytes(F_44));
            bytes.AddRange(BitConverter.GetBytes(F_48));
            bytes.AddRange(BitConverter.GetBytes(F_52));
            bytes.AddRange(BitConverter.GetBytes(F_56));
            bytes.AddRange(BitConverter.GetBytes(F_60));
            bytes.AddRange(BitConverter.GetBytes(F_64));
            bytes.AddRange(BitConverter.GetBytes(F_68));
            bytes.AddRange(BitConverter.GetBytes(F_72));
            bytes.AddRange(BitConverter.GetBytes(F_76));
            bytes.AddRange(BitConverter.GetBytes(F_80));
            bytes.AddRange(BitConverter.GetBytes(F_84));
            bytes.AddRange(BitConverter.GetBytes(F_88));
            bytes.AddRange(BitConverter.GetBytes(F_92));
            bytes.AddRange(BitConverter.GetBytes(F_96));
            bytes.AddRange(BitConverter.GetBytes(F_100));
            bytes.AddRange(BitConverter.GetBytes(F_104));
            bytes.AddRange(BitConverter.GetBytes(F_108));
            bytes.AddRange(BitConverter.GetBytes(F_112));
            bytes.AddRange(BitConverter.GetBytes(F_116));
            bytes.AddRange(BitConverter.GetBytes(F_120));
            bytes.AddRange(BitConverter.GetBytes(F_124));
            bytes.AddRange(BitConverter.GetBytes(NearDistance));
            bytes.AddRange(BitConverter.GetBytes(FarDistance));
            bytes.AddRange(BitConverter.GetBytes(MathHelpers.ToRadians(DefaultFoV)));

            if (bytes.Count != 140)
                throw new Exception("SettingsA.Write: Incorrect size!");

            return bytes.ToArray();
        }

        public static FMP_Settings Read(byte[] bytes, int offset)
        {
            return new FMP_Settings()
            {
                WidthX = BitConverter.ToSingle(bytes, offset),
                WidthY = BitConverter.ToSingle(bytes, offset + 4),
                WidthZ = BitConverter.ToSingle(bytes, offset + 8),
                CenterX = BitConverter.ToSingle(bytes, offset + 12),
                CenterY = BitConverter.ToSingle(bytes, offset + 16),
                CenterZ = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                I_28 = BitConverter.ToInt32(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52),
                F_56 = BitConverter.ToSingle(bytes, offset + 56),
                F_60 = BitConverter.ToSingle(bytes, offset + 60),
                F_64 = BitConverter.ToSingle(bytes, offset + 64),
                F_68 = BitConverter.ToSingle(bytes, offset + 68),
                F_72 = BitConverter.ToSingle(bytes, offset + 72),
                F_76 = BitConverter.ToSingle(bytes, offset + 76),
                F_80 = BitConverter.ToSingle(bytes, offset + 80),
                F_84 = BitConverter.ToSingle(bytes, offset + 84),
                F_88 = BitConverter.ToSingle(bytes, offset + 88),
                F_92 = BitConverter.ToSingle(bytes, offset + 92),
                F_96 = BitConverter.ToSingle(bytes, offset + 96),
                F_100 = BitConverter.ToSingle(bytes, offset + 100),
                F_104 = BitConverter.ToSingle(bytes, offset + 104),
                F_108 = BitConverter.ToSingle(bytes, offset + 108),
                F_112 = BitConverter.ToSingle(bytes, offset + 112),
                F_116 = BitConverter.ToSingle(bytes, offset + 116),
                F_120 = BitConverter.ToSingle(bytes, offset + 120),
                F_124 = BitConverter.ToSingle(bytes, offset + 124),
                NearDistance = BitConverter.ToSingle(bytes, offset + 128),
                FarDistance = BitConverter.ToSingle(bytes, offset + 132),
                DefaultFoV = MathHelpers.ToDegrees(BitConverter.ToSingle(bytes, offset + 136))
            };
        }
    }

    public class FMP_CollisionLayerMatrix
    {
        private const int NUM_ROWS = 64;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement)]
        public List<FMP_CollisionLayerRow> Layers { get; set; } = new(64);

        public static FMP_CollisionLayerMatrix Read(byte[] bytes, int offset)
        {
            FMP_CollisionLayerMatrix matrix = new FMP_CollisionLayerMatrix();
            BitArray bits = new BitArray(bytes.GetRange(offset, 256));
            int nameStart = offset + 256;

            //Create Layers 1 - 63. Layer zero is not included.
            for(int row = 1; row < NUM_ROWS; row++)
            {
                int nameOffset = nameStart + (4 * (row - 1)); //Layer 0 does not have a name
                nameOffset = BitConverter.ToInt32(bytes, nameOffset);
                string layerName = nameOffset > 0 ? StringEx.GetString(bytes, nameOffset) : null;
            
                matrix.Layers.Add(new FMP_CollisionLayerRow() { Layer = row, Name = layerName });
            }

            //Decode the matrix
            for(byte row = 0; row < NUM_ROWS; row++)
            {
                byte adjustedRow = (byte)(row - 1);
                int bitIndex = 64 * (row - 1) - (row - 1) * row / 2;
                int bitCount = 64 - row;

                if(bitIndex >= 0)
                {
                    for(int bit = bitIndex; bit < bitIndex + bitCount; bit++)
                    {
                        if (bits[bit])
                        {
                            byte layer = (byte)((bit - bitIndex) + row);
                            matrix.Layers[adjustedRow].Layers.Add(layer);

                            if(!matrix.Layers[layer - 1].Layers.Contains(row))
                                matrix.Layers[layer - 1].Layers.Add(row);
                        }
                    }

                }
            }

            //Sort the layer lists
            foreach(var row in matrix.Layers)
            {
                row.Layers.Sort();
            }

            return matrix;
        }

        public byte[] Write(int fileSize, List<StringWriter.StringInfo> stringWriter)
        {
            //Validate the matrix contains the expected data and is sorted
            if(Layers is null || Layers?.Count != 63)
            {
                throw new InvalidDataException($"FMP_CollisionLayerMatrix: Invalid number of rows. Expected 63.");
            }

            Layers.Sort((x, y) => x.Layer - y.Layer);

            for(int row = 0; row < 63; row++)
            {
                if (Layers[row].Layer != row + 1)
                    throw new InvalidDataException($"FMP_CollisionLayerMatrix: Invalid layer order. Expected Layer {row + 1}, found {Layers[row].Layer}");
            }

            byte[] bytes = new byte[512]; //256 for matrix, 256 for name offsets
            int nameStartOffset = fileSize + 256;
            BitArray bits = new BitArray(256 * 8, false);

            //Encode the matrix to a bitfield
            for (byte row = 0; row < NUM_ROWS; row++)
            {
                byte rowIndex = (byte)(row - 1);
                int bitIndex = 64 * (row - 1) - (row - 1) * row / 2;

                if (bitIndex >= 0)
                {
                    foreach(var layer in Layers[rowIndex].Layers)
                    {
                        if(layer >= row && layer < NUM_ROWS)
                        {
                            bits[bitIndex + (layer - row)] = true;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(Layers[rowIndex].Name))
                    {
                        stringWriter.Add(new StringWriter.StringInfo()
                        {
                            Offset = nameStartOffset + (4 * rowIndex),
                            StringToWrite = Layers[rowIndex].Name
                        });
                    }
                }
            }

            bits.CopyTo(bytes, 0);

            return bytes;
        }
    }

    [YAXSerializeAs("Layer")]
    public class FMP_CollisionLayerRow
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int Layer { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public List<byte> Layers { get; set; } = new List<byte>();

    }

    [YAXSerializeAs("PhysicsMaterial")]
    public class FMP_PhysicsMaterial
    {
        [YAXAttributeForClass]
        public string Name { get; set; }

        [CustomSerialize]
        public HavokTriggerType TriggerType { get; set; }
        [CustomSerialize(isFloat: true)]
        public float DynamicFriction { get; set; }
        [CustomSerialize(isFloat: true)]
        public float StaticFriction { get; set; }
        [CustomSerialize(isFloat: true)]
        public float Restitution { get; set; }
        [CustomSerialize(isFloat: true)]
        public float TriggerVolumeTolerance { get; set; }

        internal static void WriteAll(List<FMP_PhysicsMaterial> entries, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            for(int i = 0; i < entries.Count; i++)
            {
                bytes.AddRange(entries[i].Write(bytes.Count, stringWriter));
            }
        }

        internal List<byte> Write(int fileSize, List<StringWriter.StringInfo> stringWriter)
        {
            List<byte> bytes = new List<byte>();

            stringWriter.Add(new StringWriter.StringInfo() { Offset = fileSize, StringToWrite = Name });
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes((int)TriggerType));
            bytes.AddRange(BitConverter.GetBytes(DynamicFriction));
            bytes.AddRange(BitConverter.GetBytes(StaticFriction));
            bytes.AddRange(BitConverter.GetBytes(Restitution));
            bytes.AddRange(BitConverter.GetBytes(TriggerVolumeTolerance));

            if (bytes.Count != 24)
                throw new Exception("FMP_Section1.Write: Invalid size");

            return bytes;
        }

        public static List<FMP_PhysicsMaterial> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_PhysicsMaterial> entries = new List<FMP_PhysicsMaterial>();

            for(int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (24 * i)));
            }

            return entries;
        }

        public static FMP_PhysicsMaterial Read(byte[] bytes, int offset)
        {
            return new FMP_PhysicsMaterial()
            {
                Name = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset)),
                TriggerType = (HavokTriggerType)BitConverter.ToInt32(bytes, offset + 4),
                DynamicFriction = BitConverter.ToSingle(bytes, offset + 8),
                StaticFriction = BitConverter.ToSingle(bytes, offset + 12),
                Restitution = BitConverter.ToSingle(bytes, offset + 16),
                TriggerVolumeTolerance = BitConverter.ToSingle(bytes, offset + 20)
            };
        }
    }

    [YAXSerializeAs("MotionPreset")]
    public class FMP_MotionPreset
    {
        [YAXAttributeForClass]
        public string Name { get; set; }

        [CustomSerialize(isFloat: true)]
        public float GravityFactor { get; set; }
        [CustomSerialize(isFloat: true)]
        public float TimeFactor { get; set; }
        [CustomSerialize(isFloat: true)]
        public float MaximumLinearSpeed { get; set; }
        [CustomSerialize(isFloat: true)]
        public float MaximumAngularSpeed { get; set; }
        [CustomSerialize(isFloat: true)]
        public float LinearDamping { get; set; }
        [CustomSerialize(isFloat: true)]
        public float AngularDamping { get; set; }

        internal static void WriteAll(List<FMP_MotionPreset> entries, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                bytes.AddRange(entries[i].Write(bytes.Count, stringWriter));
            }
        }

        internal List<byte> Write(int fileSize, List<StringWriter.StringInfo> stringWriter)
        {
            List<byte> bytes = new List<byte>();

            stringWriter.Add(new StringWriter.StringInfo() { Offset = fileSize, StringToWrite = Name });
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(GravityFactor));
            bytes.AddRange(BitConverter.GetBytes(TimeFactor));
            bytes.AddRange(BitConverter.GetBytes(MaximumLinearSpeed));
            bytes.AddRange(BitConverter.GetBytes(MaximumAngularSpeed));
            bytes.AddRange(BitConverter.GetBytes(LinearDamping));
            bytes.AddRange(BitConverter.GetBytes(AngularDamping));

            if (bytes.Count != 28)
                throw new Exception("FMP_Section2.Write: Invalid size");

            return bytes;
        }

        public static List<FMP_MotionPreset> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_MotionPreset> entries = new List<FMP_MotionPreset>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (28 * i)));
            }

            return entries;
        }

        public static FMP_MotionPreset Read(byte[] bytes, int offset)
        {
            return new FMP_MotionPreset()
            {
                Name = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset)),
                GravityFactor = BitConverter.ToSingle(bytes, offset + 4),
                TimeFactor = BitConverter.ToSingle(bytes, offset + 8),
                MaximumLinearSpeed = BitConverter.ToSingle(bytes, offset + 12),
                MaximumAngularSpeed = BitConverter.ToSingle(bytes, offset + 16),
                LinearDamping = BitConverter.ToSingle(bytes, offset + 20),
                AngularDamping = BitConverter.ToSingle(bytes, offset + 24)
            };
        }
    }

    public class FMP_ObjectGroup
    {
        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ObjectIndex")]
        [YAXAttributeFor("ObjectIndex")]
        public List<FMP_ObjectIndex> ObjectIndices { get; set; } = new List<FMP_ObjectIndex>();

        internal static void WriteAll(List<FMP_ObjectGroup> entries, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            int fragmentStart = bytes.Count;

            //Main body, 12 bytes each
            for (int i = 0; i < entries.Count; i++)
            {
                stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = entries[i].Name });
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(entries[i].ObjectIndices != null ? entries[i].ObjectIndices.Count : 0));
                bytes.AddRange(new byte[4]);
            }

            //Values
            for (int i = 0; i < entries.Count; i++)
            {
                int indicesCount = entries[i].ObjectIndices != null ? entries[i].ObjectIndices.Count : 0;

                if(indicesCount > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), fragmentStart + (12 * i) + 8);

                    foreach(var entry in entries[i].ObjectIndices)
                    {
                        bytes.AddRange(BitConverter.GetBytes(entry.Index));
                    }
                }
            }

            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
        }

        public static List<FMP_ObjectGroup> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_ObjectGroup> entries = new List<FMP_ObjectGroup>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (12 * i)));
            }

            return entries;
        }

        public static FMP_ObjectGroup Read(byte[] bytes, int offset)
        {
            FMP_ObjectGroup fragmentGroup = new FMP_ObjectGroup();
            fragmentGroup.Name = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset));

            int indexCount = BitConverter.ToInt32(bytes, offset + 4);
            int indexOffset = BitConverter.ToInt32(bytes, offset + 8);

            for(int i = 0; i < indexCount; i++)
            {
                fragmentGroup.ObjectIndices.Add(new FMP_ObjectIndex() { Index = BitConverter.ToUInt16(bytes, indexOffset + (2 * i)) });
            }

            return fragmentGroup;
        }
    }

    [YAXSerializeAs("ObjectIndex")]
    public class FMP_ObjectIndex
    {
        [YAXAttributeForClass]
        public ushort Index { get; set; }

        [YAXDontSerialize]
        public FMP_Object Object { get; set; }
    }

}
