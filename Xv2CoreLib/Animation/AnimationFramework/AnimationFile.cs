using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EMA;
using Xv2CoreLib.ESK;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.AnimationFramework
{
    /// <summary>
    /// An object containing animations. Can load from and save to both EAN and EMA.
    /// Animations stored in a nuetral bind pose (no baked-in skeleton), ideal for editing and playback
    /// </summary>
    [Serializable]
    public class AnimationFile
    {
        public AnimationFileType AnimationType { get; private set; }

        public ESK_Skeleton Skeleton { get; set; }
        public AsyncObservableCollection<Animation> Animations { get; set; } = new AsyncObservableCollection<Animation>();

        #region EmaEanFileInfo
        //Additional values used in EMA and EAN files
        private readonly int emaVersion = 37568;
        private readonly int eanVersion = 37568;
        private readonly int ema_I_20;
        private readonly int ema_I_24;
        private readonly int ema_I_28;
        private readonly byte ean_I_17;

        #endregion

        public AnimationFile(EMA_File ema)
        {
            Skeleton = ema.Skeleton.Convert().Skeleton;

            switch (ema.EmaType)
            {
                case EmaType.obj:
                    AnimationType = AnimationFileType.Object;
                    break;
                //case EmaType.light:
                //    AnimationType = AnimationFileType.Light;
                //    break;
                //case EmaType.mat:
                //    AnimationType = AnimationFileType.Material;
                //    break;
                default:
                    throw new ArgumentException($"AnimationFile: Cannot construct a animation object out of this type of ema ({ema.EmaType}).");
            }

            ConcurrentBag<Animation> anims = new ConcurrentBag<Animation>();

            Parallel.ForEach(ema.Animations, animation =>
            {
                anims.Add(new Animation(AnimationType, animation, AnimationType == AnimationFileType.Object ? Skeleton : null));
            });

            Animations.AddRange(anims);
            Animations.Sort((x, y) => x.ID - y.ID);

            //foreach (var animation in ema.Animations)
            //{
            //    Animations.Add(new Animation(AnimationType, animation, AnimationType == AnimationFileType.Object ? Skeleton : null));
            //}
        }

        public AnimationFile(EAN_File ean)
        {
            Skeleton = ean.Skeleton;
            AnimationType = ean.IsCamera ? AnimationFileType.Camera : AnimationFileType.Object;
            ean_I_17 = ean.I_17;
            eanVersion = ean.I_08;

            ConcurrentBag<Animation> anims = new ConcurrentBag<Animation>();

            Parallel.ForEach(ean.Animations, animation =>
            {
                anims.Add(new Animation(AnimationType, animation, ean.IsCamera ? null : Skeleton));
            });

            Animations.AddRange(anims);
            Animations.Sort((x, y) => x.ID - y.ID);

            //foreach (var animation in ean.Animations)
            //{
            //    Animations.Add(new Animation(AnimationType, animation, ean.IsCamera ? null : Skeleton));
            //}
        }

        public EMA_File ConvertToEma()
        {
            EMA_File ema = new EMA_File();
            ema.Skeleton = Skeleton.ConvertToEmaSkeleton();
            ema.I_20 = ema_I_20;
            ema.I_24 = ema_I_24;
            ema.I_28 = ema_I_28;
            ema.Version = emaVersion;

            switch (AnimationType)
            {
                case AnimationFileType.Object:
                    ema.EmaType = EmaType.obj;
                    break;
                default:
                    throw new Exception($"AnimationFile.ConvertToEma: Cannot convert to this animation type to ema ({AnimationType})");
            }

            ConcurrentBag<EMA_Animation> anims = new ConcurrentBag<EMA_Animation>();

            Parallel.ForEach(Animations, animation =>
            {
                anims.Add(animation.ConvertToObjectEma(Skeleton));
            });

            //foreach (var animation in Animations)
            //{
            //    ema.Animations.Add(animation.ConvertToObjectEma(Skeleton));
            //}

            ema.Animations.AddRange(anims);
            ema.Animations.Sort((x, y) => x.Index - y.Index);

            return ema;
        }
    
        public EAN_File ConvertToEan()
        {
            EAN_File ean = new EAN_File();
            ean.Skeleton = Skeleton;
            ean.I_17 = ean_I_17;
            ean.I_08 = eanVersion;
            
            switch(AnimationType)
            {
                case AnimationFileType.Camera:
                    ean.IsCamera = true;
                    break;
                case AnimationFileType.Object:
                    ean.IsCamera = false;
                    break;
                default:
                    throw new Exception($"AnimationFile: AnimationType {AnimationType} cannot be saved to EAN.");
            }

            ConcurrentBag<EAN_Animation> anims = new ConcurrentBag<EAN_Animation>();

            Parallel.ForEach(Animations, animation =>
            {
                anims.Add(animation.ConvertToEan(Skeleton));
            });

            ean.Animations.AddRange(anims);
            ean.Animations.Sort((x, y) => x.IndexNumeric - y.IndexNumeric);

            return ean;
        }
    }

    [Serializable]
    public class Animation : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private int _endFrame = -1;

        public int ID { get; set; }
        public string Name { get; set; }
        public int EndFrame
        {
            get => _endFrame;
            set
            {
                if (_endFrame != value)
                {
                    _endFrame = value;
                    NotifyPropertyChanged(nameof(EndFrame));
                }
            }
        }
        public KeyframeFloatType KeyframeFloatType { get; set; }
        public List<AnimationBone> Bones { get; set; } = new List<AnimationBone>();

        private readonly AnimationFileType AnimationType;

        #region Load
        public Animation(AnimationFileType animType, EMA_Animation animation, ESK_Skeleton skeleton)
        {
            AnimationType = animType;
            ID = animation.Index;
            Name = animation.Name;
            KeyframeFloatType = (KeyframeFloatType)animation.FloatPrecision;

            foreach (var bone in animation.Nodes)
            {
                if (skeleton != null)
                {
                    //Object animation - only parse the keyframes IF the bone exists in the skeleton
                    ESK_RelativeTransform esk = skeleton.GetBone(bone.BoneName)?.RelativeTransform ?? null;

                    if (esk != null)
                        Bones.Add(new AnimationBone(animType, bone, esk));

                }
                else
                {
                    //Light, camera or material animation. Skeleton does not matter.
                    //TODO: implement logic, should be an alternative path without any skinning code
                    Bones.Add(new AnimationBone(animType, bone, null));
                }
            }

            EndFrame = GetLastFrame();
        }

        public Animation(AnimationFileType animType, EAN_Animation animation, ESK_Skeleton skeleton)
        {
            AnimationType = animType;
            ID = animation.IndexNumeric;
            Name = animation.Name;
            KeyframeFloatType = (KeyframeFloatType)animation.FloatType;

            foreach (var bone in animation.Nodes)
            {
                if (skeleton != null)
                {
                    //Object animation - only parse the keyframes IF the bone exists in the skeleton
                    ESK_RelativeTransform esk = skeleton.GetBone(bone.BoneName)?.RelativeTransform ?? null;

                    if (esk != null)
                        Bones.Add(new AnimationBone(animType, bone, esk));

                }
                else
                {
                    //Camera
                    Bones.Add(new AnimationBone(animType, bone, null));
                }
            }

            EndFrame = GetLastFrame();
        }

        public EMA_Animation ConvertToObjectEma(ESK_Skeleton skeleton)
        {
            EMA_Animation animation = new EMA_Animation();
            animation.Index = ID;
            animation.Name = Name;
            animation.FloatPrecision = (EMA.ValueType)KeyframeFloatType;

            foreach(var bone in Bones)
            {
                EMA_Node emaNode = new EMA_Node();
                emaNode.BoneName = bone.Name;

                var eskBone = skeleton.GetBone(bone.Name);
                if (eskBone == null) continue;
                Matrix4x4 bindPose = eskBone.RelativeTransform.ToMatrix();

                AnimationComponent posComponent = bone.GetComponent(AnimationComponentType.Position);
                AnimationComponent rotComponent = bone.GetComponent(AnimationComponentType.Rotation);
                AnimationComponent scaleComponent = bone.GetComponent(AnimationComponentType.Scale);

                foreach (int frame in bone.GetAllKeyframedFrames())
                {
                    AnimationKeyframe[] values = new AnimationKeyframe[9];
                    values[0] = values[1] = values[2] = values[3] = values[4] = values[5] = AnimationChannel.DefaultZeroKeyframe;
                    values[6] = values[7] = values[8] = AnimationChannel.DefaultOneKeyframe;

                    bool[] hasValue = new bool[9];
                    
                    if(posComponent != null)
                    {
                        values[0] = posComponent.GetKeyframeOrDefault(AnimationChannelType.X, frame, false, out hasValue[0]);
                        values[1] = posComponent.GetKeyframeOrDefault(AnimationChannelType.Y, frame, false, out hasValue[1]);
                        values[2] = posComponent.GetKeyframeOrDefault(AnimationChannelType.Z, frame, false, out hasValue[2]);
                    }

                    if (rotComponent != null)
                    {
                        values[3] = rotComponent.GetKeyframeOrDefault(AnimationChannelType.X, frame, false, out hasValue[3]);
                        values[4] = rotComponent.GetKeyframeOrDefault(AnimationChannelType.Y, frame, false, out hasValue[4]);
                        values[5] = rotComponent.GetKeyframeOrDefault(AnimationChannelType.Z, frame, false, out hasValue[5]);
                    }

                    if (scaleComponent != null)
                    {
                        values[6] = scaleComponent.GetKeyframeOrDefault(AnimationChannelType.X, frame, true, out hasValue[6]);
                        values[7] = scaleComponent.GetKeyframeOrDefault(AnimationChannelType.Y, frame, true, out hasValue[7]);
                        values[8] = scaleComponent.GetKeyframeOrDefault(AnimationChannelType.Z, frame, true, out hasValue[8]);
                    }

                    //Bake skeleton in
                    Vector3 pos = new Vector3(values[0].Value, values[1].Value, values[2].Value);
                    Vector3 rot = new Vector3(values[3].Value, values[4].Value, values[5].Value);
                    Vector3 scale = new Vector3(values[6].Value, values[7].Value, values[8].Value);

                    Matrix4x4 matrix = Matrix4x4.CreateScale(scale);
                    matrix *= Matrix4x4.CreateFromQuaternion(MathHelpers.EulerToQuaternion(rot));
                    matrix *= Matrix4x4.CreateTranslation(pos);
                    matrix *= bindPose;

                    rot = MathHelpers.QuaternionToEuler(Quaternion.CreateFromRotationMatrix(matrix));
                    scale = MathHelpers.ExtractScaleFromMatrix(matrix);

                    //Add keyframes to EMA
                    if (hasValue[0])
                        emaNode.AddKeyframe(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_X, frame, matrix.Translation.X, values[0].ControlPoint1, values[0].ControlPoint2, ConvertEmaInterpolation(values[0].Interpolation));

                    if (hasValue[1])
                        emaNode.AddKeyframe(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_Y, frame, matrix.Translation.Y, values[1].ControlPoint1, values[1].ControlPoint2, ConvertEmaInterpolation(values[1].Interpolation));

                    if (hasValue[2])
                        emaNode.AddKeyframe(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_Z, frame, matrix.Translation.Z, values[2].ControlPoint1, values[2].ControlPoint2, ConvertEmaInterpolation(values[2].Interpolation));

                    if (hasValue[3])
                        emaNode.AddKeyframe(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_X, frame, rot.X, values[3].ControlPoint1, values[3].ControlPoint2, ConvertEmaInterpolation(values[4].Interpolation));

                    if (hasValue[4])
                        emaNode.AddKeyframe(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_Y, frame, rot.Y, values[4].ControlPoint1, values[4].ControlPoint2, ConvertEmaInterpolation(values[4].Interpolation));

                    if (hasValue[5])
                        emaNode.AddKeyframe(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_Z, frame, rot.Z, values[5].ControlPoint1, values[5].ControlPoint2, ConvertEmaInterpolation(values[5].Interpolation));

                    if (hasValue[6])
                        emaNode.AddKeyframe(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_X, frame, scale.X, values[6].ControlPoint1, values[6].ControlPoint2, ConvertEmaInterpolation(values[6].Interpolation));

                    if (hasValue[7])
                        emaNode.AddKeyframe(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_Y, frame, scale.Y, values[7].ControlPoint1, values[7].ControlPoint2, ConvertEmaInterpolation(values[7].Interpolation));

                    if (hasValue[8])
                        emaNode.AddKeyframe(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_Z, frame, scale.Z, values[8].ControlPoint1, values[8].ControlPoint2, ConvertEmaInterpolation(values[8].Interpolation));

                }

                animation.Nodes.Add(emaNode);
            }

            return animation;
        }

        public EAN_Animation ConvertToEan(ESK_Skeleton skeleton)
        {
            EAN_Animation animation = new EAN_Animation();
            animation.IndexNumeric = ID;
            animation.Name = Name;
            animation.FloatSize = (EAN_Animation.FloatPrecision)KeyframeFloatType;

            foreach(var bone in Bones)
            {
                EAN_Node eanNode = new EAN_Node();
                eanNode.BoneName = bone.Name;

                AnimationComponent posComponent = bone.GetComponent(AnimationComponentType.Position);
                AnimationComponent rotComponent = bone.GetComponent(AnimationComponentType.Rotation);
                AnimationComponent scaleComponent = bone.GetComponent(AnimationComponentType.Scale);

                AnimationChannel posX = posComponent?.GetChannel(AnimationChannelType.X);
                AnimationChannel posY = posComponent?.GetChannel(AnimationChannelType.Y);
                AnimationChannel posZ = posComponent?.GetChannel(AnimationChannelType.Z);
                AnimationChannel rotX = rotComponent?.GetChannel(AnimationChannelType.X);
                AnimationChannel rotY = rotComponent?.GetChannel(AnimationChannelType.Y);
                AnimationChannel rotZ = rotComponent?.GetChannel(AnimationChannelType.Z);
                AnimationChannel scaleX = scaleComponent?.GetChannel(AnimationChannelType.X);
                AnimationChannel scaleY = scaleComponent?.GetChannel(AnimationChannelType.Y);
                AnimationChannel scaleZ = scaleComponent?.GetChannel(AnimationChannelType.Z);

                Matrix4x4 bindPose = Matrix4x4.Identity;

                if(skeleton != null)
                {
                    ESK_Bone eskBone = skeleton.GetBone(bone.Name);
                    if (eskBone == null) continue;
                    bindPose = eskBone.RelativeTransform.ToMatrix();
                }

                //Get bool[3] for each component on a frame, indicating if a keyframe exists. If it does, add a interpolated keyframe into the EAN
                //Add the frame to a list for this component, to keep track of keyframes

                float[] posValues = new float[3];
                float[] rotValues = new float[3];
                float[] scaleValues = new float[3];

                foreach (int frame in bone.GetAllKeyframedFrames())
                {
                    bool posExists = false, rotExists = false, scaleExists = false;

                    //Get interpolated keyframe value (if valid keyframe)
                    if(posComponent != null)
                    {
                        if(posComponent.KeyframeExists(frame) || posComponent.IsInterpolatedRange(frame))
                        {
                            posExists = true;
                            posValues[0] = posX?.InterpolateKeyframeValue(frame) ?? 0;
                            posValues[1] = posY?.InterpolateKeyframeValue(frame) ?? 0;
                            posValues[2] = posZ?.InterpolateKeyframeValue(frame) ?? 0;
                        }
                    }

                    if (rotComponent != null)
                    {
                        if (rotComponent.KeyframeExists(frame) || rotComponent.IsInterpolatedRange(frame))
                        {
                            rotExists = true;
                            rotValues[0] = rotX?.InterpolateKeyframeValue(frame) ?? 0;
                            rotValues[1] = rotY?.InterpolateKeyframeValue(frame) ?? 0;
                            rotValues[2] = rotZ?.InterpolateKeyframeValue(frame) ?? 0;
                        }
                    }

                    if (scaleComponent != null)
                    {
                        if (scaleComponent.KeyframeExists(frame) || scaleComponent.IsInterpolatedRange(frame))
                        {
                            scaleExists = true;
                            if(skeleton != null)
                            {
                                scaleValues[0] = scaleX?.InterpolateKeyframeValue(frame) ?? 1;
                                scaleValues[1] = scaleY?.InterpolateKeyframeValue(frame) ?? 1;
                                scaleValues[2] = scaleZ?.InterpolateKeyframeValue(frame) ?? 1;
                            }
                            else
                            {
                                //Camera
                                scaleValues[0] = scaleX?.InterpolateKeyframeValue(frame) ?? 0;
                                scaleValues[1] = scaleY?.InterpolateKeyframeValue(frame) ?? EAN_File.DefaultFoV;
                                scaleValues[2] = 0;
                            }
                        }
                    }

                    //If there is no keyframe, set defaults
                    if (!posExists)
                    {
                        posValues[0] = posValues[1] = posValues[2] = 0;
                    }

                    if (!rotExists)
                    {
                        rotValues[0] = rotValues[1] = rotValues[2] = 0;
                    }

                    if (!scaleExists)
                    {
                        if(skeleton != null)
                        {
                            scaleValues[0] = scaleValues[1] = scaleValues[2] = 1;
                        }
                        else
                        {
                            scaleValues[0] = 0;
                            scaleValues[1] = EAN_File.DefaultFoV;
                            scaleValues[2] = 0;
                        }
                    }

                    //Bake skeleton in (object anims)
                    Vector3 pos = new Vector3(posValues[0], posValues[1], posValues[2]);
                    Vector3 rot = new Vector3(rotValues[0], rotValues[1], rotValues[2]);
                    Vector3 scale = new Vector3(scaleValues[0], scaleValues[1], scaleValues[2]);
                    Quaternion rotQuaternion;

                    if (skeleton != null)
                    {
                        //Object animation; requires the skeleton values to be baked in

                        Matrix4x4 matrix = Matrix4x4.CreateScale(scale);
                        matrix *= Matrix4x4.CreateFromQuaternion(MathHelpers.EulerToQuaternion(rot));
                        matrix *= Matrix4x4.CreateTranslation(pos);
                        matrix *= bindPose;

                        rotQuaternion = Quaternion.CreateFromRotationMatrix(matrix);
                        scale = MathHelpers.ExtractScaleFromMatrix(matrix);
                        pos = matrix.Translation;
                    }
                    else
                    {
                        //Camera animations; need to convert from degrees into radians
                        rotQuaternion = new Quaternion(rot.X, rot.Y, rot.Z, 1f);
                        scale = new Vector3(MathHelpers.ToRadians(scale.X), MathHelpers.ToRadians(scale.Y), scale.Z);
                    }

                    if (posExists)
                    {
                        eanNode.AddKeyframe(frame, EAN_AnimationComponent.ComponentType.Position, pos.X, pos.Y, pos.Z, 1f);
                    }

                    if (rotExists)
                    {
                        eanNode.AddKeyframe(frame, EAN_AnimationComponent.ComponentType.Rotation, rotQuaternion.X, rotQuaternion.Y, rotQuaternion.Z, rotQuaternion.W);
                    }

                    if (scaleExists)
                    {
                        eanNode.AddKeyframe(frame, EAN_AnimationComponent.ComponentType.Scale, scale.X, scale.Y, scale.Z, 1f);
                    }
                }

                animation.Nodes.Add(eanNode);
            }

            return animation;
        }
        #endregion

        #region Editing
        public List<IUndoRedo> RebaseKeyframes(int startFrame, int amount)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var bone in Bones)
            {
                bone.RebaseKeyframes(startFrame, amount, undos);
            }

            return undos;
        }

        #endregion

        #region Helpers
        public int GetMinFrame()
        {
            int min = 0;

            foreach (var bone in Bones)
            {
                int boneMin = bone.GetMinFrame();

                if (boneMin < min)
                    min = boneMin;
            }

            return min;
        }

        public static AnimationInterpolationType ConvertEmaInterpolation(KeyframeInterpolation emaInterp)
        {
            switch(emaInterp)
            {
                case KeyframeInterpolation.Linear: return AnimationInterpolationType.Linear;
                case KeyframeInterpolation.QuadraticBezier: return AnimationInterpolationType.QuadraticBezier;
                case KeyframeInterpolation.CubicBezier: return AnimationInterpolationType.CubicBezier;
                default: throw new Exception($"ConvertEmaInterpolation: Unknown value " + emaInterp);
            }
        }
        
        public static KeyframeInterpolation ConvertEmaInterpolation(AnimationInterpolationType interpolation)
        {
            switch (interpolation)
            {
                case AnimationInterpolationType.Linear: return KeyframeInterpolation.Linear;
                case AnimationInterpolationType.QuadraticBezier: return KeyframeInterpolation.QuadraticBezier;
                case AnimationInterpolationType.CubicBezier: return KeyframeInterpolation.CubicBezier;
                default: throw new Exception($"ConvertEmaInterpolation: Unknown value " + interpolation);
            }
        }

        private int GetLastFrame()
        {
            //Get last frame of animation
            //Method will only be used when loading, and since animations always need a keyframe on the last frame of the animation on all components, we only need to check the first
            if(Bones?.Count > 0)
            {
                if (Bones[0]?.Components?.Count > 0)
                {
                    if (Bones[0].Components[0].Channels?.Count > 0)
                    {
                        if (Bones[0].Components[0].Channels[0].Keyframes.Count > 0)
                        {
                            return Bones[0].Components[0].Channels[0].Keyframes[Bones[0].Components[0].Channels[0].Keyframes.Count - 1].Frame;
                        }
                    }
                }
            }

            return 0;
        }

        public override string ToString()
        {
            return $"Animation: {Name}, ID: {ID}, NumBones: {Bones.Count}";
        }
        #endregion
    }

    [Serializable]
    public class AnimationBone :IAnimationNode, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region IAnimationNode
        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                NotifyPropertyChanged(nameof(IsExpanded));
            }
        }

        public IAnimationNode ParentNode => null;

        public IEnumerable<int> GetAllKeyframes()
        {
            List<int> frames = new List<int>();

            foreach(var component in Components)
            {
                foreach (var channel in component.Channels)
                {
                    foreach (var keyframe in channel.Keyframes)
                    {
                        if (!frames.Contains(keyframe.Frame))
                        {
                            frames.Add(keyframe.Frame);
                        }
                    }
                }
            }

            frames.Sort();
            return frames;
        }
        #endregion

        public string Name { get; set; }
        public List<AnimationComponent> Components { get; set; } = new List<AnimationComponent>();

        private readonly AnimationFileType AnimationType;

        #region Load
        public AnimationBone(AnimationFileType animType, EMA_Node bone, ESK_RelativeTransform relativeTransform)
        {
            AnimationType = animType;
            Name = bone.BoneName;
            CreateKeyframesFromEma(animType, bone, relativeTransform);
        }

        public AnimationBone(AnimationFileType animType, EAN_Node bone, ESK_RelativeTransform relativeTransform)
        {
            AnimationType = animType;
            Name = bone.BoneName;
            CreateKeyframesFromEan(animType, bone, relativeTransform);
        }

        private void CreateKeyframesFromEma(AnimationFileType animType, EMA_Node bone, ESK_RelativeTransform relativeTransform)
        {
            EMA_Command posX = bone.GetCommand(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_X);
            EMA_Command posY = bone.GetCommand(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_Y);
            EMA_Command posZ = bone.GetCommand(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_Z);
            EMA_Command posW = bone.GetCommand(EMA_Command.PARAMETER_POSITION, EMA_Command.COMPONENT_W);
            EMA_Command rotX = bone.GetCommand(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_X);
            EMA_Command rotY = bone.GetCommand(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_Y);
            EMA_Command rotZ = bone.GetCommand(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_Z);
            EMA_Command scaleX = bone.GetCommand(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_X);
            EMA_Command scaleY = bone.GetCommand(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_Y);
            EMA_Command scaleZ = bone.GetCommand(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_Z);
            EMA_Command scaleW = bone.GetCommand(EMA_Command.PARAMETER_SCALE, EMA_Command.COMPONENT_W);

            if(bone.GetCommand(EMA_Command.PARAMETER_ROTATION, EMA_Command.COMPONENT_W) != null)
            {
                throw new Exception("AnimationFile: This EMA has a W rotation channel");
            }

            AnimationComponent posComponent = new AnimationComponent(AnimationComponentType.Position, animType, this);
            AnimationComponent rotComponent = new AnimationComponent(AnimationComponentType.Rotation, animType, this);
            AnimationComponent scaleComponent = new AnimationComponent(AnimationComponentType.Scale, animType, this);

            Matrix4x4 inverseBindPose = relativeTransform.ToMatrix();
            if (!Matrix4x4.Invert(inverseBindPose, out inverseBindPose))
                throw new Exception("AnimationBone: failed to create inverse bind pose matrix");

            foreach (int frame in bone.GetAllKeyframesInt())
            {
                //Position
                float posXValue = relativeTransform.PositionX;
                float posYValue = relativeTransform.PositionY;
                float posZValue = relativeTransform.PositionZ;
                float posWValue = relativeTransform.PositionW;

                EMA_Keyframe xPosKeyframe = posX?.GetKeyframe(frame);
                EMA_Keyframe yPosKeyframe = posY?.GetKeyframe(frame);
                EMA_Keyframe zPosKeyframe = posZ?.GetKeyframe(frame);
                EMA_Keyframe wPosKeyframe = posW?.GetKeyframe(frame);

                if (xPosKeyframe != null)
                    posXValue = xPosKeyframe.Value;

                if (yPosKeyframe != null)
                    posYValue = yPosKeyframe.Value;

                if (zPosKeyframe != null)
                    posZValue = zPosKeyframe.Value;

                if (wPosKeyframe != null)
                    posWValue = wPosKeyframe.Value;

                //Rotation
                Vector3 euler = MathHelpers.QuaternionToEuler(relativeTransform.RotationToQuaternion());
                float rotXValue = euler.X;
                float rotYValue = euler.Y;
                float rotZValue = euler.Z;

                EMA_Keyframe xRotKeyframe = rotX?.GetKeyframe(frame);
                EMA_Keyframe yRotKeyframe = rotY?.GetKeyframe(frame);
                EMA_Keyframe zRotKeyframe = rotZ?.GetKeyframe(frame);

                if (xRotKeyframe != null)
                    rotXValue = xRotKeyframe.Value;

                if (yRotKeyframe != null)
                    rotYValue = yRotKeyframe.Value;

                if (zRotKeyframe != null)
                    rotZValue = zRotKeyframe.Value;

                //Scale
                float scaleXValue = relativeTransform.ScaleX;
                float scaleYValue = relativeTransform.ScaleY;
                float scaleZValue = relativeTransform.ScaleZ;
                float scaleWValue = relativeTransform.ScaleW;

                EMA_Keyframe xScaleKeyframe = scaleX?.GetKeyframe(frame);
                EMA_Keyframe yScaleKeyframe = scaleY?.GetKeyframe(frame);
                EMA_Keyframe zScaleKeyframe = scaleZ?.GetKeyframe(frame);
                EMA_Keyframe wScaleKeyframe = scaleW?.GetKeyframe(frame);

                if (xScaleKeyframe != null)
                    scaleXValue = xScaleKeyframe.Value;

                if (yScaleKeyframe != null)
                    scaleYValue = yScaleKeyframe.Value;

                if (zScaleKeyframe != null)
                    scaleZValue = zScaleKeyframe.Value;

                if (wScaleKeyframe != null)
                    scaleWValue = wScaleKeyframe.Value;

                //Unbake skeleton from keyframes
                Matrix4x4 keyframeMatrix = Matrix4x4.CreateScale(new Vector3(scaleXValue, scaleYValue, scaleZValue) * scaleWValue);
                keyframeMatrix *= Matrix4x4.CreateFromQuaternion(MathHelpers.EulerToQuaternion(new Vector3(rotXValue, rotYValue, rotZValue)));
                keyframeMatrix *= Matrix4x4.CreateTranslation(new Vector3(posXValue, posYValue, posZValue) * posWValue);
                keyframeMatrix *= inverseBindPose;

                Vector3 pos = keyframeMatrix.Translation;
                Vector3 rot = MathHelpers.QuaternionToEuler(Quaternion.CreateFromRotationMatrix(keyframeMatrix));
                Vector3 scale = MathHelpers.ExtractScaleFromMatrix(keyframeMatrix);

                //Add keyframes
                if (xPosKeyframe != null)
                    posComponent.AddKeyframe(AnimationChannelType.X, frame, pos.X, Animation.ConvertEmaInterpolation(xPosKeyframe.InterpolationType), xPosKeyframe.ControlPoint1, xPosKeyframe.ControlPoint2);

                if (yPosKeyframe != null)
                    posComponent.AddKeyframe(AnimationChannelType.Y, frame, pos.Y, Animation.ConvertEmaInterpolation(yPosKeyframe.InterpolationType), yPosKeyframe.ControlPoint1, yPosKeyframe.ControlPoint2);

                if (zPosKeyframe != null)
                    posComponent.AddKeyframe(AnimationChannelType.Z, frame, pos.Z, Animation.ConvertEmaInterpolation(zPosKeyframe.InterpolationType), zPosKeyframe.ControlPoint1, zPosKeyframe.ControlPoint2);

                if (xRotKeyframe != null)
                    rotComponent.AddKeyframe(AnimationChannelType.X, frame, rot.X, Animation.ConvertEmaInterpolation(xRotKeyframe.InterpolationType), xRotKeyframe.ControlPoint1, xRotKeyframe.ControlPoint2);

                if (yRotKeyframe != null)
                    rotComponent.AddKeyframe(AnimationChannelType.Y, frame, rot.Y, Animation.ConvertEmaInterpolation(yRotKeyframe.InterpolationType), yRotKeyframe.ControlPoint1, yRotKeyframe.ControlPoint2);

                if (zRotKeyframe != null)
                    rotComponent.AddKeyframe(AnimationChannelType.Z, frame, rot.Z, Animation.ConvertEmaInterpolation(zRotKeyframe.InterpolationType), zRotKeyframe.ControlPoint1, zRotKeyframe.ControlPoint2);
                
                if (xScaleKeyframe != null)
                    scaleComponent.AddKeyframe(AnimationChannelType.X, frame, scale.X, Animation.ConvertEmaInterpolation(xScaleKeyframe.InterpolationType), xScaleKeyframe.ControlPoint1, xScaleKeyframe.ControlPoint2);

                if (yScaleKeyframe != null)
                    scaleComponent.AddKeyframe(AnimationChannelType.Y, frame, scale.Y, Animation.ConvertEmaInterpolation(yScaleKeyframe.InterpolationType), yScaleKeyframe.ControlPoint1, yScaleKeyframe.ControlPoint2);

                if (zScaleKeyframe != null)
                    scaleComponent.AddKeyframe(AnimationChannelType.Z, frame, scale.Z, Animation.ConvertEmaInterpolation(zScaleKeyframe.InterpolationType), zScaleKeyframe.ControlPoint1, zScaleKeyframe.ControlPoint2);


            }
            
            posComponent.SortKeyframes();
            rotComponent.SortKeyframes();
            scaleComponent.SortKeyframes();

            //Add components if they have any channels in them
            if (posComponent.Channels.Count > 0)
                Components.Add(posComponent);

            if (rotComponent.Channels.Count > 0)
                Components.Add(rotComponent);

            if (scaleComponent.Channels.Count > 0)
                Components.Add(scaleComponent);
        }

        private void CreateKeyframesFromEan(AnimationFileType animType, EAN_Node bone, ESK_RelativeTransform relativeTransform)
        {
            EAN_AnimationComponent eanPosComponent = bone.GetComponent(EAN_AnimationComponent.ComponentType.Position);
            EAN_AnimationComponent eanRotComponent = bone.GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            EAN_AnimationComponent eanScaleComponent = bone.GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            AnimationComponent posComponent = new AnimationComponent(AnimationComponentType.Position, animType, this);
            AnimationComponent rotComponent = new AnimationComponent(AnimationComponentType.Rotation, animType, this);
            AnimationComponent scaleComponent = new AnimationComponent(AnimationComponentType.Scale, animType, this);

            Matrix4x4 inverseBindPose = Matrix4x4.Identity;
            Vector3 bonePos = Vector3.Zero;
            Quaternion boneRot = Quaternion.Identity;
            Vector3 boneScale = Vector3.One;

            if(relativeTransform != null)
            {
                bonePos = relativeTransform.PositionToVector3();
                boneRot = relativeTransform.RotationToQuaternion();
                boneScale = relativeTransform.ScaleToVector3();

                inverseBindPose = relativeTransform.ToMatrix();
                if (!Matrix4x4.Invert(inverseBindPose, out inverseBindPose))
                    throw new Exception("AnimationBone: failed to create inverse bind pose matrix");
            }

            foreach(int frame in bone.GetAllKeyframesInt())
            {
                EAN_Keyframe posKeyframe = eanPosComponent?.GetKeyframe(frame);
                EAN_Keyframe rotKeyframe = eanRotComponent?.GetKeyframe(frame);
                EAN_Keyframe scaleKeyframe = eanScaleComponent?.GetKeyframe(frame);

                //Unbake skeleton
                Vector3 pos = posKeyframe != null ? posKeyframe.ToVector3() : bonePos;
                Vector3 rot = rotKeyframe != null ? rotKeyframe.ToVector3() : Vector3.Zero;
                Vector3 scale = scaleKeyframe != null ? scaleKeyframe.ToVector3() : boneScale;

                if (relativeTransform != null)
                {
                    //Object animation
                    Quaternion rotQuaternion = rotKeyframe != null ? rotKeyframe.ToQuaternion() : boneRot;

                    Matrix4x4 keyframeMatrix = Matrix4x4.CreateScale(scale);
                    keyframeMatrix *= Matrix4x4.CreateFromQuaternion(rotQuaternion);
                    keyframeMatrix *= Matrix4x4.CreateTranslation(pos);
                    keyframeMatrix *= inverseBindPose;

                    pos = keyframeMatrix.Translation;
                    rot = MathHelpers.QuaternionToEuler(Quaternion.CreateFromRotationMatrix(keyframeMatrix));
                    scale = MathHelpers.ExtractScaleFromMatrix(keyframeMatrix);
                }
                else
                {
                    //Camera animation; convert Roll (X) and FoV (Y) into degrees
                    scale = new Vector3(MathHelpers.ToDegrees(scale.X), MathHelpers.ToDegrees(scale.Y), scale.Z);
                }

                //Create keyframes
                if(posKeyframe != null)
                {
                    posComponent.AddKeyframe(AnimationChannelType.X, frame, pos.X);
                    posComponent.AddKeyframe(AnimationChannelType.Y, frame, pos.Y);
                    posComponent.AddKeyframe(AnimationChannelType.Z, frame, pos.Z);
                }

                if (rotKeyframe != null)
                {
                    rotComponent.AddKeyframe(AnimationChannelType.X, frame, rot.X);
                    rotComponent.AddKeyframe(AnimationChannelType.Y, frame, rot.Y);
                    rotComponent.AddKeyframe(AnimationChannelType.Z, frame, rot.Z);
                }

                if (scaleKeyframe != null)
                {
                    scaleComponent.AddKeyframe(AnimationChannelType.X, frame, scale.X);
                    scaleComponent.AddKeyframe(AnimationChannelType.Y, frame, scale.Y);
                    scaleComponent.AddKeyframe(AnimationChannelType.Z, frame, scale.Z);
                }
            }

            posComponent.SortKeyframes();
            rotComponent.SortKeyframes();
            scaleComponent.SortKeyframes();

            //Add components if they have any channels in them
            if (posComponent.Channels.Count > 0)
                Components.Add(posComponent);

            if (rotComponent.Channels.Count > 0)
                Components.Add(rotComponent);

            if (scaleComponent.Channels.Count > 0)
                Components.Add(scaleComponent);
        }
        #endregion

        #region Editing
        public void RebaseKeyframes(int startFrame, int amount, List<IUndoRedo> undos = null)
        {
            foreach(var component in Components)
            {
                component.RebaseKeyframes(startFrame, amount, undos);
            }
        }

        //public static List<AnimationBone> CopyKeyframes(List<AnimationBone> selectedBones, List<int> selectedKeyframes)
        //{
        //
        //}
        #endregion

        #region Helpers
        public AnimationComponent GetComponent(AnimationComponentType componentType, bool createIfMissing = false, List<IUndoRedo> undos = null)
        {
            AnimationComponent component = Components.FirstOrDefault(x => x.Type == componentType);

            if(component == null && createIfMissing)
            {
                component = new AnimationComponent(componentType, AnimationType, this);
                Components.Add(component);

                if (undos != null)
                    undos.Add(new UndoableListAdd<AnimationComponent>(Components, component));
            }

            return component;
        }

        public int GetMinFrame()
        {
            int min = 0;

            foreach (var component in Components)
            {
                int componentMin = component.GetMinFrame();

                if (componentMin < min)
                    min = componentMin;
            }

            return min;
        }

        public int[] GetAllKeyframedFrames()
        {
            List<int> frames = new List<int>();

            foreach(var component in Components)
            {
                foreach(var channel in component.Channels)
                {
                    foreach(var keyframe in channel.Keyframes)
                    {
                        if(!frames.Contains(keyframe.Frame))
                            frames.Add(keyframe.Frame);
                    }
                }
            }

            frames.Sort();
            return frames.ToArray();
        }

        public override string ToString()
        {
            return $"Bone: {Name}";
        }
        #endregion
    }

    [Serializable]
    public class AnimationComponent : IAnimationNode, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region IAnimationNode
        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                NotifyPropertyChanged(nameof(IsExpanded));
            }
        }

        public IAnimationNode ParentNode { get; private set; }
        
        public IEnumerable<int> GetAllKeyframes()
        {
            List<int> frames = new List<int>();

            foreach(var channel in Channels)
            {
                foreach (var keyframe in channel.Keyframes)
                {
                    if (!frames.Contains(keyframe.Frame))
                    {
                        frames.Add(keyframe.Frame);
                    }
                }
            }

            frames.Sort();
            return frames;
        }
        #endregion

        public AnimationComponentType Type { get; private set; }
        public AsyncObservableCollection<AnimationChannel> Channels { get; set; } = new AsyncObservableCollection<AnimationChannel>();

        private readonly AnimationFileType AnimationType;

        public AnimationComponent(AnimationComponentType componentType, AnimationFileType animationType, IAnimationNode parent)
        {
            Type = componentType;
            AnimationType = animationType;
            ParentNode = parent;
        }

        #region Editing
        public void AddKeyframe(AnimationChannelType channel, int frame, float value, AnimationInterpolationType interpolation = AnimationInterpolationType.Linear, float cp1 = 0, float cp2 = 0, List<IUndoRedo> undos = null)
        {
            AnimationChannel animChannel = GetChannel(channel, true, undos);

            animChannel.AddKeyframe(frame, value, interpolation, cp1, cp2, undos);
        }

        public void RebaseKeyframes(int startFrame, int amount, List<IUndoRedo> undos = null)
        {
            foreach (var channel in Channels)
            {
                channel.RebaseKeyframes(startFrame, amount, undos);
            }
        }
        #endregion

        #region Helpers

        public AnimationChannel GetChannel(AnimationChannelType channel, bool createIfMissing = false, List<IUndoRedo> undos = null)
        {
            AnimationChannel chn = Channels.FirstOrDefault(x => x.Type == channel);

            if (chn == null && createIfMissing)
            {
                chn = new AnimationChannel(channel, GetDefaultKeyframeValue(channel), this);
                Channels.Add(chn);

                if (undos != null)
                    undos.Add(new UndoableListAdd<AnimationChannel>(Channels, chn));

                Channels.Sort((x, y) => (int)x.Type - (int)y.Type);
            }

            return chn;
        }

        public AnimationKeyframe GetKeyframeOrDefault(AnimationChannelType channel, int frame, bool isScale, out bool hasKeyframe)
        {
            AnimationChannel chn = GetChannel(channel);

            if (chn != null)
            {
                AnimationKeyframe keyframe = chn.GetKeyframe(frame);

                if (keyframe != null)
                {
                    hasKeyframe = true;
                    return keyframe;
                }
            }

            hasKeyframe = false;
            return isScale ? AnimationChannel.DefaultOneKeyframe : AnimationChannel.DefaultZeroKeyframe;
        }

        public int GetMinFrame()
        {
            int min = 0;

            foreach(var channel in Channels)
            {
                int channelMin = channel.GetMinFrame();

                if(channelMin < min)
                    min = channelMin;
            }

            return min;
        }

        internal void SortKeyframes()
        {
            foreach(var channel in Channels)
            {
                channel.Keyframes.Sort((x, y) => x.Frame - y.Frame);
            }
        }

        private float GetDefaultKeyframeValue(AnimationChannelType channel)
        {
            if(AnimationType == AnimationFileType.Object)
            {
                if (Type == AnimationComponentType.Position || Type == AnimationComponentType.Rotation) return 0;
                return 1; //scale
            }
            else if(AnimationType == AnimationFileType.Camera)
            {
                if (Type == AnimationComponentType.Position || Type == AnimationComponentType.Rotation) return 0;
                if (Type == AnimationComponentType.Scale && channel == AnimationChannelType.Y) return EAN_File.DefaultFoV;
                return 0;
            }

            return 0;
        }
    
        internal bool IsInterpolatedRange(int frame)
        {
            foreach (var chanel in Channels)
            {
                if (chanel.IsInterpolatedRange(frame)) return true;
            }

            return false;
        }

        internal bool KeyframeExists(int frame)
        {
            foreach (var chanel in Channels)
            {
                if (chanel.KeyframeExists(frame)) return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $"Component: {Type} (NumChannels: {Channels.Count})";
        }
        #endregion
    }

    [Serializable]
    public class AnimationChannel : IAnimationNode
    {
        internal readonly static AnimationKeyframe DefaultZeroKeyframe = AnimationKeyframe.CreateReadOnly(0);
        internal readonly static AnimationKeyframe DefaultOneKeyframe = AnimationKeyframe.CreateReadOnly(1);
        internal readonly static AnimationKeyframe DefaultCameraFOVKeyframe = AnimationKeyframe.CreateReadOnly(EAN_File.DefaultFoV);

        #region IAnimationNode
        public bool IsExpanded { get; set; }
        public IAnimationNode ParentNode { get; private set; }
        public IEnumerable<int> GetAllKeyframes()
        {
            int[] frames = new int[Keyframes.Count];

            for (int i = 0; i < Keyframes.Count; i++)
                frames[i] = Keyframes[i].Frame;

            return frames;
        }
        #endregion

        public AnimationChannelType Type { get; private set; }
        public List<AnimationKeyframe> Keyframes { get; set; } = new List<AnimationKeyframe>();

        private readonly float DefaultKeyframe;

        public AnimationChannel(AnimationChannelType channelType, float defaultKeyframe, IAnimationNode parent)
        {
            Type = channelType;
            DefaultKeyframe = defaultKeyframe;
            ParentNode = parent;
        }

        #region Editing
        public void AddKeyframe(int frame, float value, AnimationInterpolationType interpolation, float cp1 = 0, float cp2 = 0, List<IUndoRedo> undos = null)
        {
            AnimationKeyframe keyframe = GetKeyframe(frame);

            if (keyframe != null)
            {
                if (undos != null)
                {
                    undos.Add(new UndoablePropertyGeneric(nameof(AnimationKeyframe.Value), keyframe, keyframe.Value, value));
                    undos.Add(new UndoablePropertyGeneric(nameof(AnimationKeyframe.Interpolation), keyframe, keyframe.Interpolation, interpolation));
                    undos.Add(new UndoablePropertyGeneric(nameof(AnimationKeyframe.ControlPoint1), keyframe, keyframe.ControlPoint1, cp1));
                    undos.Add(new UndoablePropertyGeneric(nameof(AnimationKeyframe.ControlPoint2), keyframe, keyframe.ControlPoint2, cp2));
                }

                keyframe.Frame = frame;
                keyframe.Value = value;
                keyframe.Interpolation = interpolation;
                keyframe.ControlPoint1 = cp1;
                keyframe.ControlPoint2 = cp2;
            }
            else
            {
                keyframe = new AnimationKeyframe();
                keyframe.Frame = frame;
                keyframe.Value = value;
                keyframe.Interpolation = interpolation;
                keyframe.ControlPoint1 = cp1;
                keyframe.ControlPoint2 = cp2;

                if (undos != null)
                {
                    int insertIdx = GetInsertIndex(frame);
                    Keyframes.Insert(insertIdx, keyframe);
                    undos.Add(new UndoableListInsert<AnimationKeyframe>(Keyframes, insertIdx, keyframe));
                }
                else
                {
                    //If no undo list was supplied, dont worry about keyframe sorting (path to take when loading files)
                    Keyframes.Add(keyframe);
                }
            }
        }

        public void RebaseKeyframes(int startFrame, int amount, List<IUndoRedo> undos = null)
        {
            if (startFrame + amount < 0)
                throw new Exception("RebaseKeyframes: Frame cannot be less than zero");

            foreach(var keyframe in Keyframes.OrderBy(x => x.Frame))
            {
                if(keyframe.Frame >= startFrame)
                {
                    int newFrame = keyframe.Frame + amount;

                    if (undos != null)
                        undos.Add(new UndoablePropertyGeneric(nameof(keyframe.Frame), keyframe, keyframe.Frame, newFrame));

                    keyframe.Frame = newFrame;
                }
            }
        }
        #endregion

        #region Interpolation
        private readonly AnimationKeyframe keyframeSearchObject = new AnimationKeyframe();
        private int cacheFrameIndex = 0;

        internal bool KeyframeExists(int frame)
        {
            GetNearestKeyframes(frame, ref cacheFrameIndex, out int keyframe, out _);

            if (keyframe != -1)
            {
                return Keyframes[keyframe].Frame == frame;
            }

            return false;
        }

        internal bool IsInterpolatedRange(int frame)
        {
            GetNearestKeyframes(frame, ref cacheFrameIndex, out int keyframe, out _);

            if(keyframe != -1)
            {
                return Keyframes[keyframe].Interpolation != AnimationInterpolationType.Linear;
            }

            return false;
        }

        public float InterpolateKeyframeValue(int frame)
        {
            return InterpolateKeyframeValue(frame, ref cacheFrameIndex);
        }

        public float InterpolateKeyframeValue(float frame, ref int startIndex)
        {
            bool isWhole = Math.Floor(frame) == frame;

            if (isWhole)
            {
                return InterpolateKeyframeValue((int)frame, ref startIndex);
            }

            int flooredFrame = (int)Math.Floor(frame);
            int idx = startIndex;

            float beforeValue = InterpolateKeyframeValue(flooredFrame, ref startIndex);
            float afterValue = InterpolateKeyframeValue(flooredFrame + 1, ref startIndex);
            float factor = (float)(frame - Math.Floor(frame));

            startIndex = idx;
            return MathHelpers.Lerp(beforeValue, afterValue, factor);
        }

        public float InterpolateKeyframeValue(int frame, ref int startIndex)
        {
            AnimationKeyframe existing = GetKeyframe(frame);

            if (existing != null)
                return existing.Value;

            //No keyframe existed. Calculate the value.
            GetNearestKeyframes(frame, ref startIndex, out int keyframe1, out int keyframe2);
            if (keyframe1 == -1 && keyframe2 == -1) return DefaultKeyframe; //No keyframes

            AnimationKeyframe previousKeyframe = keyframe1 != -1 ? Keyframes[keyframe1] : null;
            AnimationKeyframe nextKeyframe = keyframe2 != -1 ? Keyframes[keyframe2] : null;

            if ((previousKeyframe != null && nextKeyframe == null) || (previousKeyframe == nextKeyframe && previousKeyframe != null))
            {
                return previousKeyframe.Value;
            }
            else if(nextKeyframe != null && previousKeyframe == null)
            {
                return nextKeyframe.Value;
            }

            float timeFactor = (float)(frame - previousKeyframe.Frame) / (nextKeyframe.Frame - previousKeyframe.Frame);

            switch (previousKeyframe.Interpolation)
            {
                case AnimationInterpolationType.Linear:
                    return MathHelpers.Lerp(previousKeyframe.Value, nextKeyframe.Value, timeFactor);
                case AnimationInterpolationType.QuadraticBezier:
                    return MathHelpers.QuadraticBezier(timeFactor, previousKeyframe.Value, previousKeyframe.ControlPoint1 + previousKeyframe.Value, nextKeyframe.Value);
                case AnimationInterpolationType.CubicBezier:
                    return MathHelpers.CubicBezier(timeFactor, previousKeyframe.Value, previousKeyframe.ControlPoint1 + previousKeyframe.Value, previousKeyframe.ControlPoint2 + previousKeyframe.Value, nextKeyframe.Value);
                default:
                    return 0f;
            }
        }

        private void GetNearestKeyframes(int frame, ref int startIdx, out int keyframe1, out int keyframe2)
        {
            //Look ahead a limited amount of frames to find the desired frame
            int endIdx = Math.Min(startIdx + 3, Keyframes.Count - 1);

            for (int i = startIdx; i < endIdx; i++)
            {
                if (Keyframes[i].Frame > frame) break; //End search if frame passed

                if (Keyframes[i].Frame == frame)
                {
                    keyframe1 = i;
                    keyframe2 = Keyframes.Count - 1 > i ? i + 1 : -1;
                    startIdx = keyframe1;
                    return;
                }
            }

            //Check previous frame
            if (startIdx > 0)
            {
                if (Keyframes[startIdx - 1].Frame == frame)
                {
                    keyframe1 = startIdx - 1;
                    keyframe2 = Keyframes.Count - 1 > keyframe1 ? startIdx : -1;
                    startIdx = keyframe1;
                    return;
                }
            }

            //If the sequential look up failed, fallback to a binary search
            int index = BinarySearchKeyframeIndex(frame);

            if (index >= 0)
            {
                keyframe1 = index;
                keyframe2 = (keyframe1 < Keyframes.Count - 1) ? keyframe1 + 1 : -1;
            }
            else
            {
                int insertionIndex = ~index;

                keyframe2 = (insertionIndex < Keyframes.Count) ? insertionIndex : -1;
                keyframe1 = (insertionIndex > 0) ? insertionIndex - 1 : -1;
            }

            if (keyframe1 != -1)
                startIdx = keyframe1;
            else if (keyframe2 != -1)
                startIdx = keyframe2;
            else
                startIdx = 0;
        }

        private int BinarySearchKeyframeIndex(int frame)
        {
            keyframeSearchObject.Frame = frame;
            return Keyframes.BinarySearch(keyframeSearchObject);
        }
        #endregion

        #region Helpers
        public override string ToString()
        {
            return $"Channel: {Type} (NumKeyframes: {Keyframes.Count})";
        }

        public AnimationKeyframe GetKeyframe(int frame)
        {
            return Keyframes.FirstOrDefault(x => x.Frame == frame);
        }

        private int GetInsertIndex(int frame)
        {
            for (int i = 0; i < Keyframes.Count; i++)
            {
                if (Keyframes[i].Frame >= frame)
                    return i;
            }

            return Keyframes.Count;
        }

        public int GetMinFrame()
        {
            return Keyframes.Count > 0 ? Keyframes[0].Frame : 0;
        }
        #endregion
    }

    [Serializable]
    public class AnimationKeyframe : IComparable<AnimationKeyframe>
    {
        private bool isReadOnly = false;
        private float _value;

        public int Frame { get; set; }
        public float Value
        {
            get => _value;
            set
            {
                if (!isReadOnly && value != _value)
                    _value = value;
            }
        }
        public AnimationInterpolationType Interpolation { get; set; }
        public float ControlPoint1 { get; set; }
        public float ControlPoint2 { get; set; }

        internal static AnimationKeyframe CreateReadOnly(float value)
        {
            AnimationKeyframe keyframe = new AnimationKeyframe()
            {
                Value = value
            };

            keyframe.isReadOnly = true;
            return keyframe;
        }

        public int CompareTo(AnimationKeyframe other)
        {
            if (other == null) return 1;
            return Frame.CompareTo(other.Frame);
        }

        public override string ToString()
        {
            return $"Frame: {Frame}, Value: {Value}, Interp: {Interpolation}, CP: ({ControlPoint1}, {ControlPoint2})";
        }
    }

}
