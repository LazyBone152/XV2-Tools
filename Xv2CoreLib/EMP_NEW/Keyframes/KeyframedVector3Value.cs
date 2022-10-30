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

        #region Init
        public KeyframedVector3Value(float x, float y, float z, KeyframedValueType valueType)
        {
            Constant = new CustomVector4(x, y, z, 1f);
            ValueType = valueType;
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

            for (int i = 0; i < tempKeyframes[0].Count; i++)
            {
                Keyframes.Add(new KeyframeVector3Value(tempKeyframes[0][i].Time / 100f, tempKeyframes[0][i].Value, tempKeyframes[1][i].Value, tempKeyframes[2][i].Value));
            }
        }

        public EMP_KeyframedValue[] CompileKeyframes()
        {
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
                CustomVector4 vector = new CustomVector4(x, y, z, 0f);
                IUndoRedo undo = new UndoablePropertyGeneric(nameof(keyframe.Value), keyframe, keyframe.Value, vector, "Add Keyframe");
                keyframe.Value = vector;

                return undo;
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
