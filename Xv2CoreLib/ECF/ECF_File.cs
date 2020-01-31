using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.HslColor;
using YAXLib;

namespace Xv2CoreLib.ECF
{

    [Serializable]
    [YAXSerializeAs("ECF")]
    public class ECF_File
    {
        [YAXAttributeForClass]
        public UInt16 I_12 { get; set; }
        //followed by 12 zero bytes
        
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ColorEffect")]
        public List<ECF_Entry> Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static ECF_File Load(string path)
        {
            return new Parser(path, false).GetEcfFile();
        }
        
        public static ECF_File Load(List<byte> bytes)
        {
            return new Parser(bytes).GetEcfFile();
        }

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();
            if (Entries == null) return colors;

            foreach(var entry in Entries)
            {
                colors.AddRange(entry.GetUsedColors());
            }
            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness)
        {
            if (Entries == null) return;

            foreach (var entry in Entries)
            {
                entry.ChangeHue(hue, saturation, lightness);
            }
        }
    }

    [Serializable]
    [YAXSerializeAs("ColorEffect")]
    public class ECF_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Node")]
        public string Unk_Str { get; set; }
        [YAXSerializeAs("StartFrame")]
        [YAXAttributeFor("Time")]
        public UInt16 I_56 { get; set; }
        [YAXSerializeAs("EndFrame")]
        [YAXAttributeFor("Time")]
        public UInt16 I_58 { get; set; }

        [YAXSerializeAs("R")]
        [YAXAttributeFor("DiffuseColor")]
        [YAXFormat("0.0######")]
        public float F_00 { get; set; }
        [YAXSerializeAs("G")]
        [YAXAttributeFor("DiffuseColor")]
        [YAXFormat("0.0######")]
        public float F_04 { get; set; }
        [YAXSerializeAs("B")]
        [YAXAttributeFor("DiffuseColor")]
        [YAXFormat("0.0######")]
        public float F_08 { get; set; }
        [YAXSerializeAs("A")]
        [YAXAttributeFor("DiffuseColor")]
        [YAXFormat("0.0######")]
        public float F_12 { get; set; }
        [YAXSerializeAs("R")]
        [YAXAttributeFor("SpecularColor")]
        [YAXFormat("0.0######")]
        public float F_16 { get; set; }
        [YAXSerializeAs("G")]
        [YAXAttributeFor("SpecularColor")]
        [YAXFormat("0.0######")]
        public float F_20 { get; set; }
        [YAXSerializeAs("B")]
        [YAXAttributeFor("SpecularColor")]
        [YAXFormat("0.0######")]
        public float F_24 { get; set; }
        [YAXSerializeAs("A")]
        [YAXAttributeFor("SpecularColor")]
        [YAXFormat("0.0######")]
        public float F_28 { get; set; }
        [YAXSerializeAs("R")]
        [YAXAttributeFor("AmbientColor")]
        [YAXFormat("0.0######")]
        public float F_32 { get; set; }
        [YAXSerializeAs("G")]
        [YAXAttributeFor("AmbientColor")]
        [YAXFormat("0.0######")]
        public float F_36 { get; set; }
        [YAXSerializeAs("B")]
        [YAXAttributeFor("AmbientColor")]
        [YAXFormat("0.0######")]
        public float F_40 { get; set; }
        [YAXSerializeAs("A")]
        [YAXAttributeFor("AmbientColor")]
        [YAXFormat("0.0######")]
        public float F_44 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("BlendingFactor")]
        [YAXFormat("0.0######")]
        public float F_48 { get; set; }
        [YAXSerializeAs("Mode")]
        [YAXAttributeFor("Loop")]
        public PlayMode I_52 { get; set; } //uint16
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_54")]
        public UInt16 I_54 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_60")]
        public UInt16 I_60 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_62")]
        public UInt16 I_62 { get; set; }
        [YAXSerializeAs("uint16")]
        [YAXAttributeFor("I_64")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public UInt16[] I_64 { get; set; } // size = 14
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_96")]
        public UInt16 I_96 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation")]
        public List<Type0> Type0 { get; set; }

        
        public enum PlayMode
        {
            NoLoop = 2,
            Loop = 3
        }


        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();
            
            //Diffuse Color
            if (F_00 != 0 || F_04 != 0 || F_08 != 0)
            {
                if (F_00 != 1 || F_04 != 1 || F_08 != 1)
                {
                    colors.Add(new RgbColor(F_00, F_04, F_08));
                }
            }

            //Specular Color
            if (F_16 != 0 || F_20 != 0 || F_24 != 0)
            {
                if (F_16 != 1 || F_20 != 1 || F_24 != 1)
                {
                    colors.Add(new RgbColor(F_16, F_20, F_24));
                }
            }

            //Ambient Color
            if (F_32 != 0 || F_36 != 0 || F_40 != 0)
            {
                if (F_32 != 1 || F_36 != 1 || F_40 != 1)
                {
                    colors.Add(new RgbColor(F_32, F_36, F_40));
                }
            }

            //Animations
            if (Type0 != null)
            {
                InitColorAnimations(ECF.Type0.Parameter.DiffuseColor, F_00, F_04, F_08);
                InitColorAnimations(ECF.Type0.Parameter.SpecularColor, F_16, F_20, F_24);
                InitColorAnimations(ECF.Type0.Parameter.AmbientColor, F_32, F_36, F_40);
            }

            return colors;
        }

        /// <summary>
        /// Ensures that an animation exists for each color component.
        /// </summary>
        public void InitColorAnimations(Type0.Parameter parameter, float defaultR, float defaultG, float defaultB)
        {
            if (Type0 == null) return;

            var r = GetColorAnimation(parameter, ECF.Type0.Component.R);
            var g = GetColorAnimation(parameter, ECF.Type0.Component.G);
            var b = GetColorAnimation(parameter, ECF.Type0.Component.B);

            //Create missing animations
            if (r != null || g != null || b != null)
            {
                if (r == null)
                {
                    var newR = ECF.Type0.GetNew(defaultR);
                    newR.I_01_a = ECF.Type0.Component.R;
                    newR.I_00 = parameter;
                    Type0.Add(newR);
                }

                if (g == null)
                {
                    var newG = ECF.Type0.GetNew(defaultG);
                    newG.I_01_a = ECF.Type0.Component.G;
                    newG.I_00 = parameter;
                    Type0.Add(newG);
                }

                if (b == null)
                {
                    var newB = ECF.Type0.GetNew(defaultB);
                    newB.I_01_a = ECF.Type0.Component.B;
                    newB.I_00 = parameter;
                    Type0.Add(newB);
                }
                
            }

            r = GetColorAnimation(parameter, ECF.Type0.Component.R);
            g = GetColorAnimation(parameter, ECF.Type0.Component.G);
            b = GetColorAnimation(parameter, ECF.Type0.Component.B);

            if (r != null && g != null && b != null)
            {
                //Add keyframe at frame 0, if it doesn't exist
                if (r.GetKeyframe(0) == null)
                {
                    r.SetValue(0, defaultR);
                }

                if (g.GetKeyframe(0) == null)
                {
                    g.SetValue(0, defaultG);
                }

                if (b.GetKeyframe(0) == null)
                {
                    b.SetValue(0, defaultB);
                }
            }

            //Add missing keyframes
            foreach (var anim in Type0)
            {
                foreach (var anim2 in Type0)
                {
                    if (anim.I_00 == parameter && anim2.I_00 == parameter && !anim.IsAlpha && !anim2.IsAlpha)
                    {
                        anim.AddKeyframesFromAnim(anim2);
                    }
                }
            }
        }

        public Type0 GetColorAnimation(Type0.Parameter parameter, Type0.Component component)
        {
            foreach (var anim in Type0)
            {
                if (anim.I_00 == parameter && anim.I_01_a == component)
                {
                    return anim;
                }
            }

            return null;
        }

        public void ChangeHue(double hue, double saturation, double lightness)
        {
            //Diffuse Color
            if (F_00 != 0 || F_04 != 0 || F_08 != 0)
            {
                if (F_00 != 1 || F_04 != 1 || F_08 != 1)
                {
                    HslColor.HslColor newCol1 = new RgbColor(F_00, F_04, F_08).ToHsl();
                    newCol1.ChangeHue(hue);
                    newCol1.ChangeSaturation(saturation);
                    newCol1.ChangeLightness(lightness);
                    RgbColor convertedColor = newCol1.ToRgb();
                    F_00 = (float)convertedColor.R;
                    F_04 = (float)convertedColor.G;
                    F_08 = (float)convertedColor.B;
                }
            }

            //Specular Color
            if (F_16 != 0 || F_20 != 0 || F_24 != 0)
            {
                if (F_16 != 1 || F_20 != 1 || F_24 != 1)
                {
                    HslColor.HslColor newCol1 = new RgbColor(F_16, F_20, F_24).ToHsl();
                    newCol1.ChangeHue(hue);
                    newCol1.ChangeSaturation(saturation);
                    newCol1.ChangeLightness(lightness);
                    RgbColor convertedColor = newCol1.ToRgb();
                    F_16 = (float)convertedColor.R;
                    F_20 = (float)convertedColor.G;
                    F_24 = (float)convertedColor.B;
                }
            }

            //Ambient Color
            if (F_32 != 0 || F_36 != 0 || F_40 != 0)
            {
                if (F_32 != 1 || F_36 != 1 || F_40 != 1)
                {
                    HslColor.HslColor newCol1 = new RgbColor(F_32, F_36, F_40).ToHsl();
                    newCol1.ChangeHue(hue);
                    newCol1.ChangeSaturation(saturation);
                    newCol1.ChangeLightness(lightness);
                    RgbColor convertedColor = newCol1.ToRgb();
                    F_32 = (float)convertedColor.R;
                    F_36 = (float)convertedColor.G;
                    F_40 = (float)convertedColor.B;
                }
            }

            if(Type0 != null)
            {
                ChangeHueForAnimations(hue, saturation, lightness, ECF.Type0.Parameter.DiffuseColor);
                ChangeHueForAnimations(hue, saturation, lightness, ECF.Type0.Parameter.SpecularColor);
                ChangeHueForAnimations(hue, saturation, lightness, ECF.Type0.Parameter.AmbientColor);
            }
        }

        private void ChangeHueForAnimations(double hue, double saturation, double lightness, Type0.Parameter parameter)
        {
            Type0 r = Type0.FirstOrDefault(e => e.I_01_a == ECF.Type0.Component.R && e.I_00 == parameter);
            Type0 g = Type0.FirstOrDefault(e => e.I_01_a == ECF.Type0.Component.G && e.I_00 == parameter);
            Type0 b = Type0.FirstOrDefault(e => e.I_01_a == ECF.Type0.Component.B && e.I_00 == parameter);
            if (r == null || g == null || b == null) return;

            foreach (var r_frame in r.Keyframes)
            {
                float g_frame = g.GetValue(r_frame.Index);
                float b_frame = b.GetValue(r_frame.Index);

                if (r_frame.Float != 0.0 || g_frame != 0.0 || b_frame != 0.0)
                {
                    HslColor.HslColor color = new RgbColor(r_frame.Float, g_frame, b_frame).ToHsl();
                    color.ChangeHue(hue);
                    color.ChangeSaturation(saturation);
                    color.ChangeLightness(lightness);
                    RgbColor convertedColor = color.ToRgb();

                    r_frame.Float = (float)convertedColor.R;
                    g.SetValue(r_frame.Index, (float)convertedColor.G);
                    b.SetValue(r_frame.Index, (float)convertedColor.B);
                }
            }

        }

    }

    [Serializable]
    [YAXSerializeAs("Animation")]
    public class Type0
    {
        [YAXDontSerialize]
        public bool IsAlpha
        {
            get
            {
                return (I_01_a == Component.A);
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Parameter")]
        public Parameter I_00 { get; set; } //int8
        [YAXAttributeForClass]
        [YAXSerializeAs("Component")]
        public Component I_01_a { get; set; } //int4
        [YAXAttributeForClass]
        [YAXSerializeAs("Interpolated")]
        public bool I_01_b { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Looped")]
        public bool I_02 { get; set; }
        [YAXAttributeFor("I_03")]
        [YAXSerializeAs("int8")]
        public byte I_03 { get; set; }

        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_04 { get; set; }
        
        public ObservableCollection<Type0_Keyframe> Keyframes { get; set; }

        public enum Parameter
        {
            DiffuseColor = 0,
            SpecularColor = 1,
            AmbientColor = 2,
            BlendingFactor = 3
        }

        public enum Component
        {
            R = 0,
            G = 1,
            B = 2,
            A = 3,
            Base = 4
        }
        
        public static Component GetComponent(Parameter parameter, int component)
        {
            switch (parameter)
            {
                case Parameter.BlendingFactor:
                    return Component.Base;
                default:
                    return (Component)component;
            }
        }

        public byte GetComponent()
        {
            switch (I_00)
            {
                case Parameter.BlendingFactor:
                    return 0;
                default:
                    return (byte)I_01_a;
            }
        }

        public static Type0 GetNew(float value = 0f)
        {
            return new Type0()
            {
                Keyframes = new ObservableCollection<Type0_Keyframe>()
                {
                    new Type0_Keyframe()
                    {
                        Index = 0,
                        Float = value
                    },
                    new Type0_Keyframe()
                    {
                        Index = 100,
                        Float = value
                    }
                }
            };
        }

        public void AddKeyframesFromAnim(Type0 anim)
        {
            foreach (var keyframe in anim.Keyframes)
            {
                var existing = GetKeyframe(keyframe.Index);

                if (existing == null)
                {
                    var newKeyframe = new Type0_Keyframe() { Index = keyframe.Index, Float = GetValue(keyframe.Index) };
                    Keyframes.Add(newKeyframe);
                }
            }
        }

        public Type0_Keyframe GetKeyframe(int time)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index == time) return keyframe;
            }

            return null;
        }

        public float GetValue(int time)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index == time) return keyframe.Float;
            }

            return CalculateKeyframeValue(time);
        }

        public void SetValue(int time, float value)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index == time)
                {
                    keyframe.Float = value;
                    return;
                }
            }

            //Keyframe doesn't exist. Add it.
            Keyframes.Add(new Type0_Keyframe() { Index = (ushort)time, Float = value });
        }

        public float CalculateKeyframeValue(int time)
        {
            Type0_Keyframe before = GetKeyframeBefore(time);
            Type0_Keyframe after = GetKeyframeAfter(time);

            if (after == null)
            {
                after = new Type0_Keyframe() { Index = ushort.MaxValue, Float = before.Float };
            }

            //Frame difference between previous frame and the current frame (current frame is AFTER the frame we want)
            int diff = after.Index - before.Index;

            //Keyframe value difference
            float keyframe2 = after.Float - before.Float;

            //Difference between the frame we WANT and the previous frame
            int diff2 = time - before.Index;

            //Divide keyframe value difference by the keyframe time difference, and then multiply it by diff2, then add the previous keyframe value
            return (keyframe2 / diff) * diff2 + before.Float;
        }

        /// <summary>
        /// Returns the keyframe that appears just before the specified frame
        /// </summary>
        /// <returns></returns>
        public Type0_Keyframe GetKeyframeBefore(int time)
        {
            SortKeyframes();
            Type0_Keyframe prev = null;

            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index >= time) break;
                prev = keyframe;
            }

            return prev;
        }

        /// <summary>
        /// Returns the keyframe that appears just after the specified frame
        /// </summary>
        /// <returns></returns>
        public Type0_Keyframe GetKeyframeAfter(int time)
        {
            SortKeyframes();
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index > time) return keyframe;
            }

            return null;
        }


        public void SortKeyframes()
        {
            var sortedList = Keyframes.ToList();
            sortedList.Sort((x, y) => x.Index - y.Index);
            Keyframes = new ObservableCollection<Type0_Keyframe>(sortedList);
        }

    }


    [Serializable]
    [YAXSerializeAs("Keyframe")]
    public class Type0_Keyframe
    {
        [YAXAttributeForClass]
        public ushort Index { get; set; }
        [YAXAttributeForClass]
        [YAXFormat("0.0######")]
        public float Float { get; set; }
    }
    
}
