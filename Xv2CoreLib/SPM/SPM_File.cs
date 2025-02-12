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

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "DSCSection")]
        public List<DSC_Section> DSC_Sections { get; set; } = new List<DSC_Section>();

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

            File.WriteAllBytes(saveLocation, spmFile.Write());
        }

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
                    spm.DSC_Sections.Add(DSC_Section.Read(bytes, dscSectionOffset + dscOffset));
            }

            return spm;
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();
            bool useDsc = false, useExtendedEntry = false;
            int entryCount = Entries?.Count ?? 0;
            int dscSections = DSC_Sections?.Count ?? 0;

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
                    bytes.AddRange(DSC_Sections[i].Write());
                }

            }

            return bytes.ToArray();
        }

        #endregion

    }

    [YAXSerializeAs("SPMEntry")]
    public class SPM_Entry
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [CustomSerialize]
        public ushort LensFlareEnabled { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_18 { get; set; }

        [CustomSerialize]
        public ushort GodRayAndSunHalo { get; set; }

        [CustomSerialize]
        public ushort I_30 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_32 { get; set; }

        [CustomSerialize("LightDir", "X")]
        public float LightDirX { get; set; }

        [CustomSerialize("LightDir", "Y")]
        public float LightDirY { get; set; }

        [CustomSerialize("LightDir", "Z")]
        public float LightDirZ { get; set; }

        [CustomSerialize("LightDir", "W")]
        public float LightDirW { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_64 { get; set; }

        [CustomSerialize]
        public int I_140 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_144 { get; set; }

        [CustomSerialize]
        public int I_236 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_240 { get; set; }

        [CustomSerialize("FogColor", "R")]
        public float FogColorR { get; set; }

        [CustomSerialize("FogColor", "G")]
        public float FogColorG { get; set; }

        [CustomSerialize("FogColor", "B")]
        public float FogColorB { get; set; }

        [CustomSerialize("FogColor", "A")]
        public float FogColorA { get; set; }

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
        public float F_364 { get; set; }

        [CustomSerialize]
        public float ShadowEndAngle { get; set; }

        [CustomSerialize]
        public float F_372 { get; set; }

        [CustomSerialize]
        public float ShadowStartAngle { get; set; }

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

        [CustomSerialize("BackgroundGlaraAdditiveColor", "R")]
        public float BackgroundGlareAdditiveColorR { get; set; }

        [CustomSerialize("BackgroundGlaraAdditiveColor", "G")]
        public float BackgroundGlareAdditiveColorG { get; set; }

        [CustomSerialize("BackgroundGlaraAdditiveColor", "B")]
        public float BackgroundGlareAdditiveColorB { get; set; }

        [CustomSerialize("BackgroundGlaraAdditiveColor", "A")]
        public float BackgroundGlareAdditiveColorA { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_416 { get; set; }

        [CustomSerialize]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int I_440 { get; set; }

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
        public int I_496 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_500 { get; set; }

        [CustomSerialize]
        public int I_512 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_516 { get; set; }

        [CustomSerialize]
        public int I_528 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_532 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_544 { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_560 { get; set; }

        [CustomSerialize("BackgroundMultiplyColor", "R")]
        public float BackgroundSpecificMultiplyColorR { get; set; }

        [CustomSerialize("BackgroundMultiplyColor", "G")]
        public float BackgroundSpecificMultiplyColorG { get; set; }

        [CustomSerialize("BackgroundMultiplyColor", "B")]
        public float BackgroundSpecificMultiplyColorB { get; set; }

        [CustomSerialize("BackgroundMultiplyColor", "A")]
        public float BackgroundSpecificMultiplyColorA { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_592 { get; set; }

        [CustomSerialize("ShadowDir", "X")]
        public float ShadowDirX { get; set; }
        [CustomSerialize("ShadowDir", "Y")]
        public float ShadowDirY { get; set; }
        [CustomSerialize("ShadowDir", "Z")]
        public float ShadowDirZ { get; set; }

        [CustomSerialize]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_620 { get; set; }

        [CustomSerialize("ShadowFilterColor", "R")]
        public float ShadowFilterColorR { get; set; }

        [CustomSerialize("ShadowFilterColor", "G")]
        public float ShadowFilterColorG { get; set; }

        [CustomSerialize("ShadowFilterColor", "B")]
        public float ShadowFilterColorB { get; set; }

        [CustomSerialize]
        public float F_684 { get; set; }

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
            List<byte> bytes = new List<byte>();

            //Check array sizes
            Assertion.AssertArraySize(I_18, 5, "SPMEntry", nameof(I_18));
            Assertion.AssertArraySize(F_32, 4, "SPMEntry", nameof(F_32));
            Assertion.AssertArraySize(F_64, 19, "SPMEntry", nameof(F_64));
            Assertion.AssertArraySize(F_144, 23, "SPMEntry", nameof(F_144));
            Assertion.AssertArraySize(F_240, 4, "SPMEntry", nameof(F_240));
            Assertion.AssertArraySize(F_416, 6, "SPMEntry", nameof(F_416));
            Assertion.AssertArraySize(F_480, 4, "SPMEntry", nameof(F_480));
            Assertion.AssertArraySize(F_500, 3, "SPMEntry", nameof(F_500));
            Assertion.AssertArraySize(F_516, 3, "SPMEntry", nameof(F_516));
            Assertion.AssertArraySize(F_532, 3, "SPMEntry", nameof(F_532));
            Assertion.AssertArraySize(I_544, 8, "SPMEntry", nameof(I_544));
            Assertion.AssertArraySize(F_560, 4, "SPMEntry", nameof(F_560));
            Assertion.AssertArraySize(F_592, 4, "SPMEntry", nameof(F_592));
            Assertion.AssertArraySize(F_620, 13, "SPMEntry", nameof(F_620));
            Assertion.AssertArraySize(F_720, 8, "SPMEntry", nameof(F_720));

            //Write main entry values
            bytes.AddRange(StringEx.WriteFixedSizeString(Name, 16));
            bytes.AddRange(BitConverter.GetBytes(LensFlareEnabled));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_18));
            bytes.AddRange(BitConverter.GetBytes(GodRayAndSunHalo));
            bytes.AddRange(BitConverter.GetBytes(I_30));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_32));
            bytes.AddRange(BitConverter.GetBytes(LightDirX));
            bytes.AddRange(BitConverter.GetBytes(LightDirY));
            bytes.AddRange(BitConverter.GetBytes(LightDirZ));
            bytes.AddRange(BitConverter.GetBytes(LightDirW));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_64));
            bytes.AddRange(BitConverter.GetBytes(I_140));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_144));
            bytes.AddRange(BitConverter.GetBytes(I_236));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_240));
            bytes.AddRange(BitConverter.GetBytes(FogColorR));
            bytes.AddRange(BitConverter.GetBytes(FogColorG));
            bytes.AddRange(BitConverter.GetBytes(FogColorB));
            bytes.AddRange(BitConverter.GetBytes(FogColorA));
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
            bytes.AddRange(BitConverter.GetBytes(F_364));
            bytes.AddRange(BitConverter.GetBytes(ShadowEndAngle));
            bytes.AddRange(BitConverter.GetBytes(F_372));
            bytes.AddRange(BitConverter.GetBytes(ShadowStartAngle));
            bytes.AddRange(BitConverter.GetBytes(SunFactor));
            bytes.AddRange(BitConverter.GetBytes(SunFactorHidden));
            bytes.AddRange(BitConverter.GetBytes(SunSize));
            bytes.AddRange(BitConverter.GetBytes(F_392));
            bytes.AddRange(BitConverter.GetBytes(F_396));
            bytes.AddRange(BitConverter.GetBytes(BackgroundGlareAdditiveColorR));
            bytes.AddRange(BitConverter.GetBytes(BackgroundGlareAdditiveColorG));
            bytes.AddRange(BitConverter.GetBytes(BackgroundGlareAdditiveColorB));
            bytes.AddRange(BitConverter.GetBytes(BackgroundGlareAdditiveColorA));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_416));
            bytes.AddRange(BitConverter.GetBytes(I_440));
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
            bytes.AddRange(BitConverter.GetBytes(I_496));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_500));
            bytes.AddRange(BitConverter.GetBytes(I_512));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_516));
            bytes.AddRange(BitConverter.GetBytes(I_528));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_532));
            bytes.AddRange(BitConverter_Ex.GetBytes(I_544));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_560));
            bytes.AddRange(BitConverter.GetBytes(BackgroundSpecificMultiplyColorR));
            bytes.AddRange(BitConverter.GetBytes(BackgroundSpecificMultiplyColorG));
            bytes.AddRange(BitConverter.GetBytes(BackgroundSpecificMultiplyColorB));
            bytes.AddRange(BitConverter.GetBytes(BackgroundSpecificMultiplyColorA));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_592));
            bytes.AddRange(BitConverter.GetBytes(ShadowDirX));
            bytes.AddRange(BitConverter.GetBytes(ShadowDirY));
            bytes.AddRange(BitConverter.GetBytes(ShadowDirZ));
            bytes.AddRange(BitConverter_Ex.GetBytes(F_620));
            bytes.AddRange(BitConverter.GetBytes(ShadowFilterColorR));
            bytes.AddRange(BitConverter.GetBytes(ShadowFilterColorG));
            bytes.AddRange(BitConverter.GetBytes(ShadowFilterColorB));
            bytes.AddRange(BitConverter.GetBytes(F_684));
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

            if (ExtendedData == null && useExtendedData)
                throw new Exception("ExtendedData is missing, but this version of SPM requires it.");

            bytes.AddRange(ExtendedData.Write());

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
                F_32 = BitConverter_Ex.ToFloat32Array(bytes, offset + 32, 4),
                LightDirX = BitConverter.ToSingle(bytes, offset + 48),
                LightDirY = BitConverter.ToSingle(bytes, offset + 52),
                LightDirZ = BitConverter.ToSingle(bytes, offset + 56),
                LightDirW = BitConverter.ToSingle(bytes, offset + 60),
                F_64 = BitConverter_Ex.ToFloat32Array(bytes, offset + 64, 19),
                I_140 = BitConverter.ToInt32(bytes, offset + 140),
                F_144 = BitConverter_Ex.ToFloat32Array(bytes, offset + 144, 23),
                I_236 = BitConverter.ToInt32(bytes, offset + 236),
                F_240 = BitConverter_Ex.ToFloat32Array(bytes, offset + 240, 4),
                FogColorR = BitConverter.ToSingle(bytes, offset + 256),
                FogColorG = BitConverter.ToSingle(bytes, offset + 260),
                FogColorB = BitConverter.ToSingle(bytes, offset + 264),
                FogColorA = BitConverter.ToSingle(bytes, offset + 268),
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
                F_364 = BitConverter.ToSingle(bytes, offset + 364),
                ShadowEndAngle = BitConverter.ToSingle(bytes, offset + 368),
                F_372 = BitConverter.ToSingle(bytes, offset + 372),
                ShadowStartAngle = BitConverter.ToSingle(bytes, offset + 376),
                SunFactor = BitConverter.ToSingle(bytes, offset + 380),
                SunFactorHidden = BitConverter.ToSingle(bytes, offset + 384),
                SunSize = BitConverter.ToSingle(bytes, offset + 388),
                F_392 = BitConverter.ToSingle(bytes, offset + 392),
                F_396 = BitConverter.ToSingle(bytes, offset + 396),
                BackgroundGlareAdditiveColorR = BitConverter.ToSingle(bytes, offset + 400),
                BackgroundGlareAdditiveColorG = BitConverter.ToSingle(bytes, offset + 404),
                BackgroundGlareAdditiveColorB = BitConverter.ToSingle(bytes, offset + 408),
                BackgroundGlareAdditiveColorA = BitConverter.ToSingle(bytes, offset + 412),
                F_416 = BitConverter_Ex.ToFloat32Array(bytes, offset + 416, 6),
                I_440 = BitConverter.ToInt32(bytes, offset + 440),
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
                I_496 = BitConverter.ToInt32(bytes, offset + 496),
                F_500 = BitConverter_Ex.ToFloat32Array(bytes, offset + 500, 3),
                I_512 = BitConverter.ToInt32(bytes, offset + 512),
                F_516 = BitConverter_Ex.ToFloat32Array(bytes, offset + 516, 3),
                I_528 = BitConverter.ToInt32(bytes, offset + 528),
                F_532 = BitConverter_Ex.ToFloat32Array(bytes, offset + 532, 3),
                I_544 = BitConverter_Ex.ToUInt16Array(bytes, offset + 544, 8),
                F_560 = BitConverter_Ex.ToFloat32Array(bytes, offset + 560, 4),
                BackgroundSpecificMultiplyColorR = BitConverter.ToSingle(bytes, offset + 576),
                BackgroundSpecificMultiplyColorG = BitConverter.ToSingle(bytes, offset + 580),
                BackgroundSpecificMultiplyColorB = BitConverter.ToSingle(bytes, offset + 584),
                BackgroundSpecificMultiplyColorA = BitConverter.ToSingle(bytes, offset + 588),
                F_592 = BitConverter_Ex.ToFloat32Array(bytes, offset + 592, 4),
                ShadowDirX = BitConverter.ToSingle(bytes, offset + 608),
                ShadowDirY = BitConverter.ToSingle(bytes, offset + 612),
                ShadowDirZ = BitConverter.ToSingle(bytes, offset + 616),
                F_620 = BitConverter_Ex.ToFloat32Array(bytes, offset + 620, 13),
                ShadowFilterColorR = BitConverter.ToSingle(bytes, offset + 672),
                ShadowFilterColorG = BitConverter.ToSingle(bytes, offset + 676),
                ShadowFilterColorB = BitConverter.ToSingle(bytes, offset + 680),
                F_684 = BitConverter.ToSingle(bytes, offset + 684),
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
        public float[] F_68 { get; set; }

        [CustomSerialize]
        public int I_76 { get; set; }

        public ulong[] UnknownData { get; set; }

        internal byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            //Check array sizes
            Assertion.AssertArraySize(F_44, 5, "ExtendedData", nameof(F_44));
            Assertion.AssertArraySize(F_68, 2, "ExtendedData", nameof(F_68));

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

            if (bytes.Count != 80)
                throw new Exception("SPM_ExtendedEntry.Write: Incorrect size!");

            //Write unknown values
            if(UnknownData != null)
            {
                Assertion.AssertArraySize(UnknownData, 32, "ExtendedData", nameof(UnknownData));
                bytes.AddRange(BitConverter_Ex.GetBytes(UnknownData));
            }
            else
            {
                //If missing, add null bytes.
                //I'm sure at least some of these values is junk data, anyway...
                bytes.AddRange(new byte[256]);
            }

            if (bytes.Count != 80 + 256)
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
                UnknownData = BitConverter_Ex.ToUInt64Array(bytes, offset + 80, 32)
            };
        }
    }

    [YAXSerializeAs("DSCSection")]
    public class DSC_Section
    {
        private const int SIGNATURE = 1129530403; //#DSC

        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        public int I_12 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "DSCEntry")]
        public List<DSC_Entry> Entries { get; set; } = new List<DSC_Entry>();

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();
            int entryCount = Entries?.Count ?? 0;

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
                bytes.AddRange(Entries[i].Write());
            }

            return bytes.ToArray();
        }

        public static DSC_Section Read(byte[] bytes, int offset)
        {
            if (BitConverter.ToInt32(bytes, offset) != SIGNATURE)
                throw new Exception("DSC_Section.Read: #DSC signature not found");

            int entryCount = BitConverter.ToInt32(bytes, offset + 4);
            int entryOffset = BitConverter.ToInt32(bytes, offset + 16);

            DSC_Section dsc = new DSC_Section();
            dsc.I_08 = BitConverter.ToInt32(bytes, offset + 8);
            dsc.I_12 = BitConverter.ToInt32(bytes, offset + 12);

            for(int i = 0; i < entryCount; i++)
            {
                int absoluteEntryOffset = BitConverter.ToInt32(bytes, entryOffset + offset + (i * 8));
                if(absoluteEntryOffset > 0)
                    dsc.Entries.Add(DSC_Entry.Read(bytes, absoluteEntryOffset + entryOffset + offset));
            }

            return dsc;
        }
    }

    [YAXSerializeAs("DSCEntry")]
    public class DSC_Entry
    {
        internal const int SIZE = 48;

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int Index { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Entry")]
        public List<DSC_Data> SubEntries { get; set; } = new List<DSC_Data>();

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int count = SubEntries?.Count ?? 0;
            bytes.AddRange(StringEx.WriteFixedSizeString(Name, 16));
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(0));
            bytes.AddRange(BitConverter.GetBytes(DSC_Data.SIZE)); //24, size
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

        public static DSC_Entry Read(byte[] bytes, int offset)
        {
            DSC_Entry entry = new DSC_Entry();
            entry.Name = StringEx.GetString(bytes, offset, false, maxSize: 16);
            entry.Index = BitConverter.ToInt32(bytes, offset + 40);

            int dataCount = BitConverter.ToInt32(bytes, offset + 16);
            int dataSize = BitConverter.ToInt32(bytes, offset + 24);
            int dataOffset = BitConverter.ToInt32(bytes, offset + 32);

            if (dataSize != DSC_Data.SIZE)
                throw new Exception("DSC_Entry: unsupported version, or bad file.");

            for(int i = 0; i < dataCount; i++)
            {
                entry.SubEntries.Add(DSC_Data.Read(bytes, dataOffset + offset + (i * DSC_Data.SIZE)));
            }

            return entry;
        }
    }

    [YAXSerializeAs("Entry")]
    public class DSC_Data
    {
        internal const int SIZE = 32;

        [CustomSerialize]
        public int I_00 { get; set; }
        [CustomSerialize]
        public int I_04 { get; set; }
        [CustomSerialize]
        public int I_08 { get; set; }
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

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(I_16));
            bytes.AddRange(BitConverter.GetBytes(ColorR));
            bytes.AddRange(BitConverter.GetBytes(ColorG));
            bytes.AddRange(BitConverter.GetBytes(ColorB));

            if (bytes.Count != 32)
                throw new Exception("DSC_Data.Write: Invalid size!");

            return bytes.ToArray();
        }

        public static DSC_Data Read(byte[] bytes, int offset)
        {
            return new DSC_Data()
            {
                I_00 = BitConverter.ToInt32(bytes, offset),
                I_04 = BitConverter.ToInt32(bytes, offset + 4),
                I_08 = BitConverter.ToInt32(bytes, offset + 8),
                I_12 = BitConverter.ToInt32(bytes, offset + 12),
                I_16 = BitConverter.ToInt32(bytes, offset + 16),
                ColorR = BitConverter.ToSingle(bytes, offset + 20),
                ColorG = BitConverter.ToSingle(bytes, offset + 24),
                ColorB = BitConverter.ToSingle(bytes, offset + 28),
            };
        }
    }
}
