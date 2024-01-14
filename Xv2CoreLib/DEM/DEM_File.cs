using System;
using System.Collections.Generic;
using YAXLib;

namespace Xv2CoreLib.DEM
{
    [YAXSerializeAs("DEM")]
    public class DEM_File
    {
        [YAXDontSerialize]
        public const int DEM_SIGNATURE = 1296385059;
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public int I_08 { get; set; }
        [YAXSerializeAs("DemoSettings")]
        public DemoSettings Settings { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Cut")]
        [YAXSerializeAs("Cuts")]
        public List<Section2Entry> Section2Entries { get; set; }
        [YAXDontSerializeIfNull]
        public List<DEM_UnknownValues> DEM_UnkValues { get; set; }
    }

    [YAXSerializeAs("DemoSettings")]
    public class DemoSettings
    {
        [YAXAttributeFor("Camera")]
        [YAXSerializeAs("File")]
        public string Str_00 { get; set; }
        [YAXAttributeFor("SE")]
        [YAXSerializeAs("File")]
        public string Str_16 { get; set; }
        [YAXAttributeFor("VOX")]
        [YAXSerializeAs("File")]
        public string Str_24 { get; set; }
        [YAXAttributeFor("BGM")]
        [YAXSerializeAs("File")]
        public string Str_32 { get; set; }
        [YAXAttributeFor("Str_40")]
        [YAXSerializeAs("value")]
        public string Str_40 { get; set; }
        [YAXAttributeFor("EEPK")]
        [YAXSerializeAs("File")]
        public string Str_48 { get; set; }
        [YAXAttributeFor("EMB")]
        [YAXSerializeAs("File")]
        public string Str_56 { get; set; }
        [YAXAttributeFor("Movies")]
        [YAXSerializeAs("value")]
        public string Str_64 { get; set; }
        [YAXAttributeFor("Stage0")]
        [YAXSerializeAs("ID")]
        public string Str_08 { get; set; }
        [YAXAttributeFor("Stage1")]
        [YAXSerializeAs("ID")]
        public string Str_72 { get; set; }
        [YAXAttributeFor("Stage2")]
        [YAXSerializeAs("ID")]
        public string Str_80 { get; set; }
        [YAXAttributeFor("Stage3")]
        [YAXSerializeAs("ID")]
        public string Str_88 { get; set; }
        [YAXAttributeFor("Stage4")]
        [YAXSerializeAs("ID")]
        public string Str_96 { get; set; }
        [YAXAttributeFor("EMS")]
        [YAXSerializeAs("File")]
        public string Str_104 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Actors")]
        public List<Character> Characters { get; set; }
    }

    [YAXSerializeAs("Actor")]
    public class Character
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Character")]
        public string Str_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("EanFile")]
        public string Str_16 { get; set; }
    }
    
    [YAXSerializeAs("Cut")]
    public class Section2Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public int I_00 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Event")]
        public List<DEM_Type> SubEntries { get; set; }
        
    }

    [YAXSerializeAs("Event")]
    public class DEM_Type
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Time")]
        public int I_00 { get; set; }
        [YAXDontSerialize]
        public int Offset { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public DemoDataTypes I_04 { get; set; } //2 x uint16

        //Code
        [YAXDontSerialize]
        public int PointerOffset = 0;
        [YAXDontSerialize]
        public List<int> ValueOffsets = new List<int>();
        [YAXDontSerialize]
        public int ValueCount = 0;

        //Types
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ScreenFade")]
        public Type0_2_7 Type0_2_7 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("LightDir")]
        public Type0_3_8 Type0_3_8 { get; set; }
        [YAXDontSerializeIfNull]
        public Type0_16_1 Type0_16_1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("AnimationSmall")]
        public Type1_0_9 Type1_0_9 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Animation")]
        public Type1_0_10 Type1_0_10 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ActorVisibility")]
        public Type1_3_2 Type1_3_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ActorDamage")]
        public Type1_9_5 Type1_9_5 { get; set; }
        [YAXDontSerializeIfNull]
        public Type1_10_8 Type1_10_8 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ScdForce")]
        public Type1_17_6 Type1_17_6 { get; set; }
        [YAXDontSerializeIfNull]
        public Type1_19_3 Type1_19_3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Camera")]
        public Type2_0_1 Type2_0_1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SetTargetLook")]
        public Type2_7_8 Type2_7_8 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Effect")]
        public Type4_0_12 Type4_0_12 { get; set; } 
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("PostEffect")]
        public Type4_1_8 Type4_1_8 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Sound")]
        public Type5_0_3 Type5_0_3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Music")]
        public Type5_2_3 Type5_2_3 { get; set; }
        [YAXDontSerializeIfNull]
        public Type5_3_2 Type5_3_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("DistanceFocus")]
        public Type6_16_6 Type6_16_6 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("YearDisplay")]
        public Type9_0_2 Type9_0_2 { get; set; }
        [YAXSerializeAs("Subtitle")]
        [YAXDontSerializeIfNull]
        public Type9_1_5 Type9_1_5 { get; set; }

        //2nd batch
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("TextureSwitch")]
        public Type0_1_6 Type0_1_6 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Position1")]
        public Type1_1_5 Type1_1_5 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("RotateY_1")]
        public Type1_2_3 Type1_2_3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Transformation")]
        public Type1_4_2 Type1_4_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SetEyes")]
        public Type1_8_6 Type1_8_6 { get; set; }
        [YAXSerializeAs("EyeColor")]
        [YAXDontSerializeIfNull]
        public Type1_13_10 Type1_13_10 { get; set; }
        [YAXDontSerializeIfNull]
        public Type2_7_5 Type2_7_5 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ToggleMap")]
        public Type3_0_1 Type3_0_1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ChangeMap")]
        public Type3_1_1 Type3_1_1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("AuraEffect")]
        public Type4_2_3 Type4_2_3 { get; set; }
        [YAXDontSerializeIfNull]
        public Type4_3_5 Type4_3_5 { get; set; }
        [YAXDontSerializeIfNull]
        public Type4_4_1 Type4_4_1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SoundSmall")]
        public Type5_0_2 Type5_0_2 { get; set; }
        [YAXDontSerializeIfNull]
        public Type5_1_2 Type5_1_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SetSpm")]
        public Type6_0_1 Type6_0_1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SetColorFilter")]
        public Type6_17_19 Type6_17_19 { get; set; }
        [YAXDontSerializeIfNull]
        public Type6_18_7 Type6_18_7 { get; set; }
        [YAXDontSerializeIfNull]
        public Type6_19_15 Type6_19_15 { get; set; }
        [YAXDontSerializeIfNull]
        public Type6_20_2 Type6_20_2 { get; set; }

        //3rd batch
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("StartTransparent")]
        public Type1_6_4 Type1_6_4 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("StopTransparent")]
        public Type1_7_1 Type1_7_1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ShadowVisible")]
        public Type1_11_2 Type1_11_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ResetEyesColor")]
        public Type1_14_1 Type1_14_1 { get; set; }
        [YAXDontSerializeIfNull]
        public Type1_16_2 Type1_16_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ScdWind")]
        public Type1_20_12 Type1_20_12 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Picture")]
        public Type7_0_5 Type7_0_5 { get; set; }

        //4th batch
        [YAXDontSerializeIfNull]
        public Type0_19_1 Type0_19_1 { get; set; }
        [YAXDontSerializeIfNull]
        public Type0_20_2 Type0_20_2 { get; set; }
        [YAXDontSerializeIfNull]
        public Type0_21_2 Type0_21_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Position2")]
        public Type1_1_9 Type1_1_9 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("RotateY_2")]
        public Type1_2_5 Type1_2_5 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("CancelAnimation")]
        public Type1_12_2 Type1_12_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Scale")]
        public Type1_26_2 Type1_26_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SetHologramMaterial")]
        public Type1_27_2 Type1_27_2 { get; set; }
        [YAXDontSerializeIfNull]
        public Type2_6_3 Type2_6_3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SetNearClip")]
        public Type2_9_2 Type2_9_2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SetFarClip")]
        public Type2_10_2 Type2_10_2 { get; set; }
        [YAXDontSerializeIfNull]
        public Type2_11_1 Type2_11_1 { get; set; }
        [YAXDontSerializeIfNull]
        public Type3_2_1 Type3_2_1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ToggleFRZShip")]
        public Type3_3_1 Type3_3_1 { get; set; }
        [YAXDontSerializeIfNull]
        public Type3_4_2 Type3_4_2 { get; set; }

        //New
        [YAXDontSerializeIfNull]
        public Type5_4_3 Type5_4_3 { get; set; } //Added in 1.14

        public static string GetTypeFlag(int I_04, int I_06, int count)
        {
            return String.Format("{0}_{1}_{2}", I_04, I_06, count);
        }

        public enum DemoDataTypes
        {
            TextureSwitch, //0_1_6
            ScreenFade, //0_2_7
            LightDir, //0_3_8 LightDir? (I_1 = Actor, F_2 - F_4 = LightDir1, F_5 - F_7 = LightDir2, I_8 = Transition time)... all guesses but values line up
            Type0_16_0,
            Type0_16_1,
            Type0_17_0,
            Type0_19_1,
            Type0_20_2,
            Type0_21_2,
            AnimationSmall, //1_0_9
            Animation, //1_0_10
            Position1, //1_1_5
            Position2, //1_5_9
            RotateY_1, //1_2_3
            RotateY_2, // 1_2_5
            ActorVisibility, //1_3_2
            Transformation, //1_4_2
            StartTransparent, //1_6_4
            StopTransparent, //1_7_1
            SetEyes, //1_8_6
            ActorDamage, //1_9_5
            Type1_10_8, 
            ShadowVisible, //1_11_2
            CancelAnimation, //1_12_2
            EyeColor, //1_13_10
            ResetEyesColor,//1_14_1
            Type1_16_2,
            ScdForce, //1_17_6
            Type1_19_3,
            ScdWind, //1_20_12
            Scale, //1_26_2
            SetHologramMaterial, //1_27_2
            Camera, //2_0_1
            Type2_6_3,
            Type2_7_5,
            SetTargetLook, //2_7_8
            SetNearClip, //2_9_2
            SetFarClip, //2_10_2
            Type2_11_1,
            ToggleMap, //3_0_1
            ChangeMap, //3_1_1
            Type3_2_1,
            ToggleFRZShip, //3_3_1
            Type3_4_2,
            Effect, //4_0_12
            PostEffect, //4_1_8
            AuraEffect, //4_2_3
            Type4_3_5, //SetDoF?
            Type4_4_1,
            SoundSmall, //5_0_2
            Sound, //5_0_3
            Type5_1_2, //5_1_2
            Music, //5_2_3
            Type5_3_2, //5_3_2
            Type5_4_3, //5_4_3
            SetSpm, //6_0_1
            DistanceFocus, //6_16_6
            SetColorFilter, //6_17_19
            Type6_18_7,
            Type6_19_15,
            Type6_20_2,
            Picture, //7_0_5
            YearDisplay, //9_0_2
            Subtitle, //9_1_5
            PlaySprite, //9_8_0
        }

        public static DemoDataTypes GetDemoDataType(int type1, int type2, int count)
        {
            //If I name any of the enums, then deal with that in this switch statement (as the generic parsing wont work)
            switch (String.Format("{0}_{1}_{2}", type1, type2, count))
            {
                case "0_1_6":
                    return DemoDataTypes.TextureSwitch;
                case "0_3_8":
                    return DemoDataTypes.LightDir;
                case "1_0_9":
                    return DemoDataTypes.AnimationSmall;
                case "1_0_10":
                    return DemoDataTypes.Animation;
                case "1_3_2":
                    return DemoDataTypes.ActorVisibility;
                case "1_1_5":
                    return DemoDataTypes.Position1;
                case "1_1_9":
                    return DemoDataTypes.Position2;
                case "1_2_3":
                    return DemoDataTypes.RotateY_1;
                case "1_2_5":
                    return DemoDataTypes.RotateY_2;
                case "1_6_4":
                    return DemoDataTypes.StartTransparent;
                case "1_7_1":
                    return DemoDataTypes.StopTransparent;
                case "1_8_6":
                    return DemoDataTypes.SetEyes;
                case "1_11_2":
                    return DemoDataTypes.ShadowVisible;
                case "1_12_2":
                    return DemoDataTypes.CancelAnimation;
                case "1_13_10":
                    return DemoDataTypes.EyeColor;
                case "1_17_6":
                    return DemoDataTypes.ScdForce;
                case "1_20_12":
                    return DemoDataTypes.ScdWind;
                case "1_26_2":
                    return DemoDataTypes.Scale;
                case "1_27_2":
                    return DemoDataTypes.SetHologramMaterial;
                case "1_14_1":
                    return DemoDataTypes.ResetEyesColor;
                case "2_0_1":
                    return DemoDataTypes.Camera;
                case "2_7_8":
                    return DemoDataTypes.SetTargetLook;
                case "2_9_2":
                    return DemoDataTypes.SetNearClip;
                case "2_10_2":
                    return DemoDataTypes.SetFarClip;
                case "3_0_1":
                    return DemoDataTypes.ToggleMap;
                case "3_1_1":
                    return DemoDataTypes.ChangeMap;
                case "3_3_1":
                    return DemoDataTypes.ToggleFRZShip;
                case "4_0_12":
                    return DemoDataTypes.Effect;
                case "4_1_8":
                    return DemoDataTypes.PostEffect;
                case "4_2_3":
                    return DemoDataTypes.AuraEffect;
                case "5_0_2":
                    return DemoDataTypes.SoundSmall;
                case "5_0_3":
                    return DemoDataTypes.Sound;
                case "5_2_3":
                    return DemoDataTypes.Music;
                case "1_4_2":
                    return DemoDataTypes.Transformation;
                case "6_0_1":
                    return DemoDataTypes.SetSpm;
                case "6_17_19":
                    return DemoDataTypes.SetColorFilter;
                case "9_0_2":
                    return DemoDataTypes.YearDisplay;
                case "1_9_5":
                    return DemoDataTypes.ActorDamage;
                case "6_16_6":
                    return DemoDataTypes.DistanceFocus;
                case "9_1_5":
                    return DemoDataTypes.Subtitle;
                case "0_2_7":
                    return DemoDataTypes.ScreenFade;
                case "7_0_5":
                    return DemoDataTypes.Picture;
                case "9_8_0":
                    return DemoDataTypes.PlaySprite;
            }

            //Generic type parsing
            return (DemoDataTypes)Enum.Parse(typeof(DemoDataTypes), String.Format("Type{0}_{1}_{2}", type1, type2, count));

        }

        public DemoDataTypes GetDemoDataType()
        {
            var values = GetDemoType();
            return DEM_Type.GetDemoDataType(values[0], values[1], values[2]);
        }


        /// <summary>
        /// 0 = type1, 1 = type2, 2 = count
        /// </summary>
        /// <returns></returns>
        public int[] GetDemoType()
        {
            switch (I_04)
            {
                case DemoDataTypes.TextureSwitch:
                    return new int[3] { 0, 1, 6 };
                case DemoDataTypes.LightDir:
                    return new int[3] { 0, 3, 8 };
                case DemoDataTypes.AnimationSmall:
                    return new int[3] { 1, 0, 9 };
                case DemoDataTypes.Animation:
                    return new int[3] { 1, 0, 10 };
                case DemoDataTypes.ActorVisibility:
                    return new int[3] { 1, 3, 2 };
                case DemoDataTypes.Position1:
                    return new int[3] { 1, 1, 5 };
                case DemoDataTypes.Position2:
                    return new int[3] { 1, 1, 9 };
                case DemoDataTypes.RotateY_1:
                    return new int[3] { 1, 2, 3 };
                case DemoDataTypes.RotateY_2:
                    return new int[3] { 1, 2, 5 };
                case DemoDataTypes.StartTransparent:
                    return new int[3] { 1, 6, 4 };
                case DemoDataTypes.StopTransparent:
                    return new int[3] { 1, 7, 1 };
                case DemoDataTypes.SetEyes:
                    return new int[3] { 1, 8, 6 };
                case DemoDataTypes.ShadowVisible:
                    return new int[3] { 1, 11, 2 };
                case DemoDataTypes.CancelAnimation:
                    return new int[3] { 1, 12, 2 };
                case DemoDataTypes.EyeColor:
                    return new int[3] { 1, 13, 10 };
                case DemoDataTypes.ScdForce:
                    return new int[3] { 1, 17, 6 };
                case DemoDataTypes.ScdWind:
                    return new int[3] { 1, 20, 12 };
                case DemoDataTypes.ResetEyesColor:
                    return new int[3] { 1, 14, 1 };
                case DemoDataTypes.Scale:
                    return new int[3] { 1, 26, 2 };
                case DemoDataTypes.SetHologramMaterial:
                    return new int[3] { 1, 27, 2 };
                case DemoDataTypes.Camera:
                    return new int[3] { 2, 0, 1 };
                case DemoDataTypes.SetTargetLook:
                    return new int[3] { 2, 7, 8 };
                case DemoDataTypes.SetNearClip:
                    return new int[3] { 2, 9, 2 };
                case DemoDataTypes.SetFarClip:
                    return new int[3] { 2, 10, 2 };
                case DemoDataTypes.ToggleMap:
                    return new int[3] { 3, 0, 1 };
                case DemoDataTypes.ChangeMap:
                    return new int[3] { 3, 1, 1 };
                case DemoDataTypes.ToggleFRZShip:
                    return new int[3] { 3, 3, 1 };
                case DemoDataTypes.Effect:
                    return new int[3] { 4, 0, 12 };
                case DemoDataTypes.PostEffect:
                    return new int[3] { 4, 1, 8 };
                case DemoDataTypes.AuraEffect:
                    return new int[3] { 4, 2, 3 };
                case DemoDataTypes.SoundSmall:
                    return new int[3] { 5, 0, 2 };
                case DemoDataTypes.Sound:
                    return new int[3] { 5, 0, 3 };
                case DemoDataTypes.Music:
                    return new int[3] { 5, 2, 3 };
                case DemoDataTypes.Transformation:
                    return new int[3] { 1, 4, 2 };
                case DemoDataTypes.YearDisplay:
                    return new int[3] { 9, 0, 2 };
                case DemoDataTypes.SetSpm:
                    return new int[3] { 6, 0, 1 };
                case DemoDataTypes.SetColorFilter:
                    return new int[3] { 6, 17, 19 };
                case DemoDataTypes.ActorDamage:
                    return new int[3] { 1, 9, 5 };
                case DemoDataTypes.DistanceFocus:
                    return new int[3] { 6, 16, 6 };
                case DemoDataTypes.Subtitle:
                    return new int[3] { 9, 1, 5 };
                case DemoDataTypes.ScreenFade:
                    return new int[3] { 0, 2, 7 };
                case DemoDataTypes.Picture:
                    return new int[3] { 7, 0, 5 };
                case DemoDataTypes.PlaySprite:
                    return new int[3] { 9, 8, 0 };
                default:
                    string[] types = I_04.ToString().Remove(0, 4).Split('_');
                    return new int[3] { int.Parse(types[0]), int.Parse(types[1]), int.Parse(types[2]) };
            }
        }
    }

    public class DEM_UnknownValues
    {
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("uint16s")]
        public ushort[] Values { get; set; } //size 40
    }

    //Helper
    public class ValueWriter
    {
        private int index = 0;
        private List<int> ValueOffsets = null;
        public List<byte> bytes { get; private set; } = null;

        public ValueWriter(List<int> valueOffsets, List<byte> _bytes)
        {
            bytes = _bytes;
            ValueOffsets = valueOffsets;
        }

        private void IndexValidation()
        {
            if (index > (ValueOffsets.Count - 1)) throw new IndexOutOfRangeException("ValueWriter index out of range.");
        }

        public void WriteValue(int value)
        {
            IndexValidation();
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), ValueOffsets[index]);
            bytes.AddRange(BitConverter.GetBytes(value));
            index++;
        }

        public void WriteValue(float value)
        {
            IndexValidation();
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), ValueOffsets[index]);
            bytes.AddRange(BitConverter.GetBytes(value));
            index++;
        }

        public void WriteValue(string value, int minLength = 4)
        {
            IndexValidation();
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), ValueOffsets[index]);
            var strBytes = Utils.GetStringBytes(value, minLength);
            strBytes = PadFile(strBytes); //Pad string bytes to be in blocks of 4 bytes
            bytes.AddRange(strBytes);
            index++;
        }

        public static List<byte> PadFile(List<byte> bytes)
        {
            float fileSizeFloat = bytes.Count;
            while ((fileSizeFloat / 4) != Math.Floor(fileSizeFloat / 4))
            {
                fileSizeFloat++;
                bytes.Add(0);
            }

            return bytes;
        }

    }

    public class ValueReader
    {
        private int ValueCount { get; set; }
        private int Offset { get; set; }
        private int Index = 0;
        private byte[] rawBytes = null;
        private List<byte> bytes = null;

        public ValueReader(byte[] _rawBytes, List<byte> _bytes, int offset, int valueCount)
        {
            bytes = _bytes;
            rawBytes = _rawBytes;
            Offset = offset;
            ValueCount = valueCount;
        }

        public int ReadInt()
        {
            int value = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, (8 * Index) + Offset));
            Index++;
            return value;
        }

        public float ReadFloat()
        {
            float value = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, (8 * Index) + Offset));
            Index++;
            return value;
        }

        public string ReadString()
        {
            string value = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, (8 * Index) + Offset));
            Index++;
            return value;
        }
    }


    //Types
    public class Type0_1_6
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("DytBase")]
        [YAXSerializeAs("Index")]
        public int I_2 { get; set; }
        [YAXAttributeFor("DytReplace")]
        [YAXSerializeAs("Index")]
        public int I_3 { get; set; }
        [YAXAttributeFor("DytBase")]
        [YAXSerializeAs("Opacity")]
        [YAXFormat("0.0#######")]
        public float F_4 { get; set; }
        [YAXAttributeFor("DytReplace")]
        [YAXSerializeAs("Opacity")]
        [YAXFormat("0.0#######")]
        public float F_5 { get; set; }
        [YAXAttributeFor("SwapDuration")]
        [YAXSerializeAs("value")]
        public int I_6 { get; set; }

        public static Type0_1_6 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type0_1_6()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(F_4);
            writer.WriteValue(F_5);
            writer.WriteValue(I_6);

            return writer.bytes;
        }
    }

    public class Type0_2_7
    {
        public enum FadeType
        {
            FadeFrom = 0,
            FadeTo = 1
        }

        [YAXAttributeFor("FadeType")]
        [YAXSerializeAs("value")]
        public FadeType I_1 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("R")]
        public int I_2 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("G")]
        public int I_3 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("B")]
        public int I_4 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("A")]
        public int I_5 { get; set; }
        [YAXAttributeFor("I_6")]
        [YAXSerializeAs("value")]
        public int I_6 { get; set; }
        [YAXAttributeFor("Length")]
        [YAXSerializeAs("value")]
        public int I_7 { get; set; }

        public static Type0_2_7 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type0_2_7()
            {
                I_1 = (FadeType)BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue((int)I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);
            writer.WriteValue(I_6);
            writer.WriteValue(I_7);

            return writer.bytes;
        }
    }

    public class Type0_3_8
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("X_From")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }
        [YAXAttributeFor("Y_From")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_3 { get; set; }
        [YAXAttributeFor("Z_From")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_4 { get; set; }
        [YAXAttributeFor("X_To")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_5 { get; set; }
        [YAXAttributeFor("Y_To")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_6 { get; set; }
        [YAXAttributeFor("Z_To")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_7 { get; set; }
        [YAXAttributeFor("SwapDuration")]
        [YAXSerializeAs("value")]
        public int I_8 { get; set; }

        public static Type0_3_8 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type0_3_8()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                F_6 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                F_7 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                I_8 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);
            writer.WriteValue(F_3);
            writer.WriteValue(F_4);
            writer.WriteValue(F_5);
            writer.WriteValue(F_6);
            writer.WriteValue(F_7);
            writer.WriteValue(I_8);

            return writer.bytes;
        }
    }

    public class Type0_16_1
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }

        public static Type0_16_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type0_16_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }


    }

    public class Type0_19_1
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }

        public static Type0_19_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type0_19_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }
    }

    public class Type0_20_2
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type0_20_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type0_20_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }
    }

    public class Type0_21_2
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type0_21_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type0_21_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }
    }

    public class Type1_0_9
    {
        public enum EanTypeEnum
        {
            Demo = 0,
            FaceBase = 2,
            FaceForehead = 3,
            Character = 5
        }

        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("EAN")]
        [YAXSerializeAs("value")]
        public EanTypeEnum I_2 { get; set; }
        [YAXAttributeFor("Animation")]
        [YAXSerializeAs("Name")]
        public string Str_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("F_5")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_5 { get; set; }
        [YAXAttributeFor("I_6")]
        [YAXSerializeAs("value")]
        public int I_6 { get; set; }
        [YAXAttributeFor("I_7")]
        [YAXSerializeAs("value")]
        public int I_7 { get; set; }
        [YAXAttributeFor("StartBlendWeight")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_8 { get; set; }
        [YAXAttributeFor("BlendWeightFrameStep")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_9 { get; set; }

        public static Type1_0_9 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_0_9()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = (EanTypeEnum)BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                Str_3 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                F_8 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
                F_9 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 64))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue((int)I_2);
            writer.WriteValue(Str_3);
            writer.WriteValue(I_4);
            writer.WriteValue(F_5);
            writer.WriteValue(I_6);
            writer.WriteValue(I_7);
            writer.WriteValue(F_8);
            writer.WriteValue(F_9);

            return writer.bytes;
        }

    }

    public class Type1_0_10
    {
        public enum EanTypeEnum
        {
            Demo = 0,
            FaceBase = 2,
            FaceForehead = 3,
            Character = 5
        }

        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("EAN")]
        [YAXSerializeAs("value")]
        public EanTypeEnum I_2 { get; set; }
        [YAXAttributeFor("Animation")]
        [YAXSerializeAs("Name")]
        public string Str_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("TimeScale")]
        [YAXSerializeAs("Amount")]
        [YAXFormat("0.0#########")]
        public float F_5 { get; set; }
        [YAXAttributeFor("I_6")]
        [YAXSerializeAs("value")]
        public int I_6 { get; set; }
        [YAXAttributeFor("I_7")]
        [YAXSerializeAs("value")]
        public int I_7 { get; set; }
        [YAXAttributeFor("StartBlendWeight")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_8 { get; set; }
        [YAXAttributeFor("BlendWeightFrameStep")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_9 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public int I_10 { get; set; }

        public static Type1_0_10 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_0_10()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = (EanTypeEnum)BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                Str_3 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                F_8 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
                F_9 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 64)),
                I_10 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 72)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue((int)I_2);
            writer.WriteValue(Str_3);
            writer.WriteValue(I_4);
            writer.WriteValue(F_5);
            writer.WriteValue(I_6);
            writer.WriteValue(I_7);
            writer.WriteValue(F_8);
            writer.WriteValue(F_9);
            writer.WriteValue(I_10);

            return writer.bytes;
        }

    }

    public class Type1_1_9
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("F_2")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("F_4")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }
        [YAXAttributeFor("I_6")]
        [YAXSerializeAs("value")]
        public int I_6 { get; set; }
        [YAXAttributeFor("I_7")]
        [YAXSerializeAs("value")]
        public int I_7 { get; set; }
        [YAXAttributeFor("I_8")]
        [YAXSerializeAs("value")]
        public int I_8 { get; set; }
        [YAXAttributeFor("I_9")]
        [YAXSerializeAs("value")]
        public int I_9 { get; set; }

        public static Type1_1_9 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_1_9()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                I_8 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
                I_9 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 64))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);
            writer.WriteValue(I_3);
            writer.WriteValue(F_4);
            writer.WriteValue(I_5);
            writer.WriteValue(I_6);
            writer.WriteValue(I_7);
            writer.WriteValue(I_8);
            writer.WriteValue(I_9);

            return writer.bytes;
        }

    }

    public class Type1_2_5
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("Angle")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }

        public static Type1_2_5 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_2_5()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);

            return writer.bytes;
        }
    }

    public class Type1_3_2
    {
        public enum Visibility
        {
            Invisible = 0,
            Visible = 1
        }

        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("Visibility")]
        [YAXSerializeAs("value")]
        public Visibility I_2 { get; set; } //Int

        public static Type1_3_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_3_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = (Visibility)BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue((int)I_2);

            return writer.bytes;
        }

    }

    public class Type1_1_5
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }

        public static Type1_1_5 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_1_5()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);

            return writer.bytes;
        }
    }

    public class Type1_4_2
    {
        [YAXAttributeFor("Actor_ID")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("Transformation")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type1_4_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_4_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }
    }

    public class Type1_2_3
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("Angle")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }

        public static Type1_2_3 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_2_3()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);
            writer.WriteValue(I_3);

            return writer.bytes;
        }
    }

    public class Type1_6_4
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("F_3")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }

        public static Type1_6_4 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_6_4()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(F_3);
            writer.WriteValue(I_4);

            return writer.bytes;
        }
    }

    public class Type1_7_1
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }

        public static Type1_7_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_7_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }
    }

    public class Type1_8_6
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }
        [YAXAttributeFor("I_6")]
        [YAXSerializeAs("value")]
        public int I_6 { get; set; }

        public static Type1_8_6 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_8_6()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);
            writer.WriteValue(I_6);

            return writer.bytes;
        }
    }

    public class Type1_9_5
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("F_3")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_3 { get; set; }
        [YAXAttributeFor("F_4")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }

        public static Type1_9_5 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_9_5()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(F_3);
            writer.WriteValue(F_4);
            writer.WriteValue(I_5);

            return writer.bytes;
        }
    }

    public class Type1_10_8
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }
        [YAXAttributeFor("I_6")]
        [YAXSerializeAs("value")]
        public int I_6 { get; set; }
        [YAXAttributeFor("I_7")]
        [YAXSerializeAs("value")]
        public int I_7 { get; set; }
        [YAXAttributeFor("I_8")]
        [YAXSerializeAs("value")]
        public int I_8 { get; set; }

        public static Type1_10_8 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_10_8()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                I_8 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);
            writer.WriteValue(I_6);
            writer.WriteValue(I_7);
            writer.WriteValue(I_8);

            return writer.bytes;
        }

    }

    public class Type1_11_2
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("IsVisible")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type1_11_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_11_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }
    }

    public class Type1_12_2
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type1_12_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_12_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }
    }

    public class Type1_13_10
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("R")]
        public int I_2 { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("G")]
        public int I_3 { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("B")]
        public int I_4 { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("A")]
        public int I_5 { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("R")]
        public int I_6 { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("G")]
        public int I_7 { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("B")]
        public int I_8 { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("A")]
        public int I_9 { get; set; }
        [YAXAttributeFor("FadeDuration")]
        [YAXSerializeAs("value")]
        public int I_10 { get; set; }

        public static Type1_13_10 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_13_10()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                I_8 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
                I_9 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 64)),
                I_10 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 72)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);
            writer.WriteValue(I_6);
            writer.WriteValue(I_7);
            writer.WriteValue(I_8);
            writer.WriteValue(I_9);
            writer.WriteValue(I_10);

            return writer.bytes;
        }

    }

    public class Type1_14_1
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }

        public static Type1_14_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_14_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }
    }

    public class Type1_16_2
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type1_16_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_16_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }
    }

    public class Type1_17_6
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("F_3")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_3 { get; set; }
        [YAXAttributeFor("F_4")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_4 { get; set; }
        [YAXAttributeFor("F_5")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_5 { get; set; }
        [YAXAttributeFor("F_6")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_6 { get; set; }

        public static Type1_17_6 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_17_6()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                F_6 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(F_3);
            writer.WriteValue(F_4);
            writer.WriteValue(F_5);
            writer.WriteValue(F_6);

            return writer.bytes;
        }

    }

    public class Type1_19_3
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }

        public static Type1_19_3 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_19_3()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);

            return writer.bytes;
        }
    }

    public class Type1_20_12
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("F_3")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_3 { get; set; }
        [YAXAttributeFor("F_4")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_4 { get; set; }
        [YAXAttributeFor("F_5")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_5 { get; set; }
        [YAXAttributeFor("I_6")]
        [YAXSerializeAs("value")]
        public int I_6 { get; set; }
        [YAXAttributeFor("I_7")]
        [YAXSerializeAs("value")]
        public int I_7 { get; set; }
        [YAXAttributeFor("F_8")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_8 { get; set; }
        [YAXAttributeFor("F_9")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_9 { get; set; }
        [YAXAttributeFor("F_10")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_10 { get; set; }
        [YAXAttributeFor("F_11")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_11 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_12 { get; set; }

        public static Type1_20_12 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_20_12()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                F_8 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
                F_9 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 64)),
                F_10 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 72)),
                F_11 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 80)),
                F_12 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 88)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(F_3);
            writer.WriteValue(F_4);
            writer.WriteValue(F_5);
            writer.WriteValue(I_6);
            writer.WriteValue(I_7);
            writer.WriteValue(F_8);
            writer.WriteValue(F_9);
            writer.WriteValue(F_10);
            writer.WriteValue(F_11);
            writer.WriteValue(F_12);

            return writer.bytes;
        }
    }

    public class Type1_26_2
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }

        public static Type1_26_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_26_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);

            return writer.bytes;
        }
    }

    public class Type1_27_2
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type1_27_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type1_27_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
              };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }
    }

    public class Type2_0_1
    {
        [YAXAttributeFor("Camera")]
        [YAXSerializeAs("Name")]
        public string Str_1 { get; set; }

        public static Type2_0_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type2_0_1()
            {
                Str_1 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 0))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(Str_1);

            return writer.bytes;
        }

    }

    public class Type2_6_3
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }

        public static Type2_6_3 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type2_6_3()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
             };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);

            return writer.bytes;
        }

    }

    public class Type2_7_5
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }

        public static Type2_7_5 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type2_7_5()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);

            return writer.bytes;
        }

    }

    public class Type2_7_8
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }
        [YAXAttributeFor("Strength")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_6 { get; set; }
        [YAXAttributeFor("I_7")]
        [YAXSerializeAs("value")]
        public int I_7 { get; set; }
        [YAXAttributeFor("I_8")]
        [YAXSerializeAs("value")]
        public int I_8 { get; set; }

        public static Type2_7_8 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type2_7_8()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                F_6 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                I_8 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);
            writer.WriteValue(F_6);
            writer.WriteValue(I_7);
            writer.WriteValue(I_8);

            return writer.bytes;
        }
    }

    public class Type2_9_2
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("F_2")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }

        public static Type2_9_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type2_9_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);

            return writer.bytes;
        }
    }

    public class Type2_10_2
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("F_2")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }

        public static Type2_10_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type2_10_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);

            return writer.bytes;
        }
    }

    public class Type2_11_1
    {
        [YAXAttributeFor("F_1")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_1 { get; set; }

        public static Type2_11_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type2_11_1()
            {
                F_1 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(F_1);

            return writer.bytes;
        }
    }

    public class Type3_0_1
    {
        [YAXAttributeFor("Switch")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; } // 0 = OFF, 1 = ON

        public static Type3_0_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type3_0_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }

    }

    public class Type3_1_1
    {
        [YAXAttributeFor("Stage_ID")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }

        public static Type3_1_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type3_1_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }

    }

    public class Type3_2_1
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }

        public static Type3_2_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type3_2_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }

    }

    public class Type3_3_1
    {
        [YAXAttributeFor("Switch")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }

        public static Type3_3_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type3_3_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }

    }

    public class Type3_4_2
    {
        [YAXAttributeFor("Str_1")]
        [YAXSerializeAs("value")]
        public string Str_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type3_4_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type3_4_2()
            {
                Str_1 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 9)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(Str_1, 4);
            writer.WriteValue(I_2);

            return writer.bytes;
        }

    }

    public class Type4_0_12
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("EEPK_Type")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("Skill_ID")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("Effect_ID")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }
        [YAXAttributeFor("F_6")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_6 { get; set; }
        [YAXAttributeFor("F_7")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_7 { get; set; }
        [YAXAttributeFor("F_8")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_8 { get; set; }
        [YAXAttributeFor("I_9")]
        [YAXSerializeAs("value")]
        public int I_9 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public int I_10 { get; set; }
        [YAXAttributeFor("I_11")]
        [YAXSerializeAs("value")]
        public int I_11 { get; set; }
        [YAXAttributeFor("Switch")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }

        public static Type4_0_12 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type4_0_12()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                F_6 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                F_7 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                F_8 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
                I_9 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 64)),
                I_10 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 72)),
                I_11 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 80)),
                I_12 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 88)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);
            writer.WriteValue(F_6);
            writer.WriteValue(F_7);
            writer.WriteValue(F_8);
            writer.WriteValue(I_9);
            writer.WriteValue(I_10);
            writer.WriteValue(I_11);
            writer.WriteValue(I_12);

            return writer.bytes;
        }
    }

    public class Type4_1_8
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("Bone_Link")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("BPE_ID")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("F_4")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_4 { get; set; }
        [YAXAttributeFor("F_5")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_5 { get; set; }
        [YAXAttributeFor("F_6")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_6 { get; set; }
        [YAXAttributeFor("I_7")]
        [YAXSerializeAs("value")]
        public int I_7 { get; set; }
        [YAXAttributeFor("Switch")]
        [YAXSerializeAs("value")]
        public int I_8 { get; set; }

        public static Type4_1_8 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type4_1_8()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                F_6 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                I_8 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(F_4);
            writer.WriteValue(F_5);
            writer.WriteValue(F_6);
            writer.WriteValue(I_7);
            writer.WriteValue(I_8);

            return writer.bytes;
        }
    }

    public class Type4_2_3
    {
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("AuraType")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("Switch")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }

        public static Type4_2_3 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type4_2_3()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);

            return writer.bytes;
        }

    }

    public class Type4_3_5
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("F_2")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }
        [YAXAttributeFor("F_3")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }

        public static Type4_3_5 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type4_3_5()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);
            writer.WriteValue(F_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);

            return writer.bytes;
        }

    }

    public class Type4_4_1
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }

        public static Type4_4_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type4_4_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }

    }

    public class Type5_0_2
    {
        public enum Acb
        {
            DemoSE,
            DemoVOX
        }
        
        [YAXAttributeFor("Acb_To_Use")]
        [YAXSerializeAs("value")]
        public Acb I_1 { get; set; }
        [YAXAttributeFor("Cue_ID")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type5_0_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type5_0_2()
            {
                I_1 = (Acb)BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue((int)I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }
    }

    public class Type5_1_2
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type5_1_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type5_1_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }
    }

    public class Type5_2_3
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("Cue_ID")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }

        public static Type5_2_3 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type5_2_3()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);

            return writer.bytes;
        }
    }

    public class Type5_0_3
    {
        public enum Acb
        {
            DemoSE,
            DemoVOX
        }

        [YAXAttributeFor("Acb_To_Use")]
        [YAXSerializeAs("value")]
        public Acb I_1 { get; set; }
        [YAXAttributeFor("Cue_ID")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }

        public static Type5_0_3 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type5_0_3()
            {
                I_1 = (Acb)BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue((int)I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);

            return writer.bytes;
        }

    }

    public class Type5_3_2
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type5_3_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type5_3_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }

    }

    public class Type5_4_3
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("I_3")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }

        public static Type5_4_3 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type5_4_3()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);

            return writer.bytes;
        }

    }


    public class Type6_0_1
    {
        [YAXAttributeFor("Section")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }

        public static Type6_0_1 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type6_0_1()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);

            return writer.bytes;
        }
    }

    public class Type6_16_6
    {
        [YAXAttributeFor("Switch")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("FocalLength")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }
        [YAXAttributeFor("Aperture")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }
        [YAXAttributeFor("TransitionLength")]
        [YAXSerializeAs("value")]
        public int I_6 { get; set; }

        public static Type6_16_6 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type6_16_6()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                I_6 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);
            writer.WriteValue(F_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);
            writer.WriteValue(I_6);

            return writer.bytes;
        }
    }

    public class Type6_17_19
    {
        [YAXAttributeFor("Enabled")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("Saturation")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_3 { get; set; }
        [YAXAttributeFor("RGB")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0##########")]
        public float F_4 { get; set; }
        [YAXAttributeFor("RGB")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0##########")]
        public float F_5 { get; set; }
        [YAXAttributeFor("RGB")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0##########")]
        public float F_6 { get; set; }
        [YAXAttributeFor("RGBCurves")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0##########")]
        public float F_7 { get; set; }
        [YAXAttributeFor("RGBCurves")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0##########")]
        public float F_8 { get; set; }
        [YAXAttributeFor("RGBCurves")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0##########")]
        public float F_9 { get; set; }
        [YAXAttributeFor("GlowRGB")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0##########")]
        public float F_10 { get; set; }
        [YAXAttributeFor("GlowRGB")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0##########")]
        public float F_11 { get; set; }
        [YAXAttributeFor("GlowRGB")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Temperature")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_13 { get; set; }
        [YAXAttributeFor("Fade")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_14 { get; set; }
        [YAXAttributeFor("F_15")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_15 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_17")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_17 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public int I_18 { get; set; }
        [YAXAttributeFor("Time")]
        [YAXSerializeAs("value")]
        public int I_19 { get; set; }

        public static Type6_17_19 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type6_17_19()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                F_6 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                F_7 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                F_8 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
                F_9 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 64)),
                F_10 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 72)),
                F_11 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 80)),
                F_12 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 88)),
                F_13 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 96)),
                F_14 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 104)),
                F_15 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 112)),
                F_16 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 120)),
                F_17 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 128)),
                I_18 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 136)),
                I_19 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 144)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(F_3);
            writer.WriteValue(F_4);
            writer.WriteValue(F_5);
            writer.WriteValue(F_6);
            writer.WriteValue(F_7);
            writer.WriteValue(F_8);
            writer.WriteValue(F_9);
            writer.WriteValue(F_10);
            writer.WriteValue(F_11);
            writer.WriteValue(F_12);
            writer.WriteValue(F_13);
            writer.WriteValue(F_14);
            writer.WriteValue(F_15);
            writer.WriteValue(F_16);
            writer.WriteValue(F_17);
            writer.WriteValue(I_18);
            writer.WriteValue(I_19);

            return writer.bytes;
        }
    }

    public class Type6_18_7
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("F_2")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_2 { get; set; }
        [YAXAttributeFor("F_3")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_3 { get; set; }
        [YAXAttributeFor("F_4")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_4 { get; set; }
        [YAXAttributeFor("F_5")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_5 { get; set; }
        [YAXAttributeFor("F_6")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#########")]
        public float F_6 { get; set; }
        [YAXAttributeFor("I_7")]
        [YAXSerializeAs("value")]
        public int I_7 { get; set; }

        public static Type6_18_7 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type6_18_7()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                F_6 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                I_7 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);
            writer.WriteValue(F_3);
            writer.WriteValue(F_4);
            writer.WriteValue(F_5);
            writer.WriteValue(F_6);
            writer.WriteValue(I_7);

            return writer.bytes;
        }
    }

    public class Type6_19_15
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("F_3")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_3 { get; set; }
        [YAXAttributeFor("F_4")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_4 { get; set; }
        [YAXAttributeFor("F_5")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_5 { get; set; }
        [YAXAttributeFor("F_6")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_6 { get; set; }
        [YAXAttributeFor("F_7")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_7 { get; set; }
        [YAXAttributeFor("F_8")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_8 { get; set; }
        [YAXAttributeFor("F_9")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_9 { get; set; }
        [YAXAttributeFor("F_10")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_10 { get; set; }
        [YAXAttributeFor("F_11")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_11 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_13")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_13 { get; set; }
        [YAXAttributeFor("F_14")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_14 { get; set; }
        [YAXAttributeFor("I_15")]
        [YAXSerializeAs("value")]
        public int I_15 { get; set; }

        public static Type6_19_15 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type6_19_15()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                F_3 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                F_4 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                F_5 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32)),
                F_6 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 40)),
                F_7 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48)),
                F_8 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56)),
                F_9 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 64)),
                F_10 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 72)),
                F_11 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 80)),
                F_12 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 88)),
                F_13 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 96)),
                F_14 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 104)),
                I_15 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 112)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);
            writer.WriteValue(F_3);
            writer.WriteValue(F_4);
            writer.WriteValue(F_5);
            writer.WriteValue(F_6);
            writer.WriteValue(F_7);
            writer.WriteValue(F_8);
            writer.WriteValue(F_9);
            writer.WriteValue(F_10);
            writer.WriteValue(F_11);
            writer.WriteValue(F_12);
            writer.WriteValue(F_13);
            writer.WriteValue(F_14);
            writer.WriteValue(I_15);

            return writer.bytes;
        }
    }

    public class Type6_20_2
    {
        [YAXAttributeFor("I_1")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("F_2")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_2 { get; set; }

        public static Type6_20_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type6_20_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                F_2 = BitConverter.ToSingle(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(F_2);

            return writer.bytes;
        }

    }

    public class Type7_0_5
    {
        [YAXAttributeFor("Str_1")]
        [YAXSerializeAs("value")]
        public string Str_1 { get; set; }
        [YAXAttributeFor("EMB_Index")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }
        [YAXAttributeFor("Stretch")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("I_5")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }

        public static Type7_0_5 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type7_0_5()
            {
                Str_1 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(Str_1, 4);
            writer.WriteValue(I_2);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);

            return writer.bytes;
        }

    }

    public class Type9_0_2
    {
        [YAXAttributeFor("Year")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("I_2")]
        [YAXSerializeAs("value")]
        public int I_2 { get; set; }

        public static Type9_0_2 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type9_0_2()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                I_2 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 8)),
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(I_2);

            return writer.bytes;
        }

    }

    public class Type9_1_5
    {
        [YAXAttributeFor("Switch")]
        [YAXSerializeAs("value")]
        public int I_1 { get; set; }
        [YAXAttributeFor("Str_2")]
        [YAXSerializeAs("value")]
        public string Str_2 { get; set; }
        [YAXAttributeFor("MSG_Name_ID")]
        [YAXSerializeAs("value")]
        public int I_3 { get; set; }
        [YAXAttributeFor("I_4")]
        [YAXSerializeAs("value")]
        public int I_4 { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("value")]
        public int I_5 { get; set; }

        public static Type9_1_5 Read(byte[] rawBytes, List<byte> bytes, int offset)
        {
            return new Type9_1_5()
            {
                I_1 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 0)),
                Str_2 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, offset + 8)),
                I_3 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 16)),
                I_4 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 24)),
                I_5 = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32))
            };
        }

        public List<byte> Write(List<byte> bytes, List<int> valueOffsets)
        {
            ValueWriter writer = new ValueWriter(valueOffsets, bytes);

            //Values
            writer.WriteValue(I_1);
            writer.WriteValue(Str_2, 4);
            writer.WriteValue(I_3);
            writer.WriteValue(I_4);
            writer.WriteValue(I_5);

            return writer.bytes;
        }

    }


}
