using System;
using System.Collections.Generic;
using System.Linq;
using LB_Common.Numbers;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMP_NEW.Keyframes
{
    [Serializable]
    public class KeyframedVector2Value : KeyframedBaseValue
    {
        public const string CLIPBOARD_ID = "EMP_KeyframedVector2Value";

        public CustomVector4 Constant { get; set; }
        public AsyncObservableCollection<KeyframeVector2Value> Keyframes { get; set; } = new AsyncObservableCollection<KeyframeVector2Value>();

        private float[] InterpolatedValues = new float[4];

        #region Init
        public KeyframedVector2Value(float x, float y, KeyframedValueType valueType)
        {
            Constant = new CustomVector4(x, y, 1f, 1f);
            ValueType = valueType;
        }

        public void DecompileKeyframes(params EMP_KeyframedValue[] empKeyframes)
        {
            if (empKeyframes.Length != 2)
                throw new ArgumentException($"KeyframedVector2Value.DecompileKeyframes: Invalid number of keyframed values. Expected 2, but there are {empKeyframes.Length}!");

            float[] constantValue = Constant.Values;

            //Returns array of 3: X, Y
            List<KeyframedGenericValue>[] tempKeyframes = Decompile(constantValue, empKeyframes);

            if (Keyframes.Count > 0)
                Keyframes.Clear();

            for (int i = 0; i < tempKeyframes[0].Count; i++)
            {
                Keyframes.Add(new KeyframeVector2Value(tempKeyframes[0][i].Time / 100f, tempKeyframes[0][i].Value, tempKeyframes[1][i].Value));
            }
        }

        public EMP_KeyframedValue[] CompileKeyframes(bool isScaleXyEnabled = false)
        {
            SetParameterAndComponents(false, isScaleXyEnabled);
            EMP_KeyframedValue[] keyframes = Compile(Constant.Values, GetGenericKeyframes());

            return keyframes;
        }

        private List<KeyframedGenericValue>[] GetGenericKeyframes()
        {
            List<KeyframedGenericValue>[] keyframes = new List<KeyframedGenericValue>[2];
            keyframes[0] = new List<KeyframedGenericValue>();
            keyframes[1] = new List<KeyframedGenericValue>();

            for (int i = 0; i < Keyframes.Count; i++)
            {
                ushort time = (ushort)(Keyframes[i].Time * 100f);
                keyframes[0].Add(new KeyframedGenericValue(time, Keyframes[i].Value.X));
                keyframes[1].Add(new KeyframedGenericValue(time, Keyframes[i].Value.Y));
            }

            return keyframes;
        }

        #endregion

        public IUndoRedo AddKeyframe(float time, float x, float y)
        {
            KeyframeVector2Value keyframe = Keyframes.FirstOrDefault(a => a.Time == time);

            if (keyframe == null)
            {
                keyframe = new KeyframeVector2Value(time, x, y);
                Keyframes.Add(keyframe);

                return new UndoableListAdd<KeyframeVector2Value>(Keyframes, keyframe, "Add Keyframe");
            }
            else
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                undos.Add(new UndoablePropertyGeneric("X", keyframe.Value, keyframe.Value.X, x));
                undos.Add(new UndoablePropertyGeneric("Y", keyframe.Value, keyframe.Value.Y, y));

                keyframe.Value.X = x;
                keyframe.Value.Y = y;

                return new CompositeUndo(undos, "Add Keyframe");
            }
        }

        public IUndoRedo RemoveKeyframe(float time)
        {
            return RemoveKeyframe(Keyframes.FirstOrDefault(x => x.Time == time));
        }

        public IUndoRedo RemoveKeyframe(KeyframeVector2Value keyframe)
        {
            if (keyframe == null) return null;
            Keyframes.Remove(keyframe);

            return new UndoableListRemove<KeyframeVector2Value>(Keyframes, keyframe);
        }

        public float[] GetInterpolatedValue(float time)
        {
            if (Keyframes.Count == 0 || !IsAnimated) return Constant.Values;

            //Check for a direct keyframe
            var currentKeyframe = Keyframes.FirstOrDefault(x => x.Time == time);

            if (currentKeyframe != null)
                return currentKeyframe.Value.Values;

            float prev = -1;
            float next = -1;

            foreach (var keyframe in Keyframes.OrderBy(x => x.Time))
            {
                if (keyframe.Time > prev && prev < time && keyframe.Time < time)
                    prev = keyframe.Time;

                if (keyframe.Time > time)
                {
                    next = keyframe.Time;
                    break;
                }
            }

            //No prev keyframe exists, so no interpolation is possible. Just use next keyframe then
            if (prev == -1)
            {
                return Keyframes.FirstOrDefault(x => x.Time == next).Value.Values;
            }

            //Same, but for next keyframe. We will use the prev keyframe here.
            if (next == -1 || prev == next)
            {
                return Keyframes.FirstOrDefault(x => x.Time == prev).Value.Values;
            }

            float factor = (time - prev) / (next - prev);
            var prevKeyframe = Keyframes.FirstOrDefault(x => x.Time == prev).Value;
            var nextKeyframe = Keyframes.FirstOrDefault(x => x.Time == next).Value;

            //Reuse the same array to save on performance
            if (Interpolate)
            {
                InterpolatedValues[0] = MathHelpers.Lerp(prevKeyframe.X, nextKeyframe.X, factor);
                InterpolatedValues[1] = MathHelpers.Lerp(prevKeyframe.Y, nextKeyframe.Y, factor);
            }
            else
            {
                InterpolatedValues[0] = prevKeyframe.X;
                InterpolatedValues[1] = prevKeyframe.Y;
            }

            return InterpolatedValues;
        }
    }

    [Serializable]
    public class KeyframeVector2Value : KeyframeBaseValue
    {
        public CustomVector4 Value { get; set; }

        public KeyframeVector2Value(float time, float x, float y)
        {
            Time = time;
            Value = new CustomVector4(x, y, 1f, 1f);
        }
    }
}
