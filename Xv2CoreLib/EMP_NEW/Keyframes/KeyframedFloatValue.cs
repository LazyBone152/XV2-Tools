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
        public KeyframedFloatValue(float value, KeyframedValueType valueType, bool isEtr = false, bool isModifier = false, bool isScale = false)
        {
            ValueType = valueType;
            Constant = value;
            IsScale = isScale;
            IsEtrValue = isEtr;
            IsModifierValue = isModifier;
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

            //Reduce time down to a 0 to 1 range for EMP and ECF, since they are based on the lifetime (ETR is directly based on frames)
            float timeScale = IsEtrValue ? 1f : 100f;

            for (int i = 0; i < tempKeyframes[0].Count; i++)
            {
                //Scale values within an EMP are half-scales, where 0.5 is full scaled. So it makes more sense, we will just scale the keyframes by 2 so 1.0 becomes full scale
                float scale = IsScale ? 2f : 1f;

                Keyframes.Add(new KeyframeFloatValue(tempKeyframes[0][i].Time / timeScale, tempKeyframes[0][i].Value * scale));
            }

            //Set constant value for modifer keyframes
            if (IsModifierValue)
            {
                if (IsEtrValue && Keyframes.Count > 0)
                {
                    //ETR has no default value defined on keyframes, so just use the keyframe values 
                    Constant = Keyframes[0].Value;
                }
                else if(!IsEtrValue)
                {
                    //EMP
                    Constant = empKeyframes[0].DefaultValue;
                }
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
            float timeScale = IsEtrValue ? 1f : 100f;

            for (int i = 0; i < Keyframes.Count; i++)
            {
                ushort time = (ushort)(Keyframes[i].Time * timeScale);
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
                keyframe.NotifyPropsChanged();

                return new CompositeUndo(new List<IUndoRedo>() { undo, new UndoActionDelegate(keyframe, "NotifyPropsChanged", true) }, "Add Keyframe");
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

        public List<IUndoRedo> RescaleValue(float scaleFactor)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            float value = Constant * scaleFactor;
            undos.Add(new UndoablePropertyGeneric(nameof(Constant), this, Constant, value));
            Constant = value;

            foreach(var keyframe in Keyframes)
            {
                value = keyframe.Value * scaleFactor;
                undos.Add(new UndoablePropertyGeneric(nameof(keyframe.Value), keyframe, keyframe.Value, value));
                keyframe.Value = value;
            }

            return undos;
        }

        public float GetInterpolatedValue(float time)
        {
            if (Keyframes.Count == 0 || !IsAnimated) return Constant;

            //Check for a direct keyframe
            KeyframeFloatValue currentKeyframe = Keyframes.FirstOrDefault(x => x.Time == time);

            if (currentKeyframe != null)
                return currentKeyframe.Value;

            //Interpolate the value from existing keyframes
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
                return Keyframes.FirstOrDefault(x => x.Time == next).Value;
            }

            //Same, but for next keyframe. We will use the prev keyframe here.
            if (next == -1 || prev == next)
            {
                return Keyframes.FirstOrDefault(x => x.Time == prev).Value;
            }

            float factor = (time - prev) / (next - prev);
            var prevKeyframe = Keyframes.FirstOrDefault(x => x.Time == prev).Value;
            var nextKeyframe = Keyframes.FirstOrDefault(x => x.Time == next).Value;

            return Interpolate ? MathHelpers.Lerp(prevKeyframe, nextKeyframe, factor) : prevKeyframe;
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
