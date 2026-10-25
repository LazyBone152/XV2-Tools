using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.EEPK
{
    [Serializable]
    [YAXSerializeAs("EEPK")]
    public class EEPK_File : ISorting
    {
        public const int EEPK_SIGNATURE = 1263551779;
        private const int EEPK_HEADER_SIZE = 24;
        private const int EEPK_EFFECT_SIZE = 16;
        private const int EEPK_EFFECT_PART_SIZE = 100;
        private const int EEPK_ASSET_CONTAINER_SIZE = 48;
        private const int EEPK_ASSET_SIZE = 12;

        public int Version = 37568;
        [YAXSerializeAs("Containers")]
        [YAXDontSerializeIfNull]
        public List<EepkAssetContainer> AssetContainers { get; set; } = new List<EepkAssetContainer>(5);
        [YAXDontSerializeIfNull]
        public List<Effect> Effects { get; set; } = new List<Effect>();

        public EEPK_File() { }

        public EEPK_File(EffectContainerFile effectContainer)
        {
            Version = (int)effectContainer.Version;
            Effects.AddRange(effectContainer.Effects);

            if (effectContainer.Emo.Assets.Count > 0)
                AssetContainers.Add(new EepkAssetContainer(effectContainer.Emo));
            
            if (effectContainer.Pbind.Assets.Count > 0)
                AssetContainers.Add(new EepkAssetContainer(effectContainer.Pbind));
            
            if (effectContainer.Tbind.Assets.Count > 0)
                AssetContainers.Add(new EepkAssetContainer(effectContainer.Tbind));
            
            if (effectContainer.LightEma.Assets.Count > 0)
                AssetContainers.Add(new EepkAssetContainer(effectContainer.LightEma));
            
            if (effectContainer.Cbind.Assets.Count > 0)
                AssetContainers.Add(new EepkAssetContainer(effectContainer.Cbind));
        }

        #region LoadSave
        public static EEPK_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static EEPK_File Load(byte[] bytes)
        {
            EEPK_File eepkFile = new EEPK_File();

            //Header
            if (BitConverter.ToInt32(bytes, 0) != EEPK_SIGNATURE)
                throw new InvalidDataException("#EPK signature not found.\nLoad failed.");

            eepkFile.Version = BitConverter.ToInt32(bytes, 8);
            ushort assetContainerCount = BitConverter.ToUInt16(bytes, 12);
            ushort effectCount = BitConverter.ToUInt16(bytes, 14);
            int assetContainerOffset = BitConverter.ToInt32(bytes, 16);
            int effectTableOffset = BitConverter.ToInt32(bytes, 20);

            //Parse assets
            for (ushort containerIdx = 0; containerIdx < assetContainerCount; containerIdx++)
            {
                int offset = assetContainerOffset + (EEPK_ASSET_CONTAINER_SIZE * containerIdx);
                var assetContainer = new EepkAssetContainer();

                assetContainer.PoolSize = BitConverter.ToInt32(bytes, offset);
                assetContainer.ParticleEngineConfig1 = BitConverter.ToInt32(bytes, offset + 4);
                assetContainer.ParticleEngineConfig2 = BitConverter.ToInt32(bytes, offset + 8);
                assetContainer.ParticleEngineConfig3 = BitConverter.ToInt32(bytes, offset + 12);
                assetContainer.AssetType = (AssetType)BitConverter.ToUInt16(bytes, offset + 16);

                ushort assetCount = BitConverter.ToUInt16(bytes, offset + 30);
                int assetStartOffset = BitConverter.ToInt32(bytes, offset + 32) + offset; //relative to container

                int embContainerOffset = BitConverter.ToInt32(bytes, offset + 36);
                int emmOffset = BitConverter.ToInt32(bytes, offset + 40);
                int embTextureOffset = BitConverter.ToInt32(bytes, offset + 44);

                assetContainer.ContainerEmbPath = embContainerOffset > 0 ? bytes.ReadStringASCII(embContainerOffset + offset) : null;
                assetContainer.MaterialEmmPath = emmOffset > 0 ? bytes.ReadStringASCII(emmOffset + offset) : null;
                assetContainer.TextureEmbPath = embTextureOffset > 0 ? bytes.ReadStringASCII(embTextureOffset + offset) : null;

                //Parse assets
                for (int assetIdx = 0; assetIdx < assetCount; assetIdx++)
                {
                    int assetOffset =  assetStartOffset + (EEPK_ASSET_SIZE * assetIdx);
                    EepkAssetEntry asset = new EepkAssetEntry();

                    asset.XML_Index = assetIdx;
                    asset.I_00 = BitConverter.ToInt16(bytes, assetOffset);
                    AssetType assetType = (AssetType)bytes[assetOffset + 2];
                    byte assetFileCount = bytes[assetOffset + 3];
                    int numberOffset = BitConverter.ToInt32(bytes, assetOffset + 4);
                    int assetFilesOffset = BitConverter.ToInt32(bytes, assetOffset + 8); //relative

                    if (assetType != assetContainer.AssetType)
                        throw new InvalidDataException($"EEPK_File.Load: AssetType mismatch; found asset with type {assetType} in {assetContainer.AssetType} container");

                    if (assetFilesOffset > 0)
                    {
                        for (int assetFileIdx = 0; assetFileIdx < assetFileCount; assetFileIdx++)
                        {
                            //EepkAssetFileType assetFileType = (EepkAssetFileType)bytes[numberOffset + assetFileIdx];
                            int stringOffset = BitConverter.ToInt32(bytes, assetOffset + assetFilesOffset + (4 * assetFileIdx));

                            if(stringOffset > 0)
                            {
                                asset.Files.Add(bytes.ReadStringASCII(stringOffset + assetOffset));
                            }
                        }
                    }

                    assetContainer.Assets.Add(asset);
                }

                eepkFile.AssetContainers.Add(assetContainer);
            }

            //Parse effects
            for(ushort effectIdx = 0; effectIdx < effectCount; effectIdx++)
            {
                int effectOffset = BitConverter.ToInt32(bytes, effectTableOffset + (effectIdx * 4));

                if(effectOffset > 0)
                {
                    if (BitConverter.ToUInt16(bytes, effectOffset) != effectIdx)
                        throw new InvalidDataException("EEPK_File.Load: EffectID does not match its index");

                    Effect effect = new Effect();
                    effect.IndexNum = effectIdx;
                    effect.I_02 = BitConverter.ToUInt16(bytes, effectOffset + 2);
                    //ushort I_04 = BitConverter.ToUInt16(bytes, effectOffset + 4); //always 0
                    //ushort I_06 = BitConverter.ToUInt16(bytes, effectOffset + 6); //always 0
                    //ushort I_08 = BitConverter.ToUInt16(bytes, effectOffset + 8); //always 0
                    ushort effectPartCount = BitConverter.ToUInt16(bytes, effectOffset + 10);
                    int effectPartOffset = BitConverter.ToInt32(bytes, effectOffset + 12) + effectOffset;

                    for (int effectPartIdx = 0; effectPartIdx < effectPartCount; effectPartIdx++)
                    {
                        EffectPart effectPart = new EffectPart();

                        //Read EffectPart
                        effectPart.AssetIndex = BitConverter.ToUInt16(bytes, effectPartOffset);
                        effectPart.AssetType = (AssetType)bytes[effectPartOffset + 2];
                        effectPart.AttachementType = (Attachment)bytes[effectPartOffset + 3];
                        effectPart.Orientation = (OrientationType)bytes[effectPartOffset + 4];
                        effectPart.Deactivation = (DeactivationMode)bytes[effectPartOffset + 5];
                        effectPart.I_06 = bytes[effectPartOffset + 6];
                        effectPart.DepthBias = BitConverter.ToSingle(bytes, effectPartOffset + 24);
                        effectPart.StartTime = BitConverter.ToUInt16(bytes, effectPartOffset + 28);
                        effectPart.EMA_AnimationIndex = BitConverter.ToUInt16(bytes, effectPartOffset + 30);
                        effectPart.Flags1 = (EepkEffectPartFlags1)BitConverter.ToUInt32(bytes, effectPartOffset + 32);
                        effectPart.Flags2 = (EepkEffectPartFlags2)BitConverter.ToUInt32(bytes, effectPartOffset + 36);
                        effectPart.PositionX = BitConverter.ToSingle(bytes, effectPartOffset + 40);
                        effectPart.PositionY = BitConverter.ToSingle(bytes, effectPartOffset + 44);
                        effectPart.PositionZ = BitConverter.ToSingle(bytes, effectPartOffset + 48);

                        effectPart.RotationX_Min = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(bytes, effectPartOffset + 52));
                        effectPart.RotationX_Max = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(bytes, effectPartOffset + 56));
                        effectPart.RotationY_Min = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(bytes, effectPartOffset + 60));
                        effectPart.RotationY_Max = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(bytes, effectPartOffset + 64));
                        effectPart.RotationZ_Min = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(bytes, effectPartOffset + 68));
                        effectPart.RotationZ_Max = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(bytes, effectPartOffset + 72));

                        effectPart.ScaleMin = BitConverter.ToSingle(bytes, effectPartOffset + 76);
                        effectPart.ScaleMax = BitConverter.ToSingle(bytes, effectPartOffset + 80);
                        effectPart.NearFadeDistance = BitConverter.ToSingle(bytes, effectPartOffset + 84);
                        effectPart.FarFadeDistance = BitConverter.ToSingle(bytes, effectPartOffset + 88);
                        effectPart.EMA_LoopStartFrame = BitConverter.ToUInt16(bytes, effectPartOffset + 92);
                        effectPart.EMA_LoopEndFrame = BitConverter.ToUInt16(bytes, effectPartOffset + 94);
                        int eskOffset = BitConverter.ToInt32(bytes, effectPartOffset + 96);

                        if(eskOffset > 0)
                        {
                            effectPart.ESK = bytes.ReadStringASCII(effectPartOffset + eskOffset);
                        }

                        effectPartOffset += EEPK_EFFECT_PART_SIZE;
                        effect.EffectParts.Add(effectPart);
                    }

                    eepkFile.Effects.Add(effect);
                }
            }

            return eepkFile;
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            //Note about file size difference for a small amount of EEPK files:
            //There are a small number of vanilla EEPKs that include "empty" effect definitions in the files. These do not exist in the effect table and have no effect parts, but take up space in the file amongst all the other valid effects
            //When loading these EEPKs, these empty effects are ignored, and thus when saving them the bytes are now different.
            //The vast majority of EEPKs are rewritten with identical bytes (allowing for minor float precision issues), and nothing important is lost in the few that don't
            
            //Calculate section length and overall file size
            int effectTableLength = Effects.Count > 0 ? Effects.Max(x => x.IndexNum) + 1 : 0;
            int effectTableSize = effectTableLength * sizeof(int);
            int assetContainerSectionSize = EEPK_ASSET_CONTAINER_SIZE * AssetContainers.Count;
            CalculateFileSectionSizes(out int assetSectionSize, out int effectSectionSize, out int stringSectionSize, out int assetFileTableSectionSize);
            //int assetSectionSize = CalculateAssetSectionSize();
            //int assetFileTableSectionSize = CalculateAssetFileTableSectionSize();
            //int effectSectionSize = CalculateEffectSectionSize();
            //int stringSectionSize = CalculateStringSectionSize();

            int fileSize = EEPK_HEADER_SIZE + effectTableSize + assetContainerSectionSize + assetSectionSize + assetFileTableSectionSize + effectSectionSize + stringSectionSize;

            //Calculate start index into buffer for effects, assets and end string section
            const int effectTableStart = EEPK_HEADER_SIZE;
            int assetContainerStart = effectTableStart + effectTableSize;
            int assetEntryStart = assetContainerStart + assetContainerSectionSize;
            int assetFileTableStart = assetEntryStart + assetSectionSize;
            int effectStart = assetFileTableStart + assetFileTableSectionSize;
            int effectPartStart = effectStart + (Effects.Count * EEPK_EFFECT_SIZE);
            int stringSectionStart = effectStart + effectSectionSize;

            //Buffer positions
            int stringBufferPosition = stringSectionStart;
            int assetFileTableBufferPosition = assetFileTableStart;
            int effectPartOffset = effectPartStart;

            //Create buffer to write into
            byte[] buffer = new byte[fileSize];
            Span<byte> span = buffer;

            //Header
            Span<byte> header = span.Slice(0, EEPK_HEADER_SIZE);
            BinaryPrimitives.WriteInt32LittleEndian(header, EEPK_SIGNATURE);
            BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(4, 2), 65534);
            BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(6, 2), EEPK_HEADER_SIZE);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(8, 4), Version);
            BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(12), (ushort)AssetContainers.Count);
            BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(14), (ushort)effectTableLength);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(16, 4), assetContainerStart);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(20, 4), effectTableStart);

            //AssetContainer
            for (int assetContainerIdx = 0; assetContainerIdx < AssetContainers.Count; assetContainerIdx++)
            {
                int containerOffset = assetContainerStart + (assetContainerIdx * EEPK_ASSET_CONTAINER_SIZE);
                Span<byte> assetContainerSpan = span.Slice(containerOffset, EEPK_ASSET_CONTAINER_SIZE);
                EepkAssetContainer assetContainer = AssetContainers[assetContainerIdx];

                BinaryPrimitives.WriteInt32LittleEndian(assetContainerSpan, assetContainer.PoolSize);
                BinaryPrimitives.WriteInt32LittleEndian(assetContainerSpan.Slice(4, 4), assetContainer.ParticleEngineConfig1);
                BinaryPrimitives.WriteInt32LittleEndian(assetContainerSpan.Slice(8, 4), assetContainer.ParticleEngineConfig2);
                BinaryPrimitives.WriteInt32LittleEndian(assetContainerSpan.Slice(12, 4), assetContainer.ParticleEngineConfig3);
                BinaryPrimitives.WriteUInt16LittleEndian(assetContainerSpan.Slice(16, 2), (ushort)assetContainer.AssetType);
                BinaryPrimitives.WriteUInt16LittleEndian(assetContainerSpan.Slice(30, 2), (ushort)assetContainer.Assets.Count);

                if (!string.IsNullOrWhiteSpace(assetContainer.ContainerEmbPath))
                {
                    BinaryPrimitives.WriteInt32LittleEndian(assetContainerSpan.Slice(36, 4), stringBufferPosition - containerOffset);
                    stringBufferPosition += buffer.WriteStringASCII(stringBufferPosition, assetContainer.ContainerEmbPath);
                }

                if (!string.IsNullOrWhiteSpace(assetContainer.MaterialEmmPath))
                {
                    BinaryPrimitives.WriteInt32LittleEndian(assetContainerSpan.Slice(40, 4), stringBufferPosition - containerOffset);
                    stringBufferPosition += buffer.WriteStringASCII(stringBufferPosition, assetContainer.MaterialEmmPath);
                }

                if (!string.IsNullOrWhiteSpace(assetContainer.TextureEmbPath))
                {
                    BinaryPrimitives.WriteInt32LittleEndian(assetContainerSpan.Slice(44, 4), stringBufferPosition - containerOffset);
                    stringBufferPosition += buffer.WriteStringASCII(stringBufferPosition, assetContainer.TextureEmbPath);
                }


                //Write offset to assets
                //int assetSectionStartOffset = CalculateAssetSectionStart(assetContainerIdx, assetEntryStart);
                BinaryPrimitives.WriteInt32LittleEndian(assetContainerSpan.Slice(32, 4), assetEntryStart - containerOffset); //Offset is relative to asset container

                //Write assets
                byte assetType = (byte)assetContainer.AssetType;

                for (int assetIdx = 0; assetIdx < assetContainer.Assets.Count; assetIdx++)
                {
                    int assetOffset = assetEntryStart + (assetIdx * EEPK_ASSET_SIZE);
                    Span<byte> assetSpan = span.Slice(assetOffset, EEPK_ASSET_SIZE);
                    var asset = assetContainer.Assets[assetIdx];

                    BinaryPrimitives.WriteInt16LittleEndian(assetSpan, asset.I_00);
                    assetSpan[2] = assetType;
                    assetSpan[3] = (byte)asset.Files.Count;

                    //File type bytes
                    BinaryPrimitives.WriteInt32LittleEndian(assetSpan.Slice(4, 4), stringBufferPosition - assetOffset); //Offset to numbers

                    for (int i = 0; i < asset.Files.Count; i++)
                        buffer[stringBufferPosition++] = (byte)EepkAssetEntry.GetAssetFileType(asset.Files[i], true);

                    //Write paths
                    BinaryPrimitives.WriteInt32LittleEndian(assetSpan.Slice(8, 4), assetFileTableBufferPosition - assetOffset); //Offset to file path table

                    for (int i = 0; i < asset.Files.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(asset.Files[i]))
                        {
                            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(assetFileTableBufferPosition, 4), stringBufferPosition - assetOffset);
                            stringBufferPosition += buffer.WriteStringASCII(stringBufferPosition, asset.Files[i]);
                        }

                        assetFileTableBufferPosition += 4;
                    }
                }

                assetEntryStart += EEPK_ASSET_SIZE * assetContainer.Assets.Count;
            }
            
            //Effect; main structure and offset table
            for(int i = 0; i < Effects.Count; i++)
            {
                Effect effect = Effects[i];
                int effectOffset = effectStart + (EEPK_EFFECT_SIZE * i);
                ushort effectPartCount = effect.EffectParts != null ? (ushort)effect.EffectParts.Count : (ushort)0;

                //Write effect offset
                int tableOffset = effectTableStart + (effect.IndexNum * 4);
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(tableOffset, 4), effectOffset);

                //Write effect
                Span<byte> effectSpan = span.Slice(effectOffset, EEPK_EFFECT_SIZE);
                BinaryPrimitives.WriteUInt16LittleEndian(effectSpan, effect.IndexNum);
                BinaryPrimitives.WriteUInt16LittleEndian(effectSpan.Slice(2, 2), effect.I_02);
                BinaryPrimitives.WriteUInt16LittleEndian(effectSpan.Slice(10, 2), effectPartCount);

                //int effectPartOffset = CalculateEffectPartStart(i, effectPartStart);
                BinaryPrimitives.WriteInt32LittleEndian(effectSpan.Slice(12, 4), effectPartOffset - effectOffset);

                for(int effectPartIdx = 0; effectPartIdx < effectPartCount; effectPartIdx++)
                {
                    Span<byte> effectPartSpan = span.Slice(effectPartOffset, EEPK_EFFECT_PART_SIZE);
                    EffectPart effectPart = effect.EffectParts[effectPartIdx];

                    BinaryPrimitives.WriteUInt16LittleEndian(effectPartSpan, effectPart.AssetIndex);
                    effectPartSpan[2] = (byte)effectPart.AssetType;
                    effectPartSpan[3] = (byte)effectPart.AttachementType;
                    effectPartSpan[4] = (byte)effectPart.Orientation;
                    effectPartSpan[5] = (byte)effectPart.Deactivation;
                    effectPartSpan[6] = effectPart.I_06;
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(24, 4), effectPart.DepthBias);
                    BinaryPrimitives.WriteUInt16LittleEndian(effectPartSpan.Slice(28, 2), effectPart.StartTime);
                    BinaryPrimitives.WriteUInt16LittleEndian(effectPartSpan.Slice(30, 2), effectPart.EMA_AnimationIndex);
                    BinaryPrimitives.WriteUInt32LittleEndian(effectPartSpan.Slice(32, 4), (uint)effectPart.Flags1);
                    BinaryPrimitives.WriteUInt32LittleEndian(effectPartSpan.Slice(36, 4), (uint)effectPart.Flags2);
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(40, 4), effectPart.PositionX);
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(44, 4), effectPart.PositionY);
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(48, 4), effectPart.PositionZ);
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(52, 4), (float)MathHelpers.ConvertDegreesToRadians(effectPart.RotationX_Min));
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(56, 4), (float)MathHelpers.ConvertDegreesToRadians(effectPart.RotationX_Max));
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(60, 4), (float)MathHelpers.ConvertDegreesToRadians(effectPart.RotationY_Min));
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(64, 4), (float)MathHelpers.ConvertDegreesToRadians(effectPart.RotationY_Max));
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(68, 4), (float)MathHelpers.ConvertDegreesToRadians(effectPart.RotationZ_Min));
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(72, 4), (float)MathHelpers.ConvertDegreesToRadians(effectPart.RotationZ_Max));
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(76, 4), effectPart.ScaleMin);
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(80, 4), effectPart.ScaleMax);
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(84, 4), effectPart.NearFadeDistance);
                    BinaryPrimitivesHelper.WriteSingleLittleEndian(effectPartSpan.Slice(88, 4), effectPart.FarFadeDistance);
                    BinaryPrimitives.WriteUInt16LittleEndian(effectPartSpan.Slice(92, 2), effectPart.EMA_LoopStartFrame);
                    BinaryPrimitives.WriteUInt16LittleEndian(effectPartSpan.Slice(94, 2), effectPart.EMA_LoopEndFrame);

                    if (!string.IsNullOrWhiteSpace(effectPart.ESK))
                    {
                        BinaryPrimitives.WriteInt32LittleEndian(effectPartSpan.Slice(96, 4), stringBufferPosition - effectPartOffset);
                        stringBufferPosition += buffer.WriteStringASCII(stringBufferPosition, effectPart.ESK);
                    }

                    effectPartOffset += EEPK_EFFECT_PART_SIZE;
                }
            }

            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateFileSectionSizes(out int assetSection, out int effectSection, out int stringSection, out int assetFileTableSection)
        {
            assetSection = 0;
            effectSection = Effects.Count * EEPK_EFFECT_SIZE;
            stringSection = 0;
            assetFileTableSection = 0;

            foreach (var effect in Effects)
            {
                effectSection += effect.EffectParts.Count * EEPK_EFFECT_PART_SIZE;

                foreach (var effectPart in effect.EffectParts)
                {
                    if (!string.IsNullOrWhiteSpace(effectPart.ESK))
                        stringSection += effectPart.ESK.Length + 1;
                }
            }

            foreach (var assetContainer in AssetContainers)
            {
                if (!string.IsNullOrWhiteSpace(assetContainer.ContainerEmbPath))
                    stringSection += assetContainer.ContainerEmbPath.Length + 1;

                if (!string.IsNullOrWhiteSpace(assetContainer.MaterialEmmPath))
                    stringSection += assetContainer.MaterialEmmPath.Length + 1;

                if (!string.IsNullOrWhiteSpace(assetContainer.TextureEmbPath))
                    stringSection += assetContainer.TextureEmbPath.Length + 1;

                foreach (var asset in assetContainer.Assets)
                {
                    assetFileTableSection += 4 * asset.Files.Count;
                    stringSection += asset.Files.Count;

                    foreach (var file in asset.Files)
                    {
                        if (!string.IsNullOrWhiteSpace(file))
                            stringSection += file.Length + 1;
                    }
                }

                assetSection += assetContainer.Assets.Count * EEPK_ASSET_SIZE;
            }
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }

        //XML
        public static void CreateXml(string path)
        {
            EEPK_File file = Load(File.ReadAllBytes(path));

            YAXSerializer serializer = new YAXSerializer(typeof(EEPK_File));
            serializer.SerializeToFile(file, path + ".xml");
        }

        public static void SaveXml(string xmlPath)
        {
            string path = string.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(EEPK_File), YAXSerializationOptions.DontSerializeNullObjects);
            EEPK_File eepkFile = (EEPK_File)serializer.DeserializeFromFile(xmlPath);
            eepkFile.Save(path);
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SortEntries()
        {
            Effects = Sorting.SortEntries(Effects);
        }

        public void RenameContainersToSkillFolder(string newName)
        {
            foreach (EepkAssetContainer container in AssetContainers)
            {
                if (container.AssetType != AssetType.EMO)
                {
                    container.ContainerEmbPath = RenameContainerPathToSkillFolder(container.ContainerEmbPath, newName);
                    container.MaterialEmmPath = RenameContainerPathToSkillFolder(container.MaterialEmmPath, newName);
                    container.TextureEmbPath = RenameContainerPathToSkillFolder(container.TextureEmbPath, newName);
                }
            }

            static string RenameContainerPathToSkillFolder(string containerPath, string newName)
            {
                if (string.IsNullOrWhiteSpace(containerPath)) return containerPath;
                string name = Path.GetFileNameWithoutExtension(containerPath);
                string ext1 = Path.GetExtension(containerPath);
                string ext2 = Path.GetExtension(Path.GetFileNameWithoutExtension(containerPath));
                name = name.Replace(name, newName);

                string newFileName = string.Format("{0}{1}{2}", name, ext2, ext1);

                return newFileName;
            }
        }
    }

    [Serializable]
    [YAXSerializeAs("Container")]
    public class EepkAssetContainer
    {
        [CustomSerialize]
        public int PoolSize { get; set; } = 8; //Sets the initial asset pool size for the games internal effects system.
        [CustomSerialize]
        public int ParticleEngineConfig1 { get; set; } = -1; //These 3 values are used to configure the games particle engine. Always -1 for non-PBIND types.
        [CustomSerialize]
        public int ParticleEngineConfig2 { get; set; } = -1;
        [CustomSerialize]
        public int ParticleEngineConfig3 { get; set; } = -1;
        [YAXAttributeForClass]
        [YAXSerializeAs("AssetType")]
        public AssetType AssetType { get; set; }
        [CustomSerialize]
        public string ContainerEmbPath { get; set; }
        [CustomSerialize]
        public string MaterialEmmPath { get; set; }
        [CustomSerialize]
        public string TextureEmbPath { get; set; }

        [YAXSerializeAs("Assets")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Asset")]
        public List<EepkAssetEntry> Assets { get; set; } = new List<EepkAssetEntry>();

        public EepkAssetContainer() { }

        public EepkAssetContainer(AssetContainerTool assetContainer)
        {
            AssetType = assetContainer.ContainerAssetType;
            ContainerEmbPath = assetContainer.File1_Name;
            MaterialEmmPath = assetContainer.File2_Name;
            TextureEmbPath = assetContainer.File3_Name;

            if (assetContainer.AutoConfiguration)
            {
                AutoCalculateConfigurationValues();
            }
            else
            {
                PoolSize = assetContainer.PoolSize;
                ParticleEngineConfig1 = assetContainer.ParticleEngineConfig1;
                ParticleEngineConfig2 = assetContainer.ParticleEngineConfig2;
                ParticleEngineConfig3 = assetContainer.ParticleEngineConfig3;
            }

            if(assetContainer.Assets != null)
            {
                foreach(var asset in assetContainer.Assets)
                {
                    EepkAssetEntry newAsset = new EepkAssetEntry();
                    newAsset.I_00 = asset.I_00;

                    for (int i = 0; i < asset.Files.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(asset.Files[i].FullFileName))
                            newAsset.Files.Add(asset.Files[i].FullFileName);
                    }

                    Assets.Add(newAsset);
                }
            }
        }

        private void AutoCalculateConfigurationValues()
        {
            if (AssetType == AssetType.PBIND)
            {
                //PoolSize is 1/4 the value for PBIND compared to other asset types (game will multiply it by 4)
                PoolSize = (int)Math.Ceiling(Assets.Count / 4.0);

                ParticleEngineConfig1 = PoolSize * 8;
                ParticleEngineConfig2 = PoolSize * 16;
                ParticleEngineConfig3 = PoolSize * 16;
            }
            else
            {
                PoolSize = Assets.Count > PoolSize ? (int)(Math.Ceiling(Assets.Count / 4.0) * 4.0) : PoolSize;
                ParticleEngineConfig1 = -1;
                ParticleEngineConfig2 = -1;
                ParticleEngineConfig3 = -1;
            }
        }
    }

    [Serializable]
    [YAXSerializeAs("Asset")]
    public class EepkAssetEntry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public int XML_Index { get; set; }

        [CustomSerialize]
        public short I_00 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "File")]
        public List<string> Files { get; set; } = new List<string>();

        public static EepkAssetFileType GetAssetFileType(string assetName, bool allowNull = false)
        {
            if (allowNull && string.IsNullOrWhiteSpace(assetName)) return EepkAssetFileType.Null;

            string extension = System.IO.Path.GetExtension(assetName);

            switch (extension)
            {
                case ".emo":
                    return EepkAssetFileType.EMO;
                case ".emm":
                    return EepkAssetFileType.EMM;
                case ".emb":
                    return EepkAssetFileType.EMB;
                case ".ema":
                    return EepkAssetFileType.EMA;
                case ".emp":
                    return EepkAssetFileType.EMP;
                case ".etr":
                    return EepkAssetFileType.ETR;
                case ".ecf":
                    return EepkAssetFileType.ECF;
                default:
                    throw new ArgumentException($"EEPK_File.GetAssetFileType: Unknown asset file type ({assetName}).");
            }
        }

    }

    [Serializable]
    [YAXSerializeAs("Effect")]
    public class Effect : IUserDefinedName, IInstallable, INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region NonSerialized
        [YAXDontSerialize]
        public int SortID { get { return IndexNum; } set { IndexNum = (ushort)value; } }
        [YAXDontSerialize]
        public string Index { get { return IndexNum.ToString(); } set { IndexNum = ushort.Parse(value); } }
        private ushort _index = 0;
        #endregion

        #region Name
        private string _userDefinedName = null; //New UserDefineName system (used in XenoKit, but still pulls names from the old system as a fallback)
        [YAXDontSerialize]
        public string UserDefinedName
        {
            get
            {
                return _userDefinedName;
            }
            set
            {
                if (value != _userDefinedName)
                {
                    _userDefinedName = value;
                    NotifyPropertyChanged(nameof(UserDefinedName));
                }
            }
        }
        [YAXDontSerialize]
        public bool HasUserDefinedName => !string.IsNullOrWhiteSpace(_userDefinedName);

        #endregion

        [YAXSerializeAs("ID")]
        [YAXAttributeForClass]
        public ushort IndexNum
        {
            get => _index;
            set
            {
                if (value != _index)
                {
                    _index = value;
                    NotifyPropertyChanged(nameof(IndexNum));
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_02")]
        public ushort I_02 { get; set; }
        [YAXSerializeAs("EffectParts")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EffectPart")]
        public AsyncObservableCollection<EffectPart> EffectParts { get; set; } = new AsyncObservableCollection<EffectPart>();

        //Installer
        [YAXDontSerialize]
        public VfxPackageExtendedEffect ExtendedEffectData { get; set; }


        public Effect Clone()
        {
            Effect newEffect = new Effect();
            newEffect.EffectParts = new();
            newEffect.IndexNum = IndexNum;
            newEffect.I_02 = I_02;

            if (EffectParts != null)
            {
                foreach (var effectPart in EffectParts)
                {
                    newEffect.EffectParts.Add(effectPart.Clone());
                }
            }

            return newEffect;
        }

        public Effect ShallowClone(ushort newID)
        {
            Effect clone = ShallowClone();
            clone.IndexNum = newID;
            return clone;
        }

        public Effect ShallowClone()
        {
            return FastCloner.FastCloner.ShallowClone(this);
        }

        public void RemoveNulls()
        {
        start:
            for (int i = 0; i < EffectParts.Count; i++)
            {
                if (EffectParts[i].AssetRef == null)
                {
                    EffectParts.RemoveAt(i);
                    goto start;
                }
            }
        }

        public void AssetRefDetailsRefresh(Asset asset)
        {
            foreach (var part in EffectParts)
            {
                part.AssetRefDetailsRefreash(asset);
            }
        }
    }

    [Serializable]
    public class EffectPart : INotifyPropertyChanged
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("ContainerType")]
        public AssetType AssetType { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ContainerIndex")]
        public ushort AssetIndex { get; set; }

        [CustomSerialize("StartTime", "Frames")]
        public ushort StartTime { get; set; }
        [CustomSerialize]
        public Attachment AttachementType { get; set; }
        [CustomSerialize]
        public OrientationType Orientation { get; set; }
        [CustomSerialize]
        public DeactivationMode Deactivation { get; set; }
        [CustomSerialize]
        public byte I_06 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float DepthBias { get; set; }

        [CustomSerialize]
        public EepkEffectPartFlags1 Flags1 { get; set; }
        [CustomSerialize]
        public EepkEffectPartFlags2 Flags2 { get; set; }


        [CustomSerialize("Position", "X", isFloat: true)]
        public float PositionX { get; set; }
        [CustomSerialize("Position", "Y", isFloat: true)]
        public float PositionY { get; set; }
        [CustomSerialize("Position", "Z", isFloat: true)]
        public float PositionZ { get; set; }
        [CustomSerialize("Orientation_X", "Min", isFloat: true)]
        public float RotationX_Min { get; set; }
        [CustomSerialize("Orientation_X", "Max", isFloat: true)]
        public float RotationX_Max { get; set; }
        [CustomSerialize("Orientation_Y", "Min", isFloat: true)]
        public float RotationY_Min { get; set; }
        [CustomSerialize("Orientation_Y", "Max", isFloat: true)]
        public float RotationY_Max { get; set; }
        [CustomSerialize("Orientation_Z", "Min", isFloat: true)]
        public float RotationZ_Min { get; set; }
        [CustomSerialize("Orientation_Z", "Max", isFloat: true)]
        public float RotationZ_Max { get; set; }
        [CustomSerialize("Scale", "Min", isFloat: true)]
        public float ScaleMin { get; set; } = 1f;
        [CustomSerialize("Scale", "Max", isFloat: true)]
        public float ScaleMax { get; set; } = 1f;
        [CustomSerialize(isFloat: true)]
        public float NearFadeDistance { get; set; }
        [CustomSerialize(isFloat: true)]
        public float FarFadeDistance { get; set; }
        [CustomSerialize("EMA_Animation", "Index")]
        public ushort EMA_AnimationIndex { get; set; }
        [CustomSerialize("EMA_Animation", "LoopStartFrame")]
        public ushort EMA_LoopStartFrame { get; set; }
        [CustomSerialize("EMA_Animation", "LoopEndFrame")]
        public ushort EMA_LoopEndFrame { get; set; }
        [CustomSerialize("BoneToAttach", "Name")]
        public string ESK { get; set; }

        //Helper Properties
        [YAXDontSerialize]
        public bool PositionUpdate => HasFlag1(EepkEffectPartFlags1.PositionUpdate);
        [YAXDontSerialize]
        public bool RotateUpdate => HasFlag1(EepkEffectPartFlags1.RotateUpdate);
        [YAXDontSerialize]
        public bool InstantUpdate => HasFlag1(EepkEffectPartFlags1.InstantUpdate);
        [YAXDontSerialize]
        public bool EnableRotationValues => HasFlag1(EepkEffectPartFlags1.EnableRotationValues);
        [YAXDontSerialize]
        public bool UseBoneDirection => HasFlag1(EepkEffectPartFlags1.UseBoneDirection);
        [YAXDontSerialize]
        public bool UseTimeScale => HasFlag1(EepkEffectPartFlags1.UseTimeScale);
        [YAXDontSerialize]
        public bool EMA_Loop => HasFlag2(EepkEffectPartFlags2.EMA_Loop);
        [YAXDontSerialize]
        public bool NoGlare => HasFlag2(EepkEffectPartFlags2.IsDecal);
        [YAXDontSerialize]
        public bool NoDistanceCulling => HasFlag2(EepkEffectPartFlags2.DisableDistanceCulling);

        public EffectPart Clone()
        {
            return new EffectPart()
            {
                AssetRef = AssetRef,
                ScaleMax = ScaleMax,
                ScaleMin = ScaleMin,
                ESK = ESK,
                RotationZ_Min = RotationZ_Min,
                RotationZ_Max = RotationZ_Max,
                PositionX = PositionX,
                PositionY = PositionY,
                PositionZ = PositionZ,
                DepthBias = DepthBias,
                RotationX_Min = RotationX_Min,
                RotationX_Max = RotationX_Max,
                RotationY_Min = RotationY_Min,
                RotationY_Max = RotationY_Max,
                NearFadeDistance = NearFadeDistance,
                FarFadeDistance = FarFadeDistance,
                AssetIndex = AssetIndex,
                AssetType = AssetType,
                AttachementType = AttachementType,
                Orientation = Orientation,
                Deactivation = Deactivation,
                I_06 = I_06,
                StartTime = StartTime,
                EMA_AnimationIndex = EMA_AnimationIndex,
                EMA_LoopStartFrame = EMA_LoopStartFrame,
                EMA_LoopEndFrame = EMA_LoopEndFrame,
                Flags1 = Flags1,
                Flags2 = Flags2
            };
        }

        public void CopyValues(EffectPart effectPart, List<IUndoRedo> undos)
        {
            undos.Add(new UndoableProperty<EffectPart>(nameof(AssetRef), this, AssetRef, effectPart.AssetRef));
            AssetRef = effectPart.AssetRef;

            undos.AddRange(Utils.CopyValues(this, effectPart));

            ObjectExtensions.NotifyPropsChanged(this);
            undos.Add(new UndoActionPropNotify(this, true));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlag1(EepkEffectPartFlags1 flag)
        {
            return (Flags1 & flag) == flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlag2(EepkEffectPartFlags2 flag)
        {
            return (Flags2 & flag) == flag;
        }


        #region UserInterface
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        [YAXDontSerialize]
        public string AssetRefDetails
        {
            get
            {
                if (AssetRef == null) return "Unassigned";
                return string.Format("[{1}] {0}", AssetRef.FileNamesPreview, AssetType);
            }
        }
        [YAXDontSerialize]
        public string EffectPartDetails
        {
            get
            {
                if (AssetRef == null) return "Unassigned";
                return !string.IsNullOrWhiteSpace(ESK) ? string.Format("[{1}] {0}  |  {2}", AssetRef.FileNamesPreview, AssetType, ESK) : AssetRefDetails;
            }
        }
        private Asset _assetRef;
        [YAXDontSerialize]
        public Asset AssetRef
        {
            get => _assetRef;
            set
            {
                if (value != _assetRef)
                {
                    _assetRef = value;
                    AssetRefDetailsRefreash(value);
                }
            }
        }

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AssetRefDetailsRefreash(Asset asset)
        {
            if (AssetRef == asset)
            {
                RefreshDetails();
            }
        }

        public void RefreshDetails()
        {
            NotifyPropertyChanged(nameof(AssetRefDetails));
            NotifyPropertyChanged(nameof(AssetRef));
            NotifyPropertyChanged(nameof(EffectPartDetails));
        }

        #endregion

    }

    public enum AssetType : ushort
    {
        EMO = 0,
        PBIND = 1,
        TBIND = 2,
        LIGHT = 3,
        CBIND = 4
    }

    public enum EepkAssetFileType : byte
    {
        EMO = 0,
        EMM = 1,
        EMB = 2,
        EMA = 3,
        EMP = 4,
        ETR = 5,
        ECF = 7,
        Null = 255
    }

    [Flags]
    public enum EepkEffectPartFlags1 : uint
    {
        PositionUpdate = 0x01,
        RotateUpdate = 0x02,
        InstantUpdate = 0x04,
        OnGroundOnly = 0x08,
        UseTimeScale = 0x10,
        UseBoneDirection = 0x20,
        EnableRotationValues = 0x40,
        UseScreenCenterToBoneDirection = 0x80
    }

    [Flags]
    public enum EepkEffectPartFlags2 : uint
    {
        EMA_Loop = 0x01,
        I_36_1 = 0x02,
        I_36_2 = 0x04,
        I_36_3 = 0x08,
        LightApplyToCharacter = 0x10, //Note: These 3 light flags act as if they are all enabled if none of them are set.
        LightApplyToStage = 0x20,
        LightUnknown = 0x40,
        I_36_7 = 0x80,
        I_37_0 = 0x100,
        I_37_1 = 0x200,
        I_37_2 = 0x400,
        I_37_3 = 0x800,
        I_37_4 = 0x1000,
        I_37_5 = 0x2000,
        I_37_6 = 0x4000,
        I_37_7 = 0x8000,
        DisableDistanceCulling = 0x10000, //Fading based on near and far fade parameters
        DisableFrustumCulling = 0x20000, //Disables frustum culling for EMO
        IsGrassDrawPass = 0x40000,
        PriorityDrawPassB = 0x80000, //Draws EMO in an earlier pass (only if PriorityDrawPassA and IsGrassDrawPass are not set)
        InheritUsersScaleOnStart = 0x100000, //On start only (EMO / PBIND). Takes the largest scale axis
        InheritNodeScaleOnStart = 0x200000, //On start only (EMO / PBIND)
        I_38_6 = 0x400000, //Something to do with orientation type 1
        I_38_7 = 0x800000,
        IsDecal = 0x1000000, //A decal, such as stage battle damage. Draws in a separate pass. (EMO only)
        I_39_1 = 0x2000000,
        PriorityDrawPassA = 0x4000000, //Draws EMO in an earlier pass (before the PriorityDrawPassB pass)
        PositionScaleUnk = 0x8000000, //Requires UseBoneDirection... and applies a scale to the position. Needs more research.
        InheritUsersScaleOnUpdate = 0x10000000, //Every frame
        InheritNodeScaleOnUpdate = 0x20000000, //Every frame, requires InheritUsersScaleOnUpdate to also be set
        I_39_6 = 0x40000000,
        ObjectOrientation_To_XXXX = 0x80000000 //Does something when orientation type is 1
    }

    public enum DeactivationMode : byte
    {
        Never = 0,
        Immediate = 1,
        LoopCancel = 2
    }

    public enum Attachment : byte
    {
        External = 0,
        Unk1 = 1,
        Bone = 2,
        Camera = 3
    }

    public enum OrientationType : byte
    {
        None = 0,
        User = 1,
        AttachmentBone = 2,
        Camera = 3,
        RotateMovement = 4
    }

}