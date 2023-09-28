using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.ECF
{

    [Serializable]
    public class ECF_File
    {
        public ushort I_12 { get; set; }
        //followed by 12 zero bytes

        public AsyncObservableCollection<ECF_Node> Nodes { get; set; } = new AsyncObservableCollection<ECF_Node>();

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static ECF_File Load(string path)
        {
            return Load(System.IO.File.ReadAllBytes(path));
        }

        public static ECF_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetEcfFile();
        }

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();
            if (Nodes == null) return colors;

            foreach (var entry in Nodes)
            {
                colors.AddRange(entry.GetUsedColors());
            }
            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0)
        {
            if (Nodes == null) return;

            foreach (var entry in Nodes)
            {
                entry.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
            }
        }
        
        public void UpdatePreviewBrush()
        {
            foreach(ECF_Node node in Nodes)
            {
                node.UpdatePreviewBrush();
            }
        }
    }

    [Serializable]
    public class ECF_Node : INotifyPropertyChanged, ISelectedKeyframedValue
    {
        #region INotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public const string CLIPBOARD_ID = "XV2_ECF_NODE";

        public enum NodeTypeEnum : ushort
        {
            UseMaterialName_NoLoop = 0,
            UseMaterialName_Loop = 1,
            AllMaterials_NoLoop = 2, //Material name is usually some variant of "node", which is apparantly just a placeholder
            AllMaterials_Loop = 3 //Material name is usually some variant of "node", which is apparantly just a placeholder
        }

        public System.Windows.Media.SolidColorBrush PreviewBrush
        {
            get
            {
                RgbColor multColor = MultiColor.GetAverageColor();
                RgbColor rimColor = RimColor.GetAverageColor();
                RgbColor addColor = AddColor.GetAverageColor();

                byte r = (byte)((multColor.R_int + rimColor.R_int + addColor.R_int) / 3);
                byte g = (byte)((rimColor.G_int + rimColor.G_int + addColor.G_int) / 3);
                byte b = (byte)((multColor.B_int + rimColor.B_int + addColor.B_int) / 3);

                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            }
        }

        //Selected KeyframedValue is binded here for the Keyframe Editor to access
        [NonSerialized]
        private KeyframedBaseValue _selectedKeyframedValue = null;
        public KeyframedBaseValue SelectedKeyframedValue
        {
            get => _selectedKeyframedValue;
            set
            {
                if (_selectedKeyframedValue != value)
                {
                    _selectedKeyframedValue = value;
                    NotifyPropertyChanged(nameof(SelectedKeyframedValue));
                }
            }
        }

        private string _material = "node";

        //Material to apply color effect too, or just "node" to apply to all
        public string Material
        {
            get => _material;
            set
            {
                if (value != _material)
                {
                    _material = value;
                    NotifyPropertyChanged(nameof(Material));
                }
            }
        }
        public ushort StartTime { get; set; }
        //There is 1 ecf where the EndTime is before the StartTime (PWW_Fade.ecf)
        public ushort EndTime { get; set; } = 60;
        public bool Loop { get; set; }
        public bool UseMaterial { get; set; }

        public KeyframedColorValue MultiColor { get; set; } = new KeyframedColorValue(0, 0, 0, KeyframedValueType.ECF_MultiColor);
        public KeyframedColorValue RimColor { get; set; } = new KeyframedColorValue(0, 0, 0, KeyframedValueType.ECF_RimColor);
        public KeyframedColorValue AddColor { get; set; } = new KeyframedColorValue(0, 0, 0, KeyframedValueType.ECF_AmbientColor);
        public KeyframedFloatValue MultiColor_Transparency { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.ECF_DiffuseTransparency);
        public KeyframedFloatValue RimColor_Transparency { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.ECF_SpecularTransparency);
        public KeyframedFloatValue AddColor_Transparency { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.ECF_AmbientTransparency);
        public KeyframedFloatValue BlendingFactor { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.ECF_BlendingFactor);

        public ushort I_54 { get; set; } //always 0
        public ushort I_60 { get; set; } //always 0
        public ushort I_62 { get; set; } //always 0
        public ulong I_64 { get; set; } //always 0
        public ulong I_72 { get; set; } //always 0
        public ulong I_80 { get; set; } //always 0
        public int I_88 { get; set; } //always 0
        public ushort I_96 { get; set; } //always 0

        public List<EMP_KeyframedValue> KeyframedValues { get; set; } = new List<EMP_KeyframedValue>();

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();

            RgbColor diffuse = MultiColor.GetAverageColor();
            RgbColor specular = RimColor.GetAverageColor();
            RgbColor ambient = AddColor.GetAverageColor();

            if (!diffuse.IsWhiteOrBlack)
                colors.Add(diffuse);

            if (!specular.IsWhiteOrBlack)
                colors.Add(specular);

            if (!ambient.IsWhiteOrBlack)
                colors.Add(ambient);

            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0)
        {
            MultiColor.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
            RimColor.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
            AddColor.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
        }

        public EMP_KeyframedValue[] GetKeyframedValues(int parameter, params int[] components)
        {
            EMP_KeyframedValue[] values = new EMP_KeyframedValue[components.Length];

            for (int i = 0; i < components.Length; i++)
            {
                EMP_KeyframedValue value = KeyframedValues.FirstOrDefault(x => x.Parameter == parameter && x.Component == components[i]);

                if (value != null)
                    values[i] = value;
                else
                    values[i] = EMP_KeyframedValue.Default;
            }

            return values;
        }

        internal void CompileAllKeyframes()
        {
            KeyframedValues.Clear();

            AddKeyframedValues(MultiColor.CompileKeyframes());
            AddKeyframedValues(RimColor.CompileKeyframes());
            AddKeyframedValues(AddColor.CompileKeyframes());
            AddKeyframedValues(MultiColor_Transparency.CompileKeyframes());
            AddKeyframedValues(RimColor_Transparency.CompileKeyframes());
            AddKeyframedValues(AddColor_Transparency.CompileKeyframes());
            AddKeyframedValues(BlendingFactor.CompileKeyframes());
        }

        internal void AddKeyframedValues(EMP_KeyframedValue[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                {
                    if (KeyframedValues.Any(x => x.Parameter == values[i].Parameter && x.Component == values[i].Component))
                    {
                        throw new Exception($"ECF_File: KeyframedValue already exists (parameter = {values[i].Parameter}, component = {values[i].Component})");
                    }

                    KeyframedValues.Add(values[i]);
                }
            }
        }

        public override string ToString()
        {
            return $"{Material}: {StartTime}, {EndTime}";
        }
    
        public void UpdatePreviewBrush()
        {
            NotifyPropertyChanged(nameof(PreviewBrush));
        }
    }
    /*
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
    */
}
