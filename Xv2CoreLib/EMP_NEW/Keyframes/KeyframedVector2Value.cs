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
        public CustomVector4 Constant { get; set; }
        public AsyncObservableCollection<KeyframeVector2Value> Keyframes { get; set; } = new AsyncObservableCollection<KeyframeVector2Value>();

        public override bool IsConstant => Keyframes.Count == 0;
        private readonly bool IsScale;

        #region Init
        public KeyframedVector2Value(float x, float y, KeyframedValueType valueType, bool isScale = false)
        {
            Constant = new CustomVector4(x, y, 1f, 1f);
            ValueType = valueType;
            IsScale = isScale;
        }

        public void DecompileKeyframes(params EMP_KeyframedValue[] empKeyframes)
        {
            if (empKeyframes.Length != 2)
                throw new ArgumentException($"KeyframedVector2Value.DecompileKeyframes: Invalid number of keyframed values. Expected 2, but there are {empKeyframes.Length}!");

            bool interpolate;
            bool loop;

            //Returns array of 3: X, Y
            List<KeyframedGenericValue>[] tempKeyframes = Decompile(Constant.Values, out interpolate, out loop, empKeyframes);

            Interpolate = interpolate;
            Loop = loop;

            if (Keyframes.Count > 0)
                Keyframes.Clear();

            for (int i = 0; i < tempKeyframes[0].Count; i++)
            {
                float scale = IsScale ? 2f : 1f;
                Keyframes.Add(new KeyframeVector2Value(tempKeyframes[0][i].Time / 100f, tempKeyframes[0][i].Value * scale, tempKeyframes[1][i].Value * scale));
            }
        }

        public EMP_KeyframedValue[] CompileKeyframes(bool isScaleXyEnabled = false)
        {
            SetParameterAndComponents(false, isScaleXyEnabled);
            EMP_KeyframedValue[] keyframes = Compile(Constant.Values, GetGenericKeyframes());

            //Rescale keyframes to EMP standards (half-scale)
            if (IsScale)
            {
                foreach(var keyframedValue in keyframes)
                {
                    if(keyframedValue != null)
                    {
                        foreach (var keyframe in keyframedValue.Keyframes)
                        {
                            keyframe.Value /= 2;
                        }
                    }
                }
            }

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

        public IUndoRedo AddKeyframe(float time, float x, float y, float z)
        {
            KeyframeVector2Value keyframe = new KeyframeVector2Value(time, x, y);
            Keyframes.Add(keyframe);

            return new UndoableListAdd<KeyframeVector2Value>(Keyframes, keyframe);
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

    }

    [Serializable]
    public class KeyframeVector2Value
    {
        public float Time { get; set; }
        public CustomVector4 Value { get; set; }

        public KeyframeVector2Value(float time, float x, float y)
        {
            Time = time;
            Value = new CustomVector4(x, y, 1f, 1f);
        }
    }
}
