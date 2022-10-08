using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMP_NEW.Keyframes
{
    [Serializable]
    public class KeyframedFloatValue : KeyframedBaseValue
    {
        public float Constant { get; set; }
        public AsyncObservableCollection<KeyframeFloatValue> Keyframes { get; set; } = new AsyncObservableCollection<KeyframeFloatValue>();

        public override bool IsConstant => Keyframes.Count == 0;

        //The EMP_KeyframedValue ID values. These will be used when re-compiling the keyframes.
        private readonly bool IsScale;

        #region Init
        public KeyframedFloatValue(float value, KeyframedValueType valueType, bool isScale = false)
        {
            ValueType = valueType;
            Constant = value;
            IsScale = isScale;
        }

        public void DecompileKeyframes(params EMP_KeyframedValue[] empKeyframes)
        {
            if (empKeyframes.Length != 1)
                throw new ArgumentException($"KeyframedFloatValue.DecompileKeyframes: Invalid number of keyframed values. Expected 1, but there are {empKeyframes.Length}!");

            bool interpolate;
            bool loop;

            //Returns array of 1
            List<KeyframedGenericValue>[] tempKeyframes = Decompile(new float[] { Constant } ,out interpolate, out loop, empKeyframes);

            Interpolate = interpolate;
            Loop = loop;

            if (Keyframes.Count > 0)
                Keyframes.Clear();

            for (int i = 0; i < tempKeyframes[0].Count; i++)
            {
                //Scale values within an EMP are half-scales, where 0.5 is full scaled. So it makes more sense, we will just scale the keyframes by 2 so 1.0 becomes full scale
                float scale = IsScale ? 2f : 1f;

                Keyframes.Add(new KeyframeFloatValue(tempKeyframes[0][i].Time / 100f, tempKeyframes[0][i].Value * scale));
            }
        }

        public EMP_KeyframedValue[] CompileKeyframes(bool isScaleXyEnabled = false, bool isSphere = false)
        {
            SetParameterAndComponents(isSphere, isScaleXyEnabled);
            EMP_KeyframedValue[] keyframes = Compile(new float[] { Constant }, GetGenericKeyframes());

            //Rescale keyframes to EMP standards (half-scale)
            if (IsScale && keyframes[0] != null)
            {
                foreach (var keyframe in keyframes[0].Keyframes)
                {
                    keyframe.Value /= 2;
                }
            }

            return keyframes;
        }

        private List<KeyframedGenericValue>[] GetGenericKeyframes()
        {
            List<KeyframedGenericValue>[] keyframes = new List<KeyframedGenericValue>[1];
            keyframes[0] = new List<KeyframedGenericValue>();

            for (int i = 0; i < Keyframes.Count; i++)
            {
                ushort time = (ushort)(Keyframes[i].Time * 100f);
                keyframes[0].Add(new KeyframedGenericValue(time, Keyframes[i].Value));
            }

            return keyframes;
        }

        #endregion

        public IUndoRedo AddKeyframe(float time, float value)
        {
            KeyframeFloatValue keyframe = new KeyframeFloatValue(time, value);
            Keyframes.Add(keyframe);

            return new UndoableListAdd<KeyframeFloatValue>(Keyframes, keyframe);
        }

        public IUndoRedo RemoveKeyframe(float time)
        {
            return RemoveKeyframe(Keyframes.FirstOrDefault(x => x.Time == time));
        }

        public IUndoRedo RemoveKeyframe(KeyframeFloatValue keyframe)
        {
            if (keyframe == null) return null;
            Keyframes.Remove(keyframe);

            return new UndoableListRemove<KeyframeFloatValue>(Keyframes, keyframe);
        }

    }

    [Serializable]
    public class KeyframeFloatValue
    {
        public float Time { get; set; }
        public float Value { get; set; }

        public KeyframeFloatValue(float time, float value)
        {
            Time = time;
            Value = value;
        }
    }
}
