using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMP_NEW.Keyframes
{
    [Serializable]
    public class KeyframedFloatValue : KeyframedBaseValue
    {
        public const string CLIPBOARD_ID = "EMP_KeyframedFloatValue";

        private float _constant = 0;
        public float Constant
        {
            get => _constant;
            set
            {
                _constant = value;
                NotifyPropertyChanged(nameof(Constant));
                NotifyPropertyChanged(nameof(UndoableConstant));
            }
        }
        public float UndoableConstant
        {
            get => Constant;
            set
            {
                if(Constant != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(Constant), this, Constant, value, ValueType.ToString()));
                    Constant = value;
                    NotifyPropertyChanged(nameof(UndoableConstant));
                }
            }
        }
        public AsyncObservableCollection<KeyframeFloatValue> Keyframes { get; set; } = new AsyncObservableCollection<KeyframeFloatValue>();

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

            float constant = IsScale ? Constant / 2 : Constant;

            //Returns array of 1
            List<KeyframedGenericValue>[] tempKeyframes = Decompile(new float[] { constant }, empKeyframes);

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
            KeyframeFloatValue keyframe = Keyframes.FirstOrDefault(x => x.Time == time);

            if(keyframe == null)
            {
                keyframe = new KeyframeFloatValue(time, value);
                Keyframes.Add(keyframe);
                return new UndoableListAdd<KeyframeFloatValue>(Keyframes, keyframe, "Add Keyframe");
            }
            else
            {
                IUndoRedo undo = new UndoablePropertyGeneric(nameof(keyframe.Value), keyframe, keyframe.Value, value, "Add Keyframe");
                keyframe.Value = value;
                return undo;
            }

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
    public class KeyframeFloatValue : KeyframeBaseValue
    {
        private float _value = 0f;
        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                NotifyPropertyChanged(nameof(Value));
                NotifyPropertyChanged(nameof(UndoableValue));
            }
        }
        public float UndoableValue
        {
            get => Value;
            set
            {
                if(value != Value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(Value), this, Value, value, "Keyframed Float Value"));
                    Value = value;
                }
            }
        }


        public KeyframeFloatValue(float time, float value)
        {
            Time = time;
            Value = value;
        }
    }
}
