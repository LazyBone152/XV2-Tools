using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.EEPK
{
    public class Deserializer 
    {
        private string saveLocation { get; set; }
        private EEPK_File eepk_File { get; set; }
        public List<byte> bytes = new List<byte>() { 35, 69, 80, 75, 254, 255, 24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            ReadXmlFile(location);
            ValidateFile();
            WriteBinaryEEPK();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EEPK_File _eepkFile, string location)
        {
            saveLocation = location;
            eepk_File = _eepkFile;
            ValidateFile();
            WriteBinaryEEPK();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EEPK_File _eepkFile)
        {
            eepk_File = _eepkFile;
            ValidateFile();
            WriteBinaryEEPK();
        }

        void ReadXmlFile(string path)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(EEPK_File), YAXSerializationOptions.DontSerializeNullObjects);
            eepk_File = (EEPK_File)serializer.DeserializeFromFile(path);
        }

        void ValidateFile()
        {
            eepk_File.SortEntries();

            if(eepk_File.Assets != null)
            {
                foreach (var container in eepk_File.Assets)
                {
                    if (container.AssetEntries == null) container.AssetEntries = new List<Asset_Entry>();
                }
            }
            if(eepk_File.Effects != null)
            {
                foreach(var effect in eepk_File.Effects)
                {
                    if (effect.EffectParts == null) effect.EffectParts = new AsyncObservableCollection<EffectPart>();
                }
            }
        }

        void WriteBinaryEEPK()
        {
            int totalEffects = 0;
            ushort totalAssetContainers = 0;
            if (eepk_File.Effects != null)
            {
                if(eepk_File.Effects.Count > 0)
                {
                    totalEffects = eepk_File.Effects[eepk_File.Effects.Count() - 1].IndexNum;
                    totalEffects++;
                }
            }
            if (eepk_File.Assets != null)
            {
                totalAssetContainers = (ushort)eepk_File.Assets.Count;
            }

            //Effect ID related Pointers
            List<int> effectIdPointers = new List<int>();//Pointer Section entries - All of these lists will be synced up in entries
            List<int> effectIdActualPosition = new List<int>();//Offsets to the Effect ID entry (the "ID List")
            List<string> eskStringsToWrite = new List<string>();
            List<int> eskStringPointers = new List<int>();

            //Asset Section lists and values
            List<int> containerOneOffset = new List<int>();//Asset container for asset header block data
            List<int> containerTwoOffset = new List<int>();
            List<int> containerThreeOffset = new List<int>();
            List<int> assetDataBlockOffset = new List<int>();
            List<int> pointerToStringPointerList = new List<int>(); //the long list of pointers which point to the strings at end of file
            List<int> unkNumberPointer = new List<int>(); //pointer just before pointerToStringPointerList
            List<int> stringPointerListOffsets = new List<int>(); //the actual list of pointers to the strings (pointerToStringPointerList points to this)
            List<string> fileStringsToWrite = new List<string>();//All asset entry strings to write at end of file

            //Initial header data
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(totalAssetContainers), 12);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(totalEffects), 14);

            //Effect ID Pointer Section (creates a pointer list and fills each entry with 4 empty bytes for now. The relevant entries will be filled with pointers when that data is written.
            int actualIds = 0;
            if (totalEffects > 0)
            {
                for (int i = 0; i < totalEffects; i++)
                {
                    bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                    if (i == eepk_File.Effects[actualIds].IndexNum)
                    {
                        effectIdPointers.Add(bytes.Count() - 4);
                        actualIds++;
                    }
                }
            }

            if (totalAssetContainers > 0)
            {
                for (int i = 0; i < totalAssetContainers; i++)
                {
                    ushort assetCount = (ushort)((eepk_File.Assets[i].AssetEntries != null) ? eepk_File.Assets[i].AssetEntries.Count() : 0);

                    bytes.AddRange(BitConverter.GetBytes(eepk_File.Assets[i].AssetSpawnLimit));
                    bytes.AddRange(new byte[4] { eepk_File.Assets[i].I_04, eepk_File.Assets[i].I_05,eepk_File.Assets[i].I_06, eepk_File.Assets[i].I_07 });
                    bytes.AddRange(BitConverter.GetBytes(eepk_File.Assets[i].AssetListLimit));
                    bytes.AddRange(BitConverter.GetBytes(eepk_File.Assets[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes((ushort)eepk_File.Assets[i].I_16));
                    bytes.AddRange(new byte[12]);
                    bytes.AddRange(BitConverter.GetBytes(assetCount));
                    assetDataBlockOffset.Add(bytes.Count());
                    bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                    containerOneOffset.Add(bytes.Count());
                    bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                    containerTwoOffset.Add(bytes.Count());
                    bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                    containerThreeOffset.Add(bytes.Count());
                    bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                }



                for (int i = 0; i < totalAssetContainers; i++)
                {
                    //Asset Entry data

                    if(eepk_File.Assets[i].AssetEntries != null)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() + 32 - assetDataBlockOffset[i]), assetDataBlockOffset[i]);

                        for (int a = 0; a < eepk_File.Assets[i].AssetEntries.Count(); a++)
                        {
                            //Adds the container entries, and placeholder data for the eventual pointers to the string section
                            bytes.AddRange(BitConverter.GetBytes(eepk_File.Assets[i].AssetEntries[a].I_00));
                            bytes.Add((byte)eepk_File.Assets[i].I_16);
                            int actualCountOfFiles = eepk_File.Assets[i].AssetEntries[a].FILES.Where(p => p.Path != "NULL").Count();
                            bytes.Add((byte)actualCountOfFiles);
                            unkNumberPointer.Add(bytes.Count());
                            bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                            pointerToStringPointerList.Add(bytes.Count());
                            bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                        }
                    }
                }

                int assetContainerIteration = 0;
                for (int i = 0; i < totalAssetContainers; i++)
                {
                    //String pointer section (between Asset Container Entries and ID List) 

                    if(eepk_File.Assets[i].AssetEntries != null)
                    {
                        for (int a = 0; a < eepk_File.Assets[i].AssetEntries.Count(); a++)
                        {
                            //Iterating over the Asset Entries of a Main Container
                            int actualCountOfFiles = eepk_File.Assets[i].AssetEntries[a].FILES.Where(p => p.Path != "NULL").Count();

                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() + 8 - pointerToStringPointerList[assetContainerIteration]), pointerToStringPointerList[assetContainerIteration]);
                            assetContainerIteration++;
                            for (int e = 0; e < actualCountOfFiles; e++)
                            {
                                //Looping for each number in I_03
                                //eepk_File.Assets[i].AssetEntries[a].FILES[e] = eepk_File.Assets[i].AssetEntries[a].FILES[e].Replace("*", " ");//bug fix for white space in file names, here the white space is restored
                                fileStringsToWrite.Add(eepk_File.Assets[i].AssetEntries[a].FILES[e].Path);
                                stringPointerListOffsets.Add(bytes.Count());
                                bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                            }
                        }
                    }
                }
            }

            if (totalEffects > 0)
            {
                for (int i = 0; i < actualIds; i++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), effectIdPointers[i]); //Setting the offsets in the initial Effect ID pointer section
                    effectIdActualPosition.Add(bytes.Count());
                    bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].IndexNum));
                    bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].I_02));
                    bytes.AddRange(new List<byte> { 0, 0, 0, 0, 0, 0 });
                    bytes.AddRange(BitConverter.GetBytes((short)eepk_File.Effects[i].EffectParts.Count()));
                    bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                }

                for (int i = 0; i < actualIds; i++)
                {
                    //Each effect
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - effectIdActualPosition[i]), effectIdActualPosition[i] + 12);
                    
                    //Above line: setting offset to Effect Entry Start in the Effect ID Entry
                    for (int a = 0; a < eepk_File.Effects[i].EffectParts.Count; a++)
                    {
                        //Each entry/size
                        BitArray compositeBits_I_32 = new BitArray(new bool[8] { eepk_File.Effects[i].EffectParts[a].PositionUpdate, eepk_File.Effects[i].EffectParts[a].RotateUpdate, eepk_File.Effects[i].EffectParts[a].InstantUpdate, eepk_File.Effects[i].EffectParts[a].OnGroundOnly, eepk_File.Effects[i].EffectParts[a].UseTimeScale, eepk_File.Effects[i].EffectParts[a].UseBoneDirection, eepk_File.Effects[i].EffectParts[a].UseBoneToCameraDirection, eepk_File.Effects[i].EffectParts[a].UseScreenCenterToBoneDirection });
                        BitArray compositeBits_I_39 = new BitArray(new bool[8] { eepk_File.Effects[i].EffectParts[a].NoGlare, eepk_File.Effects[i].EffectParts[a].I_39_1, eepk_File.Effects[i].EffectParts[a].InverseTransparentDrawOrder, eepk_File.Effects[i].EffectParts[a].RelativePositionZ_To_AbsolutePositionZ, eepk_File.Effects[i].EffectParts[a].ScaleZ_To_BonePositionZ, eepk_File.Effects[i].EffectParts[a].I_39_5, eepk_File.Effects[i].EffectParts[a].I_39_6, eepk_File.Effects[i].EffectParts[a].ObjectOrientation_To_XXXX });
                        BitArray compositeBits_I_36 = new BitArray(new bool[8] { eepk_File.Effects[i].EffectParts[a].EMA_Loop, eepk_File.Effects[i].EffectParts[a].I_36_1, eepk_File.Effects[i].EffectParts[a].I_36_2, eepk_File.Effects[i].EffectParts[a].I_36_3, eepk_File.Effects[i].EffectParts[a].I_36_4, eepk_File.Effects[i].EffectParts[a].I_36_5, eepk_File.Effects[i].EffectParts[a].I_36_6, eepk_File.Effects[i].EffectParts[a].I_36_6 });
                        BitArray compositeBits_I_37 = new BitArray(new bool[8] { eepk_File.Effects[i].EffectParts[a].I_37_0, eepk_File.Effects[i].EffectParts[a].I_37_1, eepk_File.Effects[i].EffectParts[a].I_37_2, eepk_File.Effects[i].EffectParts[a].I_37_3, eepk_File.Effects[i].EffectParts[a].I_37_4, eepk_File.Effects[i].EffectParts[a].I_37_5, eepk_File.Effects[i].EffectParts[a].I_37_6, eepk_File.Effects[i].EffectParts[a].I_37_6 });


                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].AssetIndex));
                        bytes.Add((byte)eepk_File.Effects[i].EffectParts[a].AssetType);
                        bytes.Add((byte)eepk_File.Effects[i].EffectParts[a].AttachementType);
                        bytes.Add((byte)eepk_File.Effects[i].EffectParts[a].Orientation);
                        bytes.Add((byte)eepk_File.Effects[i].EffectParts[a].Deactivation);
                        bytes.Add(eepk_File.Effects[i].EffectParts[a].I_06);
                        bytes.Add(eepk_File.Effects[i].EffectParts[a].I_07);
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].I_08));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].I_12));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].I_16));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].I_20));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].AvoidSphere));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].StartTime));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].EMA_AnimationIndex));
                        bytes.Add(Utils.ConvertToByte(compositeBits_I_32));
                        bytes.Add(0);
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].I_34));
                        bytes.Add(Utils.ConvertToByte(compositeBits_I_36));
                        bytes.Add(Utils.ConvertToByte(compositeBits_I_37));
                        bytes.Add(Int4Converter.GetByte(eepk_File.Effects[i].EffectParts[a].I_38_a, eepk_File.Effects[i].EffectParts[a].I_38_b, "Flag_38 a", "Flag_38 b"));
                        bytes.Add(Utils.ConvertToByte(compositeBits_I_39));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].PositionX));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].PositionY));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].PositionZ));

                        bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(eepk_File.Effects[i].EffectParts[a].RotationX_Min)));
                        bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(eepk_File.Effects[i].EffectParts[a].RotationX_Max)));
                        bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(eepk_File.Effects[i].EffectParts[a].RotationY_Min)));
                        bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(eepk_File.Effects[i].EffectParts[a].RotationY_Max)));
                        bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(eepk_File.Effects[i].EffectParts[a].RotationZ_Min)));
                        bytes.AddRange(BitConverter.GetBytes((float)MathHelpers.ConvertDegreesToRadians(eepk_File.Effects[i].EffectParts[a].RotationZ_Max)));

                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].ScaleMin));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].ScaleMax));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].NearFadeDistance));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].FarFadeDistance));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].EMA_LoopStartFrame));
                        bytes.AddRange(BitConverter.GetBytes(eepk_File.Effects[i].EffectParts[a].EMA_LoopEndFrame));
                        eskStringPointers.Add(bytes.Count());
                        eskStringsToWrite.Add(eepk_File.Effects[i].EffectParts[a].ESK);
                        bytes.AddRange(new List<byte> { 0, 0, 0, 0 });
                    }
                }
            }

            if(totalAssetContainers > 0) 
            { 

                int strIteration2 = 0;
                int unkNumberIteration = 0;
                int strIteration = 0;

                for (int i = 0; i < totalAssetContainers; i++)
                {
                    //Writing string section
                    if (eepk_File.Assets[i].FILES[0] != "NULL" && !string.IsNullOrWhiteSpace(eepk_File.Assets[i].FILES[0]))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() + 36 - containerOneOffset[i]), containerOneOffset[i]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(eepk_File.Assets[i].FILES[0])); bytes.Add(0);
                    }
                    if (eepk_File.Assets[i].FILES[1] != "NULL" && !string.IsNullOrWhiteSpace(eepk_File.Assets[i].FILES[1]))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() + 40 - containerTwoOffset[i]), containerTwoOffset[i]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(eepk_File.Assets[i].FILES[1])); bytes.Add(0);
                    }
                    if (eepk_File.Assets[i].FILES[2] != "NULL" && !string.IsNullOrWhiteSpace(eepk_File.Assets[i].FILES[2]))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() + 44 - containerThreeOffset[i]), containerThreeOffset[i]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(eepk_File.Assets[i].FILES[2])); bytes.Add(0);
                    }

                    if(eepk_File.Assets[i].AssetEntries != null)
                    {
                        for (int a = 0; a < eepk_File.Assets[i].AssetEntries.Count; a++)
                        {
                            int actualCountFile = eepk_File.Assets[i].AssetEntries[a].FILES.Where(p => p.Path != "NULL").Count();

                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() + 4 - unkNumberPointer[unkNumberIteration]), unkNumberPointer[unkNumberIteration]);
                            unkNumberIteration++;
                            for (int s = 0; s < actualCountFile; s++)
                            {
                                //Set number offset here
                                if (eepk_File.Assets[i].AssetEntries[a].FILES[s].Path == "NULL" && !string.IsNullOrWhiteSpace(eepk_File.Assets[i].AssetEntries[a].FILES[s].Path))
                                {
                                    bytes.Add(255);
                                }
                                else
                                {
                                    bytes.Add(Convert.ToByte(Utils.GetEepkFileTypeNumber(eepk_File.Assets[i].AssetEntries[a].FILES[s].Path)));
                                }
                            }

                            for (int s = 0; s < actualCountFile; s++)
                            {
                                //Set string offset here
                                if (eepk_File.Assets[i].AssetEntries[a].FILES[s].Path != "NULL" && !string.IsNullOrWhiteSpace(eepk_File.Assets[i].AssetEntries[a].FILES[s].Path))
                                {
                                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() + 8 - pointerToStringPointerList[strIteration2]), stringPointerListOffsets[strIteration]);
                                    bytes.AddRange(Encoding.ASCII.GetBytes(eepk_File.Assets[i].AssetEntries[a].FILES[s].Path)); bytes.Add(0);
                                }
                                strIteration++;
                            }
                            strIteration2++;

                        }

                    }

                }


                
            }

            if (totalEffects > 0)
            {
                for (int i = 0; i < eskStringsToWrite.Count(); i++)
                {

                    if (eskStringsToWrite[i] != "NULL" && !String.IsNullOrWhiteSpace(eskStringsToWrite[i]))
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() + 96 - eskStringPointers[i]), eskStringPointers[i]);
                        bytes.AddRange(Encoding.ASCII.GetBytes(eskStringsToWrite[i])); bytes.Add(0);
                    }

                }
            }

            if (totalAssetContainers > 0)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(assetDataBlockOffset[0] - 32), 16); //Pointer to Asset Section
            }
            if (totalEffects > 0) 
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(24),20); //Pointer to Effect Pointer List
            }

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(eepk_File.Version), 8); //Unknown data in header
            
        }
    }
}
