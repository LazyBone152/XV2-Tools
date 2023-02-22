using System;
using System.Collections.Generic;
using System.Linq;
using LB_Common.Numbers;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMP_NEW.Keyframes
{
    [Serializable]
    public class KeyframedVector3Value : KeyframedBaseValue
    {
        public const string CLIPBOARD_ID = "EMP_KeyframedVector3Value";

        public CustomVector4 Constant { get; set; }
        public AsyncObservableCollection<KeyframeVector3Value> Keyframes { get; set; } = new AsyncObservableCollection<KeyframeVector3Value>();

        private float[] InterpolatedValues = new float[4];

        #region Init
        public KeyframedVector3Value(float x, float y, float z, KeyframedValueType valueType, bool isEtr = false, bool isModifier = false)
        {
            Constant = new CustomVector4(x, y, z, 1f);
            ValueType = valueType;
            IsEtrValue = isEtr;
            IsModifierValue = isModifier;
        }

        public void DecompileKeyframes(params EMP_KeyframedValue[] empKeyframes)
        {
            if (empKeyframes.Length != 3)
                throw new ArgumentException($"KeyframedVector3Value.DecompileKeyframes: Invalid number of keyframed values. Expected 3, but there are {empKeyframes.Length}!");

            SetParameterAndComponents();

            //Returns array of 3: X, Y, Z
            List<KeyframedGenericValue>[] tempKeyframes = Decompile(Constant.Values, empKeyframes);

            if (Keyframes.Count > 0)
                Keyframes.Clear();

            //Reduce time down to a 0 to 1 range for EMP and ECF, since they are based on the lifetime (ETR is directly based on frames)
            float timeScale = IsEtrValue ? 1f : 100f;

            for (int i = 0; i < tempKeyframes[0].Count; i++)
            {
                Keyframes.Add(new KeyframeVector3Value(tempKeyframes[0][i].Time / timeScale, tempKeyframes[0][i].Value, tempKeyframes[1][i].Value, tempKeyframes[2][i].Value));
            }

            //Set constant value for modifer keyframes
            if (IsModifierValue)
            {
                if (IsEtrValue && Keyframes.Count > 0)
                {
                    //ETR has no default value defined on keyframes, so just use the keyframe values 
                    Constant.X = Keyframes[0].Value.X;
                    Constant.Y = Keyframes[0].Value.Y;
                    Constant.Z = Keyframes[0].Value.Z;
                }
                else if (!IsEtrValue)
                {
                    //EMP
                    Constant.X = empKeyframes[0].DefaultValue;
                    Constant.Y = empKeyframes[1].DefaultValue;
                    Constant.Z = empKeyframes[2].DefaultValue;
                }
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
                keyframes[0].Add(new KeyframedGenericValue(time, Keyframes[i].Value.X));
                keyframes[1].Add(new KeyframedGenericValue(time, Keyframes[i].Value.Y));
                keyframes[2].Add(new KeyframedGenericValue(time, Keyframes[i].Value.Z));
            }

            return keyframes;
        }

        #endregion

        public IUndoRedo AddKeyframe(float time, float x, float y, float z)
        {
            KeyframeVector3Value keyframe = Keyframes.FirstOrDefault(a => a.Time == time);

            if(keyframe == null)
            {
                keyframe = new KeyframeVector3Value(time, x, y, z);
                Keyframes.Add(keyframe);

                return new UndoableListAdd<KeyframeVector3Value>(Keyframes, keyframe, "Add Keyframe");
            }
            else
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                undos.Add(new UndoablePropertyGeneric("X", keyframe.Value, keyframe.Value.X, x));
                undos.Add(new UndoablePropertyGeneric("Y", keyframe.Value, keyframe.Value.Y, y));
                undos.Add(new UndoablePropertyGeneric("Z", keyframe.Value, keyframe.Value.Z, z));

                keyframe.Value.X = x;
                keyframe.Value.Y = y;
                keyframe.Value.Z = z;

                return new CompositeUndo(undos, "Add Keyframe");
            }
        }

        public IUndoRedo RemoveKeyframe(float time)
        {
            return RemoveKeyframe(Keyframes.FirstOrDefault(x => x.Time == time));
        }

        public IUndoRedo RemoveKeyframe(KeyframeVector3Value keyframe)
        {
            if (keyframe == null) return null;
            Keyframes.Remove(keyframe);

            return new UndoableListRemove<KeyframeVector3Value>(Keyframes, keyframe);
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
                InterpolatedValues[2] = MathHelpers.Lerp(prevKeyframe.Z, nextKeyframe.Z, factor);
            }
            else
            {
                InterpolatedValues[0] = prevKeyframe.X;
                InterpolatedValues[1] = prevKeyframe.Y;
                InterpolatedValues[2] = prevKeyframe.Z;
            }

            return InterpolatedValues;
        }
    }

    [Serializable]
    public class KeyframeVector3Value : KeyframeBaseValue
    {
        public CustomVector4 Value { get; set; }

        public KeyframeVector3Value(float time, float x, float y, float z)
        {
            Time = time;
            Value = new CustomVector4(x, y, z, 1f);
        }
    }
}
