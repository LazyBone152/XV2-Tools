using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using YAXLib;
using System.IO;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.Resource;
using System.Numerics;
using Xv2CoreLib.ESK;

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
    [Serializable]
    public class EAN_File : INotifyPropertyChanged, ISorting, IIsNull
    {
        #region INotifyPropertyChanged
        [field: NonSerialized]
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
        #endregion

        public const float DefaultFoV = 39.97836f;

        [YAXAttributeForClass]
        public bool IsCamera { get; set; } //offset 16
        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        public byte I_17 { get; set; }

        public ESK_Skeleton Skeleton { get; set; }
        private AsyncObservableCollection<EAN_Animation> AnimationsValue = null;
        public AsyncObservableCollection<EAN_Animation> Animations
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

        #region Load/Save
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

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }
        
        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }


        //Helpers
        /// <summary>
        /// Create ESK_RelativeTransform references on all nodes and components.
        /// </summary>
        public void LinkEskData()
        {
            foreach (var anim in Animations)
            {
                anim.LinkEskData(Skeleton);
            }
        }

        public void SortEntries()
        {
            Animations = Sorting.SortEntries(Animations);
        }

        public bool ValidateAnimationIndexes()
        {
            if (Animations == null) return true;
            List<int> UsedIndexes = new List<int>();

            foreach (var anim in Animations)
            {
                if (UsedIndexes.IndexOf(anim.IndexNumeric) != -1)
                {
                    throw new Exception(String.Format("Animation Index {0} was defined more than once. Animations cannot share the same Index!", anim.IndexNumeric));
                }
                UsedIndexes.Add(anim.IndexNumeric);
            }

            return true;
        }

        #endregion

        #region GetAnimation
        /// <summary>
        /// Returns the zero-index of the animation matching the parameter name. If none are found, -1 is returned.
        /// </summary>
        public int IndexOf(string name)
        {
            return Animations.IndexOf(Animations.FirstOrDefault(x => x.Name == name));
        }

        /// <summary>
        /// Returns the zero-index of the animation matching the parameter id. If none are found, -1 is returned.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int IndexOf(int id)
        {
            return Animations.IndexOf(Animations.FirstOrDefault(x => x.IndexNumeric == id));
        }

        /// <summary>
        /// Index of animation based on a split name (first 4 characters removed). 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int IndexOf_SplitName(string name)
        {
            for (int i = 0; i < Animations.Count; i++)
            {
                if (Animations[i].Name.Length > 4)
                {
                    if (name == Animations[i].Name.Remove(0, 4))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public EAN_Animation GetAnimation(int id, bool returnNull = false)
        {
            if (IndexOf(id) == -1 && !returnNull) throw new Exception("Could not find an animation with an ID matching " + id);
            if (IndexOf(id) == -1 && returnNull) return null;
            return Animations[IndexOf(id)];
        }

        /// <summary>
        /// Looks for an animation and returns its ID.
        /// </summary>
        /// <returns></returns>
        public int AnimationExists(EAN_Animation anim)
        {
            foreach (var _anim in Animations)
            {
                if (_anim.Compare(anim)) return _anim.IndexNumeric;
            }

            return -1;
        }

        public string NameOf(int id)
        {
            if (IndexOf(id) == -1) throw new Exception("Could not find an animation with an ID matching " + id);
            return Animations[IndexOf(id)].Name;
        }

        #endregion

        #region AddAnimation
        public void AddEntry(int id, EAN_Animation entry)
        {
            entry.IndexNumeric = (ushort)id;

            for (int i = 0; i < Animations.Count; i++)
            {
                if (Animations[i].IndexNumeric == id)
                {
                    Animations[i] = entry;
                    return;
                }
            }

            Animations.Add(entry);
            entry.LinkEskData(Skeleton);
        }

        /// <summary>
        /// Adds the animation and returns the auto-assigned id.
        /// </summary>
        /// <param name="anim"></param>
        /// <returns></returns>
        public int AddEntry(EAN_Animation anim)
        {
            anim.LinkEskData(Skeleton);

            int newId = NextID();
            anim.IndexNumeric = newId;
            Animations.Add(anim);

            return newId;
        }

        #endregion

        #region Defaults
        public static EAN_File DefaultCamFile()
        {
            var eskBones = AsyncObservableCollection<ESK_Bone>.Create();
            eskBones.Add(new ESK_Bone()
            {
                Name = EAN_Node.CAM_NODE,
                RelativeTransform = new ESK_RelativeTransform()
                {
                    F_28 = 1f,
                    F_32 = 1f,
                    F_36 = 1f,
                    F_40 = 1f
                }
            });

            return new EAN_File()
            {
                Animations = AsyncObservableCollection<EAN_Animation>.Create(),
                IsCamera = true,
                I_08 = 37508,
                Skeleton = new ESK_Skeleton()
                {
                    I_28 = new int[2] { 1900697063, 175112582 },
                    UseUnk2 = true,
                    ESKBones = eskBones
                }
            };
        }

        public static EAN_File DefaultFile(ESK_Skeleton skeleton)
        {
            return new EAN_File()
            {
                Animations = AsyncObservableCollection<EAN_Animation>.Create(),
                IsCamera = true,
                I_08 = 37508,
                Skeleton = skeleton.Clone()
            };
        }

        #endregion

        #region Helper
        public EAN_File Clone()
        {
            AsyncObservableCollection<EAN_Animation> anims = AsyncObservableCollection<EAN_Animation>.Create();
            foreach (var a in Animations)
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
        public int GetAnimationIndexCount()
        {
            if (Animations == null) return 0;
            if (Animations.Count == 0) return 0;

            int idx = Animations.Count() - 1;

            return Animations[idx].IndexNumeric + 1;
        }

        public int NextID(int mindID = 0)
        {
            int id = mindID;

            while (IndexOf(id) != -1)
            {
                id++;
            }

            return id;
        }
        
        public bool IsNull()
        {
            return (Animations.Count == 0);
        }

        #endregion


    }

    [YAXSerializeAs("Animation")]
    [Serializable]
    public class EAN_Animation : INotifyPropertyChanged, IInstallable
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
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
        #endregion

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

        #region WrapperProps
        [YAXDontSerialize]
        public int SortID { get { return IndexNumeric; } }
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
        [YAXDontSerialize]
        public string DisplayName
        {
            get
            {
                return String.Format("[{0}] {1}", IndexNumeric, Name);
            }
        }

        [YAXDontSerialize]
        public ushort ID_UShort
        {
            //For binding in WPF - cant be bothered doing a (int > ushort) value converter
            get { return ushort.Parse(Index); }
            set { Index = ID_UShort.ToString(); }
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
        #endregion

        internal ESK_Skeleton eskSkeleton { get; set; }

        //Values
        private string _index = null;
        private string _NameValue = String.Empty;
        private int _frameCountValue = 0;

        //Properties
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
                    NotifyPropertyChanged(nameof(Index));
                    NotifyPropertyChanged(nameof(IndexNumeric));
                    NotifyPropertyChanged(nameof(DisplayName));
                }
            }
        }
        
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
                    NotifyPropertyChanged(nameof(Name));
                    NotifyPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public int FrameCount
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
                    NotifyPropertyChanged(nameof(FrameCount));
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
        public AsyncObservableCollection<EAN_Node> Nodes { get; set; }


        #region KeyframeManipulation
        public List<IUndoRedo> RemoveKeyframe(int frame)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var bone in Nodes)
            {
                bone.RemoveKeyframe(frame, undos);
            }

            return undos;
        }

        public List<IUndoRedo> AddKeyframe(string nodeName, int frame, float posX, float posY, float posZ, float posW, float rotX, float rotY, float rotZ, float rotW,
            float scaleX, float scaleY, float scaleZ, float scaleW)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            EAN_Node node = GetNode(nodeName, true, undos);

            if (node == null) throw new InvalidDataException($"EAN_Animation.AddKeyframe: \"{nodeName}\" not found.");
            node.AddKeyframe(frame, posX, posY, posZ, posW, rotX, rotY, rotZ, rotW, scaleX, scaleY, scaleZ, scaleW, undos);

            return undos;
        }
        #endregion

        #region Get
        public EAN_Node GetNode(string BoneName, bool createNode, List<IUndoRedo> undos = null)
        {
            EAN_Node node = GetNode(BoneName);

            if (node == null && createNode)
            {
                node = CreateNode(BoneName, undos);
            }

            return node;
        }

        public EAN_Node GetNode(string BoneName)
        {
            if (Nodes == null) throw new Exception(String.Format("Could not retrieve the bone {0} becauses Nodes is null.", BoneName));

            for (int i = 0; i < Nodes.Count(); i++)
            {
                if (BoneName == Nodes[i].BoneName)
                {
                    return Nodes[i];
                }
            }

            return null;
        }

        public int GetLastKeyframe()
        {
            int max = 0;
            foreach (var e in Nodes)
            {
                foreach (var a in e.AnimationComponents)
                {
                    int thisMax = a.Keyframes.Max(x => x.FrameIndex);
                    if (thisMax > max) max = thisMax;
                }
            }
            return max;
        }

        #endregion

        #region Helpers

        internal void LinkEskData(ESK_Skeleton skeleton)
        {
            if (skeleton == null) new ArgumentNullException("EAN_Animation.LinkEskData: skeleton was null.");

            eskSkeleton = skeleton;
            foreach (var node in Nodes)
            {
                node.LinkEskData(skeleton);
            }
        }

        public EAN_Node CreateNode(string bone, List<IUndoRedo> undos = null)
        {
            if (undos == null) undos = new List<IUndoRedo>();

            EAN_Node existing = Nodes.FirstOrDefault(x => x.BoneName == bone);
            if (existing != null) return existing;

            EAN_Node node = new EAN_Node();
            node.AnimationComponents = AsyncObservableCollection<EAN_AnimationComponent>.Create();
            node.BoneName = bone;
            node.EskRelativeTransform = eskSkeleton.GetBone(bone)?.RelativeTransform;

            Nodes.Add(node);
            undos.Add(new UndoableListAdd<EAN_Node>(Nodes, node));

            return node;
        }

        public EAN_Animation Clone()
        {
            AsyncObservableCollection<EAN_Node> _Nodes = AsyncObservableCollection<EAN_Node>.Create();
            for(int i = 0; i < Nodes.Count; i++)
            {
                _Nodes.Add(Nodes[i].Clone());
            }

            return new EAN_Animation()
            {
                IndexNumeric = IndexNumeric,
                I_02 = I_02,
                I_03 = I_03,
                FrameCount = FrameCount,
                Name = (string)Name.Clone(),
                Nodes = _Nodes
            };
        }
        
        public bool Compare(EAN_Animation anim)
        {
            if (anim.Name != Name) return false;
            if (anim.FloatType != FloatType) return false;
            if (anim.I_02 != I_02) return false;
            if (anim.I_03 != I_03) return false;
            if (anim.FrameCount != FrameCount) return false;
            if (anim.Nodes == null && Nodes == null) return true;
            if (anim.Nodes == null || Nodes == null) return false;

            if (anim.Nodes.Count != Nodes.Count) return false;

            for (int i = 0; i < Nodes.Count; i++)
            {
                if (!Nodes[i].Compare(anim.Nodes[i])) return false;
            }

            return true;
        }

        public void IncreaseFrameIndex(int amount)
        {
            for(int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].IncreaseFrameIndex(amount);
            }
        }

        public List<IUndoRedo> RemoveDuplicateKeyframes()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var node in Nodes)
            {
                undos.AddRange(node.RemoveDuplicateKeyframes());
            }

            return undos;
        }
        #endregion

        #region Init

        /// <summary>
        /// Returns a list of all bones that do not exist in the current skeleton.
        /// </summary>
        public List<string> GetInvalidBones(ESK_Skeleton skeleton)
        {
            List<string> bones = new List<string>();

            foreach (var node in Nodes.Where(x => !skeleton.Exists(x.BoneName)))
            {
                bones.Add(node.BoneName);
            }

            return bones;
        }

        /// <summary>
        /// Removes all invalidBones.
        /// </summary>
        /// <param name="invalidBones"></param>
        public List<IUndoRedo> RemoveInvalidBones(List<string> invalidBones)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                if (invalidBones.Contains(Nodes[i].BoneName))
                {
                    undos.Add(new UndoableListRemove<EAN_Node>(Nodes, Nodes[i]));
                    Nodes.RemoveAt(i);
                }
            }

            return undos;
        }

        #endregion

        #region Modifiers
        public List<IUndoRedo> ApplyNodeOffset(List<string> boneFilter, EAN_AnimationComponent.ComponentType componentType, float x, float y, float z, float w)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var node in Nodes.Where(n => boneFilter.Contains(n.BoneName)))
            {
                node.ApplyNodeOffset(componentType, x, y, z, w, undos);
            }

            return undos;
        }

        public List<IUndoRedo> RemoveKeyframes(List<string> boneFilter, int minFrame, int maxFrame)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var node in Nodes.Where(x => boneFilter.Contains(x.BoneName)))
            {
                foreach (var component in node.AnimationComponents)
                {
                    for (int i = component.Keyframes.Count - 1; i >= 0; i--)
                    {
                        if (component.Keyframes[i].FrameIndex >= minFrame && component.Keyframes[i].FrameIndex <= maxFrame)
                        {
                            undos.Add(new UndoableListRemove<EAN_Keyframe>(component.Keyframes, component.Keyframes[i]));
                            component.Keyframes.RemoveAt(i);
                        }
                    }
                }
            }

            return undos;
        }

        public List<IUndoRedo> Cut(int startFrame, int endFrame, int smoothingFrames)
        {
            if (endFrame > GetLastKeyframe()) endFrame = GetLastKeyframe();
            if(startFrame > GetLastKeyframe()) throw new InvalidOperationException("EAN_Animation.Cut: StartFrame cannot be greater than the total number of animation frames.");
            if(endFrame <= startFrame) throw new InvalidOperationException("EAN_Animation.Cut: EndFrame cannot be less than or equal to StartFrame.");

            List<IUndoRedo> undos = new List<IUndoRedo>();

            for (int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].Cut(startFrame, endFrame, smoothingFrames, undos);
            }

            return undos;
        }

        public List<IUndoRedo> RemoveNodes(IList<EAN_Node> nodes)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var node in nodes.Where(x => Nodes.Contains(x)))
            {
                undos.Add(new UndoableListRemove<EAN_Node>(Nodes, node));
                Nodes.Remove(node);
            }

            return undos;
        }

        public List<IUndoRedo> PasteNodes(IList<EAN_Node> nodes)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var node in nodes)
            {
                int idx = Nodes.IndexOf(Nodes.FirstOrDefault(x => x.BoneName == node.BoneName));

                if(idx != -1)
                {
                    undos.Add(new UndoableStateChange<EAN_Node>(Nodes, idx, Nodes[idx], node));
                    Nodes[idx] = node;
                }
                else
                {
                    Nodes.Add(node);
                    undos.Add(new UndoableListAdd<EAN_Node>(Nodes, node));
                }

                node.LinkEskData(eskSkeleton);
            }

            return undos;
        }

        public List<IUndoRedo> RescaleAnimation(int newDuration)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var node in Nodes)
            {
                undos.AddRange(node.RescaleNode(newDuration));
            }

            if(FrameCount != newDuration)
            {
                undos.AddRange(ChangeDuration(newDuration));
            }

            undos.AddRange(RemoveDuplicateKeyframes());

            return undos;
        }

        /// <summary>
        /// Changes the animations durations without rescaling the keyframes.
        /// </summary>
        /// <returns></returns>
        public List<IUndoRedo> ChangeDuration(int newDuration)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //With the changes to how first and final frames are handle when saving, adding them while editing is no longer necessary. So simply changing the FrameCount is sufficent.
            undos.Add(new UndoableProperty<EAN_Animation>(nameof(FrameCount), this, FrameCount, newDuration));
            FrameCount = newDuration;

            if(newDuration > byte.MaxValue && I_02 != IntPrecision._16Bit)
            {
                undos.Add(new UndoableProperty<EAN_Animation>(nameof(I_02), this, I_02, IntPrecision._16Bit));
                I_02 = IntPrecision._16Bit;
            }
            else if(I_02 != IntPrecision._8Bit)
            {
                undos.Add(new UndoableProperty<EAN_Animation>(nameof(I_02), this, I_02, IntPrecision._8Bit));
                I_02 = IntPrecision._8Bit;
            }

            return undos;
        }

        //Old modifiers - to be reworked or removed
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
    [Serializable]
    public class EAN_Node
    {
        public const string CAM_NODE = "Node";

        [YAXDontSerialize]
        internal ESK_RelativeTransform EskRelativeTransform { get; set; }


        [YAXAttributeForClass]
        public string BoneName { get; set; } 

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Keyframes")]
        public AsyncObservableCollection<EAN_AnimationComponent> AnimationComponents { get; set; }

        #region KeyframeManipulation
        /// <summary>
        /// Add a keyframe at the specified frame (will overwrite any existing keyframe).
        /// </summary>
        public void AddKeyframe(int frame, float posX, float posY, float posZ, float posW, float rotX, float rotY, float rotZ, float rotW,
            float scaleX, float scaleY, float scaleZ, float scaleW, List<IUndoRedo> undos)
        {
            var pos = GetComponent(EAN_AnimationComponent.ComponentType.Position, true, undos, BoneName == CAM_NODE);
            pos.AddKeyframe(frame, posX, posY, posZ, posW, undos);

            var rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation, true, undos, BoneName == CAM_NODE);
            rot.AddKeyframe(frame, rotX, rotY, rotZ, rotW, undos);

            var scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale, true, undos, BoneName == CAM_NODE);
            scale.AddKeyframe(frame, scaleX, scaleY, scaleZ, scaleW, undos);


        }

        public void RemoveKeyframe(int keyframe, List<IUndoRedo> undos = null)
        {
            foreach (var component in AnimationComponents)
            {
                component.RemoveKeyframe(keyframe, undos);
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

        #region Get
        public EAN_Keyframe GetKeyframe(int frame, EAN_AnimationComponent.ComponentType type, bool createKeyframe, List<IUndoRedo> undos = null)
        {
            var component = GetComponent(type, createKeyframe, undos);

            return (component != null) ? component.GetKeyframe(frame, createKeyframe, undos) : null;
        }

        public EAN_AnimationComponent GetComponent(EAN_AnimationComponent.ComponentType _type, bool createComponent, List<IUndoRedo> undos = null, bool isCamera = false)
        {
            var component = GetComponent(_type);

            if (component == null && createComponent)
            {
                component = new EAN_AnimationComponent(_type, isCamera, EskRelativeTransform);
                undos.Add(new UndoableListAdd<EAN_AnimationComponent>(AnimationComponents, component));
                AnimationComponents.Add(component);
            }

            return component;
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

        public SerializedKeyframe GetSerializedKeyframe(int frame)
        {
            return new SerializedKeyframe(frame, GetKeyframeValues(frame));
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

            keyframes.Sort();

            return keyframes;
        }

        public List<int> GetAllKeyframesInt()
        {
            return ArrayConvert.ConvertToIntList(GetAllKeyframes());
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

        #endregion

        #region Helper
        internal void LinkEskData(ESK_Skeleton skeleton)
        {
            if (skeleton == null) new ArgumentNullException("EAN_Node.LinkEskData: skeleton was null.");

            var esk = skeleton.GetBone(BoneName);
            EskRelativeTransform = esk?.RelativeTransform;

            foreach (var component in AnimationComponents)
            {
                component.EskRelativeTransform = esk?.RelativeTransform;
            }
        }

        public bool HasKeyframe(int frame)
        {
            foreach (var component in AnimationComponents)
            {
                if (component.Keyframes.FirstOrDefault(e => e.FrameIndex == frame) != null) return true;
            }

            return false;
        }

        public EAN_Node Clone()
        {
            AsyncObservableCollection<EAN_AnimationComponent> _AnimationComponent = AsyncObservableCollection<EAN_AnimationComponent>.Create();
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

        public List<IUndoRedo> RemoveDuplicateKeyframes()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var component in AnimationComponents)
            {
                for (int i = component.Keyframes.Count - 1; i >= 0; i--)
                {
                    if(component.Keyframes.Any(x => x.FrameIndex == component.Keyframes[i].FrameIndex && x != component.Keyframes[i]))
                    {
                        undos.Add(new UndoableListRemove<EAN_Keyframe>(component.Keyframes, component.Keyframes[i]));
                        component.Keyframes.RemoveAt(i);
                    }
                }
            }

            return undos;
        }
        
        public List<IUndoRedo> CreateAllComponents()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            int endFrame = GetLastKeyframe();

            EAN_AnimationComponent pos = GetComponent(EAN_AnimationComponent.ComponentType.Position);
            EAN_AnimationComponent rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            EAN_AnimationComponent scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            if(pos == null)
            {
                pos = new EAN_AnimationComponent(EAN_AnimationComponent.ComponentType.Position, false, EskRelativeTransform);
                undos.Add(new UndoableListAdd<EAN_AnimationComponent>(AnimationComponents, pos));
                AnimationComponents.Add(pos);
            }

            if (rot == null)
            {
                rot = new EAN_AnimationComponent(EAN_AnimationComponent.ComponentType.Rotation, false, EskRelativeTransform);
                undos.Add(new UndoableListAdd<EAN_AnimationComponent>(AnimationComponents, rot));
                AnimationComponents.Add(rot);
            }

            if (scale == null)
            {
                scale = new EAN_AnimationComponent(EAN_AnimationComponent.ComponentType.Scale, false, EskRelativeTransform);
                undos.Add(new UndoableListAdd<EAN_AnimationComponent>(AnimationComponents, scale));
                AnimationComponents.Add(scale);
            }

            return undos;
        }

        #endregion

        #region Modifiers
        public List<IUndoRedo> RescaleNode(int newDuration)
        {
            if (newDuration == 0) newDuration = 1;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var component in AnimationComponents)
            {
                float oldEndFrame = component.GetLastKeyframe();
                if (oldEndFrame == (newDuration - 1)) continue;

                foreach (var keyframe in component.Keyframes)
                {
                    ushort newFrameIndex = (ushort)(keyframe.FrameIndex * ((newDuration - 1) / oldEndFrame));

                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.FrameIndex), keyframe, keyframe.FrameIndex, newFrameIndex));
                    keyframe.FrameIndex = newFrameIndex;
                }
            }

            return undos;
        }

        public List<IUndoRedo> PasteKeyframes(List<SerializedKeyframe> keyframes)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.AddRange(CreateAllComponents());
            EAN_AnimationComponent pos = GetComponent(EAN_AnimationComponent.ComponentType.Position);
            EAN_AnimationComponent rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            EAN_AnimationComponent scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            foreach (var keyframe in keyframes)
            {
                undos.AddRange(pos.SetKeyframeValue(keyframe.Frame, keyframe.PosX, Axis.X));
                undos.AddRange(pos.SetKeyframeValue(keyframe.Frame, keyframe.PosY, Axis.Y));
                undos.AddRange(pos.SetKeyframeValue(keyframe.Frame, keyframe.PosZ, Axis.Z));
                undos.AddRange(pos.SetKeyframeValue(keyframe.Frame, keyframe.PosW, Axis.W));

                undos.AddRange(rot.SetKeyframeValue(keyframe.Frame, keyframe.RotX, Axis.X));
                undos.AddRange(rot.SetKeyframeValue(keyframe.Frame, keyframe.RotY, Axis.Y));
                undos.AddRange(rot.SetKeyframeValue(keyframe.Frame, keyframe.RotZ, Axis.Z));
                undos.AddRange(rot.SetKeyframeValue(keyframe.Frame, keyframe.RotW, Axis.W));

                undos.AddRange(scale.SetKeyframeValue(keyframe.Frame, keyframe.ScaleX, Axis.X));
                undos.AddRange(scale.SetKeyframeValue(keyframe.Frame, keyframe.ScaleY, Axis.Y));
                undos.AddRange(scale.SetKeyframeValue(keyframe.Frame, keyframe.ScaleZ, Axis.Z));
                undos.AddRange(scale.SetKeyframeValue(keyframe.Frame, keyframe.ScaleW, Axis.W));
            }

            return undos;
        }

        public List<IUndoRedo> DeleteKeyframes(List<int> keyframes)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            EAN_AnimationComponent pos = GetComponent(EAN_AnimationComponent.ComponentType.Position);
            EAN_AnimationComponent rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            EAN_AnimationComponent scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            foreach (var keyframe in keyframes)
            {
                if (pos != null) pos.RemoveKeyframe(keyframe, undos);
                if (rot != null) rot.RemoveKeyframe(keyframe, undos);
                if (scale != null) scale.RemoveKeyframe(keyframe, undos);
            }

            return undos;
        }

        public void ApplyNodeOffset(EAN_AnimationComponent.ComponentType componentType, float x, float y, float z, float w, List<IUndoRedo> undos)
        {
            var _component = GetComponent(componentType);
            if (_component != null)
            {
                _component.ApplyNodeOffset(x, y, z, w, undos);
            }
        }
        
        public void Cut(int startFrame, int endFrame, int smoothingFrames, List<IUndoRedo> undos)
        {
            for(int i = 0; i < AnimationComponents.Count; i++)
            {
                AnimationComponents[i].Cut(startFrame, endFrame, smoothingFrames, undos);
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
    [Serializable]
    public class EAN_AnimationComponent
    {
        public enum ComponentType : byte
        {
            Position = 0,
            Rotation = 1,
            Scale = 2, //Or "Camera"
        }

        [YAXDontSerialize]
        internal ESK_RelativeTransform EskRelativeTransform { get; set; }

        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public ComponentType I_00 { get; set; } //int8
        [YAXAttributeForClass]
        public byte I_01 { get; set; }
        [YAXAttributeForClass]
        public short I_02 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Keyframe")]
        public AsyncObservableCollection<EAN_Keyframe> Keyframes { get; set; } = AsyncObservableCollection<EAN_Keyframe>.Create();

        public EAN_AnimationComponent() { }

        public EAN_AnimationComponent(ComponentType type, bool isCamera, ESK_RelativeTransform relativeTransform)
        {
            I_00 = type;
            I_01 = (byte)((isCamera) ? 3 : 7);
            EskRelativeTransform = relativeTransform;
        }

        #region KeyframeManipulation
        public void AddKeyframe(int frame, float x, float y, float z, float w, List<IUndoRedo> undos = null)
        {
            var keyframe = Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)frame);

            if(keyframe != null)
            {
                if(undos != null)
                {
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(keyframe.X), keyframe, keyframe.X, x));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(keyframe.Y), keyframe, keyframe.Y, y));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(keyframe.Z), keyframe, keyframe.Z, z));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(keyframe.W), keyframe, keyframe.W, w));
                }

                keyframe.X = x;
                keyframe.Y = y;
                keyframe.Z = z;
                keyframe.W = w;
            }
            else
            {
                EAN_Keyframe newKeyframe = new EAN_Keyframe()
                {
                    FrameIndex = (ushort)frame,
                    X = x,
                    Y = y,
                    Z = z,
                    W = w
                };

                if (undos != null)
                    undos.Add(new UndoableListAdd<EAN_Keyframe>(Keyframes, newKeyframe));

                Keyframes.Add(newKeyframe);
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

        public void RemoveKeyframe(int frame, List<IUndoRedo> undos = null)
        {
            EAN_Keyframe keyframe = GetKeyframe(frame);

            if (keyframe != null)
            {
                if (undos != null)
                    undos.Add(new UndoableListRemove<EAN_Keyframe>(Keyframes, keyframe));

                Keyframes.Remove(keyframe);
            }
        }

        public EAN_Keyframe AddKeyframe(int frame)
        {
            AddKeyframe(frame, GetKeyframeValue(frame, Axis.X), GetKeyframeValue(frame, Axis.Y), GetKeyframeValue(frame, Axis.Z), GetKeyframeValue(frame, Axis.W));
            return Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)frame);
        }

        public List<IUndoRedo> SetKeyframeValue(int frame, float value, Axis axis)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            EAN_Keyframe keyframe = GetKeyframe(frame);

            if (keyframe == null)
            {
                keyframe = new EAN_Keyframe((ushort)frame, GetKeyframeValue(frame, Axis.X), GetKeyframeValue(frame, Axis.Y), GetKeyframeValue(frame, Axis.Z), GetKeyframeValue(frame, Axis.W));

                Keyframes.Add(keyframe);
                undos.Add(new UndoableListAdd<EAN_Keyframe>(Keyframes, keyframe));
            }

            switch (axis)
            {
                case Axis.X:
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), keyframe, keyframe.X, value));
                    keyframe.X = value;
                    break;
                case Axis.Y:
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), keyframe, keyframe.Y, value));
                    keyframe.Y = value;
                    break;
                case Axis.Z:
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), keyframe, keyframe.Z, value));
                    keyframe.Z = value;
                    break;
                case Axis.W:
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), keyframe, keyframe.W, value));
                    keyframe.W = value;
                    break;
            }

            return undos;
        }

        #endregion

        #region Get
        public EAN_Keyframe GetKeyframe(int frame, bool createKeyframe, List<IUndoRedo> undos = null)
        {
            EAN_Keyframe keyframe = GetKeyframe(frame);

            if (keyframe == null && createKeyframe)
            {
                keyframe = new EAN_Keyframe((ushort)frame, GetKeyframeValue(frame, Axis.X), GetKeyframeValue(frame, Axis.Y), GetKeyframeValue(frame, Axis.Z), GetKeyframeValue(frame, Axis.W));
                Keyframes.Add(keyframe);

                if (undos != null)
                {
                    undos.Add(new UndoableListAdd<EAN_Keyframe>(Keyframes, keyframe));
                }
            }

            return keyframe;
        }

        public EAN_Keyframe GetKeyframe(int frame)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.FrameIndex == frame) return keyframe;
            }

            return null;
        }

        private EAN_Keyframe GetDefaultKeyframe(int frame = 0)
        {
            EAN_Keyframe keyframe = new EAN_Keyframe();
            keyframe.FrameIndex = (ushort)frame;

            if (I_00 == ComponentType.Scale && I_01 == 3)
            {
                //Camera
                keyframe.Y = (float)Utils.ConvertDegreesToRadians(EAN_File.DefaultFoV);
            }
            else if (EskRelativeTransform == null)
            {
                //Camera Pos/Rot

                if (I_00 == ComponentType.Rotation)
                    keyframe.W = 1f;

            }
            else if (I_00 == ComponentType.Scale)
            {
                keyframe.X = EskRelativeTransform.F_32;
                keyframe.Y = EskRelativeTransform.F_36;
                keyframe.Z = EskRelativeTransform.F_40;
                keyframe.W = EskRelativeTransform.F_44;
            }
            else if (I_00 == ComponentType.Position)
            {
                keyframe.X = EskRelativeTransform.F_00;
                keyframe.Y = EskRelativeTransform.F_04;
                keyframe.Z = EskRelativeTransform.F_08;
                keyframe.W = EskRelativeTransform.F_12;
            }
            else if (I_00 == ComponentType.Rotation)
            {
                keyframe.X = EskRelativeTransform.F_16;
                keyframe.Y = EskRelativeTransform.F_20;
                keyframe.Z = EskRelativeTransform.F_24;
                keyframe.W = EskRelativeTransform.F_28;
            }

            return keyframe;
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

        public int GetLastKeyframe()
        {
            if (Keyframes.Count == 0) return 0;
            return Keyframes.Max(k => k.FrameIndex);
        }

        #endregion

        #region KeyframeInterpolation
        /// <summary>
        /// Get an interpolated keyframe value, from the specified floating-point frame. Allows time-scaled animations.
        /// </summary>
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

        /// <summary>
        /// Get an interpolated keyframe value.
        /// </summary>
        public float GetKeyframeValue(int frame, Axis axis)
        {
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
            int prevFrame = 0;
            int nextFrame = 0;
            EAN_Keyframe previousKeyframe = GetNearestKeyframeBefore(frame, ref prevFrame);
            EAN_Keyframe nextKeyframe = GetNearestKeyframeAfter(frame, ref nextFrame);

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
            int diff = nextFrame - prevFrame;

            //Keyframe value difference
            float keyframe2 = currentKeyframeValue - previousKeyframeValue;

            //Difference between the frame we WANT and the previous frame
            int diff2 = frame - prevFrame;

            //Divide keyframe value difference by the keyframe time difference, and then multiply it by diff2, then add the previous keyframe value
            return (keyframe2 / diff) * diff2 + previousKeyframeValue;
        }

        /// <summary>
        /// Get a non-persistent instance of the nearest keyframe BEFORE the specified frame. This instant MAY belong to another keyframe entirely, or not exist in Keyframes at all (if its a default keyframe generated because no keyframes currently exist) - so this method is ONLY intended for reading purposes! 
        /// </summary>
        /// <param name="frame">The specified frame.</param>
        /// <param name="nearFrame">The frame the returned EAN_Keyframe belongs to (ignore FrameIndex on the keyframe) </param>
        private EAN_Keyframe GetNearestKeyframeBefore(int frame, ref int nearFrame)
        {
            EAN_Keyframe nearest = null;

            //First search for keyframe before frame
            foreach(var keyframe in Keyframes.Where(x => x.FrameIndex < frame && (x.FrameIndex > nearest?.FrameIndex || nearest == null)))
            {
                nearest = keyframe;
                nearFrame = keyframe.FrameIndex;
            }

            //If none found, search for one after
            if (nearest == null)
            {
                foreach (var keyframe in Keyframes.Where(x => x.FrameIndex > frame && (x.FrameIndex < nearest?.FrameIndex || nearest == null)))
                {
                    nearest = keyframe;
                    nearFrame = frame - 1;
                }
            }

            //None found, so use default
            if (nearest == null)
            {
                nearest = GetDefaultKeyframe();
                nearFrame = frame - 1;
            }

            return nearest;
        }

        /// <summary>
        /// Get a non-persistent instance of the nearest keyframe AFTER the specified frame. This instant MAY belong to another keyframe entirely, or not exist in Keyframes at all (if its a default keyframe generated because no keyframes currently exist) - so this method is ONLY intended for reading purposes! 
        /// </summary>
        /// <param name="frame">The specified frame.</param>
        /// <param name="nearFrame">The frame the returned EAN_Keyframe belongs to (ignore FrameIndex on the keyframe) </param>
        private EAN_Keyframe GetNearestKeyframeAfter(int frame, ref int nearFrame)
        {
            EAN_Keyframe nearest = null;

            //First search for keyframe after frame
            foreach (var keyframe in Keyframes.Where(x => x.FrameIndex > frame && (x.FrameIndex < nearest?.FrameIndex || nearest == null)))
            {
                nearest = keyframe;
                nearFrame = keyframe.FrameIndex;
            }

            //If none found, search for one before
            if (nearest == null)
            {
                foreach (var keyframe in Keyframes.Where(x => x.FrameIndex < frame && (x.FrameIndex > nearest?.FrameIndex || nearest == null)))
                {
                    nearest = keyframe;
                    nearFrame = frame + 1;
                }
            }

            //None found, so use default
            if (nearest == null)
            {
                nearest = GetDefaultKeyframe();
                nearFrame = frame + 1;
            }

            return nearest;
        }

        #endregion

        #region Helper

        public EAN_AnimationComponent Clone()
        {
            AsyncObservableCollection<EAN_Keyframe> _keyframes = AsyncObservableCollection<EAN_Keyframe>.Create();

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

        public void IncreaseFrameIndex(int amount)
        {
            for (int i = 0; i < Keyframes.Count; i++)
            {
                Keyframes[i].FrameIndex += (ushort)amount;
            }
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

        #endregion

#region Modifiers
        public void ApplyNodeOffset(float x, float y, float z, float w, List<IUndoRedo> undos)
        {
            if (Keyframes == null) throw new Exception(String.Format("Could not apply NodeOffset because Keyframes is null."));

            for (int i = 0; i < Keyframes.Count; i++)
            {
                float newX = Keyframes[i].X + x;
                float newY = Keyframes[i].Y + y;
                float newZ = Keyframes[i].Z + z;
                float newW = Keyframes[i].W + w;

                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), Keyframes[i], Keyframes[i].X, newX));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), Keyframes[i], Keyframes[i].Y, newY));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), Keyframes[i], Keyframes[i].Z, newZ));
                undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), Keyframes[i], Keyframes[i].W, newW));

                Keyframes[i].X = newX;
                Keyframes[i].Y = newY;
                Keyframes[i].Z = newZ;
                Keyframes[i].W = newW;
            }
        }

        public void Cut(int startFrame, int endFrame, int smoothingFrames, List<IUndoRedo> undos)
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
                undos.Add(new UndoableListRemove<EAN_Keyframe>(Keyframes, keyframe));
                Keyframes.Remove(keyframe);
            }

            //Adjusting frameIndex of keyframes that come after endFrame, and applying smoothingFrames
            int amountToDecreaseBy = (endFrame - startFrame) + smoothingFrames;
            for(int i = 0; i < Keyframes.Count; i++)
            {
                if(Keyframes[i].FrameIndex > endFrame)
                {
                    ushort newFrame = (ushort)(Keyframes[i].FrameIndex - amountToDecreaseBy);
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.FrameIndex), Keyframes[i], Keyframes[i].FrameIndex, newFrame));
                    Keyframes[i].FrameIndex = newFrame;
                }
            }

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
    [Serializable]
    public class EAN_Keyframe : ISortable
    {
        #region Sortable
        [YAXDontSerialize]
        public int SortID { get { return FrameIndex; } }
        #endregion

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

        public EAN_Keyframe() { }

        public EAN_Keyframe(ushort frame, float x, float y, float z, float w)
        {
            FrameIndex = frame;
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public void ScaleByWorld(List<IUndoRedo> undos = null)
        {
            if(W != 1f)
            {
                if(undos != null)
                {
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(X), this, X, (X * W)));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(Y), this, Y, (Y * W)));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(Z), this, Z, (Z * W)));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(W), this, W, 1f));
                }

                X *= W;
                Y *= W;
                Z *= W;
                W = 1f;
            }
        }

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

        #region Convert
        internal Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z) * W;
        }

        internal Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }

        internal static Matrix4x4 ToMatrix(Vector3 pos, Vector3 scale, Quaternion rotation)
        {
            Matrix4x4 matrix = Matrix4x4.Identity;

            matrix *= Matrix4x4.CreateScale(scale);
            matrix *= Matrix4x4.CreateFromQuaternion(rotation);
            matrix *= Matrix4x4.CreateTranslation(pos);

            return matrix;
        }
        #endregion
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




}
