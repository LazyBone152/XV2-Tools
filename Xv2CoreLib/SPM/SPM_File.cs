using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.SPM
{
    [YAXSerializeAs("SPM")]
    public class SPM_File
    {
        private const string SPM_SIG = "#SPM";
        private const string SPE_SIG = "#SPE";
        private const string SPX_SIG = "#SPX";
        private const int SPE_ENTRY_SIZE = 1120;

        [YAXAttributeForClass]
        public string Signature { get; set; }
        [YAXAttributeForClass]
        public int Version { get; set; }


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SPMEntry")]
        public List<SPM_Entry> Entries { get; set; } = new List<SPM_Entry>();

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BlendBlock")]
        public List<DSC_BlendBlock> BlendBlocks { get; set; } = new List<DSC_BlendBlock>();

        #region LoadSave
        public static void SerializeToXml(string path)
        {
            SPM_File spmFile = Load(File.ReadAllBytes(path));

            YAXSerializer serializer = new YAXSerializer(typeof(SPM_File));
            serializer.SerializeToFile(spmFile, path + ".xml");
        }

        public static void DeserializeFromXml(string path)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            YAXSerializer serializer = new YAXSerializer(typeof(SPM_File), YAXSerializationOptions.DontSerializeNullObjects);
            SPM_File spmFile = (SPM_File)serializer.DeserializeFromFile(path);

            File.WriteAllBytes(saveLocation, spmFile.SaveToBytes());
        }

        public static SPM_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        [FileLoad]
        public static SPM_File Load(byte[] bytes)
        {
            SPM_File spm = new SPM_File();
            spm.Signature = StringEx.GetString(bytes, 0, useNullText: false, useNullTerminator: false, maxSize: 4, allowZeroIndex: true);
            spm.Version = BitConverter.ToInt32(bytes, 4);

            int entryCount = 0;
            int entrySize = 784;
            int entryOffset = 0;
            int dscCount = 0;
            int dscOffset = 0;
            bool expandedEntry = false;

            if (spm.Signature == SPM_SIG)
            {
                entryCount = BitConverter.ToInt32(bytes, 8);
                entryOffset = BitConverter.ToInt32(bytes, 12);
            }
            else if (spm.Signature == SPE_SIG || spm.Signature == SPX_SIG)
            {
                expandedEntry = true;
                entryCount = BitConverter.ToInt32(bytes, 8);
                entrySize = BitConverter.ToInt32(bytes, 12);
                entryOffset = BitConverter.ToInt32(bytes, 16);

                if(spm.Signature == SPX_SIG)
                {
                    dscCount = BitConverter.ToInt32(bytes, 20);
                    dscOffset = BitConverter.ToInt32(bytes, 24);
                }
            }

            for(int i = 0; i < entryCount; i++)
            {
                spm.Entries.Add(SPM_Entry.Read(bytes, entryOffset + (i * entrySize), expandedEntry));
            }

            for(int i = 0; i < dscCount; i++)
            {
                int dscSectionOffset = BitConverter.ToInt32(bytes, dscOffset + (8  * i));

                if(dscSectionOffset > 0)
                    spm.BlendBlocks.Add(DSC_BlendBlock.Read(bytes, dscSectionOffset + dscOffset));
            }

            return spm;
        }

        [FileSave]
        public byte[] SaveToBytes()
        {
            List<byte> bytes = new List<byte>();
            bool useDsc = false, useExtendedEntry = false;
            int entryCount = Entries?.Count ?? 0;
            int dscSections = BlendBlocks?.Count ?? 0;

            if(Signature == SPM_SIG)
            {
                bytes.AddRange(StringEx.WriteFixedSizeString(Signature, 4));
                bytes.AddRange(BitConverter.GetBytes(Version));
                bytes.AddRange(BitConverter.GetBytes(entryCount));
                bytes.AddRange(BitConverter.GetBytes(16));
            }
            else if (Signature == SPE_SIG)
            {
                bytes.AddRange(StringEx.WriteFixedSizeString(Signature, 4));
                bytes.AddRange(BitConverter.GetBytes(Version));
                bytes.AddRange(BitConverter.GetBytes(entryCount));
                bytes.AddRange(BitConverter.GetBytes(SPE_ENTRY_SIZE));
                bytes.AddRange(BitConverter.GetBytes(32));
                bytes.AddRange(new byte[12]); //Padding

                useExtendedEntry = true;
            }
            else if (Signature == SPX_SIG)
            {
                bytes.AddRange(StringEx.WriteFixedSizeString(Signature, 4));
                bytes.AddRange(BitConverter.GetBytes(Version));
                bytes.AddRange(BitConverter.GetBytes(entryCount));
                bytes.AddRange(BitConverter.GetBytes(SPE_ENTRY_SIZE));
                bytes.AddRange(BitConverter.GetBytes(32));
                bytes.AddRange(BitConverter.GetBytes(dscSections));
                bytes.AddRange(new byte[8]);

                useExtendedEntry = true;
                useDsc = true;
            }

            if(!useDsc && dscSections > 0)
            {
                throw new Exception("SPM_File.Write: This SPM has DSCSections defined in it, but the version does not support it. Change Signature to #SPX to use DSCSections.");
            }

            for(int i = 0; i < entryCount; i++)
            {
                bytes.AddRange(Entries[i].Write(useExtendedEntry));
            }

            //Write DSC Sections
            if(useDsc && dscSections > 0)
            {
                int dscStart = bytes.Count;
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 24);

                //Create offset list
                bytes.AddRange(new byte[8 * dscSections]);
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 8)]); //Pad to 64 bit alignment

                for(int i = 0; i < dscSections; i++)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - dscStart), dscStart + (8 * i));
                    bytes.AddRange(BlendBlocks[i].Write());
                }

            }

            return bytes.ToArray();
        }

        #endregion

    }

    [YAXSerializeAs("SPMEntry")]
    public class SPM_Entry
    {
        private const int SPM_BASE_ENTRY_SIZE = 784;

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public short ID { get; set; }
        [CustomSerialize]
        public ushort LensFlareEnabled { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_18 { get; set; }

        [CustomSerialize]
        public ushort GodRayAndSunHalo { get; set; }

        [CustomSerialize]
        public ushort I_30 { get; set; }

        [CustomSerialize("BgFillColor", "R")]
        public float BgFillColorR { get; set; }
        [CustomSerialize("BgFillColor", "G")]
        public float BgFillColorG { get; set; }
        [CustomSerialize("BgFillColor", "B")]
        public float BgFillColorB { get; set; }
        [CustomSerialize("BgFillColor", "A")]
        public float BgFillColorA { get; set; }

        //The game only uses light 0
        [CustomSerialize("Light0Dir", "X")]
        public float Light0DirX { get; set; }
        [CustomSerialize("Light0Dir", "Y")]
        public float Light0DirY { get; set; }
        [CustomSerialize("Light0Dir", "Z")]
        public float Light0DirZ { get; set; }
        [CustomSerialize("Light0Dir", "W")]
        public float Light0DirW { get; set; }
        [CustomSerialize("Light0DiffuseColor", "R")]
        public float Light0DiffuseColorR { get; set; }
        [CustomSerialize("Light0DiffuseColor", "G")]
        public float Light0DiffuseColorG { get; set; }
        [CustomSerialize("Light0DiffuseColor", "B")]
        public float Light0DiffuseColorB { get; set; }
        [CustomSerialize("Light0DiffuseColor", "A")]
        public float Light0DiffuseColorA { get; set; }

        [CustomSerialize("Light0SpecularColor", "R")]
        public float Light0SpecularColorR { get; set; }
        [CustomSerialize("Light0SpecularColor", "G")]
        public float Light0SpecularColorG { get; set; }
        [CustomSerialize("Light0SpecularColor", "B")]
        public float Light0SpecularColorB { get; set; }
        [CustomSerialize("Light0SpecularColor", "A")]
        public float Light0SpecularColorA { get; set; }

        [CustomSerialize("Light1Dir", "X")]
        public float Light1DirX { get; set; }
        [CustomSerialize("Light1Dir", "Y")]
        public float Light1DirY { get; set; }
        [CustomSerialize("Light1Dir", "Z")]
        public float Light1DirZ { get; set; }
        [CustomSerialize("Light1Dir", "W")]
        public float Light1DirW { get; set; }
        [CustomSerialize("Light1DiffuseColor", "R")]
        public float Light1DiffuseColorR { get; set; }
        [CustomSerialize("Light1DiffuseColor", "G")]
        public float Light1DiffuseColorG { get; set; }
        [CustomSerialize("Light1DiffuseColor", "B")]
        public float Light1DiffuseColorB { get; set; }
        [CustomSerialize("Light1DiffuseColor", "A")]
        public float Light1DiffuseColorA { get; set; }
        [CustomSerialize("Light1SpecularColor", "R")]
        public float Light1SpecularColorR { get; set; }
        [CustomSerialize("Light1SpecularColor", "G")]
        public float Light1SpecularColorG { get; set; }
        [CustomSerialize("Light1SpecularColor", "B")]
        public float Light1SpecularColorB { get; set; }
        [CustomSerialize("Light1SpecularColor", "A")]
        public float Light1SpecularColorA { get; set; }


        //Shader parameter g_vRimColor_VS and g_vRimColor_PS
        [CustomSerialize("RimColor", "R")]
        public float RimColorR { get; set; }
        [CustomSerialize("RimColor", "G")]
        public float RimColorG { get; set; }
        [CustomSerialize("RimColor", "B")]
        public float RimColorB { get; set; }
        [CustomSerialize("RimColor", "A")]
        public float RimColorA { get; set; }
        [CustomSerialize(isFloat: true)]
        public float RimParam1 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float RimParam2 { get; set; }

        [CustomSerialize("HemiSet0_Upper", "R")]
        public float HemiSet0UpperR { get; set; }
        [CustomSerialize("HemiSet0_Upper", "G")]
        public float HemiSet0UpperG { get; set; }
        [CustomSerialize("HemiSet0_Upper", "B")]
        public float HemiSet0UpperB { get; set; }
        [CustomSerialize("HemiSet0_Upper", "A")]
        public float HemiSet0UpperA { get; set; }

        //Shader parameter g_vAmbUni_VS and g_vAmbUni_PS
        [CustomSerialize("HemiSet0_Mid", "R")]
        public float HemiSet0MidR { get; set; }
        [CustomSerialize("HemiSet0_Mid", "G")]
        public float HemiSet0MidG { get; set; }
        [CustomSerialize("HemiSet0_Mid", "B")]
        public float HemiSet0MidB { get; set; }
        [CustomSerialize("HemiSet0_Mid", "A")]
        public float HemiSet0MidA { get; set; }

        [CustomSerialize("HemiSet0_Lower", "R")]
        public float HemiSet0LowerR { get; set; }
        [CustomSerialize("HemiSet0_Lower", "G")]
        public float HemiSet0LowerG { get; set; }
        [CustomSerialize("HemiSet0_Lower", "B")]
        public float HemiSet0LowerB { get; set; }
        [CustomSerialize("HemiSet0_Lower", "A")]
        public float HemiSet0LowerA { get; set; }

        [CustomSerialize("AmbOcl", "R")]
        public float AmbOclR { get; set; }
        [CustomSerialize("AmbOcl", "G")]
        public float AmbOclG { get; set; }
        [CustomSerialize("AmbOcl", "B")]
        public float AmbOclB { get; set; }
        [CustomSerialize("AmbOcl", "A")]
        public float AmbOclA { get; set; }

        [CustomSerialize]
        public float ShadowParam48 { get; set; }
        [CustomSerialize]
        public float ShadowDepthBias { get; set; }

        [CustomSerialize("FogMultiColor", "R")]
        public float FogMultiColorR { get; set; }

        [CustomSerialize("FogMultiColor", "G")]
        public float FogMultiColorG { get; set; }

        [CustomSerialize("FogMultiColor", "B")]
        public float FogMultiColorB { get; set; }

        [CustomSerialize("FogMultiColor", "A")]
        public float FogMultiColorA { get; set; }

        [CustomSerialize("FogAddColor", "R")]
        public float FogAddColorR { get; set; }

        [CustomSerialize("FogAddColor", "G")]
        public float FogAddColorG { get; set; }

        [CustomSerialize("FogAddColor", "B")]
        public float FogAddColorB { get; set; }

        [CustomSerialize("FogAddColor", "A")]
        public float FogAddColorA { get; set; }

        [CustomSerialize]
        public float FogStartDist { get; set; }

        [CustomSerialize]
        public float FogEndDist { get; set; }

        [CustomSerialize]
        public float F_280 { get; set; }

        [CustomSerialize]
        public float ColorSaturation { get; set; }

        [CustomSerialize("MultiplyColor", "R")]
        public float MultiplyColorR { get; set; }

        [CustomSerialize("MultiplyColor", "G")]
        public float MultiplyColorG { get; set; }

        [CustomSerialize("MultiplyColor", "B")]
        public float MultiplyColorB { get; set; }

        [CustomSerialize("FilterColor", "R")]
        public float FilterColorR { get; set; }

        [CustomSerialize("FilterColor", "G")]
        public float FilterColorG { get; set; }

        [CustomSerialize("FilterColor", "B")]
        public float FilterColorB { get; set; }

        [CustomSerialize("AdditiveColor", "R")]
        public float AdditiveColorR { get; set; }

        [CustomSerialize("AdditiveColor", "G")]
        public float AdditiveColorG { get; set; }

        [CustomSerialize("AdditiveColor", "B")]
        public float AdditiveColorB { get; set; }

        [CustomSerialize]
        public float FilterHotColor { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_328 { get; set; }

        [CustomSerialize]
        public float MultiplyInverseFactor { get; set; }

        [CustomSerialize]
        public float BlurStartDist { get; set; }

        [CustomSerialize]
        public float F_356 { get; set; }

        [CustomSerialize]
        public ushort HaloAlphaRejection { get; set; }

        [CustomSerialize]
        public ushort I_362 { get; set; }

        [CustomSerialize]
        public float ShadowForceMinY { get; set; }

        [CustomSerialize]
        public float ShadowFarMax { get; set; }

        [CustomSerialize]
        public float F_372 { get; set; }

        [CustomSerialize]
        public float ShadowFadeStartRatio { get; set; }

        [CustomSerialize]
        public float SunFactor { get; set; }

        [CustomSerialize]
        public float SunFactorHidden { get; set; }

        [CustomSerialize]
        public float SunSize { get; set; }

        [CustomSerialize]
        public float F_392 { get; set; }

        [CustomSerialize]
        public float F_396 { get; set; }


        [CustomSerialize("Tone", "R")]
        public float ToneR { get; set; }

        [CustomSerialize("Tone", "G")]
        public float ToneG { get; set; }

        [CustomSerialize("Tone", "B")]
        public float ToneB { get; set; }
        [CustomSerialize("Tone", "A")]
        public float ToneA { get; set; } //Comes before RGB values
        [CustomSerialize("Tone", "Scale")]
        public float ToneScale { get; set; }


        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_420 { get; set; }

        [CustomSerialize]
        public float EffectGlareTone { get; set; }

        [CustomSerialize("CharaGlaraAdditiveColor", "R")]
        public float CharacterGlareAdditiveColorR { get; set; }

        [CustomSerialize("CharaGlaraAdditiveColor", "G")]
        public float CharacterGlareAdditiveColorG { get; set; }

        [CustomSerialize("CharaGlaraAdditiveColor", "B")]
        public float CharacterGlareAdditiveColorB { get; set; }

        [CustomSerialize]
        public int I_456 { get; set; }

        [CustomSerialize]
        public float EdgeStrokesFactor { get; set; }

        [CustomSerialize]
        public float EdgeStrokesWidth { get; set; }

        [CustomSerialize("EdgeStrokesColor", "R")]
        public float EdgeStrokesColorR { get; set; }

        [CustomSerialize("EdgeStrokesColor", "G")]
        public float EdgeStrokesColorG { get; set; }

        [CustomSerialize("EdgeStrokesColor", "B")]
        public float EdgeStrokesColorB { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_480 { get; set; }

        [CustomSerialize]
        public float LayerReceiveShadowW { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_500 { get; set; }

        [CustomSerialize(isFloat: true)]
        public float F_512 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_516 { get; set; }

        [CustomSerialize]
        public int I_528 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_532 { get; set; }

        [CustomSerialize]
        public ushort I_544 { get; set; }
        [CustomSerialize]
        public ushort I_546 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_550 { get; set; }


        [CustomSerialize("HemiSet1_Upper", "R")]
        public float HemiSet1UpperR { get; set; }
        [CustomSerialize("HemiSet1_Upper", "G")]
        public float HemiSet1UpperG { get; set; }
        [CustomSerialize("HemiSet1_Upper", "B")]
        public float HemiSet1UpperB { get; set; }
        [CustomSerialize("HemiSet1_Upper", "A")]
        public float HemiSet1UpperA { get; set; }

        [CustomSerialize("HemiSet1_Mid", "R")]
        public float HemiSet1MidR { get; set; }

        [CustomSerialize("HemiSet1_Mid", "G")]
        public float HemiSet1MidG { get; set; }

        [CustomSerialize("HemiSet1_Mid", "B")]
        public float HemiSet1MidB { get; set; }

        [CustomSerialize("HemiSet1_Mid", "A")]
        public float HemiSet1MidA { get; set; }

        [CustomSerialize("HemiSet1_Lower", "R")]
        public float HemiSet1LowerR { get; set; }
        [CustomSerialize("HemiSet1_Lower", "G")]
        public float HemiSet1LowerG { get; set; }
        [CustomSerialize("HemiSet1_Lower", "B")]
        public float HemiSet1LowerB { get; set; }
        [CustomSerialize("HemiSet1_Lower", "A")]
        public float HemiSet1LowerA { get; set; }

        [CustomSerialize("ShadowDir", "X")]
        public float BgLightDirectionX { get; set; }
        [CustomSerialize("ShadowDir", "Y")]
        public float BgLightDirectionY { get; set; }
        [CustomSerialize("ShadowDir", "Z")]
        public float BgLightDirectionZ { get; set; }
        [CustomSerialize("ShadowDir", "W")]
        public float BgLightDirectionW { get; set; }

        [CustomSerialize("BgLightDiffuse", "R")]
        public float BgLightDiffuseR { get; set; }
        [CustomSerialize("BgLightDiffuse", "G")]
        public float BgLightDiffuseG { get; set; }
        [CustomSerialize("BgLightDiffuse", "B")]
        public float BgLightDiffuseB { get; set; }
        [CustomSerialize("BgLightDiffuse", "A")]
        public float BgLightDiffuseA { get; set; }

        [CustomSerialize("BgLightSpecular", "R")]
        public float BgLightSpecularR { get; set; }
        [CustomSerialize("BgLightSpecular", "G")]
        public float BgLightSpecularG { get; set; }
        [CustomSerialize("BgLightSpecular", "B")]
        public float BgLightSpecularB { get; set; }
        [CustomSerialize("BgLightSpecular", "A")]
        public float BgLightSpecularA { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_656 { get; set; }

        [CustomSerialize("ShadowColor", "R")]
        public float ShadowColorR { get; set; }

        [CustomSerialize("ShadowColor", "G")]
        public float ShadowColorG { get; set; }

        [CustomSerialize("ShadowColor", "B")]
        public float ShadowColorB { get; set; }

        [CustomSerialize("ShadowColor", "A")]
        public float ShadowColorA { get; set; }

        [CustomSerialize("SunGodRayColor", "R")]
        public float SunGodRayColorR { get; set; }

        [CustomSerialize("SunGodRayColor", "G")]
        public float SunGodRayColorG { get; set; }

        [CustomSerialize("SunGodRayColor", "B")]
        public float SunGodRayColorB { get; set; }

        [CustomSerialize("SunGodRayColor", "Intensity")]
        public float SunGodRayColorIntensity { get; set; }

        [CustomSerialize]
        public float LimitShadowHighStart { get; set; }

        [CustomSerialize]
        public float LimitShadowHighEnd { get; set; }

        [CustomSerialize]
        public float LimitShadowRadiusStart { get; set; }

        [CustomSerialize]
        public float LimitShadowRadiusEnd { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_720 { get; set; }

        [CustomSerialize]
        public float SunGodRayPolarDistance { get; set; }

        [CustomSerialize]
        public int SunGodRayUnk { get; set; }

        [CustomSerialize]
        public float SunGodRayFadingDistance { get; set; }

        [CustomSerialize]
        public float SunGodRayFadingAttenuationFactor { get; set; }

        [CustomSerialize]
        public float SunGodRayFadingSubdivision { get; set; }

        [CustomSerialize]
        public float SunGodRayRepetitionAngle { get; set; }

        [CustomSerialize]
        public float SunGodRayRepetitionSubdivision { get; set; }

        [CustomSerialize]
        public float F_780 { get; set; }

        [YAXDontSerializeIfNull]
        public SPM_ExtendedEntry ExtendedData { get; set; }

        internal byte[] Write(bool useExtendedData)
        {
            List<byte> bytes = new List<byte>(useExtendedData ? SPM_BASE_ENTRY_SIZE + SPM_ExtendedEntry.EXPANDED_ENTRY_SIZE : SPM_BASE_ENTRY_SIZE);

            //Check array sizes
            Assertion.AssertArraySize(I_18, 5, "SPMEntry", nameof(I_18));
            Assertion.AssertArraySize(F_420, 5, "SPMEntry", nameof(F_420));
            Assertion.AssertArraySize(F_480, 4, "SPMEntry", nameof(F_480));
            Assertion.AssertArraySize(F_500, 3, "SPMEntry", nameof(F_500));
            Assertion.AssertArraySize(F_516, 3, "SPMEntry", nameof(F_516));
            Assertion.AssertArraySize(F_532, 3, "SPMEntry", nameof(F_532));
            Assertion.AssertArraySize(I_550, 5, "SPMEntry", nameof(I_550));
            Assertion.AssertArraySize(F_656, 4, "SPMEntry", nameof(F_656));
            Assertion.AssertArraySize(F_720, 8, "SPMEntry", nameof(F_720));

            //Write main entry values
            bytes.AddRange(StringEx.WriteFixedSizeString(Name, 16));
            bytes.AddRange(BitConverter.GetBytes(LensFlareEnabled));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_18));
            bytes.AddRange(BitConverter.GetBytes(GodRayAndSunHalo));
            bytes.AddRange(BitConverter.GetBytes(I_30));
            bytes.AddRange(BitConverter.GetBytes(BgFillColorR));
            bytes.AddRange(BitConverter.GetBytes(BgFillColorG));
            bytes.AddRange(BitConverter.GetBytes(BgFillColorB));
            bytes.AddRange(BitConverter.GetBytes(BgFillColorA));
            bytes.AddRange(BitConverter.GetBytes(Light0DirX));
            bytes.AddRange(BitConverter.GetBytes(Light0DirY));
            bytes.AddRange(BitConverter.GetBytes(Light0DirZ));
            bytes.AddRange(BitConverter.GetBytes(Light0DirW));
            bytes.AddRange(BitConverter.GetBytes(Light1DirX));
            bytes.AddRange(BitConverter.GetBytes(Light1DirY));
            bytes.AddRange(BitConverter.GetBytes(Light1DirZ));
            bytes.AddRange(BitConverter.GetBytes(Light1DirW));
            bytes.AddRange(BitConverter.GetBytes(Light0DiffuseColorR));
            bytes.AddRange(BitConverter.GetBytes(Light0DiffuseColorG));
            bytes.AddRange(BitConverter.GetBytes(Light0DiffuseColorB));
            bytes.AddRange(BitConverter.GetBytes(Light0DiffuseColorA));
            bytes.AddRange(BitConverter.GetBytes(Light1DiffuseColorR));
            bytes.AddRange(BitConverter.GetBytes(Light1DiffuseColorG));
            bytes.AddRange(BitConverter.GetBytes(Light1DiffuseColorB));
            bytes.AddRange(BitConverter.GetBytes(Light1DiffuseColorA));
            bytes.AddRange(BitConverter.GetBytes(Light0SpecularColorR));
            bytes.AddRange(BitConverter.GetBytes(Light0SpecularColorG));
            bytes.AddRange(BitConverter.GetBytes(Light0SpecularColorB));
            bytes.AddRange(BitConverter.GetBytes(Light0SpecularColorA));
            bytes.AddRange(BitConverter.GetBytes(Light1SpecularColorR));
            bytes.AddRange(BitConverter.GetBytes(Light1SpecularColorG));
            bytes.AddRange(BitConverter.GetBytes(Light1SpecularColorB));
            bytes.AddRange(BitConverter.GetBytes(Light1SpecularColorA));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0UpperR));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0UpperG));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0UpperB));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0UpperA));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0MidR));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0MidG));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0MidB));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0MidA));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0LowerR));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0LowerG));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0LowerB));
            bytes.AddRange(BitConverter.GetBytes(HemiSet0LowerA));
            bytes.AddRange(BitConverter.GetBytes(RimColorR));
            bytes.AddRange(BitConverter.GetBytes(RimColorG));
            bytes.AddRange(BitConverter.GetBytes(RimColorB));
            bytes.AddRange(BitConverter.GetBytes(RimColorA));
            bytes.AddRange(BitConverter.GetBytes(AmbOclR));
            bytes.AddRange(BitConverter.GetBytes(AmbOclG));
            bytes.AddRange(BitConverter.GetBytes(AmbOclB));
            bytes.AddRange(BitConverter.GetBytes(AmbOclA));
            bytes.AddRange(BitConverter.GetBytes(RimParam1));
            bytes.AddRange(BitConverter.GetBytes(RimParam2));
            bytes.AddRange(BitConverter.GetBytes(ShadowParam48));
            bytes.AddRange(BitConverter.GetBytes(ShadowDepthBias));
            bytes.AddRange(BitConverter.GetBytes(FogMultiColorR));
            bytes.AddRange(BitConverter.GetBytes(FogMultiColorG));
            bytes.AddRange(BitConverter.GetBytes(FogMultiColorB));
            bytes.AddRange(BitConverter.GetBytes(FogMultiColorA));
            bytes.AddRange(BitConverter.GetBytes(FogAddColorR));
            bytes.AddRange(BitConverter.GetBytes(FogAddColorG));
            bytes.AddRange(BitConverter.GetBytes(FogAddColorB));
            bytes.AddRange(BitConverter.GetBytes(FogAddColorA));
            bytes.AddRange(BitConverter.GetBytes(FogStartDist));
            bytes.AddRange(BitConverter.GetBytes(FogEndDist));
            bytes.AddRange(BitConverter.GetBytes(F_280));
            bytes.AddRange(BitConverter.GetBytes(ColorSaturation));
            bytes.AddRange(BitConverter.GetBytes(MultiplyColorR));
            bytes.AddRange(BitConverter.GetBytes(MultiplyColorG));
            bytes.AddRange(BitConverter.GetBytes(MultiplyColorB));
            bytes.AddRange(BitConverter.GetBytes(FilterColorR));
            bytes.AddRange(BitConverter.GetBytes(FilterColorG));
            bytes.AddRange(BitConverter.GetBytes(FilterColorB));
            bytes.AddRange(BitConverter.GetBytes(AdditiveColorR));
            bytes.AddRange(BitConverter.GetBytes(AdditiveColorG));
            bytes.AddRange(BitConverter.GetBytes(AdditiveColorB));
            bytes.AddRange(BitConverter.GetBytes(FilterHotColor));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_328));
            bytes.AddRange(BitConverter.GetBytes(MultiplyInverseFactor));
            bytes.AddRange(BitConverter.GetBytes(BlurStartDist));
            bytes.AddRange(BitConverter.GetBytes(F_356));
            bytes.AddRange(BitConverter.GetBytes(HaloAlphaRejection));
            bytes.AddRange(BitConverter.GetBytes(I_362));
            bytes.AddRange(BitConverter.GetBytes(ShadowForceMinY));
            bytes.AddRange(BitConverter.GetBytes(ShadowFarMax));
            bytes.AddRange(BitConverter.GetBytes(F_372));
            bytes.AddRange(BitConverter.GetBytes(ShadowFadeStartRatio));
            bytes.AddRange(BitConverter.GetBytes(SunFactor));
            bytes.AddRange(BitConverter.GetBytes(SunFactorHidden));
            bytes.AddRange(BitConverter.GetBytes(SunSize));
            bytes.AddRange(BitConverter.GetBytes(F_392));
            bytes.AddRange(BitConverter.GetBytes(F_396));
            bytes.AddRange(BitConverter.GetBytes(ToneA));
            bytes.AddRange(BitConverter.GetBytes(ToneR));
            bytes.AddRange(BitConverter.GetBytes(ToneG));
            bytes.AddRange(BitConverter.GetBytes(ToneB));
            bytes.AddRange(BitConverter.GetBytes(ToneScale));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_420));
            bytes.AddRange(BitConverter.GetBytes(EffectGlareTone));
            bytes.AddRange(BitConverter.GetBytes(CharacterGlareAdditiveColorR));
            bytes.AddRange(BitConverter.GetBytes(CharacterGlareAdditiveColorG));
            bytes.AddRange(BitConverter.GetBytes(CharacterGlareAdditiveColorB));
            bytes.AddRange(BitConverter.GetBytes(I_456));
            bytes.AddRange(BitConverter.GetBytes(EdgeStrokesFactor));
            bytes.AddRange(BitConverter.GetBytes(EdgeStrokesWidth));
            bytes.AddRange(BitConverter.GetBytes(EdgeStrokesColorR));
            bytes.AddRange(BitConverter.GetBytes(EdgeStrokesColorG));
            bytes.AddRange(BitConverter.GetBytes(EdgeStrokesColorB));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_480));
            bytes.AddRange(BitConverter.GetBytes(LayerReceiveShadowW));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_500));
            bytes.AddRange(BitConverter.GetBytes(F_512));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_516));
            bytes.AddRange(BitConverter.GetBytes(I_528));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_532));
            bytes.AddRange(BitConverter.GetBytes(I_544));
            bytes.AddRange(BitConverter.GetBytes(I_546));
            bytes.AddRange(BitConverter.GetBytes(ID));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_550));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1UpperR));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1UpperG));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1UpperB));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1UpperA));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1MidR));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1MidG));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1MidB));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1MidA));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1LowerR));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1LowerG));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1LowerB));
            bytes.AddRange(BitConverter.GetBytes(HemiSet1LowerA));
            bytes.AddRange(BitConverter.GetBytes(BgLightDirectionX));
            bytes.AddRange(BitConverter.GetBytes(BgLightDirectionY));
            bytes.AddRange(BitConverter.GetBytes(BgLightDirectionZ));
            bytes.AddRange(BitConverter.GetBytes(BgLightDirectionW));
            bytes.AddRange(BitConverter.GetBytes(BgLightDiffuseR));
            bytes.AddRange(BitConverter.GetBytes(BgLightDiffuseG));
            bytes.AddRange(BitConverter.GetBytes(BgLightDiffuseB));
            bytes.AddRange(BitConverter.GetBytes(BgLightDiffuseA));
            bytes.AddRange(BitConverter.GetBytes(BgLightSpecularR));
            bytes.AddRange(BitConverter.GetBytes(BgLightSpecularG));
            bytes.AddRange(BitConverter.GetBytes(BgLightSpecularB));
            bytes.AddRange(BitConverter.GetBytes(BgLightSpecularA));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_656));
            bytes.AddRange(BitConverter.GetBytes(ShadowColorR));
            bytes.AddRange(BitConverter.GetBytes(ShadowColorG));
            bytes.AddRange(BitConverter.GetBytes(ShadowColorB));
            bytes.AddRange(BitConverter.GetBytes(ShadowColorA));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayColorR));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayColorG));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayColorB));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayColorIntensity));
            bytes.AddRange(BitConverter.GetBytes(LimitShadowHighStart));
            bytes.AddRange(BitConverter.GetBytes(LimitShadowHighEnd));
            bytes.AddRange(BitConverter.GetBytes(LimitShadowRadiusStart));
            bytes.AddRange(BitConverter.GetBytes(LimitShadowRadiusEnd));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_720));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayPolarDistance));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayUnk));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayFadingDistance));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayFadingAttenuationFactor));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayFadingSubdivision));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayRepetitionAngle));
            bytes.AddRange(BitConverter.GetBytes(SunGodRayRepetitionSubdivision));
            bytes.AddRange(BitConverter.GetBytes(F_780));

            if (bytes.Count != 784)
                throw new Exception("SPM_Entry.Write: Incorrect size!");

            if(useExtendedData && ExtendedData != null)
            {
                bytes.AddRange(ExtendedData.Write());
            }
            else if (useExtendedData)
            {
                throw new Exception("ExtendedData is missing, but this version of SPM requires it.");
            }

            return bytes.ToArray();
        }

        internal static SPM_Entry Read(byte[] bytes, int offset, bool extendedEntry)
        {
            return new SPM_Entry()
            {
                Name = StringEx.GetString(bytes, offset, maxSize: 32),
                LensFlareEnabled = BitConverter.ToUInt16(bytes, offset + 16),
                I_18 = BitConverter_Ex.ToUInt16Array(bytes, offset + 18, 5),
                GodRayAndSunHalo = BitConverter.ToUInt16(bytes, offset + 28),
                I_30 = BitConverter.ToUInt16(bytes, offset + 30),
                BgFillColorR = BitConverter.ToSingle(bytes, offset + 32),
                BgFillColorG = BitConverter.ToSingle(bytes, offset + 36),
                BgFillColorB = BitConverter.ToSingle(bytes, offset + 40),
                BgFillColorA = BitConverter.ToSingle(bytes, offset + 44),
                Light0DirX = BitConverter.ToSingle(bytes, offset + 48),
                Light0DirY = BitConverter.ToSingle(bytes, offset + 52),
                Light0DirZ = BitConverter.ToSingle(bytes, offset + 56),
                Light0DirW = BitConverter.ToSingle(bytes, offset + 60),
                Light1DirX = BitConverter.ToSingle(bytes, offset + 64),
                Light1DirY = BitConverter.ToSingle(bytes, offset + 68),
                Light1DirZ = BitConverter.ToSingle(bytes, offset + 72),
                Light1DirW = BitConverter.ToSingle(bytes, offset + 76),
                Light0DiffuseColorR = BitConverter.ToSingle(bytes, offset + 80),
                Light0DiffuseColorG = BitConverter.ToSingle(bytes, offset + 84),
                Light0DiffuseColorB = BitConverter.ToSingle(bytes, offset + 88),
                Light0DiffuseColorA = BitConverter.ToSingle(bytes, offset + 92),
                Light1DiffuseColorR = BitConverter.ToSingle(bytes, offset + 96),
                Light1DiffuseColorG = BitConverter.ToSingle(bytes, offset + 100),
                Light1DiffuseColorB = BitConverter.ToSingle(bytes, offset + 104),
                Light1DiffuseColorA = BitConverter.ToSingle(bytes, offset + 108),
                Light0SpecularColorR = BitConverter.ToSingle(bytes, offset + 112),
                Light0SpecularColorG = BitConverter.ToSingle(bytes, offset + 116),
                Light0SpecularColorB = BitConverter.ToSingle(bytes, offset + 120),
                Light0SpecularColorA = BitConverter.ToSingle(bytes, offset + 124),
                Light1SpecularColorR = BitConverter.ToSingle(bytes, offset + 128),
                Light1SpecularColorG = BitConverter.ToSingle(bytes, offset + 132),
                Light1SpecularColorB = BitConverter.ToSingle(bytes, offset + 136),
                Light1SpecularColorA = BitConverter.ToSingle(bytes, offset + 140),
                HemiSet0UpperR = BitConverter.ToSingle(bytes, offset + 144),
                HemiSet0UpperG = BitConverter.ToSingle(bytes, offset + 148),
                HemiSet0UpperB = BitConverter.ToSingle(bytes, offset + 152),
                HemiSet0UpperA = BitConverter.ToSingle(bytes, offset + 156),
                HemiSet0MidR = BitConverter.ToSingle(bytes, offset + 160),
                HemiSet0MidG = BitConverter.ToSingle(bytes, offset + 164),
                HemiSet0MidB = BitConverter.ToSingle(bytes, offset + 168),
                HemiSet0MidA = BitConverter.ToSingle(bytes, offset + 172),
                HemiSet0LowerR = BitConverter.ToSingle(bytes, offset + 176),
                HemiSet0LowerG = BitConverter.ToSingle(bytes, offset + 180),
                HemiSet0LowerB = BitConverter.ToSingle(bytes, offset + 184),
                HemiSet0LowerA = BitConverter.ToSingle(bytes, offset + 188),
                RimColorR = BitConverter.ToSingle(bytes, offset + 192),
                RimColorG = BitConverter.ToSingle(bytes, offset + 196),
                RimColorB = BitConverter.ToSingle(bytes, offset + 200),
                RimColorA = BitConverter.ToSingle(bytes, offset + 204),
                AmbOclR = BitConverter.ToSingle(bytes, offset + 208),
                AmbOclG = BitConverter.ToSingle(bytes, offset + 212),
                AmbOclB = BitConverter.ToSingle(bytes, offset + 216),
                AmbOclA = BitConverter.ToSingle(bytes, offset + 220),
                RimParam1 = BitConverter.ToSingle(bytes, offset + 224),
                RimParam2 = BitConverter.ToSingle(bytes, offset + 228),
                ShadowParam48 = BitConverter.ToSingle(bytes, offset + 232),
                ShadowDepthBias = BitConverter.ToSingle(bytes, offset + 236),
                FogMultiColorR = BitConverter.ToSingle(bytes, offset + 240),
                FogMultiColorG = BitConverter.ToSingle(bytes, offset + 244),
                FogMultiColorB = BitConverter.ToSingle(bytes, offset + 248),
                FogMultiColorA = BitConverter.ToSingle(bytes, offset + 252),
                FogAddColorR = BitConverter.ToSingle(bytes, offset + 256),
                FogAddColorG = BitConverter.ToSingle(bytes, offset + 260),
                FogAddColorB = BitConverter.ToSingle(bytes, offset + 264),
                FogAddColorA = BitConverter.ToSingle(bytes, offset + 268),
                FogStartDist = BitConverter.ToSingle(bytes, offset + 272),
                FogEndDist = BitConverter.ToSingle(bytes, offset + 276),
                F_280 = BitConverter.ToSingle(bytes, offset + 280),
                ColorSaturation = BitConverter.ToSingle(bytes, offset + 284),
                MultiplyColorR = BitConverter.ToSingle(bytes, offset + 288),
                MultiplyColorG = BitConverter.ToSingle(bytes, offset + 292),
                MultiplyColorB = BitConverter.ToSingle(bytes, offset + 296),
                FilterColorR = BitConverter.ToSingle(bytes, offset + 300),
                FilterColorG = BitConverter.ToSingle(bytes, offset + 304),
                FilterColorB = BitConverter.ToSingle(bytes, offset + 308),
                AdditiveColorR = BitConverter.ToSingle(bytes, offset + 312),
                AdditiveColorG = BitConverter.ToSingle(bytes, offset + 316),
                AdditiveColorB = BitConverter.ToSingle(bytes, offset + 320),
                FilterHotColor = BitConverter.ToSingle(bytes, offset + 324),
                F_328 = BitConverter_Ex.ToFloat32Array(bytes, offset + 328, 5),
                MultiplyInverseFactor = BitConverter.ToSingle(bytes, offset + 348),
                BlurStartDist = BitConverter.ToSingle(bytes, offset + 352),
                F_356 = BitConverter.ToSingle(bytes, offset + 356),
                HaloAlphaRejection = BitConverter.ToUInt16(bytes, offset + 360),
                I_362 = BitConverter.ToUInt16(bytes, offset + 362),
                ShadowForceMinY = BitConverter.ToSingle(bytes, offset + 364),
                ShadowFarMax = BitConverter.ToSingle(bytes, offset + 368),
                F_372 = BitConverter.ToSingle(bytes, offset + 372),
                ShadowFadeStartRatio = BitConverter.ToSingle(bytes, offset + 376),
                SunFactor = BitConverter.ToSingle(bytes, offset + 380),
                SunFactorHidden = BitConverter.ToSingle(bytes, offset + 384),
                SunSize = BitConverter.ToSingle(bytes, offset + 388),
                F_392 = BitConverter.ToSingle(bytes, offset + 392),
                F_396 = BitConverter.ToSingle(bytes, offset + 396),
                ToneA = BitConverter.ToSingle(bytes, offset + 400),
                ToneR = BitConverter.ToSingle(bytes, offset + 404),
                ToneG = BitConverter.ToSingle(bytes, offset + 408),
                ToneB = BitConverter.ToSingle(bytes, offset + 412),
                ToneScale = BitConverter.ToSingle(bytes, offset + 416),
                F_420 = BitConverter_Ex.ToFloat32Array(bytes, offset + 420, 5),
                EffectGlareTone = BitConverter.ToSingle(bytes, offset + 440),
                CharacterGlareAdditiveColorR = BitConverter.ToSingle(bytes, offset + 444),
                CharacterGlareAdditiveColorG = BitConverter.ToSingle(bytes, offset + 448),
                CharacterGlareAdditiveColorB = BitConverter.ToSingle(bytes, offset + 452),
                I_456 = BitConverter.ToInt32(bytes, offset + 456),
                EdgeStrokesFactor = BitConverter.ToSingle(bytes, offset + 460),
                EdgeStrokesWidth = BitConverter.ToSingle(bytes, offset + 464),
                EdgeStrokesColorR = BitConverter.ToSingle(bytes, offset + 468),
                EdgeStrokesColorG = BitConverter.ToSingle(bytes, offset + 472),
                EdgeStrokesColorB = BitConverter.ToSingle(bytes, offset + 476),
                F_480 = BitConverter_Ex.ToFloat32Array(bytes, offset + 480, 4),
                LayerReceiveShadowW = BitConverter.ToSingle(bytes, offset + 496),
                F_500 = BitConverter_Ex.ToFloat32Array(bytes, offset + 500, 3),
                F_512 = BitConverter.ToSingle(bytes, offset + 512),
                F_516 = BitConverter_Ex.ToFloat32Array(bytes, offset + 516, 3),
                I_528 = BitConverter.ToInt32(bytes, offset + 528),
                F_532 = BitConverter_Ex.ToFloat32Array(bytes, offset + 532, 3),
                I_544 = BitConverter.ToUInt16(bytes, offset + 544),
                I_546 = BitConverter.ToUInt16(bytes, offset + 546),
                ID = BitConverter.ToInt16(bytes, offset + 548),
                I_550 = BitConverter_Ex.ToUInt16Array(bytes, offset + 550, 5),
                HemiSet1UpperR = BitConverter.ToSingle(bytes, offset + 560),
                HemiSet1UpperG = BitConverter.ToSingle(bytes, offset + 564),
                HemiSet1UpperB = BitConverter.ToSingle(bytes, offset + 568),
                HemiSet1UpperA = BitConverter.ToSingle(bytes, offset + 572),
                HemiSet1MidR = BitConverter.ToSingle(bytes, offset + 576),
                HemiSet1MidG = BitConverter.ToSingle(bytes, offset + 580),
                HemiSet1MidB = BitConverter.ToSingle(bytes, offset + 584),
                HemiSet1MidA = BitConverter.ToSingle(bytes, offset + 588),
                HemiSet1LowerR = BitConverter.ToSingle(bytes, offset + 592),
                HemiSet1LowerG = BitConverter.ToSingle(bytes, offset + 596),
                HemiSet1LowerB = BitConverter.ToSingle(bytes, offset + 600),
                HemiSet1LowerA = BitConverter.ToSingle(bytes, offset + 604),
                BgLightDirectionX = BitConverter.ToSingle(bytes, offset + 608),
                BgLightDirectionY = BitConverter.ToSingle(bytes, offset + 612),
                BgLightDirectionZ = BitConverter.ToSingle(bytes, offset + 616),
                BgLightDirectionW = BitConverter.ToSingle(bytes, offset + 620),
                BgLightDiffuseR = BitConverter.ToSingle(bytes, offset + 624),
                BgLightDiffuseG = BitConverter.ToSingle(bytes, offset + 628),
                BgLightDiffuseB = BitConverter.ToSingle(bytes, offset + 632),
                BgLightDiffuseA = BitConverter.ToSingle(bytes, offset + 636),
                BgLightSpecularR = BitConverter.ToSingle(bytes, offset + 640),
                BgLightSpecularG = BitConverter.ToSingle(bytes, offset + 644),
                BgLightSpecularB = BitConverter.ToSingle(bytes, offset + 648),
                BgLightSpecularA = BitConverter.ToSingle(bytes, offset + 652),
                F_656 = BitConverter_Ex.ToFloat32Array(bytes, offset + 656, 4),
                ShadowColorR = BitConverter.ToSingle(bytes, offset + 672),
                ShadowColorG = BitConverter.ToSingle(bytes, offset + 676),
                ShadowColorB = BitConverter.ToSingle(bytes, offset + 680),
                ShadowColorA = BitConverter.ToSingle(bytes, offset + 684),
                SunGodRayColorR = BitConverter.ToSingle(bytes, offset + 688),
                SunGodRayColorG = BitConverter.ToSingle(bytes, offset + 692),
                SunGodRayColorB = BitConverter.ToSingle(bytes, offset + 696),
                SunGodRayColorIntensity = BitConverter.ToSingle(bytes, offset + 700),
                LimitShadowHighStart = BitConverter.ToSingle(bytes, offset + 704),
                LimitShadowHighEnd = BitConverter.ToSingle(bytes, offset + 708),
                LimitShadowRadiusStart = BitConverter.ToSingle(bytes, offset + 712),
                LimitShadowRadiusEnd = BitConverter.ToSingle(bytes, offset + 716),
                F_720 = BitConverter_Ex.ToFloat32Array(bytes, offset + 720, 8),
                SunGodRayPolarDistance = BitConverter.ToSingle(bytes, offset + 752),
                SunGodRayUnk = BitConverter.ToInt32(bytes, offset + 756),
                SunGodRayFadingDistance = BitConverter.ToSingle(bytes, offset + 760),
                SunGodRayFadingAttenuationFactor = BitConverter.ToSingle(bytes, offset + 764),
                SunGodRayFadingSubdivision = BitConverter.ToSingle(bytes, offset + 768),
                SunGodRayRepetitionAngle = BitConverter.ToSingle(bytes, offset + 772),
                SunGodRayRepetitionSubdivision = BitConverter.ToSingle(bytes, offset + 776),
                F_780 = BitConverter.ToSingle(bytes, offset + 780),
                ExtendedData = extendedEntry ? SPM_ExtendedEntry.Read(bytes, offset + 784) : null
            };
        }

    }

    public class SPM_ExtendedEntry
    {
        internal const int EXPANDED_ENTRY_SIZE = 336;

        [CustomSerialize]
        public ushort I_00 { get; set; }

        [CustomSerialize]
        public ushort I_02 { get; set; }

        [CustomSerialize]
        public ushort I_04 { get; set; }

        [CustomSerialize]
        public ushort I_06 { get; set; }

        [CustomSerialize]
        public ushort I_08 { get; set; }

        [CustomSerialize]
        public ushort I_10 { get; set; }

        [CustomSerialize]
        public ushort I_12 { get; set; }

        [CustomSerialize]
        public ushort I_14 { get; set; }

        [CustomSerialize]
        public int CharacterBorderStrokeEnable { get; set; }

        [CustomSerialize]
        public float CharacterBorderStrokeFarWidth { get; set; }

        [CustomSerialize]
        public float CharacterBorderStrokeNearWidth { get; set; }

        [CustomSerialize]
        public float CharacterBorderStrokeStartDist { get; set; }

        [CustomSerialize]
        public float CharacterBorderStrokeEndDist { get; set; }

        [CustomSerialize("CharacterBorderStrokeColor", "R")]
        public byte CharacterBorderStrokeColorR { get; set; }

        [CustomSerialize("CharacterBorderStrokeColor", "G")]
        public byte CharacterBorderStrokeColorG { get; set; }

        [CustomSerialize("CharacterBorderStrokeColor", "B")]
        public byte CharacterBorderStrokeColorB { get; set; }

        [CustomSerialize("CharacterBorderStrokeColor", "A")]
        public byte CharacterBorderStrokeColorA { get; set; }

        [CustomSerialize]
        public int I_40 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_44 { get; set; }

        [CustomSerialize]
        public int I_64 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_68 { get; set; } //[0] = g_vMotionBlur_VS.z

        [CustomSerialize]
        public int I_76 { get; set; }

        [CustomSerialize(IsHex = true)]
        public int I_80 { get; set; }
        [CustomSerialize(IsHex = true)]
        public int I_84 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ShadowFarMin { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ShadowFarExponent { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ShadowHeightStart { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ShadowHeightRange { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ShadowAabbFloorHeight { get; set; }
        [CustomSerialize(isFloat: true)]
        public float ShadowAabbCeilingHeight { get; set; }
        [CustomSerialize]
        public int ShadowPerspective { get; set; }
        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_116 { get; set; } //Size 4

        [CustomSerialize(isFloat: true)]
        public float F_132 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_136 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_140 { get; set; }
        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_144 { get; set; } //Size 2
        [CustomSerialize(isFloat: true)]
        public float F_152 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_156 { get; set; }
        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_160 { get; set; } //Size 4
        [CustomSerialize(isFloat: true)]
        public float F_176 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_180 { get; set; }
        [CustomSerialize]
        public int I_184 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_188 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_192 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_196 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_200 { get; set; }
        [CustomSerialize]
        public int ShadowMovingAabb { get; set; }
        [CustomSerialize]
        public int I_208 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_212 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_216 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_220 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_224 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_228 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_232 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_236 { get; set; }
        [CustomSerialize(isFloat: true)]
        public float F_240 { get; set; }
        [CustomSerialize]
        public int I_244 { get; set; }
        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_248 { get; set; } //Size 6
        [CustomSerialize(isFloat: true)]
        public float F_272 { get; set; } //g_vColor14_PS.x (characters)
        [CustomSerialize(isFloat: true)]
        public float F_276 { get; set; } //g_vColor14_PS.y (characters)
        [CustomSerialize(isFloat: true)]
        public float F_280 { get; set; } //g_vColor14_PS.z (g_vColor14_PS.w is 1.0, not F_284) (characters)
        [CustomSerialize(isFloat: true)]
        public float F_284 { get; set; }
        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_288 { get; set; } //Size 10
        [CustomSerialize(isFloat: true)]
        public float F_328 { get; set; }
        [CustomSerialize]
        public int I_332 { get; set; }

        internal byte[] Write()
        {
            List<byte> bytes = new List<byte>(EXPANDED_ENTRY_SIZE);

            //Check array sizes
            Assertion.AssertArraySize(F_44, 5, "ExtendedData", nameof(F_44));
            Assertion.AssertArraySize(F_68, 2, "ExtendedData", nameof(F_68)); 
            Assertion.AssertArraySize(I_116, 4, nameof(SPM_ExtendedEntry), nameof(I_116));
            Assertion.AssertArraySize(I_144, 2, nameof(SPM_ExtendedEntry), nameof(I_144));
            Assertion.AssertArraySize(I_160, 4, nameof(SPM_ExtendedEntry), nameof(I_160));
            Assertion.AssertArraySize(I_248, 6, nameof(SPM_ExtendedEntry), nameof(I_248));
            Assertion.AssertArraySize(I_288, 10, nameof(SPM_ExtendedEntry), nameof(I_288));

            //Write values
            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_02));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_06));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_14));
            bytes.AddRange(BitConverter.GetBytes(CharacterBorderStrokeEnable));
            bytes.AddRange(BitConverter.GetBytes(CharacterBorderStrokeFarWidth));
            bytes.AddRange(BitConverter.GetBytes(CharacterBorderStrokeNearWidth));
            bytes.AddRange(BitConverter.GetBytes(CharacterBorderStrokeStartDist));
            bytes.AddRange(BitConverter.GetBytes(CharacterBorderStrokeEndDist));
            bytes.Add(CharacterBorderStrokeColorR);
            bytes.Add(CharacterBorderStrokeColorG);
            bytes.Add(CharacterBorderStrokeColorB);
            bytes.Add(CharacterBorderStrokeColorA);
            bytes.AddRange(BitConverter.GetBytes(I_40));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_44));
            bytes.AddRange(BitConverter.GetBytes(I_64));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_68));
            bytes.AddRange(BitConverter.GetBytes(I_76));
            bytes.AddRange(BitConverter.GetBytes(I_80));
            bytes.AddRange(BitConverter.GetBytes(I_84));
            bytes.AddRange(BitConverter.GetBytes(ShadowFarMin));
            bytes.AddRange(BitConverter.GetBytes(ShadowFarExponent));
            bytes.AddRange(BitConverter.GetBytes(ShadowHeightStart));
            bytes.AddRange(BitConverter.GetBytes(ShadowHeightRange));
            bytes.AddRange(BitConverter.GetBytes(ShadowAabbFloorHeight));
            bytes.AddRange(BitConverter.GetBytes(ShadowAabbCeilingHeight));
            bytes.AddRange(BitConverter.GetBytes(ShadowPerspective));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_116, 4));
            bytes.AddRange(BitConverter.GetBytes(F_132));
            bytes.AddRange(BitConverter.GetBytes(F_136));
            bytes.AddRange(BitConverter.GetBytes(F_140));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_144));
            bytes.AddRange(BitConverter.GetBytes(F_152));
            bytes.AddRange(BitConverter.GetBytes(F_156));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_160));
            bytes.AddRange(BitConverter.GetBytes(F_176));
            bytes.AddRange(BitConverter.GetBytes(F_180));
            bytes.AddRange(BitConverter.GetBytes(I_184));
            bytes.AddRange(BitConverter.GetBytes(F_188));
            bytes.AddRange(BitConverter.GetBytes(F_192));
            bytes.AddRange(BitConverter.GetBytes(F_196));
            bytes.AddRange(BitConverter.GetBytes(F_200));
            bytes.AddRange(BitConverter.GetBytes(ShadowMovingAabb));
            bytes.AddRange(BitConverter.GetBytes(I_208));
            bytes.AddRange(BitConverter.GetBytes(F_212));
            bytes.AddRange(BitConverter.GetBytes(F_216));
            bytes.AddRange(BitConverter.GetBytes(F_220));
            bytes.AddRange(BitConverter.GetBytes(F_224));
            bytes.AddRange(BitConverter.GetBytes(F_228));
            bytes.AddRange(BitConverter.GetBytes(F_232));
            bytes.AddRange(BitConverter.GetBytes(F_236));
            bytes.AddRange(BitConverter.GetBytes(F_240));
            bytes.AddRange(BitConverter.GetBytes(I_244));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_248));
            bytes.AddRange(BitConverter.GetBytes(F_272));
            bytes.AddRange(BitConverter.GetBytes(F_276));
            bytes.AddRange(BitConverter.GetBytes(F_280));
            bytes.AddRange(BitConverter.GetBytes(F_284));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_288));
            bytes.AddRange(BitConverter.GetBytes(F_328));
            bytes.AddRange(BitConverter.GetBytes(I_332));

            if (bytes.Count != EXPANDED_ENTRY_SIZE)
                throw new Exception("SPM_ExtendedEntry.Write: Incorrect size!");

            return bytes.ToArray();
        }

        internal static SPM_ExtendedEntry Read(byte[] bytes, int offset)
        {
            return new SPM_ExtendedEntry()
            {
                I_00 = BitConverter.ToUInt16(bytes, offset),
                I_02 = BitConverter.ToUInt16(bytes, offset + 2),
                I_04 = BitConverter.ToUInt16(bytes, offset + 4),
                I_06 = BitConverter.ToUInt16(bytes, offset + 6),
                I_08 = BitConverter.ToUInt16(bytes, offset + 8),
                I_10 = BitConverter.ToUInt16(bytes, offset + 10),
                I_12 = BitConverter.ToUInt16(bytes, offset + 12),
                I_14 = BitConverter.ToUInt16(bytes, offset + 14),
                CharacterBorderStrokeEnable = BitConverter.ToInt32(bytes, offset + 16),
                CharacterBorderStrokeFarWidth = BitConverter.ToSingle(bytes, offset + 20),
                CharacterBorderStrokeNearWidth = BitConverter.ToSingle(bytes, offset + 24),
                CharacterBorderStrokeStartDist = BitConverter.ToSingle(bytes, offset + 28),
                CharacterBorderStrokeEndDist = BitConverter.ToSingle(bytes, offset + 32),
                CharacterBorderStrokeColorR = bytes[offset + 36],
                CharacterBorderStrokeColorG = bytes[offset + 37],
                CharacterBorderStrokeColorB = bytes[offset + 38],
                CharacterBorderStrokeColorA = bytes[offset + 39],
                I_40 = BitConverter.ToInt32(bytes, offset + 40),
                F_44 = BitConverter_Ex.ToFloat32Array(bytes, offset + 44, 5),
                I_64 = BitConverter.ToInt32(bytes, offset + 64),
                F_68 = BitConverter_Ex.ToFloat32Array(bytes, offset + 68, 2),
                I_76 = BitConverter.ToInt32(bytes, offset + 76),
                I_80 = BitConverter.ToInt32(bytes, offset + 80),
                I_84 = BitConverter.ToInt32(bytes, offset + 84),
                ShadowFarMin = BitConverter.ToSingle(bytes, offset + 88),
                ShadowFarExponent = BitConverter.ToSingle(bytes, offset + 92),
                ShadowHeightStart = BitConverter.ToSingle(bytes, offset + 96),
                ShadowHeightRange = BitConverter.ToSingle(bytes, offset + 100),
                ShadowAabbFloorHeight = BitConverter.ToSingle(bytes, offset + 104),
                ShadowAabbCeilingHeight = BitConverter.ToSingle(bytes, offset + 108),
                ShadowPerspective = BitConverter.ToInt32(bytes, offset + 112),
                I_116 = BitConverter_Ex.ToInt32Array(bytes, offset + 116, 4),
                F_132 = BitConverter.ToSingle(bytes, offset + 132),
                F_136 = BitConverter.ToSingle(bytes, offset + 136),
                F_140 = BitConverter.ToSingle(bytes, offset + 140),
                I_144 = BitConverter_Ex.ToInt32Array(bytes, offset + 144, 2),
                F_152 = BitConverter.ToSingle(bytes, offset + 152),
                F_156 = BitConverter.ToSingle(bytes, offset + 156),
                I_160 = BitConverter_Ex.ToInt32Array(bytes, offset + 160, 4),
                F_176 = BitConverter.ToSingle(bytes, offset + 176),
                F_180 = BitConverter.ToSingle(bytes, offset + 180),
                I_184 = BitConverter.ToInt32(bytes, offset + 184),
                F_188 = BitConverter.ToSingle(bytes, offset + 188),
                F_192 = BitConverter.ToSingle(bytes, offset + 192),
                F_196 = BitConverter.ToSingle(bytes, offset + 196),
                F_200 = BitConverter.ToSingle(bytes, offset + 200),
                ShadowMovingAabb = BitConverter.ToInt32(bytes, offset + 204),
                I_208 = BitConverter.ToInt32(bytes, offset + 208),
                F_212 = BitConverter.ToSingle(bytes, offset + 212),
                F_216 = BitConverter.ToSingle(bytes, offset + 216),
                F_220 = BitConverter.ToSingle(bytes, offset + 220),
                F_224 = BitConverter.ToSingle(bytes, offset + 224),
                F_228 = BitConverter.ToSingle(bytes, offset + 228),
                F_232 = BitConverter.ToSingle(bytes, offset + 232),
                F_236 = BitConverter.ToSingle(bytes, offset + 236),
                F_240 = BitConverter.ToSingle(bytes, offset + 240),
                I_244 = BitConverter.ToInt32(bytes, offset + 244),
                I_248 = BitConverter_Ex.ToInt32Array(bytes, offset + 248, 6),
                F_272 = BitConverter.ToSingle(bytes, offset + 272),
                F_276 = BitConverter.ToSingle(bytes, offset + 276),
                F_280 = BitConverter.ToSingle(bytes, offset + 280),
                F_284 = BitConverter.ToSingle(bytes, offset + 284),
                I_288 = BitConverter_Ex.ToInt32Array(bytes, offset + 288, 10),
                F_328 = BitConverter.ToSingle(bytes, offset + 328),
                I_332 = BitConverter.ToInt32(bytes, offset + 332)
            };
        }
    }

    [YAXSerializeAs("BlendBlock")]
    public class DSC_BlendBlock
    {
        private const int SIGNATURE = 1129530403; //#DSC

        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        public int I_12 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Descriptor")]
        public List<DSC_Descriptor> Descriptors { get; set; } = new List<DSC_Descriptor>();

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();
            int entryCount = Descriptors?.Count ?? 0;

            bytes.AddRange(BitConverter.GetBytes(SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(entryCount));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(entryCount > 0 ? 24 : 0));
            bytes.AddRange(new byte[4]);

            int offsetStart = bytes.Count;
            bytes.AddRange(new byte[8 * entryCount]); //Offsets
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 8)]);

            //Write entries
            for (int i = 0; i < entryCount; i++)
            {
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - offsetStart), offsetStart + (8 * i));
                bytes.AddRange(Descriptors[i].Write());
            }

            return bytes.ToArray();
        }

        public static DSC_BlendBlock Read(byte[] bytes, int offset)
        {
            if (BitConverter.ToInt32(bytes, offset) != SIGNATURE)
                throw new Exception("DSC_BlendBlock.Read: #DSC signature not found");

            int entryCount = BitConverter.ToInt32(bytes, offset + 4);
            int entryOffset = BitConverter.ToInt32(bytes, offset + 16);

            DSC_BlendBlock dsc = new DSC_BlendBlock();
            dsc.I_08 = BitConverter.ToInt32(bytes, offset + 8);
            dsc.I_12 = BitConverter.ToInt32(bytes, offset + 12);

            for(int i = 0; i < entryCount; i++)
            {
                int absoluteEntryOffset = BitConverter.ToInt32(bytes, entryOffset + offset + (i * 8));
                if(absoluteEntryOffset > 0)
                    dsc.Descriptors.Add(DSC_Descriptor.Read(bytes, absoluteEntryOffset + entryOffset + offset));
            }

            return dsc;
        }
    }

    [YAXSerializeAs("Descriptor")]
    public class DSC_Descriptor
    {
        internal const int SIZE = 48;

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Layer")]
        public List<DSC_Layer> SubEntries { get; set; } = new List<DSC_Layer>();

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int count = SubEntries?.Count ?? 0;
            bytes.AddRange(StringEx.WriteFixedSizeString(Name, 16));
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(0));
            bytes.AddRange(BitConverter.GetBytes(DSC_Layer.SIZE)); //24, size
            bytes.AddRange(BitConverter.GetBytes(0));
            bytes.AddRange(BitConverter.GetBytes(count > 0 ? SIZE : 0)); //32, offset
            bytes.AddRange(BitConverter.GetBytes(0));
            bytes.AddRange(BitConverter.GetBytes(Index)); //40
            bytes.AddRange(BitConverter.GetBytes(0));

            for(int i = 0; i < count; i++)
            {
                bytes.AddRange(SubEntries[i].Write());
            }

            return bytes.ToArray();
        }

        public static DSC_Descriptor Read(byte[] bytes, int offset)
        {
            DSC_Descriptor entry = new DSC_Descriptor();
            entry.Name = StringEx.GetString(bytes, offset, false, maxSize: 16);
            entry.Index = BitConverter.ToInt32(bytes, offset + 40);

            int dataCount = BitConverter.ToInt32(bytes, offset + 16);
            int dataSize = BitConverter.ToInt32(bytes, offset + 24);
            int dataOffset = BitConverter.ToInt32(bytes, offset + 32);

            if (dataSize != DSC_Layer.SIZE)
                throw new Exception("DSC_Descriptor: unsupported version, or bad file.");

            for(int i = 0; i < dataCount; i++)
            {
                entry.SubEntries.Add(DSC_Layer.Read(bytes, dataOffset + offset + (i * DSC_Layer.SIZE)));
            }

            return entry;
        }
    }

    [YAXSerializeAs("Layer")]
    public class DSC_Layer
    {
        internal const int SIZE = 32;

        [CustomSerialize]
        public int SpmEntryID { get; set; }
        [CustomSerialize(isHex: true)]
        public int GroupFlagMask { get; set; }
        [CustomSerialize]
        public int Mode { get; set; }
        [CustomSerialize]
        public int I_12 { get; set; }
        [CustomSerialize]
        public int I_16 { get; set; }
        [CustomSerialize("Color", "R", isFloat: true)]
        public float ColorR { get; set; }
        [CustomSerialize("Color", "G", isFloat: true)]
        public float ColorG { get; set; }
        [CustomSerialize("Color", "B", isFloat: true)]
        public float ColorB { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(32);

            bytes.AddRange(BitConverter.GetBytes(SpmEntryID));
            bytes.AddRange(BitConverter.GetBytes(GroupFlagMask));
            bytes.AddRange(BitConverter.GetBytes(Mode));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(ColorR));
            bytes.AddRange(BitConverter.GetBytes(ColorG));
            bytes.AddRange(BitConverter.GetBytes(ColorB));

            if (bytes.Count != 32)
                throw new Exception("DSC_Layer.Write: Invalid size!");

            return bytes.ToArray();
        }

        public static DSC_Layer Read(byte[] bytes, int offset)
        {
            return new DSC_Layer()
            {
                SpmEntryID = BitConverter.ToInt32(bytes, offset),
                GroupFlagMask = BitConverter.ToInt32(bytes, offset + 4),
                Mode = BitConverter.ToInt32(bytes, offset + 8),
                I_12 = BitConverter.ToInt32(bytes, offset + 12),
                I_16 = BitConverter.ToInt32(bytes, offset + 16),
                ColorR = BitConverter.ToSingle(bytes, offset + 20),
                ColorG = BitConverter.ToSingle(bytes, offset + 24),
                ColorB = BitConverter.ToSingle(bytes, offset + 28),
            };
        }
    }
}
