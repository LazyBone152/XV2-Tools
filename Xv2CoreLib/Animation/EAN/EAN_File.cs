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
using Xv2CoreLib.EMA;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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
    public class EAN_File : ISorting, IIsNull
    {
        public const float DefaultFoV = 39.97835f;

        [YAXAttributeForClass]
        public bool IsCamera { get; set; }
        [YAXAttributeForClass]
        public int I_08 { get; set; }
        [YAXAttributeForClass]
        public byte I_17 { get; set; } = 4;

        /// <summary>
        /// If the EAN is unique to a specific character (either a character EAN file, or a skill EAN with a chara suffix).
        /// Used only by XenoKit to help with correctly rescaling common animations. This value is only set and read from there.
        /// </summary>
        [YAXDontSerialize]
        public bool IsCharaUnique { get; set; }

        public ESK_Skeleton Skeleton { get; set; }
        public AsyncObservableCollection<EAN_Animation> Animations { get; set; } = new AsyncObservableCollection<EAN_Animation>();

        #region Load/Save
        public static EAN_File Load(byte[] rawBytes, bool linkEskToAnims = false)
        {
            return new Parser(rawBytes, linkEskToAnims).eanFile;
        }

        public static EAN_File Load(string path, bool linkEskToAnims = false)
        {
            return new Parser(path, false, linkEskToAnims).eanFile;
        }

        public void Save(string path)
        {
            string dirname = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirname) && !string.IsNullOrWhiteSpace(dirname))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            new Deserializer(Clone(), path);
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

        public EMA_File ConvertToEma()
        {
            EMA_File ema = new EMA_File();
            ema.EmaType = EmaType.obj;
            ema.Skeleton = Skeleton.ConvertToEmaSkeleton();
            ema.Animations.AddRange(SerializedAnimation.DeserializeToEma(SerializedAnimation.Serialize(Animations, Skeleton)));

            return ema;
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
            Animations.Sort((x, y) => x.IndexNumeric - y.IndexNumeric);
        }

        /// <summary>
        /// Order animation nodes by bone index.
        /// </summary>
        public void SortAnimationNodes()
        {
            if (Skeleton == null) return;

            foreach(var animation in Animations)
            {
                animation.Nodes.Sort((x, y) => Skeleton.GetBoneIndex(x.BoneName) - Skeleton.GetBoneIndex(y.BoneName));
            }
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

        public bool AnimationExists(int id)
        {
            return Animations.Any(x => x.SortID == id);
        }

        public string NameOf(int id)
        {
            if (IndexOf(id) == -1) throw new Exception("Could not find an animation with an ID matching " + id);
            return Animations[IndexOf(id)].Name;
        }

        #endregion

        #region AddAnimation
        public void AddEntry(int id, EAN_Animation entry, List<IUndoRedo> undos = null)
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

            if (undos != null) undos.Add(new UndoableListAdd<EAN_Animation>(Animations, entry));
        }

        /// <summary>
        /// Adds the animation and returns the auto-assigned id.
        /// </summary>
        /// <param name="anim"></param>
        /// <returns></returns>
        public int AddEntry(EAN_Animation anim, List<IUndoRedo> undos = null)
        {
            anim.LinkEskData(Skeleton);

            int newId = NextID();
            anim.IndexNumeric = newId;
            Animations.Add(anim);
            anim.LinkEskData(Skeleton);

            //Sort animation list by ID.
            Animations.Sort((x, y) => x.IndexNumeric - y.IndexNumeric);

            if (undos != null) undos.Add(new UndoableListAdd<EAN_Animation>(Animations, anim));

            return newId;
        }

        public EAN_Animation AddNewAnimation(List<IUndoRedo> undos = null)
        {
            EAN_Animation anim = new EAN_Animation();

            anim.ID_UShort = (ushort)NextID();
            anim.Name = $"NewAnimation_{anim.ID_UShort}";
            anim.LinkEskData(Skeleton);

            Animations.Add(anim);

            //Sort animation list by ID.
            Animations.Sort((x, y) => x.IndexNumeric - y.IndexNumeric);

            if (undos != null)
                undos.Add(new UndoableListAdd<EAN_Animation>(Animations, anim));

            return anim;
        }

        public EAN_Animation AddNewCamera(List<IUndoRedo> undos = null)
        {
            EAN_Animation anim = new EAN_Animation();

            anim.ID_UShort = (ushort)NextID();
            anim.Name = $"NewCamera_{anim.ID_UShort}";

            //Node
            EAN_Node node = new EAN_Node();
            node.BoneName = EAN_Node.CAM_NODE;
            anim.Nodes.Add(node);

            //Components
            EAN_AnimationComponent pos = new EAN_AnimationComponent();
            EAN_AnimationComponent targetPos = new EAN_AnimationComponent();
            EAN_AnimationComponent camera = new EAN_AnimationComponent();
            pos.Type = EAN_AnimationComponent.ComponentType.Position;
            targetPos.Type = EAN_AnimationComponent.ComponentType.Rotation;
            camera.Type = EAN_AnimationComponent.ComponentType.Scale;
            pos.I_01 = 3;
            targetPos.I_01 = 3;
            camera.I_01 = 3;

            node.AnimationComponents.Add(pos);
            node.AnimationComponents.Add(targetPos);
            node.AnimationComponents.Add(camera);

            Animations.Add(anim);

            //Sort animation list by ID.
            Animations.Sort((x, y) => x.IndexNumeric - y.IndexNumeric);

            if (undos != null)
                undos.Add(new UndoableListAdd<EAN_Animation>(Animations, anim));

            return anim;
        }
        #endregion

        #region Defaults
        public static EAN_File DefaultCamFile()
        {
            AsyncObservableCollection<ESK_Bone> eskBones = new AsyncObservableCollection<ESK_Bone>();
            eskBones.Add(new ESK_Bone()
            {
                Name = EAN_Node.CAM_NODE,
                RelativeTransform = new ESK_RelativeTransform()
                {
                    RotationW = 1f,
                    ScaleX = 1f,
                    ScaleY = 1f,
                    ScaleZ = 1f
                }
            });

            return new EAN_File()
            {
                IsCamera = true,
                I_08 = 37508,
                Skeleton = new ESK_Skeleton()
                {
                    SkeletonID = 752102814708815335,
                    UseExtraValues = true,
                    ESKBones = eskBones
                }
            };
        }

        public static EAN_File DefaultFile(ESK_Skeleton skeleton)
        {
            return new EAN_File()
            {
                IsCamera = true,
                I_08 = 37508,
                Skeleton = skeleton.Copy()
            };
        }

        #endregion

        #region Helper
        public EAN_File Clone()
        {
            AsyncObservableCollection<EAN_Animation> anims = new AsyncObservableCollection<EAN_Animation>();
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

            return Animations.Max(x => x.IndexNumeric) + 1;
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
            _16Bit = 1,
            _32Bit = 2
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
            get { return ushort.Parse(Index); }
            set { Index = value.ToString(); }
        }

        [YAXDontSerialize]
        public byte FloatType
        {
            get
            {
                return (byte)FloatSize;
            }
            set
            {
                FloatSize = (FloatPrecision)value;
            }
        }
        #endregion

        [YAXDontSerialize]
        internal ESK_Skeleton eskSkeleton { get; set; }

        //Values
        private string _index = "0";
        private string _NameValue = String.Empty;

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

        private int _frameCount = -1;
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public int FrameCount
        {
            get
            {
                _frameCount = CalculateFrameCount();
                return _frameCount;
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("IndexSize")]
        public IntPrecision IndexSize
        {
            get => GetFrameCount() <= byte.MaxValue ? IntPrecision._8Bit : IntPrecision._16Bit;
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("FloatSize")]
        public FloatPrecision FloatSize { get; set; } = FloatPrecision._32Bit;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AnimationNode")]
        public AsyncObservableCollection<EAN_Node> Nodes { get; set; } = new AsyncObservableCollection<EAN_Node>();

        #region GetAndAdd
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

            for (int i = 0; i < Nodes.Count; i++)
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
        
        public EAN_Node CreateNode(string bone, List<IUndoRedo> undos = null)
        {
            if (undos == null) undos = new List<IUndoRedo>();

            EAN_Node existing = Nodes.FirstOrDefault(x => x.BoneName == bone);
            if (existing != null) return existing;

            EAN_Node node = new EAN_Node();
            node.AnimationComponents = new AsyncObservableCollection<EAN_AnimationComponent>();
            node.BoneName = bone;
            node.EskRelativeTransform = eskSkeleton?.GetBone(bone)?.RelativeTransform;

            Nodes.Add(node);
            undos.Add(new UndoableListAdd<EAN_Node>(Nodes, node));

            return node;
        }

        public IUndoRedo AddBone(string boneName)
        {
            if (Nodes.Any(x => x.BoneName == boneName)) return null;

            EAN_Node node = new EAN_Node();
            node.BoneName = boneName;
            node.LinkEskData(eskSkeleton);
            Nodes.Add(node);

            return new UndoableListAdd<EAN_Node>(Nodes, node, "Add Bone");
        }
        #endregion

        #region Helpers
        public int CalculateFrameCount()
        {
            int frameCount = 0;

            foreach(var bone in Nodes)
            {
                if(bone.AnimationComponents != null)
                {
                    foreach (var comp in bone.AnimationComponents)
                    {
                        int max = (comp.Keyframes.Count > 0) ? comp.Keyframes.Max(x => x.FrameIndex) + 1 : 0;

                        if (max > frameCount)
                            frameCount = max;
                    }
                }
            }

            return frameCount;
        }

        public int GetFrameCount(bool recalculate = false)
        {
            if(_frameCount == -1 || recalculate)
            {
                return FrameCount;
            }

            return _frameCount;
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
                FloatSize = FloatSize,
                Name = (string)Name.Clone(),
                Nodes = _Nodes
            };
        }
        
        public bool Compare(EAN_Animation anim)
        {
            if (anim.Name != Name) return false;
            if (anim.FloatType != FloatType) return false;
            if (anim.IndexSize != IndexSize) return false;
            if (anim.FloatSize != FloatSize) return false;
            if (anim.GetFrameCount() != GetFrameCount()) return false;
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

        #endregion

        #region Init
        internal void LinkEskData(ESK_Skeleton skeleton)
        {
            if (skeleton == null) new ArgumentNullException("EAN_Animation.LinkEskData: skeleton was null.");
            if (Nodes == null) Nodes = new AsyncObservableCollection<EAN_Node>();

            eskSkeleton = skeleton;
            foreach (var node in Nodes)
            {
                node.LinkEskData(skeleton);
            }
        }

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

        #region KeyframeManipulation
        public List<IUndoRedo> AddKeyframe(string nodeName, int frame, float posX, float posY, float posZ, float posW, float rotX, float rotY, float rotZ, float rotW,
            float scaleX, float scaleY, float scaleZ, float scaleW, bool hasPos = true, bool hasRot = true, bool hasScale = true)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            EAN_Node node = GetNode(nodeName, true, undos);

            if (node == null) throw new InvalidDataException($"EAN_Animation.AddKeyframe: \"{nodeName}\" not found.");
            node.AddKeyframe(frame, posX, posY, posZ, posW, rotX, rotY, rotZ, rotW, scaleX, scaleY, scaleZ, scaleW, undos, hasPos, hasRot, hasScale);

            return undos;
        }

        public List<IUndoRedo> AddKeyframes(List<SerializedBone> bones, bool removeCollisions, bool rebase, bool addPos, bool addRot, bool addScale)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //Remove collisions 
            if (removeCollisions && !rebase)
            {
                int min = SerializedBone.GetMinKeyframe(bones);
                int max = SerializedBone.GetMaxKeyframe(bones);

                if(min != max)
                {
                    foreach (var bone in bones)
                    {
                        EAN_Node node = GetNode(bone.Name);

                        if (node != null)
                            node.RemoveCollisions(min, max, undos);
                    }
                }
            }

            if (rebase)
            {
                int min = SerializedBone.GetMinKeyframe(bones);
                int max = SerializedBone.GetMaxKeyframe(bones);
                int rebaseAmount = max - min + 1;

                foreach (var bone in bones)
                {
                    EAN_Node node = GetNode(bone.Name);

                    if (node != null)
                        node.RebaseKeyframes(min, rebaseAmount, removeCollisions, addPos, addRot, addScale, undos);
                }
            }

            //Paste keyframes
            foreach (var bone in bones)
            {
                EAN_Node node = GetNode(bone.Name, true, undos);

                if (node == null) throw new InvalidDataException($"EAN_Animation.AddKeyframes: \"{bone.Name}\" not found.");

                bool hasPos = addPos ? bone.HasPos : false;
                bool hasRot = addRot ? bone.HasRot : false;
                bool hasScale = addScale ? bone.HasScale : false;

                foreach (var keyframe in bone.Keyframes)
                {
                    node.AddKeyframe(keyframe.Frame, keyframe.PosX, keyframe.PosY, keyframe.PosZ, keyframe.PosW,
                                     keyframe.RotX, keyframe.RotY, keyframe.RotZ, keyframe.RotW,
                                     keyframe.ScaleX, keyframe.ScaleY, keyframe.ScaleZ, keyframe.ScaleW, undos, hasPos, hasRot, hasScale);
                }
            }

            return undos;
        }

        public List<IUndoRedo> RemoveKeyframe(int frame, bool removePos = true, bool removeRot = true, bool removeScale = true)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var bone in Nodes)
            {
                bone.RemoveKeyframe(frame, undos, removePos, removeRot, removeScale);
            }

            return undos;
        }

        public List<IUndoRedo> RemoveDuplicateKeyframes()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var node in Nodes)
            {
                undos.AddRange(node.RemoveDuplicateKeyframes());
            }

            return undos;
        }

        public List<IUndoRedo> RemoveKeyframeRange(int startFrame, int endFrame, bool rebase, bool pos = true, bool rot = true, bool scale = true)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            int rebaseAmount = endFrame - startFrame;

            foreach (var node in Nodes)
            {
                for(int i = startFrame; i <= endFrame; i++)
                {
                    node.RemoveKeyframe(i, undos);
                }

                if(rebase)
                    node.RebaseKeyframes(endFrame + 1, -rebaseAmount, false, pos, rot, scale, undos);
            }

            return undos;
        }

        public void RebaseKeyframes(int startFrame, int rebaseAmount, List<string> boneNames, bool pos = true, bool rot = true, bool scale = true, List<IUndoRedo> undos = null)
        {
            foreach(var node in Nodes.Where(x => boneNames.Contains(x.BoneName)))
            {
                node.RebaseKeyframes(startFrame, rebaseAmount, false, pos, rot, scale, undos);
            }
        }

        /// <summary>
        /// Extend the animation by duplicating the last frame. If no keyframes exist, then a default one will be added.
        /// </summary>
        /// <param name="newDuration">The new duration of the animation. Must be greater than current frame count.</param>
        public List<IUndoRedo> ExtendAnimation(int newDuration)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (newDuration <= FrameCount) return undos;

            foreach (var node in Nodes)
                node.ExtendAnimation(newDuration, undos);

            return undos;
        }

        public List<IUndoRedo> ReverseAnimation(int startFrame, int endFrame)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach(var bone in Nodes)
            {
                bone.ReverseAnimation(startFrame, endFrame, undos);
            }

            return undos;
        }

        public List<IUndoRedo> ApplyCameraShake(int startFrame, int endFrame, float factor)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var bone in Nodes)
            {
                bone.ApplyBoneShake(startFrame, endFrame, factor, true, true, false, undos);
            }

            return undos;
        }

        public List<SerializedBone> SerializeKeyframeRange(int startFrame, int endFrame, List<string> boneNames, bool pos = true, bool rot = true, bool scale = true)
        {
            List<SerializedBone> bones = new List<SerializedBone>();

            foreach (var boneName in boneNames)
            {
                EAN_Node node = GetNode(boneName);

                if (node != null)
                {
                    var serBone = new SerializedBone(node, startFrame, endFrame, pos, rot, scale);

                    if(serBone.Keyframes.Length > 0)
                       bones.Add(serBone);
                }
            }

            SerializedBone.StartFrameRebaseToZero(bones);

            return bones;
        }

        public List<SerializedBone> SerializeKeyframes(List<int> frames, List<string> boneNames, bool usePos = true, bool useRot = true, bool useScale = true, bool isCamera = false)
        {
            List<SerializedBone> bones = new List<SerializedBone>();

            foreach (var boneName in boneNames)
            {
                EAN_Node node = GetNode(boneName);

                if (node != null)
                {
                    var serBone = new SerializedBone(node, frames, usePos, useRot, useScale, isCamera);

                    if (serBone.Keyframes.Length > 0)
                        bones.Add(serBone);
                }
            }

            SerializedBone.StartFrameRebaseToZero(bones);

            return bones;
        }
        
        //These are the old EanManager functions.
        public List<IUndoRedo> ApplyNodeOffset(int startFrame, int endFrame, List<string> boneFilter, EAN_AnimationComponent.ComponentType componentType, float x, float y, float z, float w)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var node in Nodes.Where(n => boneFilter.Contains(n.BoneName)))
            {
                node.ApplyNodeOffset(startFrame, endFrame, componentType, x, y, z, w, undos);
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
            if (nodes == null) return undos;

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

        public List<IUndoRedo> RescaleAnimation(int newDuration, int startFrame = 0, int endFrame = -1)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (endFrame < 0)
                endFrame = FrameCount - 1;

            foreach(var node in Nodes)
            {
                undos.AddRange(node.RescaleNode(newDuration, startFrame, endFrame));
            }

            return undos;
        }

        #endregion
    }

    [YAXSerializeAs("AnimationNode")]
    [Serializable]
    public class EAN_Node
    {
        public const string CAM_NODE = "Node";

        [YAXDontSerialize]
        public ESK_RelativeTransform EskRelativeTransform { get; internal set; }

        [YAXAttributeForClass]
        public string BoneName { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Keyframes")]
        public AsyncObservableCollection<EAN_AnimationComponent> AnimationComponents { get; set; } = new AsyncObservableCollection<EAN_AnimationComponent>();

        #region KeyframeManipulation
        /// <summary>
        /// Add a keyframe at the specified frame (will overwrite any existing keyframe).
        /// </summary>
        public void AddKeyframe(int frame, float posX, float posY, float posZ, float posW, float rotX, float rotY, float rotZ, float rotW,
            float scaleX, float scaleY, float scaleZ, float scaleW, List<IUndoRedo> undos, bool hasPosition = true, bool hasRotation = true, bool hasScale = true)
        {
            //Only add the keyframe if the values aren't default.
            if(hasPosition)
            {
                var pos = GetComponent(EAN_AnimationComponent.ComponentType.Position, true, undos, BoneName == CAM_NODE);
                pos.AddKeyframe(frame, posX, posY, posZ, posW, undos);
            }

            if(hasRotation)
            {
                var rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation, true, undos, BoneName == CAM_NODE);
                rot.AddKeyframe(frame, rotX, rotY, rotZ, rotW, undos);
            }

            if(hasScale)
            {
                var scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale, true, undos, BoneName == CAM_NODE);
                scale.AddKeyframe(frame, scaleX, scaleY, scaleZ, scaleW, undos);
            }
        }

        public void AddKeyframe(int frame, EAN_AnimationComponent.ComponentType type, float x, float y, float z, float w, List<IUndoRedo> undos = null)
        {
            var pos = GetComponent(type, true, undos, BoneName == CAM_NODE);
            pos.AddKeyframe(frame, x, y, z, w, undos);
        }

        public void RemoveKeyframe(int keyframe, List<IUndoRedo> undos = null, bool removePos = true, bool removeRot = true, bool removeScale = true)
        {
            foreach (var component in AnimationComponents)
            {
                if (!IsValidComponent(component, removePos, removeRot, removeScale)) continue;

                component.RemoveKeyframe(keyframe, undos);
            }
        }

        public List<IUndoRedo> SetKeyframe(int frame, EAN_AnimationComponent.ComponentType componentType, Axis axis, bool isCamera, float value)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            var component = GetComponent(componentType, true, undos, isCamera);

            var keyframe = component.GetKeyframe(frame, true, undos);

            switch (axis)
            {
                case Axis.X:
                    if (undos != null) undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), keyframe, keyframe.X, value));
                    keyframe.X = value;
                    break;
                case Axis.Y:
                    if (undos != null) undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), keyframe, keyframe.Y, value));
                    keyframe.Y = value;
                    break;
                case Axis.Z:
                    if (undos != null) undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), keyframe, keyframe.Z, value));
                    keyframe.Z = value;
                    break;
                case Axis.W:
                    if (undos != null) undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.W), keyframe, keyframe.W, value));
                    keyframe.W = value;
                    break;
            }

            return undos;
        }

        public void RebaseKeyframes(int startFrame, int rebaseAmount, bool removeCollisions, bool pos = true, bool rot = true, bool scale = true, List<IUndoRedo> undos = null)
        {
            if (rebaseAmount == 0) return;

            var frames = GetAllKeyframesInt();

            int endFrame = frames.Max();
            int min = startFrame + rebaseAmount;

            //The calculation differs if rebase is negative or positive
            int max = (rebaseAmount > 0) ? min + (endFrame - startFrame) : min + Math.Abs(rebaseAmount);

            foreach (var component in AnimationComponents)
            {
                if (!IsValidComponent(component, pos, rot, scale)) continue;

                if (removeCollisions)
                    component.RemoveCollisions(min, max, undos);

                foreach (var keyframe in component.Keyframes.Where(x => x.FrameIndex >= startFrame))
                {
                    ushort frameIndex = (ushort)(keyframe.FrameIndex + rebaseAmount);

                    if (undos != null)
                        undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(keyframe.FrameIndex), keyframe, keyframe.FrameIndex, frameIndex));

                    keyframe.FrameIndex = frameIndex;
                }
            }
        }

        public void RebaseKeyframes(List<int> frames, int rebaseAmount, bool removeCollisions, bool pos = true, bool rot = true, bool scale = true, List<IUndoRedo> undos = null)
        {
            if (frames.Count == 0 || rebaseAmount == 0) return;

            int startFrame = frames.Min();
            int endFrame = frames.Max();
            int min = startFrame + rebaseAmount;

            //The calculation differs if rebase is negative or positive
            int max = (rebaseAmount > 0) ? min + (endFrame - startFrame) : min + Math.Abs(rebaseAmount);

            foreach (var component in AnimationComponents)
            {
                if (!IsValidComponent(component, pos, rot, scale)) continue;

                if (removeCollisions)
                    component.RemoveCollisions(min, max, undos);

                foreach (var keyframe in component.Keyframes.Where(x => frames.Contains(x.FrameIndex)))
                {
                    ushort frameIndex = (ushort)(keyframe.FrameIndex + rebaseAmount);

                    if (undos != null)
                        undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(keyframe.FrameIndex), keyframe, keyframe.FrameIndex, frameIndex));

                    keyframe.FrameIndex = frameIndex;
                }

                component.SortKeyframes(undos);
            }
        }

        public void ExtendAnimation(int newDuration, List<IUndoRedo> undos = null)
        {
            foreach (var comp in AnimationComponents)
                comp.ExtendKeyframes(newDuration, undos);
        }
        
        public void ReverseAnimation(int startFrame, int endFrame, List<IUndoRedo> undos = null)
        {
            foreach (var component in AnimationComponents)
                component.ReverseKeyframes(startFrame, endFrame, undos);
        }

        public void RemoveCollisions(int startFrame, int endFrame, List<IUndoRedo> undos = null)
        {
            foreach (var component in AnimationComponents)
                component.RemoveCollisions(startFrame, endFrame, undos);
        }

        public void RemoveComponent(EAN_AnimationComponent.ComponentType type, List<IUndoRedo> undos = null)
        {
            var component = AnimationComponents.FirstOrDefault(x => x.Type == type);

            if(component != null)
            {
                AnimationComponents.Remove(component);

                if (undos != null)
                    undos.Add(new UndoableListRemove<EAN_AnimationComponent>(AnimationComponents, component));
            }
        }
        
        public void ApplyBoneShake(int startFrame, int endFrame, float factor, bool pos, bool rot, bool includeZ, List<IUndoRedo> undos = null)
        {
            foreach(var component in AnimationComponents)
            {
                if (!IsValidComponent(component, pos, rot, false)) continue;

                component.ApplyBoneShake(startFrame, endFrame, factor, includeZ, undos);
            }
        }

        public List<IUndoRedo> RescaleNode(int newDuration, int startFrame = 0, int endFrame = -1)
        {
            if (newDuration == 0) newDuration = 1;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var component in AnimationComponents)
            {
                float oldEndFrame = endFrame == -1 ? component.GetLastKeyframe() : endFrame;
                float newEndFrame = startFrame + newDuration;
                if (oldEndFrame == (newDuration - 1)) continue;
                int rebaseAmount = -(int)(oldEndFrame - newEndFrame);

                //Insert keyframes
                component.GetKeyframe((int)oldEndFrame, true, undos);
                component.GetKeyframe(startFrame, true, undos);

                if (rebaseAmount > 0)
                    component.Rebase(endFrame + 1, rebaseAmount, undos);

                float newDurationFloat = newDuration;
                float oldDuration = endFrame - startFrame;

                foreach (var keyframe in component.Keyframes)
                {
                    if (keyframe.FrameIndex >= startFrame && keyframe.FrameIndex <= endFrame)
                    {
                        ushort newFrameIndex = (ushort)(((keyframe.FrameIndex - startFrame) * (newDurationFloat / oldDuration)) + startFrame);

                        undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.FrameIndex), keyframe, keyframe.FrameIndex, newFrameIndex));
                        keyframe.FrameIndex = newFrameIndex;
                    }

                    
                }

                if (rebaseAmount < 0)
                    component.Rebase(endFrame, rebaseAmount, undos);

                component.SortKeyframes(undos);
            }


            //Remove dupliate keyframes
            undos.AddRange(RemoveDuplicateKeyframes());

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

        public List<IUndoRedo> DeleteKeyframes(List<int> keyframes, bool deletePos, bool deleteRot, bool deleteScale)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            EAN_AnimationComponent pos = GetComponent(EAN_AnimationComponent.ComponentType.Position);
            EAN_AnimationComponent rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            EAN_AnimationComponent scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            foreach (var keyframe in keyframes)
            {
                if (pos != null && deletePos) pos.RemoveKeyframe(keyframe, undos);
                if (rot != null && deleteRot) rot.RemoveKeyframe(keyframe, undos);
                if (scale != null && deleteScale) scale.RemoveKeyframe(keyframe, undos);
            }

            return undos;
        }

        public void ApplyNodeOffset(int startFrame, int endFrame, EAN_AnimationComponent.ComponentType componentType, float x, float y, float z, float w, List<IUndoRedo> undos)
        {
            var _component = GetComponent(componentType);
            if (_component != null)
            {
                _component.ApplyNodeOffset(startFrame, endFrame, x, y, z, w, undos);
            }
        }

        /// <summary>
        /// Insert a keyframe at the specified frame with interpolated values.
        /// </summary>
        public void InsertKeyframe(int frame, List<IUndoRedo> undos = null)
        {
            foreach(var component in AnimationComponents)
            {
                component.GetKeyframe(frame, true, undos);
            }
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
                component.EskRelativeTransform = EskRelativeTransform;
                undos?.Add(new UndoableListAdd<EAN_AnimationComponent>(AnimationComponents, component));
                AnimationComponents.Add(component);
            }

            return component;
        }

        public EAN_AnimationComponent GetComponent(EAN_AnimationComponent.ComponentType _type)
        {
            for (int i = 0; i < AnimationComponents.Count(); i++)
            {
                if (_type == AnimationComponents[i].Type)
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
        public List<ushort> GetAllKeyframes(bool usePos = true, bool useRot = true, bool useScale = true)
        {
            List<ushort> keyframes = new List<ushort>();

            foreach (var component in AnimationComponents)
            {
                if (component.Type == EAN_AnimationComponent.ComponentType.Position && !usePos) continue;
                if (component.Type == EAN_AnimationComponent.ComponentType.Rotation && !useRot) continue;
                if (component.Type == EAN_AnimationComponent.ComponentType.Scale && !useScale) continue;

                foreach (var keyframe in component.Keyframes)
                {
                    if (!keyframes.Contains(keyframe.FrameIndex))
                        keyframes.Add(keyframe.FrameIndex);
                }
            }

            keyframes.Sort();

            return keyframes;
        }

        public List<int> GetAllKeyframesInt(bool usePos = true, bool useRot = true, bool useScale = true)
        {
            return ArrayConvert.ConvertToIntList(GetAllKeyframes(usePos, useRot, useScale));
        }

        public float[] GetKeyframeValues(int frame, bool usePos = true, bool useRot = true, bool useScale = true)
        {
            float[] values = new float[12];
            var pos = GetComponent(EAN_AnimationComponent.ComponentType.Position);
            var rot = GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
            var scale = GetComponent(EAN_AnimationComponent.ComponentType.Scale);

            //Set default values
            values[3] = 1f; // Pos W
            values[7] = 1f; //Rot W

            if (BoneName != CAM_NODE)
            {
                values[8] = 1f; //Scale X
                values[9] = 1f; //Scale Y
                values[10] = 1f; //Scale Z
                values[11] = 1f; //Scale W
            }
            else
            {
                values[9] = (float)MathHelpers.ConvertDegreesToRadians(EAN_File.DefaultFoV); //FoV
            }

            if (pos != null && usePos)
            {
                EAN_Keyframe posKeyframe = pos.GetKeyframe(frame);

                if(posKeyframe != null)
                {
                    values[0] = posKeyframe.X;
                    values[1] = posKeyframe.Y;
                    values[2] = posKeyframe.Z;
                    values[3] = posKeyframe.W;
                }
            }
            if (rot != null && useRot)
            {
                EAN_Keyframe rotKeyframe = rot.GetKeyframe(frame);

                if(rotKeyframe != null)
                {
                    values[4] = rotKeyframe.X;
                    values[5] = rotKeyframe.Y;
                    values[6] = rotKeyframe.Z;
                    values[7] = rotKeyframe.W;
                }
            }
            if (scale != null && useScale)
            {
                EAN_Keyframe scaleKeyframe = scale.GetKeyframe(frame);

                if(scaleKeyframe != null)
                {
                    values[8] = scaleKeyframe.X;
                    values[9] = scaleKeyframe.Y;
                    values[10] = scaleKeyframe.Z;
                    values[11] = scaleKeyframe.W;
                }
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
            if (AnimationComponents == null) AnimationComponents = new AsyncObservableCollection<EAN_AnimationComponent>();

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
        
        public bool HasComponent(EAN_AnimationComponent.ComponentType componentType)
        {
            foreach (var component in AnimationComponents)
            {
                if (component.Type == componentType) return true;
            }

            return false;
        }

        public EAN_Node Clone()
        {
            AsyncObservableCollection<EAN_AnimationComponent> _AnimationComponent = new AsyncObservableCollection<EAN_AnimationComponent>();
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

        private bool IsValidComponent(EAN_AnimationComponent component, bool pos, bool rot, bool scale)
        {
            if (component.Type == EAN_AnimationComponent.ComponentType.Position && !pos) return false;
            if (component.Type == EAN_AnimationComponent.ComponentType.Rotation && !rot) return false;
            if (component.Type == EAN_AnimationComponent.ComponentType.Scale && !scale) return false;

            return true;
        }
        #endregion

        public static List<int> GetAllKeyframes(IList<EAN_Node> nodes, bool pos = true, bool rot = true, bool scale = true)
        {
            List<int> keyframes = new List<int>();

            foreach(var node in nodes)
            {
                List<int> nodeKeyframes = node.GetAllKeyframesInt(pos, rot, scale);

                foreach(var val in nodeKeyframes)
                {
                    if (!keyframes.Contains(val))
                        keyframes.Add(val);
                }
            }

            keyframes.Sort();
            return keyframes;
        }
    }

    [YAXSerializeAs("Keyframes")]
    [Serializable]
    public class EAN_AnimationComponent
    {
        public enum ComponentType : byte
        {
            Position = 0,
            Rotation = 1, //TargetPosition for camera
            Scale = 2, //Or "Camera"
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public ComponentType Type { get; set; } //int8
        [YAXAttributeForClass]
        public byte I_01 { get; set; }
        [YAXAttributeForClass]
        public short I_02 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Keyframe")]
        public AsyncObservableCollection<EAN_Keyframe> Keyframes { get; set; } = new AsyncObservableCollection<EAN_Keyframe>();

        public EAN_AnimationComponent() { }

        public EAN_AnimationComponent(ComponentType type, bool isCamera, ESK_RelativeTransform relativeTransform)
        {
            Type = type;
            I_01 = (byte)((isCamera) ? 3 : 7);
            EskRelativeTransform = relativeTransform;
        }

        #region BoneLinking
        //Bone Linking for animation playback:
        private ESK_RelativeTransform _eskRelativeTransform = null;
        [YAXDontSerialize]
        public ESK_RelativeTransform EskRelativeTransform
        {
            get { return _eskRelativeTransform; }
            internal set
            {
                _eskRelativeTransform = value;
                _defaultKeyframe = null;
            }
        }

        private EAN_Keyframe _defaultKeyframe = null;
        [YAXDontSerialize]
        internal EAN_Keyframe DefaultKeyframe
        {
            get
            {
                if (_defaultKeyframe == null)
                    _defaultKeyframe = GetDefaultKeyframe();
                return _defaultKeyframe;
            }
        }

        #endregion

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
                SortKeyframes(undos);
            }
        }

        public bool ChangeKeyframe(int oldKeyframe, int newKeyframe, List<IUndoRedo> undos = null)
        {
            EAN_Keyframe keyframe = Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)oldKeyframe);
            EAN_Keyframe _newKeyframe = Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)newKeyframe);

            if (keyframe == null || _newKeyframe != null) return false;

            if (undos != null)
            {
                undos.Add(new UndoablePropertyGeneric(nameof(EAN_Keyframe.FrameIndex), keyframe, keyframe.FrameIndex, newKeyframe));
            }

            keyframe.FrameIndex = (ushort)newKeyframe;

            SortKeyframes(undos);

            return true;
        }

        public void RemoveKeyframe(int frame, List<IUndoRedo> undos = null)
        {
            EAN_Keyframe keyframe = GetKeyframe(frame);

            if (keyframe != null)
            {
                if (undos != null)
                {
                    undos.Add(new UndoableListRemove<EAN_Keyframe>(Keyframes, keyframe));
                }

                Keyframes.Remove(keyframe);
                SortKeyframes(undos);
            }
        }

        public EAN_Keyframe AddKeyframe(int frame, List<IUndoRedo> undos = null)
        {
            AddKeyframe(frame, GetKeyframeValue(frame, Axis.X), GetKeyframeValue(frame, Axis.Y), GetKeyframeValue(frame, Axis.Z), GetKeyframeValue(frame, Axis.W), undos);

            return Keyframes.FirstOrDefault(k => k.FrameIndex == (ushort)frame);
        }

        public void ExtendKeyframes(int newDuration, List<IUndoRedo> undos = null)
        {
            EAN_Keyframe keyframe;

            if(Keyframes.Count == 0)
            {
                keyframe = DefaultKeyframe.Copy();
                keyframe.FrameIndex = (ushort)newDuration;
            }
            else
            {
                keyframe = Keyframes[Keyframes.Count - 1].Copy(); //Keyframes must always be in order, so this is valid
                keyframe.FrameIndex = (ushort)newDuration;
            }

            Keyframes.Add(keyframe);

            if (undos != null)
                undos.Add(new UndoableListAdd<EAN_Keyframe>(Keyframes, keyframe));
        }

        public void ReverseKeyframes(int startFrame, int endFrame, List<IUndoRedo> undos = null)
        {
            foreach(var keyframe in Keyframes)
            {
                if(keyframe.FrameIndex >= startFrame && keyframe.FrameIndex <= endFrame)
                {
                    ushort newFrameIndex = (ushort)(endFrame - keyframe.FrameIndex + startFrame);

                    if (undos != null)
                        undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.FrameIndex), keyframe, keyframe.FrameIndex, newFrameIndex));

                    keyframe.FrameIndex = newFrameIndex;
                }
            }

            SortKeyframes(undos);
        }

        public void RemoveCollisions(int startFrame, int endFrame, List<IUndoRedo> undos = null)
        {
            for (int i = Keyframes.Count - 1; i >= 0; i--)
            {
                if(Keyframes[i].FrameIndex >= startFrame && Keyframes[i].FrameIndex <= endFrame)
                {
                    if (undos != null)
                        undos.Add(new UndoableListRemove<EAN_Keyframe>(Keyframes, Keyframes[i]));

                    Keyframes.RemoveAt(i);
                }
            }
        }

        public void Rebase(int startFrame, int rebaseAmount, List<IUndoRedo> undos = null)
        {

            foreach(var keyframe in Keyframes)
            {
                if(keyframe.FrameIndex >= startFrame)
                {
                    ushort newFrame = (ushort)(keyframe.FrameIndex + rebaseAmount);

                    if (undos != null)
                        undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.FrameIndex), keyframe, keyframe.FrameIndex, newFrame));

                    keyframe.FrameIndex = newFrame;
                }
            }
        }

        public void ApplyBoneShake(int startFrame, int endFrame, float factor, bool includeZ, List<IUndoRedo> undos = null)
        {
            SharpNoise.Modules.Perlin perlin = new SharpNoise.Modules.Perlin();
            perlin.Seed = Random.Range(0, int.MaxValue);

            for (int i = startFrame; i <= endFrame; i++)
            {
                int multi = i - startFrame + 1;

                var keyframe = GetKeyframe(i, true, undos);

                float x = ((float)perlin.GetValue(0.1 * multi, 0, 0)) * factor * 0.02f;
                float y = ((float)perlin.GetValue(0.2 * multi, 0, 0)) * factor * 0.02f;
                float z = ((float)perlin.GetValue(0.3 * multi, 0, 0)) * factor * 0.02f;

                float newX = keyframe.X + x;
                float newY = keyframe.Y + y;
                float newZ = keyframe.Z + z;

                if (undos != null)
                {
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.X), keyframe, keyframe.X, newX));
                    undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Y), keyframe, keyframe.Y, newY));

                    if(includeZ)
                        undos.Add(new UndoableProperty<EAN_Keyframe>(nameof(EAN_Keyframe.Z), keyframe, keyframe.Z, newZ));
                }

                keyframe.X = newX;
                keyframe.Y = newY;

                if(includeZ)
                    keyframe.Z = newZ;
            }
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

        public void ApplyNodeOffset(int startFrame, int endFrame, float x, float y, float z, float w, List<IUndoRedo> undos)
        {
            if (Keyframes == null) throw new Exception(String.Format("Could not apply NodeOffset because Keyframes is null."));

            for (int i = 0; i < Keyframes.Count; i++)
            {
                if (Keyframes[i].FrameIndex >= startFrame && (Keyframes[i].FrameIndex <= endFrame || endFrame == -1))
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
        }

        /// <summary>
        /// Sort keyframes to be sequential. Required for keyframe interpolation to function correctly!
        /// </summary>
        public void SortKeyframes(List<IUndoRedo> undos = null)
        {
            Keyframes.Sort((x, y) => x.FrameIndex - y.FrameIndex);

            if(undos != null)
                undos.Add(new UndoActionDelegate(this, nameof(SortKeyframes), true, null, new object[1] { null }));
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
                SortKeyframes(undos);

                if (undos != null)
                {
                    undos.Add(new UndoableListAdd<EAN_Keyframe>(Keyframes, keyframe));
                }
            }

            return keyframe;
        }

        public EAN_Keyframe GetKeyframe(int frame, int tryIndex = -1)
        {
            //First try seeking a keyframe from the provided index, avoiding the for loop lookup all together (optional)
            if (tryIndex != -1)
            {
                EAN_Keyframe keyframeMatch = TrySeekKeyframe(frame, tryIndex);

                if (keyframeMatch != null)
                    return keyframeMatch;
            }

            for(int i = 0; i < Keyframes.Count; i++)
            {
                if (Keyframes[i].FrameIndex == frame)
                    return Keyframes[i];
            }

            return null;
            //return DefaultKeyframe;
            //return Keyframes.FirstOrDefault(x => x.FrameIndex == frame);
        }

        private EAN_Keyframe TrySeekKeyframe(int frame, int startIndex)
        {
            if (startIndex < Keyframes.Count && Keyframes[startIndex].FrameIndex == frame)
                return Keyframes[startIndex];

            int seekIdx = startIndex + 1;
            if (seekIdx < Keyframes.Count && Keyframes[seekIdx].FrameIndex == frame)
                return Keyframes[seekIdx];

            seekIdx = startIndex - 1;
            if (seekIdx >= 0 && seekIdx < Keyframes.Count && Keyframes[seekIdx].FrameIndex == frame)
                return Keyframes[seekIdx];

            return null;
        }

        private EAN_Keyframe GetDefaultKeyframe(int frame = 0)
        {
            return GetDefaultKeyframe(EskRelativeTransform, Type, I_01 == 3, frame);
        }

        public static EAN_Keyframe GetDefaultKeyframe(ESK_RelativeTransform EskRelativeTransform, ComponentType Type, bool isCam, int frame = 0)
        {
            EAN_Keyframe keyframe = new EAN_Keyframe();
            keyframe.FrameIndex = (ushort)frame;

            if (Type == ComponentType.Scale && isCam)
            {
                //Camera
                keyframe.Y = (float)MathHelpers.ConvertDegreesToRadians(EAN_File.DefaultFoV);
            }
            else if (EskRelativeTransform == null)
            {
                //Camera Pos/Rot

                if (Type == ComponentType.Rotation)
                    keyframe.W = 1f;

            }
            else if (Type == ComponentType.Scale)
            {
                keyframe.X = EskRelativeTransform.ScaleX;
                keyframe.Y = EskRelativeTransform.ScaleY;
                keyframe.Z = EskRelativeTransform.ScaleZ;
                keyframe.W = EskRelativeTransform.ScaleW;
            }
            else if (Type == ComponentType.Position)
            {
                keyframe.X = EskRelativeTransform.PositionX;
                keyframe.Y = EskRelativeTransform.PositionY;
                keyframe.Z = EskRelativeTransform.PositionZ;
                keyframe.W = EskRelativeTransform.PositionW;
            }
            else if (Type == ComponentType.Rotation)
            {
                keyframe.X = EskRelativeTransform.RotationX;
                keyframe.Y = EskRelativeTransform.RotationY;
                keyframe.Z = EskRelativeTransform.RotationZ;
                keyframe.W = EskRelativeTransform.RotationW;
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
        public float GetKeyframeValue(float frame, Axis axis)
        {
            int index = 0;
            return GetKeyframeValue(frame, axis, ref index);
        }

        public float GetKeyframeValue(int frame, Axis axis)
        {
            int index = 0;
            return GetKeyframeValue(frame, axis, ref index);
        }

        /// <summary>
        /// Get an interpolated keyframe value, from the specified floating-point frame. Allows time-scaled animations.
        /// </summary>
        public float GetKeyframeValue(float frame, Axis axis, ref int index, int startIdx = 0)
        {
            bool isWhole = Math.Floor(frame) == frame;

            if (isWhole)
            {
                return GetKeyframeValue((int)frame, axis, ref index, startIdx);
            }

            int flooredFrame = (int)Math.Floor(frame);

            float beforeValue = GetKeyframeValue(flooredFrame, axis, ref index, startIdx);
            float afterValue = GetKeyframeValue(flooredFrame + 1, axis, ref index, startIdx);
            float factor = (float)(frame - Math.Floor(frame));

            return MathHelpers.Lerp(beforeValue, afterValue, factor);
        }

        /// <summary>
        /// Get an interpolated keyframe value.
        /// </summary>
        public float GetKeyframeValue(int frame, Axis axis, ref int index, int startIdx = 0)
        {
            EAN_Keyframe existing = GetKeyframe(frame, startIdx);

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
            EAN_Keyframe previousKeyframe = GetNearestKeyframeBefore(frame, startIdx, ref prevFrame, ref index);
            EAN_Keyframe nextKeyframe = GetNearestKeyframeAfter(frame, startIdx, ref nextFrame, ref index);

            if ((previousKeyframe != null && nextKeyframe == null) || (previousKeyframe == nextKeyframe && previousKeyframe != null))
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
        /// Get a non-persistent instance of the nearest keyframe BEFORE the specified frame. This instance MAY belong to another keyframe entirely, or not exist in Keyframes at all (if its a default keyframe generated because no keyframes currently exist) - so this method is ONLY intended for reading purposes! 
        /// </summary>
        /// <param name="frame">The specified frame.</param>
        /// <param name="nearFrame">The frame the returned <see cref="EAN_Keyframe"/> belongs to (ignore FrameIndex on the keyframe) </param>
        private EAN_Keyframe GetNearestKeyframeBefore(int frame, int startIdx, ref int nearFrame, ref int index)
        {
            EAN_Keyframe nearest = null;

            int nearIdx = GetClosestKeyframeIndexBefore(frame, startIdx, ref index);

            if(nearIdx != -1)
            {
                nearest = Keyframes[nearIdx];
                nearFrame = nearest.FrameIndex;
            }

            /*

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

            */

            //None found, so use default
            if (nearest == null)
            {
                nearest = DefaultKeyframe;
                nearFrame = frame - 1;
            }

            return nearest;
        }

        /// <summary>
        /// Get a non-persistent instance of the nearest keyframe AFTER the specified frame. This instant MAY belong to another keyframe entirely, or not exist in Keyframes at all (if its a default keyframe generated because no keyframes currently exist) - so this method is ONLY intended for reading purposes! 
        /// </summary>
        /// <param name="frame">The specified frame.</param>
        /// <param name="nearFrame">The frame the returned <see cref="EAN_Keyframe"/> belongs to (ignore <see cref="EAN_Keyframe.FrameIndex"/> on the keyframe) </param>
        private EAN_Keyframe GetNearestKeyframeAfter(int frame, int startIdx, ref int nearFrame, ref int index)
        {
            EAN_Keyframe nearest = null;

            int nearIdx = GetClosestKeyframeIndexAfter(frame, startIdx, ref index);

            if (nearIdx != -1)
            {
                nearest = Keyframes[nearIdx];
                nearFrame = nearest.FrameIndex;
            }

            /*
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
            */
            //None found, so use default
            if (nearest == null)
            {
                nearest = DefaultKeyframe;
                nearFrame = frame + 1;
            }

            return nearest;
        }
        
        private int GetClosestKeyframeIndexBefore(int frame, int startIdx, ref int index)
        {
            if (startIdx < 0) startIdx = 0;

            if (Keyframes.Count == 1)
            {
                index = 0;
                return index;
            }

            for (int i = startIdx; i < Keyframes.Count; i++)
            {
                if (Keyframes[i].FrameIndex >= frame)
                {
                    index = i - 1;

                    if (index < 0)
                        index = 0;

                    return index;
                }
            }

            index = Keyframes.Count - 1;
            return Keyframes.Count - 1;
        }

        private int GetClosestKeyframeIndexAfter(int frame, int startIdx, ref int index)
        {
            if (startIdx < 0) startIdx = 0;

            if (Keyframes.Count == 1)
            {
                index = 0;
                return index;
            }

            for (int i = startIdx; i < Keyframes.Count; i++)
            {
                if (Keyframes[i].FrameIndex >= frame)
                {
                    index = i;
                    return index;
                }
            }

            index = Keyframes.Count - 1;
            return Keyframes.Count - 1;

            /*
            int before = GetClosestKeyframeIndexBefore(frame, startIdx, ref index);
            if (before == -1) return -1;

            if((Keyframes.Count - 1) >= before + 1)
            {
                if(Keyframes[before + 1].FrameIndex > frame)
                {
                    index = before + 1;
                    return index;
                }
            }

            if ((Keyframes.Count - 1) >= before + 2)
            {
                if (Keyframes[before + 2].FrameIndex > frame)
                {
                    index = before + 2;
                    return index;
                }
            }

            index = before;
            return before;
            */
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
                Type = Type,
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
            if (Type != component.Type) return false;
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

        public bool HasKeyframe(int frame)
        {
            for(int i = 0; i < Keyframes.Count; i++)
            {
                if (Keyframes[i].FrameIndex == frame) return true;
            }

            return false;
        }

        public static string GetCameraTypeString(ComponentType type, Axis axis)
        {
            switch (type)
            {
                case ComponentType.Position:
                    return $"Position {axis}";
                case ComponentType.Rotation:
                    return $"TargetPosition {axis}";
                case ComponentType.Scale:
                    if (axis == Axis.X) return "Roll";
                    if (axis == Axis.Y) return "FoV";
                    return $"Camera {axis}";
            }

            return type.ToString();
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

        public static float GetDefaultValue(EAN_AnimationComponent.ComponentType type, Axis axis, bool isCamera = false)
        {
            switch (type)
            {
                case EAN_AnimationComponent.ComponentType.Rotation:
                case EAN_AnimationComponent.ComponentType.Position:
                    return (axis != Axis.W) ? 0f : 1f;
                case EAN_AnimationComponent.ComponentType.Scale:
                    if (isCamera)
                        return (float)(axis == Axis.Y ? MathHelpers.ConvertDegreesToRadians(EAN_File.DefaultFoV) : 0);
                    return 1f;

            }

            return 0f;
        }

        public override string ToString()
        {
            return $"{FrameIndex}: {X} {Y} {Z} {W}";
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
    public class SerializedAnimation
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public EAN_Animation.FloatPrecision FloatType { get; set; }
        public List<SerializedBone> Bones { get; set; }
        private bool isDeserialized = false;

        public SerializedAnimation(EAN_Animation animation, ESK_Skeleton bindPose)
        {
            ID = animation.ID_UShort;
            Name = animation.Name;
            FloatType = animation.FloatSize;
            Bones = new List<SerializedBone>(animation.Nodes.Count);

            for(int i = 0; i < animation.Nodes.Count; i++)
            {
                Bones.Add(new SerializedBone(animation.Nodes[i]));
            }

            SerializedBone.SetNeutralBindPose(Bones, bindPose);
        }

        public SerializedAnimation(EMA_Animation animation, ESK_Skeleton bindPose)
        {
            ID = animation.Index;
            Name = animation.Name;
            FloatType = (EAN_Animation.FloatPrecision)animation.FloatPrecision;
            Bones = new List<SerializedBone>(animation.Nodes.Count);

            for(int i = 0; i < animation.Nodes.Count; i++)
            {
                Bones.Add(new SerializedBone(animation.Nodes[i]));
            }

            ConvertRotationFormat(KeyframeRotationFormat.Quaternion);
            SerializedBone.SetNeutralBindPose(Bones, bindPose);
        }

        public static List<SerializedAnimation> Serialize(IList<EMA_Animation> animations, ESK_Skeleton bindPose)
        {
            ConcurrentBag<SerializedAnimation> serializedAnims = new ConcurrentBag<SerializedAnimation>();

            Parallel.ForEach(animations, animation =>
            {
                serializedAnims.Add(new SerializedAnimation(animation, bindPose));
            });

            return serializedAnims.ToList();
        }

        public static List<SerializedAnimation> Serialize(IList<EAN_Animation> animations, ESK_Skeleton bindPose)
        {
            ConcurrentBag<SerializedAnimation> serializedAnims = new ConcurrentBag<SerializedAnimation>();

            Parallel.ForEach(animations, animation =>
            {
                serializedAnims.Add(new SerializedAnimation(animation, bindPose));
            });

            return serializedAnims.ToList();
        }

        public static List<EAN_Animation> DeserializeToEan(IList<SerializedAnimation> serialziedAnimations, ESK_Skeleton bindPose)
        {
            ConcurrentBag<EAN_Animation> animations = new ConcurrentBag<EAN_Animation>();

            Parallel.ForEach(serialziedAnimations, animation =>
            {
                animations.Add(animation.DeserializeToEan(bindPose));
            });

            return animations.ToList();
        }

        public static List<EMA_Animation> DeserializeToEma(IList<SerializedAnimation> serialziedAnimations)
        {
            ConcurrentBag<EMA_Animation> animations = new ConcurrentBag<EMA_Animation>();

            Parallel.ForEach(serialziedAnimations, animation =>
            {
                animations.Add(animation.DeserializeToEma());
            });

            return animations.ToList();
        }

        public EAN_Animation DeserializeToEan(ESK_Skeleton bindPose)
        {
            if (isDeserialized)
                throw new InvalidOperationException($"SerializedAnimation.Deserialize: This animation has already been deserialized!");

            ConvertRotationFormat(KeyframeRotationFormat.Quaternion);
            SerializedBone.SetBindPose(Bones, bindPose);


            EAN_Animation animation = new EAN_Animation();
            animation.eskSkeleton = bindPose;
            animation.ID_UShort = (ushort)ID;
            animation.Name = Name;
            animation.FloatSize = FloatType;
            animation.AddKeyframes(Bones, false, false, true, true, true);

            isDeserialized = true;
            return animation;
        }

        public EMA_Animation DeserializeToEma()
        {
            if (isDeserialized)
                throw new InvalidOperationException($"SerializedAnimation.DeserializeToEma: This animation has already been deserialized!");

            ConvertRotationFormat(KeyframeRotationFormat.EulerAngles);

            EMA_Animation animation = new EMA_Animation();
            animation.Index = ID;
            animation.Name = Name;
            animation.FloatPrecision = (EMA.ValueType)FloatType;
            animation.AddKeyframes(Bones, false, false, true, true, true);

            isDeserialized = true;
            return animation;
        }

        internal void ConvertRotationFormat(KeyframeRotationFormat format)
        {
            foreach(var bone in Bones)
            {
                bone.ConvertRotation(format);
            }
        }

    }

    [Serializable]
    public class SerializedBone
    {
        public string Name { get; set; }
        public SerializedKeyframe[] Keyframes { get; set; }
        private bool IsCamera = false;
        public bool HasPos { get; set; }
        public bool HasRot { get; set; }
        public bool HasScale { get; set; }
        private KeyframeRotationFormat RotationFormat { get; set; }

        public SerializedBone(EAN_Node node, int startFrame = -1, int endFrame = -1, bool pos = true, bool rot = true, bool scale = true)
        {
            RotationFormat = KeyframeRotationFormat.Quaternion;
            Name = node.BoneName;
            
            List<ushort> keyframes = node.GetAllKeyframes(pos, rot, scale);

            //if (startFrame != -1 && endFrame != -1 && startFrame == endFrame)
           // {
             //   throw new ArgumentException("SerializedBone: StartFrame cannot be the same as EndFrame.");
            //}

            if(startFrame != -1)
                keyframes.RemoveAll(x => x < startFrame);

            if (endFrame != -1)
                keyframes.RemoveAll(x => x > endFrame);

            Keyframes = new SerializedKeyframe[keyframes.Count];

            HasPos = pos && node.HasComponent(EAN_AnimationComponent.ComponentType.Position);
            HasRot = rot && node.HasComponent(EAN_AnimationComponent.ComponentType.Rotation);
            HasScale = scale && node.HasComponent(EAN_AnimationComponent.ComponentType.Scale);

            for (int i = 0; i < keyframes.Count; i++)
                Keyframes[i] = new SerializedKeyframe(keyframes[i], node.GetKeyframeValues(keyframes[i], HasPos, HasRot, HasScale));
        }

        public SerializedBone(EAN_Node node, List<int> frames, bool usePos, bool useRot, bool useScale, bool isCamera)
        {
            RotationFormat = KeyframeRotationFormat.Quaternion;
            Name = node.BoneName;
            IsCamera = isCamera;

            List<ushort> keyframes = node.GetAllKeyframes(usePos, useRot, useScale);

            keyframes.RemoveAll(x => !frames.Contains(x));

            Keyframes = new SerializedKeyframe[keyframes.Count];

            HasPos = usePos && node.HasComponent(EAN_AnimationComponent.ComponentType.Position);
            HasRot = useRot && node.HasComponent(EAN_AnimationComponent.ComponentType.Rotation);
            HasScale = useScale && node.HasComponent(EAN_AnimationComponent.ComponentType.Scale);

            for (int i = 0; i < keyframes.Count; i++)
                Keyframes[i] = new SerializedKeyframe(keyframes[i], node.GetKeyframeValues(keyframes[i], usePos, useRot, useScale));
        }

        public SerializedBone(EMA_Node node, int startFrame = -1, int endFrame = -1, bool pos = true, bool rot = true, bool scale = true)
        {
            RotationFormat = KeyframeRotationFormat.EulerAngles;
            Name = node.BoneName;

            List<ushort> keyframes = node.GetAllKeyframes(pos, rot, scale);

            if (startFrame != -1)
                keyframes.RemoveAll(x => x < startFrame);

            if (endFrame != -1)
                keyframes.RemoveAll(x => x > endFrame);

            Keyframes = new SerializedKeyframe[keyframes.Count];

            HasPos = pos && node.HasParameter(EMA_Command.PARAMETER_POSITION);
            HasRot = rot && node.HasParameter(EMA_Command.PARAMETER_ROTATION);
            HasScale = scale && node.HasParameter(EMA_Command.PARAMETER_SCALE);

            for (int i = 0; i < keyframes.Count; i++)
                Keyframes[i] = new SerializedKeyframe(keyframes[i], node.GetKeyframeValues(keyframes[i], pos, rot, scale));
        }

        /// <summary>
        /// Rebases the animations first frame to be 0. If the lowest frame in the keyframes is already 0, then nothing is changed.
        /// </summary>
        public static void StartFrameRebaseToZero(List<SerializedBone> bones)
        {
            //Determine the start frame of these copied bones
            int startFrame = GetMinKeyframe(bones);

            //Rebase the animation start frame
            foreach (var bone in bones)
            {
                foreach (var keyframe in bone.Keyframes)
                    keyframe.Frame -= startFrame;
            }
            
        }

        public static void StartFrameRebase(List<SerializedBone> bones, int newStartFrame)
        {
            //Rebase the animation start frame
            foreach (var bone in bones)
            {
                foreach (var keyframe in bone.Keyframes)
                    keyframe.Frame += newStartFrame;
            }

        }

        public static int GetMinKeyframe(List<SerializedBone> bones)
        {
            int absoluteMin = ushort.MaxValue;

            foreach (var bone in bones)
            {
                int min = bone.Keyframes.Length > 0 ?  bone.Keyframes.Min(x => x.Frame) : 0;

                if (min < absoluteMin)
                    absoluteMin = min;
            }

            return absoluteMin;
        }

        public static int GetMaxKeyframe(List<SerializedBone> bones)
        {
            int absoluteMax = ushort.MaxValue;

            foreach (var bone in bones)
            {
                int max = bone.Keyframes.Length > 0 ? bone.Keyframes.Max(x => x.Frame) : 0;

                if (max > absoluteMax)
                    absoluteMax = max;
            }

            return absoluteMax;
        }
    
        public static List<string> GetBoneList(List<SerializedBone> bones)
        {
            List<string> names = new List<string>();

            foreach (var bone in bones)
                names.Add(bone.Name);

            return names;
        }

        internal void ConvertRotation(KeyframeRotationFormat format)
        {
            if (format == RotationFormat) return; //Already in correct format

            if (format == KeyframeRotationFormat.EulerAngles)
            {
                //Convert Quaternion -> Euler Angles
                foreach(var keyframe in Keyframes)
                {
                    Vector3 angles = MathHelpers.QuaternionToEuler(new Quaternion(keyframe.RotX, keyframe.RotY, keyframe.RotZ, keyframe.RotW));
                    keyframe.RotX = angles.X;
                    keyframe.RotY = angles.Y;
                    keyframe.RotZ = angles.Z;
                    keyframe.RotW = 1;
                }
            }
            else if(format == KeyframeRotationFormat.Quaternion)
            {
                //Convert Euler Angles -> Quaternion
                foreach (var keyframe in Keyframes)
                {
                    //Quaternion tempX = MathHelpers.AxisAngleToQuaternion(Vector3.UnitX, keyframe.RotX);
                    //Quaternion tempY = MathHelpers.AxisAngleToQuaternion(Vector3.UnitY, keyframe.RotY);
                    //Quaternion tempZ = MathHelpers.AxisAngleToQuaternion(Vector3.UnitZ, keyframe.RotZ);
                    //Quaternion quaternion = MathHelpers.MultiplyQuaternions(tempZ, MathHelpers.MultiplyQuaternions(tempY, tempX));

                    Quaternion quaternion = MathHelpers.EulerToQuaternion(new Vector3(keyframe.RotX, keyframe.RotY, keyframe.RotZ) * keyframe.RotW);
                    keyframe.RotX = quaternion.X;
                    keyframe.RotY = quaternion.Y;
                    keyframe.RotZ = quaternion.Z;
                    keyframe.RotW = quaternion.W;
                }
            }

            RotationFormat = format;
        }

        #region BindPose
        public static void SetBindPose(List<SerializedBone> serialziedBones, ESK_Skeleton bindPose)
        {
            SetBindPoseState(serialziedBones, bindPose, false);
        }

        public static void SetNeutralBindPose(List<SerializedBone> serialziedBones, ESK_Skeleton bindPose)
        {
            SetBindPoseState(serialziedBones, bindPose, true);
        }

        private static void SetBindPoseState(List<SerializedBone> serialziedBones, ESK_Skeleton bindPose, bool invert)
        {
            if (bindPose == null) return;

            //Nuke all nodes that dont exist in the target skeleton
            serialziedBones.RemoveAll(x => !bindPose.Exists(x.Name));

            foreach(var bone in serialziedBones)
            {
                Matrix4x4 bindBone = GetBindPose(bone.Name, bindPose, invert);

                foreach(var keyframe in bone.Keyframes)
                {
                    //Vector3 scale = new Vector3(keyframe.ScaleX, keyframe.ScaleY, keyframe.ScaleZ) * keyframe.ScaleW;
                    Quaternion rot = new Quaternion(keyframe.RotX, keyframe.RotY, keyframe.RotZ, keyframe.RotW);
                    Vector3 pos = new Vector3(keyframe.PosX, keyframe.PosY, keyframe.PosZ) * keyframe.PosW;

                    Matrix4x4 keyframeMatrix = Matrix4x4.Identity;
                    //keyframeMatrix *= Matrix4x4.CreateScale(scale);
                    keyframeMatrix *= Matrix4x4.CreateFromQuaternion(rot);
                    keyframeMatrix *= Matrix4x4.CreateTranslation(pos);

                    //Based on the invert flag, this will either set the bind pose on the keyframes or remove it
                    keyframeMatrix *= bindBone;

                    //Convert back to floats
                    pos = keyframeMatrix.Translation;
                    rot = Quaternion.CreateFromRotationMatrix(keyframeMatrix);

                    keyframe.PosX = pos.X;
                    keyframe.PosY = pos.Y;
                    keyframe.PosZ = pos.Z;
                    keyframe.PosW = 1f;
                    keyframe.RotX = rot.X;
                    keyframe.RotY = rot.Y;
                    keyframe.RotZ = rot.Z;
                    keyframe.RotW = rot.W;
                }
            }
        }

        private static Matrix4x4 GetBindPose(string boneName, ESK_Skeleton bindPose, bool invert)
        {
            var eskBone = bindPose.NonRecursiveBones.FirstOrDefault(x => x.Name.Equals(boneName, StringComparison.OrdinalIgnoreCase));
            if (eskBone == null) return Matrix4x4.Identity;

            ESK_RelativeTransform transform = eskBone.RelativeTransform;

            Vector3 ean_initialBonePosition = new Vector3(transform.PositionX, transform.PositionY, transform.PositionZ) * transform.PositionW;
            Quaternion ean_initialBoneOrientation = new Quaternion(transform.RotationX, transform.RotationY, transform.RotationZ, transform.RotationW);
            //Vector3 ean_initialBoneScale = new Vector3(transform.ScaleX, transform.ScaleY, transform.ScaleZ) * transform.ScaleW;

            Matrix4x4 relativeMatrix_EanBone_inv = Matrix4x4.Identity;
            //relativeMatrix_EanBone_inv *= Matrix4x4.CreateScale(ean_initialBoneScale);
            relativeMatrix_EanBone_inv *= Matrix4x4.CreateFromQuaternion(ean_initialBoneOrientation);
            relativeMatrix_EanBone_inv *= Matrix4x4.CreateTranslation(ean_initialBonePosition);

            if (invert)
            {
                Matrix4x4 invertedMatrix;
                Matrix4x4.Invert(relativeMatrix_EanBone_inv, out invertedMatrix);
                return invertedMatrix;
            }
            else
            {
                return relativeMatrix_EanBone_inv;
            }
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

    public enum KeyframeRotationFormat
    {
        EulerAngles,
        Quaternion
    }
}
