using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YAXLib;
using System.Resources;
using System.Reflection;
using System.Globalization;
using Xv2CoreLib;
using System.IO;

namespace Xv2CoreLib.EAN
{
    public enum Axis
    {
        X,
        Y,
        Z,
        W
    }

    [YAXSerializeAs("EAN")]
    public class EAN_File : INotifyPropertyChanged, ISorting, IIsNull
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXAttributeForClass]
        public bool IsCamera { get; set; } //offset 16
        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        public byte I_17 { get; set; }

        public ESK_Skeleton Skeleton { get; set; }
        private ObservableCollection<EAN_Animation> AnimationsValue = null;
        public ObservableCollection<EAN_Animation> Animations
        {
            get
            {
                return this.AnimationsValue;
            }

            set
            {
                if (value != this.AnimationsValue)
                {
                    this.AnimationsValue = value;
                    NotifyPropertyChanged("Animations");
                }
            }
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            Animations = Sorting.SortEntries(Animations);
        }

        public static EAN_File Load(byte[] rawBytes)
        {
            return new Parser(rawBytes).eanFile;
        }

        public static EAN_File Load(string path)
        {
            return new Parser(path, false).eanFile;
        }

        public void Save(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }
        
        public bool ValidateAnimationIndexes()
        {
            if (Animations == null) return true;
            List<int> UsedIndexes = new List<int>();

            foreach(var anim in Animations)
            {
                if(UsedIndexes.IndexOf(anim.IndexNumeric) != -1)
                {
                    throw new Exception(String.Format("Animation Index {0} was defined more than once. Animations cannot share the same Index!", anim.IndexNumeric));
                }
                UsedIndexes.Add(anim.IndexNumeric);
            }

            return true;
        }

        public int GetSortedIndexOfValue(int valueIndex)
        {
            //Assumes that no animation with an Index equal to valueIndex exists. (There should be 1, but it is irrelevant)
            if (Animations == null) return -1;
            
            foreach(var anim in Animations)
            {
                if(anim.IndexNumeric > valueIndex)
                {
                    return Animations.IndexOf(anim);
                }
            }

            return Animations.Count - 1;
        }

        public int GetNextUnusedIndexValue()
        {
            if (Animations == null) return -1;

            return Animations[Animations.Count - 1].IndexNumeric + 1;
        }
        
        /// <summary>
        /// Returns the zero-index of the animation matching the parameter name. If none are found, -1 is returned.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int IndexOf(string name)
        {
            for(int i = 0; i < Animations.Count; i++)
            {
                if(name == Animations[i].Name)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the zero-index of the animation matching the parameter id. If none are found, -1 is returned.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int IndexOf(int id)
        {
            for (int i = 0; i < Animations.Count; i++)
            {
                if (id == Animations[i].IndexNumeric)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Index of animation based on a split name (e.g. no chara code)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int IndexOf_SplitName(string name)
        {
            for (int i = 0; i < Animations.Count; i++)
            {
                if(Animations[i].Name. Length > 4)
                {
                    if (name == Animations[i].Name.Remove(0, 4))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static EAN_File DefaultCamFile()
        {
            return new EAN_File()
            {
                Animations = new ObservableCollection<EAN_Animation>(),
                IsCamera = true,
                I_08 = 37508,
                Skeleton = new ESK_Skeleton()
                {
                    I_28 = new int[2] { 1900697063, 175112582 },
                    UseUnk2 = true,
                    ESKBones = new ObservableCollection<ESK_Bone>()
                    {
                        new ESK_Bone()
                        {
                            Name = "Node",
                            RelativeTransform = new ESK_RelativeTransform()
                            {
                                F_28 = 1f,
                                F_32 = 1f,
                                F_36 = 1f,
                                F_40 = 1f
                            }
                        }
                    }
                }
            };
        }

        public static EAN_File DefaultFile(ESK_Skeleton skeleton)
        {
            return new EAN_File()
            {
                Animations = new ObservableCollection<EAN_Animation>(),
                IsCamera = true,
                I_08 = 37508,
                Skeleton = skeleton.Copy()
            };
        }


        public EAN_File Clone()
        {
            ObservableCollection<EAN_Animation> anims = new ObservableCollection<EAN_Animation>();
            foreach(var a in Animations)
            {
                anims.Add(a.Clone());
            }

            return new EAN_File()
            {
                IsCamera = IsCamera,
                I_08 = I_08,
                I_17 = I_17,
                Skeleton = Skeleton.Clone(),
                Animations = anims
            };
        }

        /// <summary>
        /// Gets the total number of animation indexes, including any null ones.
        /// </summary>
        public int AnimationCount()
        {
            if (Animations == null) return 0;
            if (Animations.Count == 0) return 0;

            int idx = Animations.Count() - 1;

            return Animations[idx].IndexNumeric + 1;
        }

        public int IndexOfAnimation(int id)
        {
            if (Animations == null) return -1;

            for(int i = 0; i < Animations.Count; i++)
            {
                if (Animations[i].IndexNumeric == id) return i;
            }

            return -1;
        }

        public void AddEntry(int id, EAN_Animation entry)
        {
            for(int i = 0; i < Animations.Count; i++)
            {
                if(Animations[i].IndexNumeric == id)
                {
                    Animations[i] = entry;
                    return;
                }
            }

            Animations.Add(entry);
        }

        /// <summary>
        /// Adds the animation and returns the auto-assigned id.
        /// </summary>
        /// <param name="anim"></param>
        /// <returns></returns>
        public int AddEntry(EAN_Animation anim)
        {
            int newId = NextID();
            anim.IndexNumeric = newId;
            Animations.Add(anim);
            return newId;
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public int NextID(int mindID = 0)
        {
            int id = mindID;

            while(IndexOf(id) != -1)
            {
                id++;
            }

            return id;
        }

        public EAN_Animation GetAnimation(int id, bool returnNull = false)
        {
            if (IndexOf(id) == -1 && !returnNull) throw new Exception("Could not find an animation with an ID matching " + id);
            if (IndexOf(id) == -1 && returnNull) return null;
                return Animations[IndexOf(id)];
        }

        public string NameOf(int id)
        {
            if (IndexOf(id) == -1) throw new Exception("Could not find an animation with an ID matching " + id);
            return Animations[IndexOf(id)].Name;
        }
        
        /// <summary>
        /// Looks for an animation and returns its ID.
        /// </summary>
        /// <returns></returns>
        public int AnimationExists(EAN_Animation anim)
        {
            foreach(var _anim in Animations)
            {
                if (_anim.Compare(anim)) return _anim.IndexNumeric;
            }

            return -1;
        }

        public void Normalize()
        {
            foreach(var anim in Animations)
            {
                anim.Normalize();
            }
        }

        public bool IsNull()
        {
            return (Animations.Count == 0);
        }
    }

    [YAXSerializeAs("Animation")]
    public class EAN_Animation : INotifyPropertyChanged, IInstallable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _index = null;
        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index
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
                    NotifyPropertyChanged("IndexNumeric");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }
        [YAXDontSerialize]
        public int SortID { get { return IndexNumeric; } }
        

        public enum IntPrecision
        {
            _8Bit = 0,
            _16Bit = 1
        }

        public enum FloatPrecision
        {
            _8Bit = 0,
            _16Bit = 1,
            _32Bit = 2,
            _64Bit = 3
        }
        
        [YAXDontSerialize]
        public int IndexNumeric
        {
            get
            {
                return int.Parse(Index);
            }
            set
            {
                Index = value.ToString();
            }
        }
        private string _NameValue = String.Empty;
        [YAXAttributeForClass]
        public string Name
        {
            get
            {
                return this._NameValue;
            }

            set
            {
                if (value != this._NameValue)
                {
                    this._NameValue = value;
                    NotifyPropertyChanged("Name");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }

        [YAXDontSerialize]
        public string DisplayName
        {
            get
            {
                return String.Format("[{0}] {1}", IndexNumeric, Name);
            }
        }
        [YAXDontSerialize]
        public byte FloatType
        {
            get
            {
                return (byte)I_03;
            }
            set
            {
                I_03 = (FloatPrecision)value;
            }
        }


        [YAXAttributeForClass]
        [YAXSerializeAs("FrameCount")]
        private int _frameCountValue = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public int I_04
        {
            get
            {
                return this._frameCountValue;
            }
            set
            {
                if (value != this._frameCountValue)
                {
                    this._frameCountValue = value;
                    NotifyPropertyChanged("I_04");
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("IndexSize")]
        public IntPrecision I_02 { get; set; } //int8
        [YAXAttributeForClass]
        [YAXSerializeAs("FloatSize")]
        public FloatPrecision I_03 { get; set; } //int8

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AnimationNode")]
        public ObservableCollection<EAN_Node> Nodes { get; set; }

        #region KeyframeManipulation
        public void RemoveKeyframe(int frame)
        {
            foreach (var bone in Nodes)
            {
                bone.RemoveKeyframe(frame);
            }
        }

        /// <summary>
        /// (CAMERA ONLY) Adds a keyframe to the camera bone "Node". Animation must have all 3 components for it to work properly (use Normalize() first)
        /// </summary>
        public void AddKeyframe(int frame, float posX, float posY, float posZ, float posW, float rotX, float rotY, float rotZ, float rotW,
            float scaleX, float scaleY, float scaleZ, float scaleW)
        {
            var node = GetNode("Node");
            if (node == null) throw new InvalidDataException("EAN_Animation.AddKeyframe: \"Node\" not found. (this function is only intended for use on cameras)");
            node.AddKeyframe(frame, posX, posY, posZ, posW, rotX, rotY, rotZ, rotW, scaleX, scaleY, scaleZ, scaleW);
        }
        #endregion

        #region Helpers
        public int GetLastKeyframe()
        {
            int lastKeyframe = 0;
            foreach (var e in Nodes)
            {
                foreach (var a in e.AnimationComponents)
                {
                    if (a.Keyframes[a.Keyframes.Count() - 1].FrameIndex > lastKeyframe)
                    {
                        lastKeyframe = a.Keyframes[a.Keyframes.Count() - 1].FrameIndex;
                    }
                }
            }
            return lastKeyframe;
        }

        public EAN_Node GetNode(string BoneName)
        {
            if (Nodes == null) throw new Exception(String.Format("Could not retrieve the bone {0} becauses Nodes is null.", BoneName));

            for (int i = 0; i < Nodes.Count(); i++)
            {
                if(BoneName == Nodes[i].BoneName)
                {
                    return Nodes[i];
                }
            }

            return null;
        }

        public EAN_Animation Clone()
        {
            ObservableCollection<EAN_Node> _Nodes = new ObservableCollection<EAN_Node>();
            for(int i = 0; i < Nodes.Count; i++)
            {
                _Nodes.Add(Nodes[i].Clone());
            }

            return new EAN_Animation()
            {
                IndexNumeric = IndexNumeric,
                I_02 = I_02,
                I_03 = I_03,
                I_04 = I_04,
                Name = (string)Name.Clone(),
                Nodes = _Nodes
            };
        }

        public void IncreaseFrameIndex(int amount)
        {
            for(int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].IncreaseFrameIndex(amount);
            }
        }

        public void RecalculateFrameCount()
        {
            I_04 = GetLastKeyframe() + 1;

            if(I_04 > 255)
            {
                I_02 = IntPrecision._16Bit;
            }
            else
            {
                I_02 = IntPrecision._8Bit;
            }
        }
        
        public bool Compare(EAN_Animation anim)
        {
            if (anim.Name != Name) return false;
            if (anim.FloatType != FloatType) return false;
            if (anim.I_02 != I_02) return false;
            if (anim.I_03 != I_03) return false;
            if (anim.I_04 != I_04) return false;
            if (anim.Nodes == null && Nodes == null) return true;
            if (anim.Nodes == null || Nodes == null) return false;

            if (anim.Nodes.Count != Nodes.Count) return false;
            
            for(int i = 0; i < Nodes.Count; i++)
            {
                if (!Nodes[i].Compare(anim.Nodes[i])) return false;
            }

            return true;
        }

        public void Normalize()
        {
            foreach(var bone in Nodes)
            {
                bone.NormalizeNode();
            }
        }
        
        #endregion

        #region Modifiers

        public void ApplyNodeOffset(string bone, EAN_AnimationComponent.ComponentType componentType, float x, float y, float z, float w)
        {
            var node = GetNode(bone);
            
            if (node != null)
            {
                node.ApplyNodeOffset(componentType, x, y, z, w);
            }
        }

        public void Cut(int startFrame, int endFrame, int smoothingFrames)
        {
            if (endFrame > GetLastKeyframe()) endFrame = GetLastKeyframe();
            if(startFrame > GetLastKeyframe())
            {
                throw new InvalidOperationException("StartFrame cannot be greater than the total number of animation frames.");
            }
            if(endFrame <= startFrame)
            {
                throw new InvalidOperationException("EndFrame cannot be less than or equal to StartFrame.");
            }

            for (int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].Cut(startFrame, endFrame, smoothingFrames);
            }
        }

        public void Scale(int startFrame, int endFrame, float scaleFactor)
        {
            if (endFrame > GetLastKeyframe()) endFrame = GetLastKeyframe();
            if (startFrame > GetLastKeyframe())
            {
                throw new InvalidOperationException("StartFrame cannot be greater than the total number of animation frames.");
            }
            if (endFrame <= startFrame)
            {
                throw new InvalidOperationException("EndFrame cannot be less than or equal to StartFrame.");
            }
            if (Nodes == null) throw new Exception("Scale failed. Nodes was null.");

            for(int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].Scale(startFrame, endFrame, scaleFactor);
            }
        }

        public void Join(EAN_Animation secondAnimation, int smoothingFrames)
        {
            //This method is ported from LB Animation Tool and should be rewritten at some point.
            //It has one major problem with it: If a node does NOT exist in the first animation, then that node will never be added if it exists on the secondAnimation!
            

            int frameIndexIncease = GetLastKeyframe() + 1 + smoothingFrames;
            int newFinalFrame = secondAnimation.GetLastKeyframe() + frameIndexIncease;

            for (int i = 0; i < Nodes.Count(); i++)
            {
                EAN_Node addNode = secondAnimation.GetNode(Nodes[i].BoneName);

                //If its null, then the node doesn't exist in the second animation
                if (addNode != null)
                {
                    for (int a = 0; a < Nodes[i].AnimationComponents.Count(); a++)
                    {
                        var addComponent = addNode.GetComponent(Nodes[i].AnimationComponents[a].I_00);

                        //Check if the component exists (Position/Rot/Scale)
                        if (addComponent != null)
                        {
                            for (int g = 0; g < addComponent.Keyframes.Count(); g++)
                            {
                                Nodes[i].AnimationComponents[a].Keyframes.Add(new EAN_Keyframe()
                                {
                                    X = addComponent.Keyframes[g].X,
                                    Y = addComponent.Keyframes[g].Y,
                                    Z = addComponent.Keyframes[g].Z,
                                    W = addComponent.Keyframes[g].W,
                                    FrameIndex = (ushort)(addComponent.Keyframes[g].FrameIndex + frameIndexIncease)
                                });
                            }
                        }
                    }
                }
                else
                {
                    for (int a = 0; a < Nodes[i].AnimationComponents.Count(); a++)
                    {
                        int lastIndex = Nodes[i].AnimationComponents[a].Keyframes.Count() - 1;
                        Nodes[i].AnimationComponents[a].Keyframes.Add(new EAN_Keyframe()
                        {
                            FrameIndex = (ushort)newFinalFrame,
                            X = Nodes[i].AnimationComponents[a].Keyframes[lastIndex].X,
                            Y = Nodes[i].AnimationComponents[a].Keyframes[lastIndex].Y,
                            Z = Nodes[i].AnimationComponents[a].Keyframes[lastIndex].Z,
                            W = Nodes[i].AnimationComponents[a].Keyframes[lastIndex].W,
                        });
                    }
                }
                
            }


            //EXTENSION. Deals with adding nodes to the original animation if they dont exist and are needed for the secondAnimation.
            //This may need to be improved by adding zeroed keyframes that cover the original animations duration.
            for (int i = 0; i < secondAnimation.Nodes.Count; i++)
            {
                if(GetNode(secondAnimation.Nodes[i].BoneName) == null)
                {
                    secondAnimation.Nodes[i].IncreaseFrameIndex(frameIndexIncease);
                    Nodes.Add(secondAnimation.Nodes[i].Clone());
                }
            }

        }

        public void FixDemoAnimation()
        {
            var b_C_Base = GetNode("b_C_Base");

            if(b_C_Base != null)
            {
                var pos = b_C_Base.GetComponent(EAN_AnimationComponent.ComponentType.Position);
                var rot = b_C_Base.GetComponent(EAN_AnimationComponent.ComponentType.Rotation);

                if (pos != null)
                {
                    if(pos.Keyframes == null)
                    {
                        throw new Exception(String.Format("FixDemoAnimation could not be applied to {0} as no keyframes were found on the Position component.", Name));
                    }

                    float _xOffset = pos.Keyframes[0].X;
                    float _yOffset = pos.Keyframes[0].Y;
                    float _zOffset = pos.Keyframes[0].Z;

                    for (int i = 0; i < pos.Keyframes.Count; i++)
                    {
                        pos.Keyframes[i].X -= _xOffset;
                        pos.Keyframes[i].Y -= _yOffset;
                        pos.Keyframes[i].Z -= _zOffset;
                    }
                }
                else
                {
                    throw new Exception(String.Format("FixDemoAnimation could not be applied to {0} as a Position component on b_C_Base could not be found.", Name));
                }
                if (rot != null)
                {
                    if (rot.Keyframes == null)
                    {
                        throw new Exception(String.Format("FixDemoAnimation could not be applied to {0} as no keyframes were found on the Rotation component.", Name));
                    }

                    float _xOffset = rot.Keyframes[0].X;
                    float _yOffset = rot.Keyframes[0].Y;
                    float _zOffset = rot.Keyframes[0].Z;

                    for (int i = 0; i < rot.Keyframes.Count; i++)
                    {
                        rot.Keyframes[i].X -= _xOffset;
                        rot.Keyframes[i].Y -= _yOffset;
                        rot.Keyframes[i].Z -= _zOffset;
                    }

                }

            }
            else
            {
                throw new Exception(String.Format("FixDemoAnimation could not be applied to {0} as b_C_Base could not be found.", Name));
            }
        }

        public void ApplyZeroAxis(string bone, EAN_AnimationComponent.ComponentType componentType, Axis axis)
        {
            var node = GetNode(bone);

            if (node != null)
            {
                node.ApplyZeroAxis(componentType, axis);
            }
        }

        #endregion

    }

    [YAXSerializeAs("AnimationNode")]
    public class EAN_Node
    {
        [YAXAttributeForClass]
        public string BoneName { get; set; } //Find it in Skeleton data, if not found then show a error

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Keyframes")]
        public ObservableCollection<EAN_AnimationComponent> AnimationComponents { get; set; }

        #region KeyframeManipulation
        /// <summary>
        /// Add a keyframe at the specified frame (will overwrite any existing keyframe). Node must have all 3 components for it to work properly (use Normalize() first).
        /// </summary>
        public void AddKeyframe(int frame, float posX, float posY, float posZ, float posW, float rotX, float rotY, float rotZ, float rotW,
            float scaleX, float scaleY, float scaleZ, float scaleW)
        {
            var pos = GetComponent(EAN_AnimationComponent.ComponentType.Position);
            if (pos == null) throw new InvalidDataException("EAN_Node.AddKeyframe: position component was null (Use Normalize() before calling this function!).");
            pos.AddKeyframe(frame, posX, posY, posZ, posW);

            var rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            if (rot == null) throw new InvalidDataException("EAN_Node.AddKeyframe: rotation component was null (Use Normalize() before calling this function!).");
            rot.AddKeyframe(frame, rotX, rotY, rotZ, rotW);

            var scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale);
            if (scale == null) throw new InvalidDataException("EAN_Node.AddKeyframe: scale component was null (Use Normalize() before calling this function!).");
            scale.AddKeyframe(frame, scaleX, scaleY, scaleZ, scaleW);


        }

        public void RemoveKeyframe(int keyframe)
        {
            foreach (var component in AnimationComponents)
            {
                component.RemoveKeyframe(keyframe);
            }
        }

        /// <summary>
        /// Changes keyframe. 
        /// </summary>
        /// <returns>Was the change successful or not. Returns false if a keyframe already existed at that frame.</returns>
        public bool ChangeKeyframe(int oldKeyframe, int newKeyframe)
        {
            foreach (var components in AnimationComponents)
            {
                bool result = components.ChangeKeyframe(oldKeyframe, newKeyframe);

                if (!result) return false;
            }

            return true;
        }

        #endregion

        #region Helper
        public bool HasKeyframe(int frame)
        {
            foreach (var component in AnimationComponents)
            {
                if (component.Keyframes.FirstOrDefault(e => e.FrameIndex == frame) != null) return true;
            }

            return false;
        }

        public float[] GetKeyframeValues(int frame)
        {
            float[] values = new float[12];
            var pos = GetComponent(EAN_AnimationComponent.ComponentType.Position);
            var rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            var scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            //Set default values
            values[3] = 1f;
            values[7] = 1f;
            values[8] = 1f;
            values[9] = 1f;
            values[10] = 1f;
            values[11] = 1f;

            if (pos != null)
            {
                values[0] = pos.GetKeyframeValue(frame, Axis.X);
                values[1] = pos.GetKeyframeValue(frame, Axis.Y);
                values[2] = pos.GetKeyframeValue(frame, Axis.Z);
                values[3] = pos.GetKeyframeValue(frame, Axis.W);
            }
            if (rot != null)
            {
                values[4] = rot.GetKeyframeValue(frame, Axis.X);
                values[5] = rot.GetKeyframeValue(frame, Axis.Y);
                values[6] = rot.GetKeyframeValue(frame, Axis.Z);
                values[7] = rot.GetKeyframeValue(frame, Axis.W);
            }
            if (scale != null)
            {
                values[8] = scale.GetKeyframeValue(frame, Axis.X);
                values[9] = scale.GetKeyframeValue(frame, Axis.Y);
                values[10] = scale.GetKeyframeValue(frame, Axis.Z);
                values[11] = scale.GetKeyframeValue(frame, Axis.W);
            }

            return values;
        }

        public int GetLastKeyframe()
        {
            int lastKeyframe = 0;

            foreach (var a in AnimationComponents)
            {
                if (a.Keyframes[a.Keyframes.Count() - 1].FrameIndex > lastKeyframe)
                {
                    lastKeyframe = a.Keyframes[a.Keyframes.Count() - 1].FrameIndex;
                }
            }

            return lastKeyframe;
        }

        public EAN_AnimationComponent GetComponent(EAN_AnimationComponent.ComponentType _type)
        {
            for (int i = 0; i < AnimationComponents.Count(); i++)
            {
                if (_type == AnimationComponents[i].I_00)
                {
                    return AnimationComponents[i];
                }
            }

            return null;
        }

        public EAN_Node Clone()
        {
            ObservableCollection<EAN_AnimationComponent> _AnimationComponent = new ObservableCollection<EAN_AnimationComponent>();
            for (int i = 0; i < AnimationComponents.Count; i++)
            {
                _AnimationComponent.Add(AnimationComponents[i].Clone());
            }

            return new EAN_Node()
            {
                BoneName = (string)BoneName.Clone(),
                AnimationComponents = _AnimationComponent
            };
        }

        public bool Compare(EAN_Node node)
        {
            if (node.BoneName != BoneName) return false;
            if (node.AnimationComponents == null && AnimationComponents == null) return true;
            if (node.AnimationComponents == null || AnimationComponents == null) return false;

            for (int i = 0; i < AnimationComponents.Count; i++)
            {
                if (!AnimationComponents[i].Compare(node.AnimationComponents[i])) return false;
            }

            return true;
        }

        public void IncreaseFrameIndex(int amount)
        {
            for (int i = 0; i < AnimationComponents.Count; i++)
            {
                AnimationComponents[i].IncreaseFrameIndex(amount);
            }
        }


        /// <summary>
        /// Add Keyframes and AnimationComponents for everything and interpolate the values.
        /// </summary>
        public void NormalizeNode(bool camera = true)
        {
            List<ushort> usedKeyframes = new List<ushort>();

            //Position
            var pos = GetComponent(EAN_AnimationComponent.ComponentType.Position);

            if (pos == null)
            {
                pos = new EAN_AnimationComponent();
                pos.I_00 = EAN_AnimationComponent.ComponentType.Position;
                AnimationComponents.Add(pos);

                //Set default values
                pos.Keyframes.Add(new EAN_Keyframe() { FrameIndex = 0, });
                pos.Keyframes.Add(new EAN_Keyframe() { FrameIndex = usedKeyframes.Max() });
            }

            foreach (var usedFrame in usedKeyframes)
            {
                if (pos.Keyframes.FirstOrDefault(k => k.FrameIndex == usedFrame) == null)
                {
                    pos.Keyframes.Add(new EAN_Keyframe()
                    {
                        FrameIndex = usedFrame,
                        X = pos.GetKeyframeValue(usedFrame, Axis.X),
                        Y = pos.GetKeyframeValue(usedFrame, Axis.Y),
                        Z = pos.GetKeyframeValue(usedFrame, Axis.Z),
                        W = pos.GetKeyframeValue(usedFrame, Axis.W)
                    });
                }
            }

            //Rotation
            var rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation);

            if (rot == null)
            {
                rot = new EAN_AnimationComponent();
                rot.I_00 = EAN_AnimationComponent.ComponentType.Rotation;
                AnimationComponents.Add(rot);

                //Set default values
                rot.Keyframes.Add(new EAN_Keyframe() { FrameIndex = 0, });
                rot.Keyframes.Add(new EAN_Keyframe() { FrameIndex = usedKeyframes.Max() });
            }

            foreach (var usedFrame in usedKeyframes)
            {
                if (rot.Keyframes.FirstOrDefault(k => k.FrameIndex == usedFrame) == null)
                {
                    rot.Keyframes.Add(new EAN_Keyframe()
                    {
                        FrameIndex = usedFrame,
                        X = rot.GetKeyframeValue(usedFrame, Axis.X),
                        Y = rot.GetKeyframeValue(usedFrame, Axis.Y),
                        Z = rot.GetKeyframeValue(usedFrame, Axis.Z),
                        W = rot.GetKeyframeValue(usedFrame, Axis.W)
                    });
                }
            }

            //Scale (or Camera)
            var scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            if (scale == null)
            {
                scale = new EAN_AnimationComponent();
                scale.I_00 = EAN_AnimationComponent.ComponentType.Scale;
                AnimationComponents.Add(rot);

                if (camera)
                {
                    //Set default values
                    scale.Keyframes.Add(new EAN_Keyframe() { FrameIndex = 0, Y = 0.785398f, W = 0f });
                    scale.Keyframes.Add(new EAN_Keyframe() { FrameIndex = usedKeyframes.Max(), Y = 0.785398f, W = 0f });
                }
                else
                {
                    scale.Keyframes.Add(new EAN_Keyframe() { FrameIndex = 0, });
                    scale.Keyframes.Add(new EAN_Keyframe() { FrameIndex = usedKeyframes.Max() });
                }
            }

            foreach (var usedFrame in usedKeyframes)
            {
                if (scale.Keyframes.FirstOrDefault(k => k.FrameIndex == usedFrame) == null)
                {
                    scale.Keyframes.Add(new EAN_Keyframe()
                    {
                        FrameIndex = usedFrame,
                        X = scale.GetKeyframeValue(usedFrame, Axis.X),
                        Y = scale.GetKeyframeValue(usedFrame, Axis.Y),
                        Z = scale.GetKeyframeValue(usedFrame, Axis.Z),
                        W = scale.GetKeyframeValue(usedFrame, Axis.W)
                    });
                }
            }


        }

        /// <summary>
        /// Get a list of all used keyframes for this Node.
        /// </summary>
        public List<ushort> GetAllKeyframes()
        {
            List<ushort> keyframes = new List<ushort>();

            foreach (var component in AnimationComponents)
            {
                foreach (var keyframe in component.Keyframes)
                {
                    if (!keyframes.Contains(keyframe.FrameIndex))
                        keyframes.Add(keyframe.FrameIndex);
                }
            }

            return keyframes;
        }

        #endregion

        #region Modifiers
        public void ApplyNodeOffset(EAN_AnimationComponent.ComponentType componentType, float x, float y, float z, float w)
        {
            var _component = GetComponent(componentType);
            if (_component != null)
            {
                _component.ApplyNodeOffset(x, y, z, w);
            }
        }
        
        public void Cut(int startFrame, int endFrame, int smoothingFrames)
        {
            for(int i = 0; i < AnimationComponents.Count; i++)
            {
                AnimationComponents[i].Cut(startFrame, endFrame, smoothingFrames);
            }
        }
        
        public void Scale(int startFrame, int endFrame, float scaleFactor)
        {
            if (AnimationComponents == null) throw new Exception("Scale failed. AnimationComponents was null.");

            for(int i = 0; i < AnimationComponents.Count; i++)
            {
                AnimationComponents[i].Scale(startFrame, endFrame, scaleFactor);
            }

        }

        public void ApplyZeroAxis(EAN_AnimationComponent.ComponentType componentType, Axis axis)
        {
            var _component = GetComponent(componentType);
            if (_component != null)
            {
                _component.ApplyZeroAxis(axis);
            }
        }
        #endregion

    }

    [YAXSerializeAs("Keyframes")]
    public class EAN_AnimationComponent
    {
        public enum ComponentType : byte
        {
            Position = 0,
            Rotation = 1,
            Scale = 2, //Or "Camera"
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public ComponentType I_00 { get; set; } //int8
        [YAXAttributeForClass]
        public byte I_01 { get; set; }
        [YAXAttributeForClass]
        public short I_02 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Keyframe")]
        public ObservableCollection<EAN_Keyframe> Keyframes { get; set; } = new ObservableCollection<EAN_Keyframe>();

        #region KeyframeManipulation
        public void AddKeyframe(int frame, float x, float y, float z, float w)
        {
            var keyframe = Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)frame);

            if(keyframe != null)
            {
                keyframe.X = x;
                keyframe.Y = y;
                keyframe.Z = z;
                keyframe.W = w;
            }
            else
            {
                Keyframes.Add(new EAN_Keyframe()
                {
                    FrameIndex = (ushort)frame,
                    X = x,
                    Y = y,
                    Z = z,
                    W = w
                });
            }
        }

        public bool ChangeKeyframe(int oldKeyframe, int newKeyframe)
        {
            var keyframe = Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)oldKeyframe);
            var _newKeyframe = Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)newKeyframe);
            if (keyframe == null || _newKeyframe != null) return false;
            keyframe.FrameIndex = (ushort)newKeyframe;
            return true;
        }

        public void RemoveKeyframe(int frame)
        {
            var keyframe = Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)frame);

            if (keyframe != null)
                Keyframes.Remove(keyframe);
        }

        public EAN_Keyframe AddKeyframe(int frame)
        {
            AddKeyframe(frame, GetKeyframeValue(frame, Axis.X), GetKeyframeValue(frame, Axis.Y), GetKeyframeValue(frame, Axis.Z), GetKeyframeValue(frame, Axis.W));
            return Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)frame);
        }
        #endregion

        #region Helper

        public float GetKeyframeValue(float frame, Axis axis)
        {
            bool isWhole = Math.Floor(frame) == frame;

            if (isWhole)
            {
                return GetKeyframeValue((int)frame, axis);
            }

            int flooredFrame = (int)Math.Floor(frame);

            float beforeValue = GetKeyframeValue(flooredFrame, axis);
            float afterValue = GetKeyframeValue(flooredFrame + 1, axis);
            float factor = (float)(frame - Math.Floor(frame));

            return Utils.Lerp(beforeValue, afterValue, factor);
        }

        public float GetKeyframeValue(int frame, Axis axis)
        {
            //todo: rework to use Lerp
            EAN_Keyframe existing = GetKeyframe(frame);

            if (existing != null)
            {
                //Return this exact frame
                switch (axis)
                {
                    case Axis.X:
                        return existing.X;
                    case Axis.Y:
                        return existing.Y;
                    case Axis.Z:
                        return existing.Z;
                    case Axis.W:
                        return existing.W;
                }
                return 0f;
            }

            //No keyframe existed. Calculate the value.
            EAN_Keyframe previousKeyframe = GetNearestKeyframeBefore(frame);
            EAN_Keyframe nextKeyframe = GetNearestKeyframeAfter(frame);

            if (previousKeyframe != null && nextKeyframe == null)
            {
                switch (axis)
                {
                    case Axis.X:
                        return previousKeyframe.X;
                    case Axis.Y:
                        return previousKeyframe.Y;
                    case Axis.Z:
                        return previousKeyframe.Z;
                    case Axis.W:
                        return previousKeyframe.W;
                }
            }

            //calculate the inbetween value from this frame and the previous frame
            float currentKeyframeValue = 0;
            float previousKeyframeValue = 0;

            switch (axis)
            {
                case Axis.X:
                    currentKeyframeValue = nextKeyframe.X;
                    previousKeyframeValue = previousKeyframe.X;
                    break;
                case Axis.Y:
                    currentKeyframeValue = nextKeyframe.Y;
                    previousKeyframeValue = previousKeyframe.Y;
                    break;
                case Axis.Z:
                    currentKeyframeValue = nextKeyframe.Z;
                    previousKeyframeValue = previousKeyframe.Z;
                    break;
                case Axis.W:
                    currentKeyframeValue = nextKeyframe.W;
                    previousKeyframeValue = previousKeyframe.W;
                    break;
            }

            //Frame difference between previous frame and the current frame (current frame is AFTER the frame we want)
            int diff = nextKeyframe.FrameIndex - previousKeyframe.FrameIndex;

            //Keyframe value difference
            float keyframe2 = currentKeyframeValue - previousKeyframeValue;

            //Difference between the frame we WANT and the previous frame
            int diff2 = frame - previousKeyframe.FrameIndex;

            //Divide keyframe value difference by the keyframe time difference, and then multiply it by diff2, then add the previous keyframe value
            return (keyframe2 / diff) * diff2 + previousKeyframeValue;
        }

        public int GetLastKeyframe()
        {
            if (Keyframes.Count == 0) return 0;
            return Keyframes.Max(k => k.FrameIndex);
        }

        public EAN_AnimationComponent Clone()
        {
            ObservableCollection<EAN_Keyframe> _keyframes = new ObservableCollection<EAN_Keyframe>();

            for (int i = 0; i < Keyframes.Count(); i++)
            {
                _keyframes.Add(Keyframes[i].Clone());
            }

            return new EAN_AnimationComponent()
            {
                I_00 = I_00,
                I_02 = I_02,
                Keyframes = _keyframes
            };
        }

        public void RemoveDuplicateKeyframes()
        {
            if (Keyframes == null) return;

            List<int> usedKeyframes = new List<int>();
            for (int i = 0; i < Keyframes.Count; i++)
            {
                if (usedKeyframes.Contains(Keyframes[i].FrameIndex))
                {
                    Keyframes.RemoveAt(i);
                    i--;
                }
                else
                {
                    usedKeyframes.Add(Keyframes[i].FrameIndex);
                }
            }
        }

        public void IncreaseFrameIndex(int amount)
        {
            for (int i = 0; i < Keyframes.Count; i++)
            {
                Keyframes[i].FrameIndex += (ushort)amount;
            }
        }

        /// <summary>
        /// Gets the keyframed values if they exist. Returns a boolean indicating whether the frame had any keyframed values.
        /// </summary>
        public bool GetKeyframe(int frame, ref float x, ref float y, ref float z, ref float w)
        {
            x = GetKeyframeValue(frame, Axis.X);
            y = GetKeyframeValue(frame, Axis.Y);
            z = GetKeyframeValue(frame, Axis.Z);
            w = GetKeyframeValue(frame, Axis.W);

            foreach (var keyframe in Keyframes)
            {
                if (keyframe.FrameIndex == frame) return true;
            }
            return false;
        }

        public bool Compare(EAN_AnimationComponent component)
        {
            if (I_00 != component.I_00) return false;
            if (I_01 != component.I_01) return false;
            if (I_02 != component.I_02) return false;
            if (Keyframes == null && component.Keyframes == null) return true;
            if (Keyframes == null || component.Keyframes == null) return false;

            for (int i = 0; i < Keyframes.Count; i++)
            {
                if (!Keyframes[i].Compare(component.Keyframes[i])) return false;
            }

            return true;
        }

        /// <summary>
        /// Add two EAN_AnimationComponents together.
        /// </summary>
        /// <param name="component"></param>
        public void Add(EAN_AnimationComponent component)
        {
            //Expand this entry if required
            if (component.GetLastKeyframe() > GetLastKeyframe())
            {
                Keyframes.Add(new EAN_Keyframe()
                {
                    FrameIndex = (ushort)component.GetLastKeyframe()
                });
            }

            foreach (var keyframe in Keyframes)
            {
                if (component.GetLastKeyframe() >= keyframe.FrameIndex)
                {
                    keyframe.X += component.GetKeyframeValue(keyframe.FrameIndex, Axis.X) * component.GetKeyframeValue(keyframe.FrameIndex, Axis.W);
                    keyframe.Y += component.GetKeyframeValue(keyframe.FrameIndex, Axis.Y) * component.GetKeyframeValue(keyframe.FrameIndex, Axis.W);
                    keyframe.Z += component.GetKeyframeValue(keyframe.FrameIndex, Axis.Z) * component.GetKeyframeValue(keyframe.FrameIndex, Axis.W);
                    //keyframe.W += component.GetKeyframeValue(keyframe.FrameIndex, Axis.W);
                }
            }

        }

        public EAN_Keyframe GetKeyframe(int frame)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.FrameIndex == frame) return keyframe;
            }

            return null;
        }

        private EAN_Keyframe GetNearestKeyframeBefore(int frame)
        {
            EAN_Keyframe nearest = GetKeyframe(0);

            if (nearest == null)
                nearest = GetDefaultKeyframe();

            foreach (var keyframe in Keyframes)
            {
                if (keyframe.FrameIndex < frame && keyframe.FrameIndex > nearest.FrameIndex)
                {
                    nearest = keyframe;
                }
            }

            return nearest;
        }

        private EAN_Keyframe GetNearestKeyframeAfter(int frame)
        {
            int lastKeyframe = GetLastKeyframe();

            if (frame >= lastKeyframe) return null;
            EAN_Keyframe nearest = GetKeyframe(GetLastKeyframe());

            if (nearest == null)
                nearest = GetDefaultKeyframe();

            foreach (var keyframe in Keyframes)
            {
                if (keyframe.FrameIndex > frame && keyframe.FrameIndex < nearest.FrameIndex)
                {
                    nearest = keyframe;
                }
            }

            return nearest;
        }

        private EAN_Keyframe GetDefaultKeyframe()
        {
            EAN_Keyframe keyframe = new EAN_Keyframe();

            if(I_00 == ComponentType.Scale && I_01 == 3)
            {
                //Camera
                keyframe.Y = 0.785398f; //Radians, for 45 degree FOV
            }
            else
            {
                keyframe.W = 1f;
            }

            return keyframe;
        }

        #endregion

        #region Modifiers
        public void ApplyNodeOffset(float x, float y, float z, float w)
        {
            if (Keyframes == null) throw new Exception(String.Format("Could not apply NodeOffset because Keyframes is null."));

            for (int i = 0; i < Keyframes.Count; i++)
            {
                Keyframes[i].X += x;
                Keyframes[i].Y += y;
                Keyframes[i].Z += z;
                Keyframes[i].W += w;
            }
        }

        public void Cut(int startFrame, int endFrame, int smoothingFrames)
        {
            //Removing keyframes
            List<EAN_Keyframe> _keyframesToRemove = new List<EAN_Keyframe>();

            for(int i = 0; i < Keyframes.Count; i++)
            {
                if(Keyframes[i].FrameIndex >= startFrame && Keyframes[i].FrameIndex <= endFrame)
                {
                    _keyframesToRemove.Add(Keyframes[i]);
                }
            }
            
            foreach(var keyframe in _keyframesToRemove)
            {
                Keyframes.Remove(keyframe);
            }

            //Adjusting frameIndex of keyframes that come after endFrame, and applying smoothingFrames
            int amountToDecreaseBy = (endFrame - startFrame) + smoothingFrames;
            for(int i = 0; i < Keyframes.Count; i++)
            {
                if(Keyframes[i].FrameIndex > endFrame)
                {
                    Keyframes[i].FrameIndex -= (ushort)amountToDecreaseBy;
                }
            }

        }

        public void Scale(int startFrame, int endFrame, float scaleFactor)
        {
            if (Keyframes == null) throw new Exception("Scale failed. Keyframes was null.");

            for(int i = 0; i < Keyframes.Count; i++)
            {
                if(Keyframes[i].FrameIndex < startFrame)
                {
                    //Keyframes before the scaled range, so nothing really needs to be done here...
                }
                else if(Keyframes[i].FrameIndex >= startFrame && Keyframes[i].FrameIndex < endFrame || Keyframes[i].FrameIndex == endFrame)
                {
                    //Duration range to apply scaling effect to.
                    float newFrameIndex = (Keyframes[i].FrameIndex - startFrame) * scaleFactor;
                    Keyframes[i].FrameIndex = (ushort)((int)Math.Floor(newFrameIndex) + startFrame);
                }
                else if (Keyframes[i].FrameIndex > endFrame)
                {
                    //Keyframes that come after the scaled duration, so the frameIndex needs to be adjusted accordinly.
                    float amountToInceaseBy = (endFrame - startFrame) * scaleFactor - (endFrame - startFrame);
                    Keyframes[i].FrameIndex += (ushort)Math.Floor(amountToInceaseBy);
                }
            }
            
            RemoveDuplicateKeyframes(); //Remove any keyframes that now happen to have the same frameIndex, which can happen when shortening an animation. Neglecting to do this WILL result in a broken animation!
        }

        public void ApplyZeroAxis(Axis axis)
        {
            if (Keyframes == null) throw new Exception(String.Format("Could not apply ZeroAxis because Keyframes is null."));

            for (int i = 0; i < Keyframes.Count; i++)
            {
                switch (axis)
                {
                    case Axis.X:
                        Keyframes[i].X = 0;
                        break;
                    case Axis.Y:
                        Keyframes[i].Y = 0;
                        break;
                    case Axis.Z:
                        Keyframes[i].Z = 0;
                        break;
                    case Axis.W:
                        Keyframes[i].W = 0;
                        break;
                }
            }
        }

        #endregion

        #region CameraShake
        public void ApplyCameraShake(int startFrame, int duration, float threshold, bool cameraComponent)
        {
            //Create keyframes if needed. Do this BEFORE modifying any keyframes.
            for (int frame = startFrame; frame < startFrame + duration; frame++)
            {
                var keyframe = Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)frame);

                if (keyframe == null)
                    keyframe = AddKeyframe(frame);
            }

            //Now apply the noise
            for (int frame = startFrame; frame < startFrame + duration; frame++)
            {
                var keyframe = Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)frame);

                if (!cameraComponent)
                {
                    keyframe.X += Random.Range(-threshold, threshold);
                    keyframe.Z += Random.Range(-threshold, threshold);
                    keyframe.Y += Random.Range(-threshold, threshold);
                }
                else
                {
                    float newThreshold = (float)Utils.ConvertDegreesToRadians(threshold);
                    keyframe.X += Random.Range(-newThreshold, newThreshold);
                    keyframe.Z += Random.Range(-newThreshold, newThreshold);
                }
            }

        }
        
        #endregion

    }

    [YAXSerializeAs("Keyframe")]
    public class EAN_Keyframe
    {
        [YAXAttributeForClass]
        public ushort FrameIndex { get; set; }
        [YAXAttributeForClass]
        public float X { get; set; } = 0f;
        [YAXAttributeForClass]
        public float Y { get; set; } = 0f;
        [YAXAttributeForClass]
        public float Z { get; set; } = 0f;
        [YAXAttributeForClass]
        public float W { get; set; } = 1f;

        public EAN_Keyframe Clone()
        {
        return new EAN_Keyframe()
        {
            FrameIndex = FrameIndex,
            X = X,
            Y = Y,
            Z = Z,
            W = W
        };
        }

        public bool Compare(EAN_Keyframe keyframe)
        {
            if (FrameIndex != keyframe.FrameIndex) return false;
            if (X != keyframe.X) return false;
            if (Y != keyframe.Y) return false;
            if (Z != keyframe.Z) return false;
            if (W != keyframe.W) return false;

            return true;
        }
    }


    #region CameraWrapper

    public class KeyframeListEntry : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private int _frame = 0;
        public int Frame
        {
            get
            {
                return _frame;
            }
            set
            {
                if(_frame != value)
                {
                    //Change the frame in the underlying data
                    bool result = nodeRef.ChangeKeyframe(_frame, value);

                    if (result)
                    {
                        //Change the property only IF the keyframe change was accepted.
                        _frame = value;
                    }

                    NotifyPropertyChanged("Frame");
                }
            }
        }

        //Refs
        EAN_Node nodeRef;

        public KeyframeListEntry(int frame, EAN_Node node)
        {
            if (node == null) throw new InvalidDataException("KeyframeListEntry.nodeRef cannot be null.");
            nodeRef = node;
            _frame = frame;
        }

        public static ObservableCollection<KeyframeListEntry> GetKeyframeList(EAN_Node node)
        {
            if (node == null) return null;
            ObservableCollection<KeyframeListEntry> keyframes = new ObservableCollection<KeyframeListEntry>();

            foreach(var frames in node.GetAllKeyframes())
            {
                keyframes.Add(new KeyframeListEntry(frames, node));
            }

            return keyframes;
        }

        public SerializedKeyframe SerializeKeyframe()
        {
            return new SerializedKeyframe(Frame, nodeRef.GetKeyframeValues(Frame));
        }
    }

    [Serializable]
    public class SerializedKeyframe
    {
        public SerializedKeyframe(int frame, float posX, float posY, float posZ, float posW, float rotX, float rotY, float rotZ, float rotW,
            float scaleX, float scaleY, float scaleZ, float scaleW)
        {
            Frame = frame;
            PosX = posX;
            PosY = posY;
            PosZ = posZ;
            PosW = posW;
            RotX =  rotX;
            RotY = rotY;
            RotZ = rotZ;
            RotW = rotW;
            ScaleX = scaleX;
            ScaleY = scaleY;
            ScaleZ = scaleZ;
            ScaleW = scaleW;
        }

        public SerializedKeyframe(int frame, float[] keyframeValues)
        {
            Frame = frame;
            PosX = keyframeValues[0];
            PosY = keyframeValues[1];
            PosZ = keyframeValues[2];
            PosW = keyframeValues[3];
            RotX = keyframeValues[4];
            RotY = keyframeValues[5];
            RotZ = keyframeValues[6];
            RotW = keyframeValues[7];
            ScaleX = keyframeValues[8];
            ScaleY = keyframeValues[9];
            ScaleZ = keyframeValues[10];
            ScaleW = keyframeValues[11];
        }

        public int Frame { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float PosW { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }
        public float RotZ { get; set; }
        public float RotW { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public float ScaleZ { get; set; }
        public float ScaleW { get; set; }
    }

    #endregion




    //Skeleton

    [YAXSerializeAs("Skeleton")]
    public class ESK_Skeleton
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Flag")]
        public short I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("UseUnk2")]
        public bool UseUnk2 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_28")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_28 { get; set; } // size 2

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        [YAXDontSerializeIfNull]
        public ObservableCollection<ESK_Bone> ESKBones { get; set; }
        [YAXDontSerializeIfNull]
        public ESK_Unk1 Unk1 { get; set; }

        //Non-hierarchy list
        List<ESK_BoneNonHierarchal> listBones = null;

        public List<string> GetSelectedBones()
        {
            List<string> selectedBones = new List<string>();

            for(int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].isSelected) selectedBones.Add(ESKBones[i].Name);

                if(ESKBones[i].ESK_Bones != null)
                {
                    selectedBones.AddRange(GetSelectedBonesRecursive(ESKBones[i].ESK_Bones));
                }
            }

            return selectedBones;
        }

        private List<string> GetSelectedBonesRecursive(ObservableCollection<ESK_Bone> eskBones)
        {
            List<string> selectedBones = new List<string>();

            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].isSelected) selectedBones.Add(eskBones[i].Name);

                if (eskBones[i].ESK_Bones != null)
                {
                    selectedBones.AddRange(GetSelectedBonesRecursive(eskBones[i].ESK_Bones));
                }
            }

            return selectedBones;
        }

        public void SetSelectionState(bool isSelected)
        {
            for(int i = 0; i < ESKBones.Count; i++)
            {
                ESKBones[i].isSelected = isSelected;

                if(ESKBones[i].ESK_Bones != null)
                {
                    SetSelectionStateRecursive(isSelected, ESKBones[i].ESK_Bones);
                }
            }
        }

        private void SetSelectionStateRecursive(bool isSelected, ObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                eskBones[i].isSelected = isSelected;

                if (eskBones[i].ESK_Bones != null)
                {
                    SetSelectionStateRecursive(isSelected, eskBones[i].ESK_Bones);
                }
            }
        }

        public bool Exists(string boneName)
        {
            for(int i = 0; i < ESKBones.Count; i++)
            {
                if(ESKBones[i].Name == boneName)
                {
                    return true;
                }

                if(ESKBones[i].ESK_Bones != null)
                {
                    bool result = ExistsRecursive(boneName, ESKBones[i].ESK_Bones);

                    if(result == true)
                    {
                        return true;
                    }
                }
                
            }

            return false;
        }

        private bool ExistsRecursive(string boneName, ObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == boneName)
                {
                    return true;
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    bool result = ExistsRecursive(boneName, eskBones[i].ESK_Bones);

                    if (result == true)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void AddBones(List<ESK_BoneNonHierarchal> bonesToAdd)
        {
            foreach (var bone in bonesToAdd)
            {
                if(Exists(bone.Name) == false)
                {
                    ESK_Bone _eskBone = bone.GetAsEskBone();
                    bool result = AddBone(bone.Index1_Name, _eskBone);

                    if(result == false)
                    {
                        throw new Exception(String.Format("Failed to add bone. Parent = {0}, Name = {1}", bone.Index1_Name, bone.Name));
                    }

                }
            }
        }
        
        private bool AddBone(string parent, ESK_Bone boneToAdd)
        {
            string[] name = boneToAdd.Name.Split('_');
            try
            {
                if (String.Format("{0}_{1}", name[1], name[2]) == "BAS_BONE") return true;
            }
            catch
            {
                //Most likely got an index out of range exception, which will happen a lot with the above code. In the event that this exception happens then the above code is not needed anyway, so do nothing here.
            }

            if (parent == String.Empty)
            {
                ESKBones.Add(boneToAdd.Clone());

                listBones = GetNonHierarchalBoneList();
                return true;
            }

            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == parent)
                {
                    ESKBones[i].ESK_Bones.Add(boneToAdd.Clone());

                    listBones = GetNonHierarchalBoneList();
                    return true;
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    bool result = AddBoneRecursive(parent, boneToAdd, ESKBones[i].ESK_Bones);

                    if (result == true)
                    {
                        listBones = GetNonHierarchalBoneList();
                        return true;
                    }
                }

            }

            return false;
        }

        private bool AddBoneRecursive(string parent, ESK_Bone boneToAdd, ObservableCollection<ESK_Bone> eskBones)
        {

            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == parent)
                {
                    if(eskBones[i].ESK_Bones == null)
                    {
                        eskBones[i].ESK_Bones = new ObservableCollection<ESK_Bone>();
                    }

                    eskBones[i].ESK_Bones.Add(boneToAdd.Clone());
                    return true;
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    bool result = AddBoneRecursive(parent, boneToAdd, eskBones[i].ESK_Bones);

                    if (result == true) return true;
                }

            }

            return false;

        }

        public static int IndexOf(string bone, List<ESK_BoneNonHierarchal> bones)
        {
            if (bones != null)
            {
                for (int i = 0; i < bones.Count; i++)
                {
                    if (bones[i].Name == bone)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public string NameOf(int index)
        {
            if (ESKBones == null) throw new Exception(String.Format("Could not get the name of the bone at index {0} because ESKBones is null.", index));
            if (index > ESKBones.Count - 1) throw new Exception(String.Format("Could not get the name of the bone at index {0} because a bone does not exist there.", index));

            if (index == -1)
            {
                return string.Empty;
            }
            else
            {
                return ESKBones[index].Name;
            }
        }

        public string GetSibling(string bone)
        {
            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == bone)
                {
                    if (i != ESKBones.Count - 1)
                    {
                        return ESKBones[i + 1].Name;
                    }
                    else
                    {
                        break;
                    }
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    string result = GetSiblingRecursive(bone, ESKBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        private string GetSiblingRecursive(string bone, ObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == bone)
                {
                    if (i != eskBones.Count - 1)
                    {
                        return eskBones[i + 1].Name;
                    }
                    else
                    {
                        break;
                    }
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    string result = GetSiblingRecursive(bone, eskBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        public string GetChild(string bone)
        {
            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == bone)
                {
                    if (ESKBones[i].ESK_Bones != null)
                    {
                        if (ESKBones[i].ESK_Bones.Count > 0)
                        {
                            return ESKBones[i].ESK_Bones[0].Name;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    string result = GetChildRecursive(bone, ESKBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        private string GetChildRecursive(string bone, ObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == bone)
                {
                    if (eskBones[i].ESK_Bones != null)
                    {
                        if (eskBones[i].ESK_Bones.Count > 0)
                        {
                            return eskBones[i].ESK_Bones[0].Name;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    string result = GetChildRecursive(bone, eskBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        public string GetParent(string bone)
        {
            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == bone)
                {
                    break;
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    string result = GetParentRecursive(bone, ESKBones[i].Name, ESKBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        private string GetParentRecursive(string bone, string parentBone, ObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == bone)
                {
                    return parentBone;
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    string result = GetParentRecursive(bone, eskBones[i].Name, eskBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        public ESK_Skeleton Clone()
        {
            ObservableCollection<ESK_Bone> bones = new ObservableCollection<ESK_Bone>();

            foreach (var e in ESKBones)
            {
                bones.Add(e.CloneAll());
            }

            return new ESK_Skeleton()
            {
                I_02 = I_02,
                I_28 = I_28,
                Unk1 = Unk1,
                UseUnk2 = UseUnk2,
                ESKBones = bones
            };
        }

        public List<ESK_BoneNonHierarchal> GetNonHierarchalBoneList()
        {
            List<ESK_BoneNonHierarchal> bones = new List<ESK_BoneNonHierarchal>();

            //Generating the list
            foreach (var bone in ESKBones)
            {
                bones.Add(new ESK_BoneNonHierarchal()
                {
                    RelativeTransform = (bone.RelativeTransform != null) ? bone.RelativeTransform.Clone() : new ESK_RelativeTransform(),
                    Name = bone.Name,
                    Index1_Name = GetParent(bone.Name),
                    Index2_Name = GetChild(bone.Name),
                    Index3_Name = GetSibling(bone.Name),
                    Index4 = bone.Index4
                });

                if (bone.ESK_Bones != null)
                {
                    bones.AddRange(GetNonHierarchalBoneListRecursive(bone.ESK_Bones));
                }

            }

            //Setting index numbers
            for (int i = 0; i < bones.Count; i++)
            {
                bones[i].Index1 = (short)IndexOf(bones[i].Index1_Name, bones);
                bones[i].Index2 = (short)IndexOf(bones[i].Index2_Name, bones);
                bones[i].Index3 = (short)IndexOf(bones[i].Index3_Name, bones);
            }

            //Returning the list
            return bones;
        }

        private List<ESK_BoneNonHierarchal> GetNonHierarchalBoneListRecursive(ObservableCollection<ESK_Bone> eskBones)
        {
            List<ESK_BoneNonHierarchal> bones = new List<ESK_BoneNonHierarchal>();

            foreach (var bone in eskBones)
            {
                bones.Add(new ESK_BoneNonHierarchal()
                {
                    RelativeTransform = (bone.RelativeTransform != null) ? bone.RelativeTransform.Clone() : new ESK_RelativeTransform(),
                    Name = bone.Name,
                    Index1_Name = GetParent(bone.Name),
                    Index2_Name = GetChild(bone.Name),
                    Index3_Name = GetSibling(bone.Name),
                    Index4 = bone.Index4
                });

                if (bone.ESK_Bones != null)
                {
                    bones.AddRange(GetNonHierarchalBoneListRecursive(bone.ESK_Bones));
                }
            }

            return bones;
        }

        public ESK_Bone GetBone(string boneName)
        {
            if (listBones == null)
                listBones = GetNonHierarchalBoneList();

            for (int i = 0; i < listBones.Count; i++)
                if (listBones[i].Name == boneName)
                    return listBones[i].GetAsEskBone();

            return null;
        }



    }

    [YAXSerializeAs("Bone")]
    public class ESK_Bone : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool isSelectedValue = true;
        [YAXDontSerialize]
        public bool isSelected
        {
            get
            {
                return this.isSelectedValue;
            }

            set
            {
                if (value != this.isSelectedValue)
                {
                    this.isSelectedValue = value;
                    NotifyPropertyChanged("isSelected");
                }
            }
        }

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("UnkIndex")]
        public short Index4 { get; set; }

        //Transforms
        public ESK_RelativeTransform RelativeTransform { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        public ObservableCollection<ESK_Bone> ESK_Bones { get; set; }

        /// <summary>
        /// Does not clone children ESK_Bones!!
        /// </summary>
        public ESK_Bone Clone()
        {
            return new ESK_Bone()
            {
                Name = (string)Name.Clone(),
                Index4 = Index4,
                RelativeTransform = RelativeTransform.Clone(),
                ESK_Bones = new ObservableCollection<ESK_Bone>(),
                isSelected = true
            };
        }

        /// <summary>
        /// Clones all children ESK_Bones as well.
        /// </summary>
        public ESK_Bone CloneAll()
        {
            return new ESK_Bone()
            {
                Name = (string)Name.Clone(),
                Index4 = Index4,
                RelativeTransform = RelativeTransform.Clone(),
                ESK_Bones = CloneAllRecursive(ESK_Bones),
                isSelected = true
            };
        }

        private ObservableCollection<ESK_Bone> CloneAllRecursive(ObservableCollection<ESK_Bone> bones)
        {
            ObservableCollection<ESK_Bone> newBones = new ObservableCollection<ESK_Bone>();

            if (ESK_Bones != null)
            {
                for (int i = 0; i < bones.Count; i++)
                {
                    newBones.Add(bones[i].CloneAll());
                }
            }
            
            return newBones;
        }

        public static ESK_Bone Read(List<byte> listBytes, byte[] bytes, int[] offsets)
        {
            int boneIndexOffset = offsets[0];
            int nameOffset = offsets[1];
            int skinningMatrixOffset = offsets[2];
            int transformMatrixOffset = offsets[3];

            return new ESK_Bone()
            {
                Index4 = BitConverter.ToInt16(bytes, boneIndexOffset + 6),
                Name = Utils.GetString(listBytes, nameOffset),
                RelativeTransform = ESK_RelativeTransform.Read(bytes, skinningMatrixOffset)
            };
        }

    }

    [YAXSerializeAs("RelativeTransform")]
    public class ESK_RelativeTransform
    {
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_44 { get; set; }

        public ESK_RelativeTransform Clone()
        {
            return new ESK_RelativeTransform()
            {
                F_00 = F_00,
                F_04 = F_04,
                F_08 = F_08,
                F_12 = F_12,
                F_16 = F_16,
                F_20 = F_20,
                F_24 = F_24,
                F_28 = F_28,
                F_32 = F_32,
                F_36 = F_36,
                F_40 = F_40,
                F_44 = F_44
            };
        }

        public static ESK_RelativeTransform Read(byte[] bytes, int offset)
        {
            if (offset == 0) return null;

            return new ESK_RelativeTransform()
            {
                F_00 = BitConverter.ToSingle(bytes, offset + 0),
                F_04 = BitConverter.ToSingle(bytes, offset + 4),
                F_08 = BitConverter.ToSingle(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                F_28 = BitConverter.ToSingle(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44)
            };
        }
    }

    [YAXSerializeAs("AbsoluteTransform")]
    public class ESK_AbsoluteTransform
    {
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_44 { get; set; }

        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_60 { get; set; }

        public ESK_AbsoluteTransform Clone()
        {
            return new ESK_AbsoluteTransform()
            {
                F_00 = F_00,
                F_04 = F_04,
                F_08 = F_08,
                F_12 = F_12,
                F_16 = F_16,
                F_20 = F_20,
                F_24 = F_24,
                F_28 = F_28,
                F_32 = F_32,
                F_36 = F_36,
                F_40 = F_40,
                F_44 = F_44,
                F_48 = F_48,
                F_52 = F_52,
                F_56 = F_56,
                F_60 = F_60
            };
        }

        public static ESK_AbsoluteTransform Read(byte[] bytes, int offset)
        {
            if (offset == 0) return null;

            return new ESK_AbsoluteTransform()
            {
                F_00 = BitConverter.ToSingle(bytes, offset + 0),
                F_04 = BitConverter.ToSingle(bytes, offset + 4),
                F_08 = BitConverter.ToSingle(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                F_28 = BitConverter.ToSingle(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52),
                F_56 = BitConverter.ToSingle(bytes, offset + 56),
                F_60 = BitConverter.ToSingle(bytes, offset + 60),
            };
        }

    }

    [YAXSerializeAs("Unk1")]
    public class ESK_Unk1
    {
        //All are Int32!
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public int I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public int I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public int I_76 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        public int I_80 { get; set; }
        [YAXAttributeFor("I_84")]
        [YAXSerializeAs("value")]
        public int I_84 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        public int I_88 { get; set; }
        [YAXAttributeFor("I_92")]
        [YAXSerializeAs("value")]
        public int I_92 { get; set; }
        [YAXAttributeFor("I_96")]
        [YAXSerializeAs("value")]
        public int I_96 { get; set; }
        [YAXAttributeFor("I_100")]
        [YAXSerializeAs("value")]
        public int I_100 { get; set; }
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        public int I_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        public int I_108 { get; set; }
        [YAXAttributeFor("I_112")]
        [YAXSerializeAs("value")]
        public int I_112 { get; set; }
        [YAXAttributeFor("I_116")]
        [YAXSerializeAs("value")]
        public int I_116 { get; set; }
        [YAXAttributeFor("I_120")]
        [YAXSerializeAs("value")]
        public int I_120 { get; set; }

        public static ESK_Unk1 Read(byte[] bytes, int offset)
        {
            var unk1 = new ESK_Unk1();
            try
            {
                unk1.I_00 = BitConverter.ToInt32(bytes, offset + 0);
                unk1.I_04 = BitConverter.ToInt32(bytes, offset + 4);
                unk1.I_08 = BitConverter.ToInt32(bytes, offset + 8);
                unk1.I_12 = BitConverter.ToInt32(bytes, offset + 12);
                unk1.I_16 = BitConverter.ToInt32(bytes, offset + 16);
                unk1.I_20 = BitConverter.ToInt32(bytes, offset + 20);
                unk1.I_24 = BitConverter.ToInt32(bytes, offset + 24);
                unk1.I_28 = BitConverter.ToInt32(bytes, offset + 28);
                unk1.I_32 = BitConverter.ToInt32(bytes, offset + 32);
                unk1.I_36 = BitConverter.ToInt32(bytes, offset + 36);
                unk1.I_40 = BitConverter.ToInt32(bytes, offset + 40);
                unk1.I_44 = BitConverter.ToInt32(bytes, offset + 44);
                unk1.I_48 = BitConverter.ToInt32(bytes, offset + 48);
                unk1.I_52 = BitConverter.ToInt32(bytes, offset + 52);
                unk1.I_56 = BitConverter.ToInt32(bytes, offset + 56);
                unk1.I_60 = BitConverter.ToInt32(bytes, offset + 60);
                unk1.I_64 = BitConverter.ToInt32(bytes, offset + 64);
                unk1.I_68 = BitConverter.ToInt32(bytes, offset + 68);
                unk1.I_72 = BitConverter.ToInt32(bytes, offset + 72);
                unk1.I_76 = BitConverter.ToInt32(bytes, offset + 76);
                unk1.I_80 = BitConverter.ToInt32(bytes, offset + 80);
                unk1.I_84 = BitConverter.ToInt32(bytes, offset + 84);
                unk1.I_88 = BitConverter.ToInt32(bytes, offset + 88);
                unk1.I_92 = BitConverter.ToInt32(bytes, offset + 92);
                unk1.I_96 = BitConverter.ToInt32(bytes, offset + 96);
                unk1.I_100 = BitConverter.ToInt32(bytes, offset + 100);
                unk1.I_104 = BitConverter.ToInt32(bytes, offset + 104);
                unk1.I_108 = BitConverter.ToInt32(bytes, offset + 108);
                unk1.I_112 = BitConverter.ToInt32(bytes, offset + 112);
                unk1.I_116 = BitConverter.ToInt32(bytes, offset + 116);
                unk1.I_120 = BitConverter.ToInt32(bytes, offset + 120);
                return unk1;

            }
            catch
            {
                return unk1;
            }
        }

    }


    //Special, for rewriting binary file
    public class ESK_BoneNonHierarchal
    {
        public string Name { get; set; }
        public short Index1 { get; set; }
        public short Index2 { get; set; }
        public short Index3 { get; set; }
        public short Index4 { get; set; }


        public string Index1_Name { get; set; }
        public string Index2_Name { get; set; }
        public string Index3_Name { get; set; }

        //Transforms
        public ESK_RelativeTransform RelativeTransform { get; set; }


        public ESK_Bone GetAsEskBone()
        {
            return new ESK_Bone()
            {
                isSelected = true,
                Index4 = Index4,
                ESK_Bones = new ObservableCollection<ESK_Bone>(),
                Name = (string)Name.Clone(),
                RelativeTransform = RelativeTransform.Clone()
            };
        }

    }



}
