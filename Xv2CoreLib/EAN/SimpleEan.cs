using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Xv2CoreLib.EAN.EAN_Animation;
using static Xv2CoreLib.EAN.EAN_AnimationComponent;

namespace Xv2CoreLib.EAN
{
    /// <summary>
    /// A simplified ean structure meant for easier editing and viewing.
    /// </summary>
    public class SimpleEan : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        
        public bool IsCamera { get; set; }
        public int I_08 { get; set; }
        public byte I_17 { get; set; }

        public ESK_Skeleton Skeleton { get; set; } //Taken directly from EAN
        public ObservableCollection<SimpleAnimation> Animations { get; set; }


        public static SimpleEan Load(EAN_File eanFile)
        {
            SimpleEan simpleEan = new SimpleEan();
            simpleEan.IsCamera = eanFile.IsCamera;
            simpleEan.I_08 = eanFile.I_08;
            simpleEan.I_17 = eanFile.I_17;
            simpleEan.Skeleton = eanFile.Skeleton;
            simpleEan.Animations = new ObservableCollection<SimpleAnimation>();

            foreach(var anim in eanFile.Animations)
            {
                SimpleAnimation newAnim = new SimpleAnimation();
                newAnim.Index = anim.IndexNumeric;
                newAnim.FloatSize = anim.I_03;
                newAnim.Duration = anim.I_04;
                newAnim.Nodes = new ObservableCollection<SimpleNode>();

                foreach(var node in anim.Nodes)
                {
                    SimpleNode newNode = new SimpleNode();
                    newNode.BoneName = node.BoneName;
                    newNode.Keyframes = new ObservableCollection<SimpleKeyframe>();

                    var pos = node.GetComponent(EAN_AnimationComponent.ComponentType.Position);
                    var rot = node.GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
                    var scale = node.GetComponent(EAN_AnimationComponent.ComponentType.Scale);

                    for(int i = 0; i < newAnim.Duration; i++)
                    {
                        if (node.HasKeyframe(i))
                        {
                            newNode.AddKeyframe(i, node.GetKeyframeValues(i));
                        }
                    }

                    newAnim.Nodes.Add(newNode);
                }

                simpleEan.Animations.Add(newAnim);
            }

            return simpleEan;

        }

        public int IndexOfAnimation(int IndexValue_ID)
        {
            if (Animations == null) return -1;

            for (int i = 0; i < Animations.Count; i++)
            {
                if (Animations[i].Index == IndexValue_ID) return i;
            }

            return -1;
        }

    }

    public class SimpleAnimation : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Properties
        private int _index = 0;
        public int Index
        {
            get
            {
                return this._index;
            }
            set
            {
                if (value != this._index)
                {
                    this._index = value;
                    NotifyPropertyChanged("Index");
                }
            }
        }

        private int _duration = 0;
        public int Duration
        {
            get
            {
                return this._duration;
            }
            set
            {
                if (value != this._duration)
                {
                    this._duration = value;
                    NotifyPropertyChanged("Duration");
                }
            }
        }
        
        public FloatPrecision FloatSize { get; set; } //int8
        #endregion

        public ObservableCollection<SimpleNode> Nodes { get; set; }
    }

    public class SimpleNode : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private string _boneName = null;
        public string BoneName
        {
            get
            {
                return this._boneName;
            }
            set
            {
                if (value != this._boneName)
                {
                    this._boneName = value;
                    NotifyPropertyChanged("BoneName");
                }
            }
        }

        public ObservableCollection<SimpleKeyframe> Keyframes { get; set; }

        public void AddKeyframe(int frame, float[] values)
        {
            SimpleKeyframe keyframe = GetKeyframe(frame);
            if(keyframe == null)
                keyframe = new SimpleKeyframe(frame);

            keyframe.PositionX = values[0];
            keyframe.PositionY = values[1];
            keyframe.PositionZ = values[2];
            keyframe.PositionW = values[3];

            keyframe.RotationX = values[4];
            keyframe.RotationY = values[5];
            keyframe.RotationZ = values[6];
            keyframe.RotationW = values[7];

            keyframe.ScaleX = values[8];
            keyframe.ScaleY = values[9];
            keyframe.ScaleZ = values[10];
            keyframe.ScaleW = values[11];

            if (!Keyframes.Contains(keyframe))
                Keyframes.Add(keyframe);
        }
        
        /// <summary>
        /// Gets the keyframe at the specified frame. Will return null if one doesn't exist.
        /// </summary>
        /// <returns></returns>
        public SimpleKeyframe GetKeyframe(int frame)
        {
            foreach(var keyframe in Keyframes)
            {
                if (keyframe.Frame == frame) return keyframe;
            }

            return null;
        }

        /// <summary>
        /// Creates a new keyframe at the specified frame. (If already exists, then returns that)
        /// </summary>
        /// <returns></returns>
        public SimpleKeyframe CreateKeyframe(int frame)
        {
            var existing = GetKeyframe(frame);
            if (existing != null) return existing;

            existing = new SimpleKeyframe(frame);

            if(Keyframes.Count > 0)
            {
                existing.PositionX = GetKeyframeValue(frame, ComponentType.Position, Axis.X);
                existing.PositionY = GetKeyframeValue(frame, ComponentType.Position, Axis.Y);
                existing.PositionZ = GetKeyframeValue(frame, ComponentType.Position, Axis.Z);
                existing.PositionW = GetKeyframeValue(frame, ComponentType.Position, Axis.W);

                existing.RotationX = GetKeyframeValue(frame, ComponentType.Rotation, Axis.X);
                existing.RotationY = GetKeyframeValue(frame, ComponentType.Rotation, Axis.Y);
                existing.RotationZ = GetKeyframeValue(frame, ComponentType.Rotation, Axis.Z);
                existing.RotationW = GetKeyframeValue(frame, ComponentType.Rotation, Axis.W);

                existing.ScaleX = GetKeyframeValue(frame, ComponentType.Scale, Axis.X);
                existing.ScaleY = GetKeyframeValue(frame, ComponentType.Scale, Axis.Y);
                existing.ScaleZ = GetKeyframeValue(frame, ComponentType.Scale, Axis.Z);
                existing.ScaleW = GetKeyframeValue(frame, ComponentType.Scale, Axis.W);
            }

            Keyframes.Add(existing);

            return existing;
        }

        /// <summary>
        /// Gets an interpolated keyframe value.
        /// </summary>
        /// <returns></returns>
        public float GetKeyframeValue(int frame, ComponentType component, Axis axis)
        {
            //Check if a keyframe exists, and return that if it does.
            var existing = GetKeyframe(frame);

            if(existing != null)
            {
                if (component == ComponentType.Position)
                {
                    switch (axis)
                    {
                        case Axis.X:
                            return existing.PositionX;
                        case Axis.Y:
                            return existing.PositionY;
                        case Axis.Z:
                            return existing.PositionZ;
                        case Axis.W:
                            return existing.PositionW;
                    }
                }
                else if (component == ComponentType.Rotation)
                {
                    switch (axis)
                    {
                        case Axis.X:
                            return existing.RotationX;
                        case Axis.Y:
                            return existing.RotationY;
                        case Axis.Z:
                            return existing.RotationZ;
                        case Axis.W:
                            return existing.RotationW;
                    }
                }
                else if (component == ComponentType.Scale)
                {
                    switch (axis)
                    {
                        case Axis.X:
                            return existing.ScaleX;
                        case Axis.Y:
                            return existing.ScaleY;
                        case Axis.Z:
                            return existing.ScaleZ;
                        case Axis.W:
                            return existing.ScaleW;
                    }
                }
            }

            //A keyframe doesn't exist so we must calculate the value
            SimpleKeyframe previousKeyframe = GetNearestKeyframeBefore(frame);
            SimpleKeyframe nextKeyframe = GetNearestKeyframeAfter(frame);

            //calculate the inbetween value from this frame and the previous frame
            float currentKeyframeValue = 0;
            float previousKeyframeValue = 0;

            if(component == ComponentType.Position)
            {
                switch (axis)
                {
                    case Axis.X:
                        currentKeyframeValue = nextKeyframe.PositionX;
                        previousKeyframeValue = previousKeyframe.PositionX;
                        break;
                    case Axis.Y:
                        currentKeyframeValue = nextKeyframe.PositionY;
                        previousKeyframeValue = previousKeyframe.PositionY;
                        break;
                    case Axis.Z:
                        currentKeyframeValue = nextKeyframe.PositionZ;
                        previousKeyframeValue = previousKeyframe.PositionZ;
                        break;
                    case Axis.W:
                        currentKeyframeValue = nextKeyframe.PositionW;
                        previousKeyframeValue = previousKeyframe.PositionW;
                        break;
                }
            }
            else if (component == ComponentType.Rotation)
            {
                switch (axis)
                {
                    case Axis.X:
                        currentKeyframeValue = nextKeyframe.RotationX;
                        previousKeyframeValue = previousKeyframe.RotationX;
                        break;
                    case Axis.Y:
                        currentKeyframeValue = nextKeyframe.RotationY;
                        previousKeyframeValue = previousKeyframe.RotationY;
                        break;
                    case Axis.Z:
                        currentKeyframeValue = nextKeyframe.RotationZ;
                        previousKeyframeValue = previousKeyframe.RotationZ;
                        break;
                    case Axis.W:
                        currentKeyframeValue = nextKeyframe.RotationW;
                        previousKeyframeValue = previousKeyframe.RotationW;
                        break;
                }
            }
            else if (component == ComponentType.Scale)
            {
                switch (axis)
                {
                    case Axis.X:
                        currentKeyframeValue = nextKeyframe.ScaleX;
                        previousKeyframeValue = previousKeyframe.ScaleX;
                        break;
                    case Axis.Y:
                        currentKeyframeValue = nextKeyframe.ScaleY;
                        previousKeyframeValue = previousKeyframe.ScaleY;
                        break;
                    case Axis.Z:
                        currentKeyframeValue = nextKeyframe.ScaleZ;
                        previousKeyframeValue = previousKeyframe.ScaleZ;
                        break;
                    case Axis.W:
                        currentKeyframeValue = nextKeyframe.ScaleW;
                        previousKeyframeValue = previousKeyframe.ScaleW;
                        break;
                }
            }

            //Frame difference between previous frame and the current frame (current frame is AFTER the frame we want)
            int diff = nextKeyframe.Frame - previousKeyframe.Frame;

            //Keyframe value difference
            float keyframe2 = currentKeyframeValue - previousKeyframeValue;

            //Difference between the frame we WANT and the previous frame
            int diff2 = frame - previousKeyframe.Frame;

            //Divide keyframe value difference by the keyframe time difference, and then multiply it by diff2, then add the previous keyframe value
            return (keyframe2 / diff) * diff2 + previousKeyframeValue;
        }
        
        private SimpleKeyframe GetNearestKeyframeBefore(int frame)
        {
            SimpleKeyframe nearest = GetKeyframe(0);
            if (nearest == null)
                nearest = CreateKeyframe(0);

            foreach(var keyframe in Keyframes)
            {
                if(keyframe.Frame < frame && keyframe.Frame > nearest.Frame)
                {
                    nearest = keyframe;
                }
            }

            return nearest;
        }

        private SimpleKeyframe GetNearestKeyframeAfter(int frame)
        {
            SimpleKeyframe nearest = GetKeyframe(GetLastKeyframeValue());
            if (nearest == null)
                nearest = CreateKeyframe(0);

            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Frame > frame && keyframe.Frame < nearest.Frame)
                {
                    nearest = keyframe;
                }
            }

            return nearest;
        }

        public int GetLastKeyframeValue()
        {
            int value = 0;

            foreach(var keyframe in Keyframes)
            {
                if (keyframe.Frame > value) value = keyframe.Frame;
            }

            return value;
        }
    }

    public class SimpleKeyframe : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion



        #region Fields
        private int _frame = 0;

        private float _positionX = 0f;
        private float _positionY = 0f;
        private float _positionZ = 0f;
        private float _positionW = 0f;

        private float _rotationX = 0f;
        private float _rotationY = 0f;
        private float _rotationZ = 0f;
        private float _rotationW = 0f;

        private float _scaleX = 0f;
        private float _scaleY = 0f;
        private float _scaleZ = 0f;
        private float _scaleW = 0f;
        #endregion

        #region Properties
        public int Frame
        {
            get
            {
                return this._frame;
            }
            set
            {
                if (value != this._frame)
                {
                    this._frame = value;
                    NotifyPropertyChanged("Frame");
                }
            }
        }
        
        public float PositionX
        {
            get
            {
                return this._positionX;
            }
            set
            {
                if (value != this._positionX)
                {
                    this._positionX = value;
                    NotifyPropertyChanged("PositionX");
                }
            }
        }
        public float PositionY
        {
            get
            {
                return this._positionY;
            }
            set
            {
                if (value != this._positionY)
                {
                    this._positionY = value;
                    NotifyPropertyChanged("PositionY");
                }
            }
        }
        public float PositionZ
        {
            get
            {
                return this._positionZ;
            }
            set
            {
                if (value != this._positionZ)
                {
                    this._positionZ = value;
                    NotifyPropertyChanged("PositionZ");
                }
            }
        }
        public float PositionW
        {
            get
            {
                return this._positionW;
            }
            set
            {
                if (value != this._positionW)
                {
                    this._positionW = value;
                    NotifyPropertyChanged("PositionW");
                }
            }
        }

        public float RotationX
        {
            get
            {
                return this._rotationX;
            }
            set
            {
                if (value != this._rotationX)
                {
                    this._rotationX = value;
                    NotifyPropertyChanged("RotationX");
                }
            }
        }
        public float RotationY
        {
            get
            {
                return this._rotationY;
            }
            set
            {
                if (value != this._rotationY)
                {
                    this._rotationY = value;
                    NotifyPropertyChanged("RotationY");
                }
            }
        }
        public float RotationZ
        {
            get
            {
                return this._rotationZ;
            }
            set
            {
                if (value != this._rotationZ)
                {
                    this._rotationZ = value;
                    NotifyPropertyChanged("RotationZ");
                }
            }
        }
        public float RotationW
        {
            get
            {
                return this._rotationW;
            }
            set
            {
                if (value != this._rotationW)
                {
                    this._rotationW = value;
                    NotifyPropertyChanged("RotationW");
                }
            }
        }

        public float ScaleX
        {
            get
            {
                return this._scaleX;
            }
            set
            {
                if (value != this._scaleX)
                {
                    this._scaleX = value;
                    NotifyPropertyChanged("ScaleX");
                }
            }
        }
        public float ScaleY
        {
            get
            {
                return this._scaleY;
            }
            set
            {
                if (value != this._scaleY)
                {
                    this._scaleY = value;
                    NotifyPropertyChanged("ScaleY");
                }
            }
        }
        public float ScaleZ
        {
            get
            {
                return this._scaleZ;
            }
            set
            {
                if (value != this._scaleZ)
                {
                    this._scaleZ = value;
                    NotifyPropertyChanged("ScaleZ");
                }
            }
        }
        public float ScaleW
        {
            get
            {
                return this._scaleW;
            }
            set
            {
                if (value != this._scaleW)
                {
                    this._scaleW = value;
                    NotifyPropertyChanged("ScaleW");
                }
            }
        }

        public float Focale
        {
            get
            {
                return this._scaleX;
            }
            set
            {
                if (value != this._scaleX)
                {
                    this._scaleX = value;
                    NotifyPropertyChanged("Focale");
                }
            }
        }
        public float Roll
        {
            get
            {
                return this._scaleY;
            }
            set
            {
                if (value != this._scaleY)
                {
                    this._scaleY = value;
                    NotifyPropertyChanged("Roll");
                }
            }
        }
        #endregion

        public SimpleKeyframe() { }

        public SimpleKeyframe(int frame)
        {
            Frame = frame;
        }
    }

}
