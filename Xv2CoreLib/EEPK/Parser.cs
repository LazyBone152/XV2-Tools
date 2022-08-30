using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;
using System.Collections;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.EEPK
{
    public class Parser
    {
        //Bad code. Should really rewrite it...

        private readonly byte[] rawBytes;
        public EEPK_File eepkFile { get; private set; } = new EEPK_File();

        //Offsets
        private int assetSectionLocation;
        private int pointerSectionLocation;

        //Counts
        private ushort totalEffects; //includes blank spaces
        private ushort totalAssetContainerEntries;

        public Parser(string fileLocation, bool writeXml)
        {
            rawBytes = File.ReadAllBytes(fileLocation);
            ParseHeader();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EEPK_File));
                serializer.SerializeToFile(eepkFile, fileLocation + ".xml");
            }
        }

        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            ParseHeader();
        }

        private void ParseHeader()
        {
            //Header
            eepkFile.Version = BitConverter.ToInt32(rawBytes, 8);
            totalAssetContainerEntries = BitConverter.ToUInt16(rawBytes, 12);
            totalEffects = BitConverter.ToUInt16(rawBytes, 14);
            assetSectionLocation = BitConverter.ToInt32(rawBytes, 16);
            pointerSectionLocation = BitConverter.ToInt32(rawBytes, 20);

            if (BitConverter.ToInt32(rawBytes, 0) != EEPK_File.EEPK_SIGNATURE)
                throw new InvalidDataException("#EPK signature not found.\nLoad failed.");

            //Assets
            if (totalAssetContainerEntries > 0)
            {
                ParseAssets();
            }

            //Effects
            if(totalEffects > 0) 
            { 
                ParseEffects();
            }
        }

        private void ParseEffects()
        {
            eepkFile.Effects = new List<Effect>();
            List<ushort> effectIds = new List<ushort>(); //all lists are in sync with each other
            List<int> effectOffsets = new List<int>(); 
            List<short> effectSize = new List<short>();
            List<int> effectInfoOffsets = new List<int>();

            for (int i = 0; i < totalEffects * 4; i += 4)
            {
                //Getting ID List data
                int current = BitConverter.ToInt32(rawBytes, i + pointerSectionLocation);
                if (current != 0)
                {
                    effectIds.Add((Convert.ToUInt16(i / 4)));
                    effectOffsets.Add(BitConverter.ToInt32(rawBytes, i + pointerSectionLocation));
                }
            }

            for (int i = 0; i < effectIds.Count(); i++)
            {
                //Getting EffectInfo data, and making the representation instances
                effectSize.Add(BitConverter.ToInt16(rawBytes, effectOffsets[i] + 10));
                effectInfoOffsets.Add(BitConverter.ToInt32(rawBytes, effectOffsets[i] + 12));

                eepkFile.Effects.Add(new Effect());
                eepkFile.Effects[i].EffectParts = AsyncObservableCollection<EffectPart>.Create();
                eepkFile.Effects[i].IndexNum = effectIds[i];
                eepkFile.Effects[i].I_02 = BitConverter.ToUInt16(rawBytes, effectOffsets[i] + 2);

                int addedOffset = 0;
                for (int a = 0; a < effectSize[i]; a++)
                {
                    //Fill in the Effect Info fields, adding a new entry per size
                    BitArray composite_I_32 = new BitArray(new byte[1] { rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 32] });
                    BitArray composite_I_39 = new BitArray(new byte[1] { rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 39] });
                    BitArray composite_I_36 = new BitArray(new byte[1] { rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 36] });
                    BitArray composite_I_37 = new BitArray(new byte[1] { rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 37] });

                    eepkFile.Effects[i].EffectParts.Add(new EffectPart());
                    eepkFile.Effects[i].EffectParts[a].I_00 = BitConverter.ToUInt16(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset);
                    eepkFile.Effects[i].EffectParts[a].I_02 = (AssetType)rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 2];
                    eepkFile.Effects[i].EffectParts[a].I_03 = (EffectPart.Attachment)rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 3];
                    eepkFile.Effects[i].EffectParts[a].I_04 = rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 4];
                    eepkFile.Effects[i].EffectParts[a].I_05 = (EffectPart.DeactivationMode)rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 5];
                    eepkFile.Effects[i].EffectParts[a].I_06 = rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 6];
                    eepkFile.Effects[i].EffectParts[a].I_07 = rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 7];
                    eepkFile.Effects[i].EffectParts[a].I_08 = BitConverter.ToInt32(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 8);
                    eepkFile.Effects[i].EffectParts[a].I_12 = BitConverter.ToInt32(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 12);
                    eepkFile.Effects[i].EffectParts[a].I_16 = BitConverter.ToInt32(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 16);
                    eepkFile.Effects[i].EffectParts[a].I_20 = BitConverter.ToInt32(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 20);
                    eepkFile.Effects[i].EffectParts[a].F_24 = BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 24);
                    eepkFile.Effects[i].EffectParts[a].I_28 = BitConverter.ToUInt16(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 28);
                    eepkFile.Effects[i].EffectParts[a].I_30 = BitConverter.ToUInt16(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 30);
                    eepkFile.Effects[i].EffectParts[a].I_32_0 = composite_I_32[0];
                    eepkFile.Effects[i].EffectParts[a].I_32_1 = composite_I_32[1];
                    eepkFile.Effects[i].EffectParts[a].I_32_2 = composite_I_32[2];
                    eepkFile.Effects[i].EffectParts[a].I_32_3 = composite_I_32[3];
                    eepkFile.Effects[i].EffectParts[a].I_32_4 = composite_I_32[4];
                    eepkFile.Effects[i].EffectParts[a].I_32_5 = composite_I_32[5];
                    eepkFile.Effects[i].EffectParts[a].I_32_6 = composite_I_32[6];
                    eepkFile.Effects[i].EffectParts[a].I_32_7 = composite_I_32[7];
                    eepkFile.Effects[i].EffectParts[a].I_34 = BitConverter.ToInt16(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 34);
                    eepkFile.Effects[i].EffectParts[a].I_36_0 = composite_I_36[0];
                    eepkFile.Effects[i].EffectParts[a].I_36_1 = composite_I_36[1];
                    eepkFile.Effects[i].EffectParts[a].I_36_2 = composite_I_36[2];
                    eepkFile.Effects[i].EffectParts[a].I_36_3 = composite_I_36[3];
                    eepkFile.Effects[i].EffectParts[a].I_36_4 = composite_I_36[4];
                    eepkFile.Effects[i].EffectParts[a].I_36_5 = composite_I_36[5];
                    eepkFile.Effects[i].EffectParts[a].I_36_6 = composite_I_36[6];
                    eepkFile.Effects[i].EffectParts[a].I_36_7 = composite_I_36[7];
                    eepkFile.Effects[i].EffectParts[a].I_37_0 = composite_I_37[0];
                    eepkFile.Effects[i].EffectParts[a].I_37_1 = composite_I_37[1];
                    eepkFile.Effects[i].EffectParts[a].I_37_2 = composite_I_37[2];
                    eepkFile.Effects[i].EffectParts[a].I_37_3 = composite_I_37[3];
                    eepkFile.Effects[i].EffectParts[a].I_37_4 = composite_I_37[4];
                    eepkFile.Effects[i].EffectParts[a].I_37_5 = composite_I_37[5];
                    eepkFile.Effects[i].EffectParts[a].I_37_6 = composite_I_37[6];
                    eepkFile.Effects[i].EffectParts[a].I_37_7 = composite_I_37[7];
                    eepkFile.Effects[i].EffectParts[a].I_38_a = HexConverter.GetHexString(Int4Converter.ToInt4(rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 38])[0]);
                    eepkFile.Effects[i].EffectParts[a].I_38_b = HexConverter.GetHexString(Int4Converter.ToInt4(rawBytes[effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 38])[1]);
                    eepkFile.Effects[i].EffectParts[a].I_39_0 = composite_I_39[0];
                    eepkFile.Effects[i].EffectParts[a].I_39_1 = composite_I_39[1];
                    eepkFile.Effects[i].EffectParts[a].I_39_2 = composite_I_39[2];
                    eepkFile.Effects[i].EffectParts[a].I_39_3 = composite_I_39[3];
                    eepkFile.Effects[i].EffectParts[a].I_39_4 = composite_I_39[4];
                    eepkFile.Effects[i].EffectParts[a].I_39_5 = composite_I_39[5];
                    eepkFile.Effects[i].EffectParts[a].I_39_6 = composite_I_39[6];
                    eepkFile.Effects[i].EffectParts[a].I_39_7 = composite_I_39[7];
                    eepkFile.Effects[i].EffectParts[a].POSITION_X = BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 40);
                    eepkFile.Effects[i].EffectParts[a].POSITION_Y = BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 44);
                    eepkFile.Effects[i].EffectParts[a].POSITION_Z = BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 48);

                    eepkFile.Effects[i].EffectParts[a].F_52 = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 52));
                    eepkFile.Effects[i].EffectParts[a].F_56 = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 56));
                    eepkFile.Effects[i].EffectParts[a].F_60 = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 60));
                    eepkFile.Effects[i].EffectParts[a].F_64 = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 64));
                    eepkFile.Effects[i].EffectParts[a].F_68 = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 68));
                    eepkFile.Effects[i].EffectParts[a].F_72 = (float)MathHelpers.ConvertRadiansToDegrees(BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 72));

                    eepkFile.Effects[i].EffectParts[a].SIZE_1 = BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 76);
                    eepkFile.Effects[i].EffectParts[a].SIZE_2 = BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 80);
                    eepkFile.Effects[i].EffectParts[a].F_84 = BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 84);
                    eepkFile.Effects[i].EffectParts[a].F_88 = BitConverter.ToSingle(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 88);
                    eepkFile.Effects[i].EffectParts[a].I_92 = BitConverter.ToUInt16(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 92);
                    eepkFile.Effects[i].EffectParts[a].I_94 = BitConverter.ToUInt16(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 94);
                    int eskOffset = BitConverter.ToInt32(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + 96);
                    if (eskOffset != 0)
                    {
                        try
                        {
                            eepkFile.Effects[i].EffectParts[a].ESK = StringEx.GetString(rawBytes, effectOffsets[i] + effectInfoOffsets[i] + addedOffset + eskOffset);
                        }
                        catch
                        {
                            throw new ArgumentOutOfRangeException("Unable to get string!");
                        }
                        
                    }
                    else
                    {
                        eepkFile.Effects[i].EffectParts[a].ESK = string.Empty;
                    }
                    addedOffset += 100;
                }
                
            }
        }

        private void ParseAssets()
        {
            eepkFile.Assets = new List<AssetContainer>();
            int addedOffset = 0;
            List<short> assetEntrySize = new List<short>();
            List<int> assetEntryStartOffset = new List<int>();
            
            for (int i = 0; i < totalAssetContainerEntries; i++)
            {
                eepkFile.Assets.Add(new AssetContainer());
                eepkFile.Assets[i].I_00 = BitConverter.ToInt32(rawBytes, assetSectionLocation + addedOffset);
                eepkFile.Assets[i].I_04 = rawBytes[assetSectionLocation + addedOffset + 4];
                eepkFile.Assets[i].I_05 = rawBytes[assetSectionLocation + addedOffset + 5];
                eepkFile.Assets[i].I_06 = rawBytes[assetSectionLocation + addedOffset + 6];
                eepkFile.Assets[i].I_07 = rawBytes[assetSectionLocation + addedOffset + 7];
                eepkFile.Assets[i].AssetLimit = BitConverter.ToInt32(rawBytes, assetSectionLocation + addedOffset + 8);
                eepkFile.Assets[i].I_12 = BitConverter.ToInt32(rawBytes, assetSectionLocation + addedOffset + 12);
                eepkFile.Assets[i].I_16 = (AssetType)BitConverter.ToUInt16(rawBytes, assetSectionLocation + addedOffset + 16);
                eepkFile.Assets[i].FILES = new string[3];
                int count = 0;
                for (int e = 0; e < 3 * 4; e += 4)
                {
                    //Getting the file string if it exists, and assigning it to the correct field
                    int fileOffset = BitConverter.ToInt32(rawBytes, assetSectionLocation + addedOffset + 36 + e);
                    
                    if (fileOffset != 0)
                    {
                        eepkFile.Assets[i].FILES[count] = StringEx.GetString(rawBytes, fileOffset + assetSectionLocation + addedOffset);
                    }
                    else if (fileOffset == 0)
                    {
                        //If no string, then set it to null
                        eepkFile.Assets[i].FILES[count] = "NULL";
                    }

                    count++;
                }
                assetEntrySize.Add(BitConverter.ToInt16(rawBytes, assetSectionLocation + addedOffset + 30));
                assetEntryStartOffset.Add(BitConverter.ToInt32(rawBytes, assetSectionLocation + addedOffset + 32) + assetSectionLocation + addedOffset);
                addedOffset += 48;
            }

            for (int i = 0; i < assetEntrySize.Count; i++)
            {
                //iterate over the Asset Containers
                addedOffset = 0;
                int index = 0;
                eepkFile.Assets[i].AssetEntries = new List<Asset_Entry>();
                for (int f = 0; f < assetEntrySize[i]; f++)
                {
                    //iterate over Asset Entries of the current Asset Container 
                    int totalStrings = rawBytes[assetEntryStartOffset[i] + addedOffset + 3];
                    eepkFile.Assets[i].AssetEntries.Add(new Asset_Entry());
                    eepkFile.Assets[i].AssetEntries[f].ReadOnly_Index = index;
                    index++;
                    eepkFile.Assets[i].AssetEntries[f].I_00 = BitConverter.ToInt16(rawBytes, assetEntryStartOffset[i] + addedOffset);

                    int offsetForStringInital = BitConverter.ToInt32(rawBytes, assetEntryStartOffset[i] + addedOffset + 8) + assetEntryStartOffset[i] + addedOffset;
                    eepkFile.Assets[i].AssetEntries[f].FILES = new Asset_File[totalStrings].ToList();
                    eepkFile.Assets[i].AssetEntries[f].UNK_NUMBERS = new String[5] { "NULL", "NULL", "NULL", "NULL", "NULL" };

                    if (BitConverter.ToInt32(rawBytes, assetEntryStartOffset[i] + addedOffset + 8) != 0)//Safety check
                    {
                        List<int> offsetsForFileString = new List<int>();
                        for (int z = 0; z < totalStrings * 4; z += 4)
                        {
                            //offsetsForFileString.Add(BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, assetEntryStartOffset[i] + addedOffset + 8) + assetEntryStartOffset[i] + addedOffset) + assetEntryStartOffset[i] + addedOffset + z);
                            if (BitConverter.ToInt32(rawBytes, offsetForStringInital + z) != 0)
                            {
                                offsetsForFileString.Add(BitConverter.ToInt32(rawBytes, offsetForStringInital + z) + +assetEntryStartOffset[i] + addedOffset);
                            }
                            else
                            {
                                //No file string in this space
                                offsetsForFileString.Add(0);
                            }
                            
                            if (offsetsForFileString[z / 4] != 0)
                            {
                                eepkFile.Assets[i].AssetEntries[f].FILES[z / 4] = new Asset_File() { Path = StringEx.GetString(rawBytes, offsetsForFileString[z / 4]) };
                            }
                            else
                            {
                                eepkFile.Assets[i].AssetEntries[f].FILES[z / 4] = new Asset_File() { Path = "NULL" };
                            }
                        }
                    }
                    addedOffset += 12;
                    
                }
            }
        }

        public EEPK_File GetEepkFile()
        {
            return eepkFile;
        }

    }
}
