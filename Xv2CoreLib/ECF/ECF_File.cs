using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.ECF
{

    [Serializable]
    [YAXSerializeAs("ECF")]
    public class ECF_File
    {
        [YAXAttributeForClass]
        public ushort I_12 { get; set; }
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
        
        public static ECF_File Load(byte[] bytes)
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

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0)
        {
            if (Entries == null) return;

            foreach (var entry in Entries)
            {
                entry.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
            }
        }
    }

    [Serializable]
    [YAXSerializeAs("ColorEffect")]
    public class ECF_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("MaterialLink")]
        public string MaterialLink { get; set; } //Material linkage. "Node" seems to be the default value, applying to all materials.
        [YAXSerializeAs("StartFrame")]
        [YAXAttributeFor("Time")]
        public ushort I_56 { get; set; }
        [YAXSerializeAs("EndFrame")]
        [YAXAttributeFor("Time")]
        public ushort I_58 { get; set; }

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
        public ushort I_54 { get; set; } //always 0
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_60")]
        public ushort I_60 { get; set; } //always 0
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_62")]
        public ushort I_62 { get; set; } //always 0
        [YAXSerializeAs("uint16")]
        [YAXAttributeFor("I_64")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_64 { get; set; } // size = 14, all always 0
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_96")]
        public ushort I_96 { get; set; } //always 0

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation")]
        public List<Type0> Animations { get; set; } = new List<Type0>();

        
        public enum PlayMode : ushort
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
            if (Animations != null)
            {
                InitColorAnimations(ECF.Type0.ParameterEnum.DiffuseColor, F_00, F_04, F_08);
                InitColorAnimations(ECF.Type0.ParameterEnum.SpecularColor, F_16, F_20, F_24);
                InitColorAnimations(ECF.Type0.ParameterEnum.AmbientColor, F_32, F_36, F_40);
            }

            return colors;
        }

        /// <summary>
        /// Ensures that an animation exists for each color component.
        /// </summary>
        public void InitColorAnimations(Type0.ParameterEnum parameter, float defaultR, float defaultG, float defaultB)
        {
            if (Animations == null) return;

            var r = GetColorAnimation(parameter, ECF.Type0.ComponentEnum.R);
            var g = GetColorAnimation(parameter, ECF.Type0.ComponentEnum.G);
            var b = GetColorAnimation(parameter, ECF.Type0.ComponentEnum.B);

            //Create missing animations
            if (r != null || g != null || b != null)
            {
                if (r == null)
                {
                    var newR = ECF.Type0.GetNew(defaultR);
                    newR.Component = ECF.Type0.ComponentEnum.R;
                    newR.Parameter = parameter;
                    Animations.Add(newR);
                }

                if (g == null)
                {
                    var newG = ECF.Type0.GetNew(defaultG);
                    newG.Component = ECF.Type0.ComponentEnum.G;
                    newG.Parameter = parameter;
                    Animations.Add(newG);
                }

                if (b == null)
                {
                    var newB = ECF.Type0.GetNew(defaultB);
                    newB.Component = ECF.Type0.ComponentEnum.B;
                    newB.Parameter = parameter;
                    Animations.Add(newB);
                }
                
            }

            r = GetColorAnimation(parameter, ECF.Type0.ComponentEnum.R);
            g = GetColorAnimation(parameter, ECF.Type0.ComponentEnum.G);
            b = GetColorAnimation(parameter, ECF.Type0.ComponentEnum.B);

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
            foreach (var anim in Animations)
            {
                foreach (var anim2 in Animations)
                {
                    if (anim.Parameter == parameter && anim2.Parameter == parameter && !anim.IsAlpha && !anim2.IsAlpha)
                    {
                        anim.AddKeyframesFromAnim(anim2);
                    }
                }
            }
        }

        public Type0 GetColorAnimation(Type0.ParameterEnum parameter, Type0.ComponentEnum component)
        {
            foreach (var anim in Animations)
            {
                if (anim.Parameter == parameter && anim.Component == component)
                {
                    return anim;
                }
            }

            return null;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0)
        {
            //Diffuse Color
            if (F_00 != 0 || F_04 != 0 || F_08 != 0)
            {
                if (F_00 != 1 || F_04 != 1 || F_08 != 1)
                {
                    HslColor.HslColor newCol1 = new RgbColor(F_00, F_04, F_08).ToHsl();
                    RgbColor convertedColor;

                    if (hueSet)
                    {
                        newCol1.SetHue(hue, variance);
                    }
                    else
                    {
                        newCol1.ChangeHue(hue);
                        newCol1.ChangeSaturation(saturation);
                        newCol1.ChangeLightness(lightness);
                    }

                    convertedColor = newCol1.ToRgb();

                    undos.Add(new UndoableProperty<ECF_Entry>(nameof(F_00), this, F_00, (float)convertedColor.R));
                    undos.Add(new UndoableProperty<ECF_Entry>(nameof(F_04), this, F_04, (float)convertedColor.G));
                    undos.Add(new UndoableProperty<ECF_Entry>(nameof(F_08), this, F_08, (float)convertedColor.B));

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
                    RgbColor convertedColor;

                    if (hueSet)
                    {
                        newCol1.SetHue(hue, variance);
                    }
                    else
                    {
                        newCol1.ChangeHue(hue);
                        newCol1.ChangeSaturation(saturation);
                        newCol1.ChangeLightness(lightness);
                    }

                    convertedColor = newCol1.ToRgb();

                    undos.Add(new UndoableProperty<ECF_Entry>(nameof(F_16), this, F_16, (float)convertedColor.R));
                    undos.Add(new UndoableProperty<ECF_Entry>(nameof(F_20), this, F_20, (float)convertedColor.G));
                    undos.Add(new UndoableProperty<ECF_Entry>(nameof(F_24), this, F_24, (float)convertedColor.B));

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
                    RgbColor convertedColor;

                    if (hueSet)
                    {
                        newCol1.SetHue(hue, variance);
                    }
                    else
                    {
                        newCol1.ChangeHue(hue);
                        newCol1.ChangeSaturation(saturation);
                        newCol1.ChangeLightness(lightness);
                    }

                    convertedColor = newCol1.ToRgb();

                    undos.Add(new UndoableProperty<ECF_Entry>(nameof(F_32), this, F_32, (float)convertedColor.R));
                    undos.Add(new UndoableProperty<ECF_Entry>(nameof(F_36), this, F_36, (float)convertedColor.G));
                    undos.Add(new UndoableProperty<ECF_Entry>(nameof(F_40), this, F_40, (float)convertedColor.B));

                    F_32 = (float)convertedColor.R;
                    F_36 = (float)convertedColor.G;
                    F_40 = (float)convertedColor.B;
                }
            }

            if(Animations != null)
            {
                ChangeHueForAnimations(hue, saturation, lightness, ECF.Type0.ParameterEnum.DiffuseColor, undos, hueSet, variance);
                ChangeHueForAnimations(hue, saturation, lightness, ECF.Type0.ParameterEnum.SpecularColor, undos, hueSet, variance);
                ChangeHueForAnimations(hue, saturation, lightness, ECF.Type0.ParameterEnum.AmbientColor, undos, hueSet, variance);
            }
        }

        private void ChangeHueForAnimations(double hue, double saturation, double lightness, Type0.ParameterEnum parameter, List<IUndoRedo> undos, bool hueSet = false, int variance = 0)
        {
            Type0 r = Animations.FirstOrDefault(e => e.Component == ECF.Type0.ComponentEnum.R && e.Parameter == parameter);
            Type0 g = Animations.FirstOrDefault(e => e.Component == ECF.Type0.ComponentEnum.G && e.Parameter == parameter);
            Type0 b = Animations.FirstOrDefault(e => e.Component == ECF.Type0.ComponentEnum.B && e.Parameter == parameter);
            if (r == null || g == null || b == null) return;

            foreach (var r_frame in r.Keyframes)
            {
                float g_frame = g.GetValue(r_frame.Index);
                float b_frame = b.GetValue(r_frame.Index);

                if (r_frame.Float != 0.0 || g_frame != 0.0 || b_frame != 0.0)
                {
                    HslColor.HslColor color = new RgbColor(r_frame.Float, g_frame, b_frame).ToHsl();
                    RgbColor convertedColor;

                    if (hueSet)
                    {
                        color.SetHue(hue, variance);
                    }
                    else
                    {
                        color.ChangeHue(hue);
                        color.ChangeSaturation(saturation);
                        color.ChangeLightness(lightness);
                    }

                    convertedColor = color.ToRgb();

                    r.SetValue(r_frame.Index, (float)convertedColor.R, undos);
                    g.SetValue(r_frame.Index, (float)convertedColor.G, undos);
                    b.SetValue(r_frame.Index, (float)convertedColor.B, undos);
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
                return (Component == ComponentEnum.A);
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Parameter")]
        public ParameterEnum Parameter { get; set; } //int8
        [YAXAttributeForClass]
        [YAXSerializeAs("Component")]
        public ComponentEnum Component { get; set; } //int4
        [YAXAttributeForClass]
        [YAXSerializeAs("Interpolated")]
        public bool Interpolated { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Looped")]
        public bool Loop { get; set; }
        [YAXAttributeFor("I_03")]
        [YAXSerializeAs("int8")]
        public byte I_03 { get; set; }

        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_04 { get; set; }
        
        public AsyncObservableCollection<Type0_Keyframe> Keyframes { get; set; }

        public enum ParameterEnum
        {
            DiffuseColor = 0,
            SpecularColor = 1,
            AmbientColor = 2,
            BlendingFactor = 3
        }

        public enum ComponentEnum
        {
            R = 0,
            G = 1,
            B = 2,
            A = 3,
            Base = 4
        }
        
        public static ComponentEnum GetComponent(ParameterEnum parameter, int component)
        {
            switch (parameter)
            {
                case ParameterEnum.BlendingFactor:
                    return ComponentEnum.Base;
                default:
                    return (ComponentEnum)component;
            }
        }

        public byte GetComponent()
        {
            switch (Parameter)
            {
                case ParameterEnum.BlendingFactor:
                    return 0;
                default:
                    return (byte)Component;
            }
        }

        public static Type0 GetNew(float value = 0f)
        {
            var keyframes = AsyncObservableCollection<Type0_Keyframe>.Create();
            keyframes.Add(new Type0_Keyframe()
            {
                Index = 0,
                Float = value
            });
            keyframes.Add(new Type0_Keyframe()
            {
                Index = 100,
                Float = value
            });

            return new Type0()
            {
                Keyframes = keyframes
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

        public void SetValue(int time, float value, List<IUndoRedo> undos = null)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index == time)
                {
                    float oldValue = keyframe.Float;
                    keyframe.Float = value;

                    if (undos != null)
                        undos.Add(new UndoableProperty<Type0_Keyframe>(nameof(keyframe.Float), keyframe, oldValue, keyframe.Float));

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
            Sorting.SortEntries2(Keyframes);
        }

    }


    [Serializable]
    [YAXSerializeAs("Keyframe")]
    public class Type0_Keyframe : ISortable
    {
        [YAXDontSerialize]
        public int SortID { get { return Index; } }

        [YAXAttributeForClass]
        public ushort Index { get; set; }
        [YAXAttributeForClass]
        [YAXFormat("0.0######")]
        public float Float { get; set; }
    }
    
}
