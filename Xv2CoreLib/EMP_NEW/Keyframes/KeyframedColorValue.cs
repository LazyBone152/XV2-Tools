using System;
using System.Collections.Generic;
using System.Linq;
using LB_Common.Numbers;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMP_NEW.Keyframes
{
    [Serializable]
    public class KeyframedColorValue : KeyframedBaseValue
    {
        public CustomColor Constant { get; set; }
        public AsyncObservableCollection<KeyframeColorValue> Keyframes { get; set; } = new AsyncObservableCollection<KeyframeColorValue>();

        public override bool IsConstant => Keyframes.Count == 0;

        #region Init
        public KeyframedColorValue(float r, float g, float b, KeyframedValueType valueType)
        {
            Constant = new CustomColor(r, g, b, 1f);
            ValueType = valueType;
        }

        public void DecompileKeyframes(params EMP_KeyframedValue[] empKeyframes)
        {
            if (empKeyframes.Length != 3)
                throw new ArgumentException($"KeyframedColorValue.DecompileKeyframes: Invalid number of keyframed values. Expected 3, but there are {empKeyframes.Length}!");

            bool interpolate;
            bool loop;

            //Returns array of 3: R, G, B
            List<KeyframedGenericValue>[] tempKeyframes = Decompile(Constant.Values, out interpolate, out loop, empKeyframes);

            Interpolate = interpolate;
            Loop = loop;

            if (Keyframes.Count > 0)
                Keyframes.Clear();

            for(int i = 0; i < tempKeyframes[0].Count; i++)
            {
                Keyframes.Add(new KeyframeColorValue(tempKeyframes[0][i].Time / 100f, tempKeyframes[0][i].Value, tempKeyframes[1][i].Value, tempKeyframes[2][i].Value));
            }
        }

        public EMP_KeyframedValue[] CompileKeyframes()
        {
            SetParameterAndComponents();
            EMP_KeyframedValue[] keyframes = Compile(Constant.Values, GetGenericKeyframes());

            return keyframes;
        }

        private List<KeyframedGenericValue>[] GetGenericKeyframes()
        {
            List<KeyframedGenericValue>[] keyframes = new List<KeyframedGenericValue>[3];
            keyframes[0] = new List<KeyframedGenericValue>();
            keyframes[1] = new List<KeyframedGenericValue>();
            keyframes[2] = new List<KeyframedGenericValue>();

            for (int i = 0; i < Keyframes.Count; i++)
            {
                ushort time = (ushort)(Keyframes[i].Time * 100f);
                keyframes[0].Add(new KeyframedGenericValue(time, Keyframes[i].Value.R));
                keyframes[1].Add(new KeyframedGenericValue(time, Keyframes[i].Value.G));
                keyframes[2].Add(new KeyframedGenericValue(time, Keyframes[i].Value.B));
            }

            return keyframes;
        }

        #endregion

        public IUndoRedo AddKeyframe(float time, float r, float g, float b)
        {
            KeyframeColorValue keyframe = new KeyframeColorValue(time, r, g, b);
            Keyframes.Add(keyframe);

            return new UndoableListAdd<KeyframeColorValue>(Keyframes, keyframe);
        }

        public IUndoRedo RemoveKeyframe(float time)
        {
            return RemoveKeyframe(Keyframes.FirstOrDefault(x => x.Time == time));
        }

        public IUndoRedo RemoveKeyframe(KeyframeColorValue keyframe)
        {
            if (keyframe == null) return null;
            Keyframes.Remove(keyframe);

            return new UndoableListRemove<KeyframeColorValue>(Keyframes, keyframe);
        }

        public List<IUndoRedo> RemoveAllKeyframes()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>(Keyframes.Count);

            for (int i = Keyframes.Count - 1; i >= 0; i--)
            {
                undos.Add(new UndoableListRemove<KeyframeColorValue>(Keyframes, Keyframes[i]));
                Keyframes.RemoveAt(i);
            }

            return undos;
        }

        /// <summary>
        /// Gets the average color between the constant value and all keyframes.
        /// </summary>
        public RgbColor GetAverageColor()
        {
            List<RgbColor> colors = new List<RgbColor>();

            if(!Constant.IsWhiteOrBlack())
                colors.Add(new RgbColor(Constant));

            foreach(var keyframe in Keyframes)
            {
                if (!keyframe.Value.IsWhiteOrBlack())
                    colors.Add(new RgbColor(keyframe.Value));
            }

            return colors.Count > 0 ? ColorEx.GetAverageColor(colors) : new RgbColor(Constant);
        }
    
        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0)
        {
            Constant.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);

            foreach(var keyframe in Keyframes)
            {
                keyframe.Value.ChangeHue( hue, saturation, lightness, undos, hueSet, variance);
            }
        }

    }

    [Serializable]
    public class KeyframeColorValue
    {
        public float Time { get; set; }
        public CustomColor Value { get; set; }

        public KeyframeColorValue(float time, float r, float g, float b)
        {
            Time = time;
            Value = new CustomColor(r, g, b, 1f);
        }
    }
}
