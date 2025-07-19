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
        public const string CLIPBOARD_ID = "EMP_KeyframedColorValue";

        public CustomColor Constant { get; set; }
        public AsyncObservableCollection<KeyframeColorValue> Keyframes { get; set; } = new AsyncObservableCollection<KeyframeColorValue>();

        private float[] InterpolatedValues = new float[4];

        #region Init
        public KeyframedColorValue(float r, float g, float b, KeyframedValueType valueType, bool isEtr = false, bool isModifier = false)
        {
            Constant = new CustomColor(r, g, b, 1f);
            ValueType = valueType;
            IsEtrValue = isEtr;
            IsModifierValue = isModifier;
        }

        public void DecompileKeyframes(params EMP_KeyframedValue[] empKeyframes)
        {
            if (empKeyframes.Length != 3)
                throw new ArgumentException($"KeyframedColorValue.DecompileKeyframes: Invalid number of keyframed values. Expected 3, but there are {empKeyframes.Length}!");

            //Returns array of 3: R, G, B
            List<KeyframedGenericValue>[] tempKeyframes = Decompile(Constant.Values, empKeyframes);

            if (Keyframes.Count > 0)
                Keyframes.Clear();

            //Reduce time down to a 0 to 1 range for EMP and ECF, since they are based on the lifetime (ETR is directly based on frames)
            float timeScale = IsEtrValue ? 1f : 100f;

            for (int i = 0; i < tempKeyframes[0].Count; i++)
            {
                Keyframes.Add(new KeyframeColorValue(tempKeyframes[0][i].Time / timeScale, tempKeyframes[0][i].Value, tempKeyframes[1][i].Value, tempKeyframes[2][i].Value));
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

            float timeScale = IsEtrValue ? 1f : 100f;

            for (int i = 0; i < Keyframes.Count; i++)
            {
                ushort time = (ushort)(Keyframes[i].Time * timeScale);
                keyframes[0].Add(new KeyframedGenericValue(time, Keyframes[i].Value.R));
                keyframes[1].Add(new KeyframedGenericValue(time, Keyframes[i].Value.G));
                keyframes[2].Add(new KeyframedGenericValue(time, Keyframes[i].Value.B));
            }

            return keyframes;
        }

        #endregion

        public IUndoRedo AddKeyframe(float time, float r, float g, float b)
        {
            KeyframeColorValue keyframe = Keyframes.FirstOrDefault(a => a.Time == time);

            if (keyframe == null)
            {
                keyframe = new KeyframeColorValue(time, r, g, b);
                Keyframes.Add(keyframe);

                return new UndoableListAdd<KeyframeColorValue>(Keyframes, keyframe, "Add Keyframe");
            }
            else
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                undos.Add(new UndoablePropertyGeneric("R", keyframe.Value, keyframe.Value.R, r));
                undos.Add(new UndoablePropertyGeneric("G", keyframe.Value, keyframe.Value.G, g));
                undos.Add(new UndoablePropertyGeneric("B", keyframe.Value, keyframe.Value.B, b));

                keyframe.Value.R = r;
                keyframe.Value.G = g;
                keyframe.Value.B = b;

                return new CompositeUndo(undos, "Add Keyframe");
            }
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
        public RgbColor GetAverageColor(bool allowWhiteAndBlack = false)
        {
            List<RgbColor> colors = new List<RgbColor>();

            if (!Constant.IsWhiteOrBlack() || allowWhiteAndBlack)
                colors.Add(new RgbColor(Constant));

            if (IsAnimated)
            {
                foreach (KeyframeColorValue keyframe in Keyframes)
                {
                    if (!keyframe.Value.IsWhiteOrBlack() || allowWhiteAndBlack)
                        colors.Add(new RgbColor(keyframe.Value));
                }
            }

            return colors.Count > 0 ? ColorEx.GetAverageColor(colors) : new RgbColor(Constant);
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0, bool invertColor = false)
        {
            Constant.ChangeHue(hue, saturation, lightness, undos, hueSet, variance, invertColor);

            foreach (var keyframe in Keyframes)
            {
                keyframe.Value.ChangeHue(hue, saturation, lightness, undos, hueSet, variance, invertColor);
            }
        }

        public float[] GetInterpolatedValue(float time)
        {
            if (Keyframes.Count == 0 || !IsAnimated) return Constant.Values;

            //Check for a direct keyframe
            for (int i = 0; i < Keyframes.Count; i++)
            {
                if (time == Keyframes[i].Time)
                {
                    return Keyframes[i].Value.Values;
                }
            }

            //Interpolate the value from existing keyframes
            KeyframeColorValue prevKeyframe = null;
            KeyframeColorValue nextKeyframe = null;

            for (int i = 0; i < Keyframes.Count; i++)
            {
                var keyframe = Keyframes[i];

                if (keyframe.Time < time && (prevKeyframe?.Time < keyframe.Time || prevKeyframe == null))
                {
                    prevKeyframe = keyframe;
                }

                if (keyframe.Time >= time && (nextKeyframe?.Time > keyframe.Time || nextKeyframe == null))
                {
                    nextKeyframe = keyframe;
                }
            }

            //No prev keyframe exists, so no interpolation is possible. Just use next keyframe then
            if (prevKeyframe == null)
            {
                return nextKeyframe.Value.Values;
            }

            //Same, but for next keyframe. We will use the prev keyframe here.
            if (nextKeyframe == null || prevKeyframe == nextKeyframe)
            {
                return prevKeyframe.Value.Values;
            }

            float factor = (time - prevKeyframe.Time) / (nextKeyframe.Time - prevKeyframe.Time);

            //Reuse the same array to save on performance
            if (Interpolate)
            {
                InterpolatedValues[0] = MathHelpers.Lerp(prevKeyframe.Value.R, nextKeyframe.Value.R, factor);
                InterpolatedValues[1] = MathHelpers.Lerp(prevKeyframe.Value.G, nextKeyframe.Value.G, factor);
                InterpolatedValues[2] = MathHelpers.Lerp(prevKeyframe.Value.B, nextKeyframe.Value.B, factor);
            }
            else
            {
                InterpolatedValues[0] = prevKeyframe.Value.R;
                InterpolatedValues[1] = prevKeyframe.Value.G;
                InterpolatedValues[2] = prevKeyframe.Value.B;
            }

            return InterpolatedValues;

            //return Interpolate ? new CustomColor(MathHelpers.Lerp(prevKeyframe.R, nextKeyframe.R, factor), MathHelpers.Lerp(prevKeyframe.G, nextKeyframe.G, factor), MathHelpers.Lerp(prevKeyframe.B, nextKeyframe.B, factor), 0f) : prevKeyframe;
        }

    }

    [Serializable]
    public class KeyframeColorValue : KeyframeBaseValue
    {
        public CustomColor Value { get; set; }

        public KeyframeColorValue(float time, float r, float g, float b)
        {
            Time = time;
            Value = new CustomColor(r, g, b, 1f);
        }
    }
}
