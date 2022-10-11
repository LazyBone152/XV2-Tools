using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMP_NEW.Keyframes
{
    [Serializable]
    public abstract class KeyframedBaseValue : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private bool _loop = false;
        private bool _interpolate = true;

        public bool IsAnimated { get; set; }
        public bool UndoableIsAnimated
        {
            get => IsAnimated;
            set
            {
                if(IsAnimated != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(IsAnimated), this, IsAnimated, value, value ? $"{ValueType} animation enabled" : $"{ValueType} animation disabled"));
                    IsAnimated = value;
                    NotifyPropertyChanged(nameof(UndoableIsAnimated));
                }
            }
        }
        public bool Loop
        {
            get => _loop;
            set
            {
                if(_loop != value)
                {
                    _loop = value;
                    NotifyPropertyChanged(nameof(Loop));
                }
            }
        }
        public bool Interpolate
        {
            get => _interpolate;
            set
            {
                if (_interpolate != value)
                {
                    _interpolate = value;
                    NotifyPropertyChanged(nameof(Interpolate));
                }
            }
        }
        public virtual KeyframedValueType ValueType { get; protected set; }
        protected byte Parameter { get; set; }
        public byte[] Components { get; set; }

        /// <summary>
        /// Decompiles an array of <see cref="EMP_KeyframedValue"/> into an array of keyframes with synchronized timings. 
        /// </summary>
        protected List<KeyframedGenericValue>[] Decompile(float[] constant, params EMP_KeyframedValue[] keyframeValues)
        {
            //Interpolation setting should be shared between all EMP_KeyframedValues for the same parameter/value (and it is the case in all vanilla EMP files for XV2, SDBH and Breakers)
            Interpolate = keyframeValues[0].Interpolate;
            IsAnimated = false;

            List<KeyframedGenericValue>[] values = new List<KeyframedGenericValue>[keyframeValues.Length];

            //Add all keyframes from EMP
            for(int i = 0; i < keyframeValues.Length; i++)
            {
                if (!keyframeValues[i].IsDefault)
                {
                    IsAnimated = true;
                }

                values[i] = new List<KeyframedGenericValue>();

                if(keyframeValues[i].Keyframes.Count == 0 || keyframeValues[i].IsDefault)
                {
                    //In this case, the value isn't animated and so the constant should be used
                    values[i].Add(new KeyframedGenericValue(0, constant[i]));
                }
                else
                {
                    foreach (var keyframe in keyframeValues[i].Keyframes)
                    {
                        int idx = values[i].IndexOf(values[i].FirstOrDefault(x => x.Time == keyframe.Time));

                        if(idx == -1)
                        {
                            values[i].Add(new KeyframedGenericValue(keyframe.Time, keyframe.Value));
                        }
                        else
                        {
                            //For whatever MORONIC reason... some EMPs have multiple keyframes at the same time. 
                            //We will just be keeping the last keyframe of these duplicates, so just overwrite the previous here
                            values[i][idx] = new KeyframedGenericValue(keyframe.Time, keyframe.Value);
                        }
                    }
                }
            }

            //Handle loops
            Loop = false;

            if(keyframeValues.All(x => x.Loop))
            {
                Loop = true;
            }
            else if (keyframeValues.Any(x => x.Loop))
            {
                //At least one set of keyframes have loop enabled, but not all do
                //The way we're decompiling the keyframes doesn't allow mixed-loops like this, so we must manually add looped keyframes to fill out the duration.
                //The duration must be a minimum of 101 (the particles whole life time). This also means that non-looped values will need to be extended as well, if they are less than that.
                int newDuration = keyframeValues.Max(x => x.Duration) > 101 ? keyframeValues.Max(x => x.Duration) : 101;

                for(int i = 0; i < values.Length; i++)
                {
                    if (!keyframeValues[i].Loop) continue;

                    int loopDuration = keyframeValues[i].Duration;
                    int currentDuration = loopDuration;

                    while(currentDuration < newDuration)
                    {
                        for (int a = 0; a < loopDuration; a++)
                        {
                            if (currentDuration + a >= newDuration) break;

                            values[i].Add(new KeyframedGenericValue((ushort)(currentDuration + a), EMP_Keyframe.GetInterpolatedKeyframe(values[i], a, keyframeValues[i].Interpolate)));
                        }

                        currentDuration += loopDuration;
                    }

                }
            }

            //Add interpolated values for missing keyframes, or if interpolation is disabled, add in the last actual keyframe
            //This aligns all keyframes list together, ensuring they have keyframes at the same timings
            foreach(var keyframe in KeyframedGenericValue.AllKeyframeTimes(values))
            {
                for(int i = 0; i < values.Length; i++)
                {
                    //Check if keyframe exists at this time in the current values
                    if (values[i].FirstOrDefault(x => x.Time == keyframe) == null)
                    {
                        values[i].Add(new KeyframedGenericValue(keyframe, EMP_Keyframe.GetInterpolatedKeyframe(values[i], keyframe, Interpolate)));
                    }
                }
            }

            //Final sorting pass
            for(int i = 0; i < values.Length; i++)
            {
                values[i].Sort((x, y) => x.Time - y.Time);
            }

            if(values.Any(x => x.Count != values[0].Count))
            {
                throw new Exception("KeyframedBaseValue.Decompile: Decompiled keyframes are out of sync.");
            }

            return values;
        }

        /// <summary>
        /// Compiles the keyframes back to an array of <see cref="EMP_KeyframedValue"/>
        /// </summary>
        protected EMP_KeyframedValue[] Compile(float[] constant, List<KeyframedGenericValue>[] keyframes)
        {
            EMP_KeyframedValue[] empKeyframes = new EMP_KeyframedValue[keyframes.Length];

            if (IsAnimated)
            {
                for (int i = 0; i < keyframes.Length; i++)
                {
                    if (keyframes[i].All(x => x.Value != constant[i]))
                    {
                        empKeyframes[i] = new EMP_KeyframedValue();
                        empKeyframes[i].Loop = Loop;
                        empKeyframes[i].Interpolate = Interpolate;
                        empKeyframes[i].Value = Parameter;
                        empKeyframes[i].Component = Components[i];

                        for (int a = 0; a < keyframes[i].Count; a++)
                        {
                            empKeyframes[i].Keyframes.Add(new EMP_Keyframe()
                            {
                                Time = keyframes[i][a].Time,
                                Value = keyframes[i][a].Value
                            });
                        }
                    }
                }
            }

            return empKeyframes;
        }

        protected void SetParameterAndComponents(bool isSphere = false, bool isScaleXyEnabled = false)
        {
            Parameter = EMP_KeyframedValue.GetParameter(ValueType, isSphere);
            Components = EMP_KeyframedValue.GetComponent(ValueType, isScaleXyEnabled);
        }
    }

    public class KeyframedGenericValue : IKeyframe
    {
        public ushort Time { get; set; }
        public float Value { get; set; }

        public KeyframedGenericValue(ushort time, float value)
        {
            Time = time;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Time}: {Value}";
        }

        public static IEnumerable<ushort> AllKeyframeTimes(IList<KeyframedGenericValue>[] values)
        {
            if (values.Length == 0) yield break;
            List<int> current = new List<int>(values[0].Count);

            for(int i = 0; i < values.Length; i++)
            {
                for(int a = 0; a < values[i].Count; a++)
                {
                    if (current.Contains(values[i][a].Time)) continue;
                    current.Add(values[i][a].Time);
                    yield return values[i][a].Time;
                }
            }
        }
    }
}
