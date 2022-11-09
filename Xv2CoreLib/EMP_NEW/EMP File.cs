using LB_Common.Numbers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW.Keyframes;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMP_NEW
{
    public enum VersionEnum : ushort
    {
        DBXV2 = 37568,
        SDBH = 37632
    }
    public enum ParticleNodeType : byte
    {
        Null = 0,
        Emitter = 1,
        Emission = 2
    }

    public enum ParticleBillboardType : ushort
    {
        Camera = 0,
        Front = 1,
        None = 2
    }

    [Flags]
    public enum NodeFlags1 : ushort
    {
        Unk1 = 0x1, //Not used
        Loop = 0x2, //Emission, emitter
        Unk3 = 0x4, //Not used
        FlashOnGen = 0x8, //Appears on all node types
        Unk5 = 0x10, //Appears on all node types
        Unk6 = 0x20, //Not used
        Unk7 = 0x40, //Not used
        Unk8 = 0x80, //Not used
        Unk9 = 0x100, //Not used
        Unk10 = 0x200, //Not used
        Unk11 = 0x400, //Not used
        Hide = 0x800, //Never used by any vanilla EMP file. Hides any emission node, no effect on emitters or nulls.
        EnableScaleXY = 0x1000, //Appears on emission and null types
        Unk14 = 0x2000, //Appears on all node types
        EnableSecondaryColor = 0x4000, //Emission only
        Unk16 = 0x8000 //Appears on all node types
    }

    [Flags]
    public enum NodeFlags2 : byte
    {
        Unk1 = 0x1, //Only appears on Emissions
        Unk2 = 0x2, //Only appears on Emissions
        Unk3 = 0x4, //Not used
        Unk4 = 0x8, //Not used
        RandomRotationDir = 0x10, //Only appears on emissions with rotation values, or emitters with angles
        Unk6 = 0x20, //Only appears on ConeExtrude
        RandomUpVector = 0x40, //Only appears on Default and Mesh (these are the only 2 node types that use a manually defined Rotation Axis)
        Unk8 = 0x80 //Not used
    }

    [Serializable]
    public class EMP_File : ITexture
    {
        public const int EMP_SIGNATURE = 1347241251;
        public const string CLIPBOARD_NODE = "EMP_NODE";
        public const string CLIPBOARD_TEXTURE_SAMPLER = "EMP_TEXTURE_SAMPLER";
        public const string CLIPBOARD_TEXTURE_KEYFRAME = "EMP_TEXTURE_KEYFRAME";
        public const string CLIPBOARD_KEYFRAMED_VALUE = "EMP_KEYFRAMED_VALUE";
        public const string CLIPBOARD_KEYFRAME = "EMP_KEYFRAME";
        public const string CLIPBOARD_SHAP_DRAW_POINT = "EMP_SHAPW_DRAW_POINT";
        public const string CLIPBOARD_CONE_EXTRUSION = "EMP_CONE_EXTRUSION";

        public VersionEnum Version { get; set; } = VersionEnum.DBXV2;
        /// <summary>
        /// Full Decompile will decompile keyframed values into a more edit-friendly state. Should only be enabled for editor tools (EEPK Organiser / XenoKit), and disabled for the installer. 
        /// This can only be set when initially loading the file.
        /// </summary>
        internal bool FullDecompile { get; set; }

        public AsyncObservableCollection<ParticleNode> ParticleNodes { get; set; } = new AsyncObservableCollection<ParticleNode>();
        public AsyncObservableCollection<EMP_TextureSamplerDef> Textures { get; set; } = new AsyncObservableCollection<EMP_TextureSamplerDef>();

        #region LoadSave
        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static EMP_File Load(string path, bool fullDecompile)
        {
            return new Parser(path, fullDecompile).EmpFile;
        }

        public static EMP_File Load(byte[] bytes, bool fullDecompile)
        {
            return new Parser(bytes, fullDecompile).EmpFile;
        }

        #endregion

        #region References
        /// <summary>
        /// Parses all ParticleEffects and removes the specified Texture ref, if found.
        /// </summary>
        public void RemoveTextureReferences(EMP_TextureSamplerDef textureRef, List<IUndoRedo> undos = null)
        {
            if (ParticleNodes != null)
            {
                RemoveTextureReferences_Recursive(ParticleNodes, textureRef, undos);
            }
        }

        private void RemoveTextureReferences_Recursive(AsyncObservableCollection<ParticleNode> children, EMP_TextureSamplerDef textureRef, List<IUndoRedo> undos = null)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].ChildParticleNodes != null)
                {
                    RemoveTextureReferences_Recursive(children[i].ChildParticleNodes, textureRef, undos);
                }

                if (children[i].NodeType == ParticleNodeType.Emission)
                {
                startPoint:
                    foreach (var e in children[i].EmissionNode.Texture.TextureEntryRef)
                    {
                        if (e.TextureRef == textureRef)
                        {
                            if (undos != null)
                                undos.Add(new UndoableListRemove<TextureEntry_Ref>(children[i].EmissionNode.Texture.TextureEntryRef, e));

                            children[i].EmissionNode.Texture.TextureEntryRef.Remove(e);
                            goto startPoint;
                        }
                    }
                }
            }
        }

        public void RefactorTextureRef(EMP_TextureSamplerDef oldTextureRef, EMP_TextureSamplerDef newTextureRef, List<IUndoRedo> undos)
        {
            if (ParticleNodes != null)
            {
                RemoveTextureReferences_Recursive(ParticleNodes, oldTextureRef, newTextureRef, undos);
            }
        }

        private void RemoveTextureReferences_Recursive(AsyncObservableCollection<ParticleNode> children, EMP_TextureSamplerDef oldTextureRef, EMP_TextureSamplerDef newTextureRef, List<IUndoRedo> undos)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].ChildParticleNodes != null)
                {
                    RemoveTextureReferences_Recursive(children[i].ChildParticleNodes, oldTextureRef, newTextureRef, undos);
                }

                if (children[i].NodeType == ParticleNodeType.Emission)
                {
                    foreach (var e in children[i].EmissionNode.Texture.TextureEntryRef)
                    {
                        if (e.TextureRef == oldTextureRef)
                        {
                            undos.Add(new UndoableProperty<TextureEntry_Ref>(nameof(e.TextureRef), e, e.TextureRef, newTextureRef));
                            e.TextureRef = newTextureRef;
                        }
                    }
                }
            }
        }

        #endregion

        #region AddRemove
        /// <summary>
        /// Add a new ParticleEffect entry.
        /// </summary>
        /// <param name="index">Where in the collection to insert the new entry. The default value of -1 will result in it being added to the end, as will out of range values.</param>
        public void AddNew(int index = -1, List<IUndoRedo> undos = null)
        {
            if (index < -1 || index > ParticleNodes.Count() - 1) index = -1;

            var newEffect = ParticleNode.GetNew();

            if (index == -1)
            {
                ParticleNodes.Add(newEffect);

                if (undos != null)
                    undos.Add(new UndoableListAdd<ParticleNode>(ParticleNodes, newEffect));
            }
            else
            {
                ParticleNodes.Insert(index, newEffect);

                if (undos != null)
                    undos.Add(new UndoableStateChange<ParticleNode>(ParticleNodes, index, ParticleNodes[index], newEffect));
            }
        }

        public bool RemoveParticleEffect(ParticleNode effectToRemove, List<IUndoRedo> undos = null)
        {
            bool result = false;
            for (int i = 0; i < ParticleNodes.Count; i++)
            {
                result = RemoveInChildren(ParticleNodes[i].ChildParticleNodes, effectToRemove, undos);

                if (ParticleNodes[i] == effectToRemove)
                {
                    if (undos != null)
                        undos.Add(new UndoableListRemove<ParticleNode>(ParticleNodes, effectToRemove));

                    ParticleNodes.Remove(effectToRemove);
                    return true;
                }

                if (result == true)
                {
                    break;
                }
            }

            return result;
        }

        private bool RemoveInChildren(AsyncObservableCollection<ParticleNode> children, ParticleNode effectToRemove, List<IUndoRedo> undos = null)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].ChildParticleNodes.Count > 0)
                {
                    RemoveInChildren(children[i].ChildParticleNodes, effectToRemove, undos);
                }

                if (children[i] == effectToRemove)
                {
                    if (undos != null)
                        undos.Add(new UndoableListRemove<ParticleNode>(children, effectToRemove));

                    children.Remove(effectToRemove);
                    return true;
                }

            }

            return false;
        }

        #endregion

        #region Get
        public bool NodeExists(ParticleNode node)
        {
            return NodeExists(node, ParticleNodes);
        }

        private bool NodeExists(ParticleNode node, IList<ParticleNode> nodes)
        {
            foreach(var _node in nodes)
            {
                if (_node == node) return true;
                if (NodeExists(node, _node.ChildParticleNodes)) return true;
            }

            return false;
        }

        /// <summary>
        /// Get the parent of this node. If this node is at the root level, then nullwill be returned.
        /// </summary>
        public ParticleNode GetParent(ParticleNode node)
        {
            if (ParticleNodes.Contains(node))
                return null;

            foreach(var _node in ParticleNodes)
            {
                ParticleNode result = GetParent_Recursive(node, _node);

                if (result != null)
                    return result;
            }

            return null;

        }

        private ParticleNode GetParent_Recursive(ParticleNode node, ParticleNode nodeToSearch)
        {
            foreach(var childNode in nodeToSearch.ChildParticleNodes)
            {
                if (childNode == node) return childNode;

                ParticleNode result = GetParent_Recursive(node, childNode);

                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Get the collection that this node belongs to. 
        /// If node is null, the root node collection will be returned.
        /// </summary>
        public AsyncObservableCollection<ParticleNode> GetParentList(ParticleNode particleNode)
        {
            if (particleNode == null) return ParticleNodes;

            foreach (ParticleNode node in ParticleNodes)
            {
                AsyncObservableCollection<ParticleNode> result = null;

                if (node.ChildParticleNodes.Count > 0)
                {
                    result = GetParentList_Recursive(node.ChildParticleNodes, particleNode);
                }
                if (result != null)
                {
                    return result;
                }

                if (node == particleNode)
                {
                    return ParticleNodes;
                }
            }

            return null;
        }

        private AsyncObservableCollection<ParticleNode> GetParentList_Recursive(AsyncObservableCollection<ParticleNode> children, ParticleNode particleEffect)
        {
            AsyncObservableCollection<ParticleNode> result = null;

            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].ChildParticleNodes.Count > 0)
                {
                    result = GetParentList_Recursive(children[i].ChildParticleNodes, particleEffect);
                }

                if (children[i] == particleEffect)
                {
                    return children;
                }
            }

            if (result != null)
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public List<EMP_TextureSamplerDef> GetTextureEntriesThatUseRef(EMB_CLASS.EmbEntry textureRef)
        {
            List<EMP_TextureSamplerDef> textures = new List<EMP_TextureSamplerDef>();

            foreach (var texture in Textures)
            {
                if (texture.TextureRef == textureRef)
                {
                    textures.Add(texture);
                }
            }

            return textures;
        }

        public List<ParticleTexture> GetTexturePartsThatUseMaterialRef(EmmMaterial materialRef)
        {
            List<ParticleTexture> textureParts = new List<ParticleTexture>();

            foreach (var particleEffect in ParticleNodes)
            {
                if (particleEffect.EmissionNode.Texture.MaterialRef == materialRef)
                {
                    textureParts.Add(particleEffect.EmissionNode.Texture);
                }

                if (particleEffect.ChildParticleNodes != null)
                {
                    textureParts = GetTexturePartsThatUseMaterialRef_Recursive(materialRef, textureParts, particleEffect.ChildParticleNodes);
                }

            }

            return textureParts;
        }

        private List<ParticleTexture> GetTexturePartsThatUseMaterialRef_Recursive(EmmMaterial materialRef, List<ParticleTexture> textureParts, AsyncObservableCollection<ParticleNode> particleEffects)
        {
            foreach (var particleEffect in particleEffects)
            {
                if (particleEffect.EmissionNode.Texture.MaterialRef == materialRef)
                {
                    textureParts.Add(particleEffect.EmissionNode.Texture);
                }

                if (particleEffect.ChildParticleNodes != null)
                {
                    textureParts = GetTexturePartsThatUseMaterialRef_Recursive(materialRef, textureParts, particleEffect.ChildParticleNodes);
                }

            }

            return textureParts;
        }

        public List<ITextureRef> GetNodesThatUseTexture(EMP_TextureSamplerDef embEntryRef)
        {
            List<ITextureRef> textureParts = new List<ITextureRef>();

            foreach (var particleEffect in ParticleNodes)
            {
                foreach (var textureEntry in particleEffect.EmissionNode.Texture.TextureEntryRef)
                {
                    if (textureEntry.TextureRef == embEntryRef)
                    {
                        textureParts.Add(particleEffect.EmissionNode.Texture);
                        break;
                    }
                }

                if (particleEffect.ChildParticleNodes != null)
                {
                    textureParts = GetTexturePartsThatUseEmbEntryRef_Recursive(embEntryRef, textureParts, particleEffect.ChildParticleNodes);
                }

            }

            return textureParts;
        }

        private List<ITextureRef> GetTexturePartsThatUseEmbEntryRef_Recursive(EMP_TextureSamplerDef embEntryRef, List<ITextureRef> textureParts, AsyncObservableCollection<ParticleNode> particleEffects)
        {
            foreach (var particleEffect in particleEffects)
            {
                foreach (var textureEntry in particleEffect.EmissionNode.Texture.TextureEntryRef)
                {
                    if (textureEntry.TextureRef == embEntryRef)
                    {
                        textureParts.Add(particleEffect.EmissionNode.Texture);
                        break;
                    }
                }

                if (particleEffect.ChildParticleNodes != null)
                {
                    textureParts = GetTexturePartsThatUseEmbEntryRef_Recursive(embEntryRef, textureParts, particleEffect.ChildParticleNodes);
                }

            }

            return textureParts;
        }

        /// <summary>
        /// Finds an identical texture. Returns null if none exists.
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public EMP_TextureSamplerDef GetTexture(EMP_TextureSamplerDef texture)
        {
            if (texture == null) return null;

            foreach (var tex in Textures)
            {
                if (tex.Compare(texture)) return tex;
            }

            return null;
        }

        #endregion

        #region Color

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();

            foreach (var particleEffect in ParticleNodes)
            {
                colors.AddRange(particleEffect.GetUsedColors());
            }

            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (ParticleNodes == null) return;
            if (undos == null) undos = new List<IUndoRedo>();

            foreach (var particleEffects in ParticleNodes)
            {
                particleEffects.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
            }
        }

        public List<IUndoRedo> RemoveColorAnimations(AsyncObservableCollection<ParticleNode> particleEffects = null, bool root = true)
        {
            if (particleEffects == null && root) particleEffects = ParticleNodes;
            if (particleEffects == null && !root) return new List<IUndoRedo>();

            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var particleEffect in particleEffects)
            {
                undos.AddRange(particleEffect.EmissionNode.Texture.Color1.RemoveAllKeyframes());
                undos.AddRange(particleEffect.EmissionNode.Texture.Color2.RemoveAllKeyframes());

                if (particleEffect.ChildParticleNodes != null)
                    undos.AddRange(RemoveColorAnimations(particleEffect.ChildParticleNodes, false));
            }

            return undos;
        }

        public List<IUndoRedo> RemoveRandomColorRange(AsyncObservableCollection<ParticleNode> particleEffects = null, bool root = true)
        {
            if (particleEffects == null && root) particleEffects = ParticleNodes;
            if (particleEffects == null && !root) return new List<IUndoRedo>();

            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var particleEffect in particleEffects)
            {
                particleEffect.RemoveColorRandomRange(undos);

                if (particleEffect.ChildParticleNodes != null)
                    undos.AddRange(RemoveRandomColorRange(particleEffect.ChildParticleNodes, false));
            }

            return undos;
        }

        #endregion

        public EMP_File Clone()
        {
            AsyncObservableCollection<ParticleNode> _ParticleEffects = new AsyncObservableCollection<ParticleNode>();
            AsyncObservableCollection<EMP_TextureSamplerDef> _Textures = new AsyncObservableCollection<EMP_TextureSamplerDef>();
            foreach (var e in ParticleNodes)
            {
                _ParticleEffects.Add(e.Clone());
            }
            foreach (var e in Textures)
            {
                _Textures.Add(e.Clone());
            }

            return new EMP_File()
            {
                ParticleNodes = _ParticleEffects,
                Textures = _Textures
            };
        }

        //DEBUG:
        public List<ParticleNode> GetAllParticleEffects_DEBUG()
        {
            List<ParticleNode> GetAllParticleEffectsRecursive_DEBUG(IList<ParticleNode> particleEffects)
            {
                List<ParticleNode> total = new List<ParticleNode>();

                foreach (var particle in particleEffects)
                {
                    total.Add(particle);

                    if (particle.ChildParticleNodes != null)
                    {
                        total.AddRange(GetAllParticleEffectsRecursive_DEBUG(particle.ChildParticleNodes));
                    }
                }

                return total;
            }

            return GetAllParticleEffectsRecursive_DEBUG(ParticleNodes);
        }
    }

    [Serializable]
    public class ParticleNode : INotifyPropertyChanged, IName, ISelectedKeyframedValue
    {
        public const int ENTRY_SIZE = 160;

        #region UI
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Selected KeyframedValue is binded here for the Keyframe Editor to access
        [NonSerialized]
        private KeyframedBaseValue _selectedKeyframedValue = null;
        public KeyframedBaseValue SelectedKeyframedValue
        {
            get => _selectedKeyframedValue;
            set
            {
                if(_selectedKeyframedValue != value)
                {
                    _selectedKeyframedValue = value;
                    NotifyPropertyChanged(nameof(SelectedKeyframedValue));
                }
            }
        }

        public bool IsEmitter => NodeType == ParticleNodeType.Emitter;
        public bool IsEmission => NodeType == ParticleNodeType.Emission;
        public bool IsNull => NodeType == ParticleNodeType.Null;

        //Exposed directly to TreeView.
        public bool UndoableIsVisible
        {
            get => !NodeFlags.HasFlag(NodeFlags1.Hide);
            set
            {
                if(value != UndoableIsVisible)
                {
                    NodeFlags1 flag = NodeFlags.SetFlag(NodeFlags1.Hide, !value);
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(NodeFlags), this, NodeFlags, flag, "Node Visibility"));
                    NodeFlags = flag;
                }
            }
        }
        #endregion

        //Prop values
        private string _name = null;
        private ParticleNodeType _nodeType = 0;
        private NodeFlags1 _nodeFlags = 0;

        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
            }
        }

        public ParticleNodeType NodeType
        {
            get => _nodeType;
            set
            {
                if (value != _nodeType)
                {
                    _nodeType = value;
                    NotifyPropertyChanged(nameof(NodeType));
                    NotifyPropertyChanged(nameof(IsEmitter));
                    NotifyPropertyChanged(nameof(IsEmission));
                    NotifyPropertyChanged(nameof(IsNull));
                }
            }
        }
        /// <summary>
        /// Returns the old "ComponentType" that was used in the old EMP parser, and are how the node types are saved to binary. Read-only.
        /// </summary>
        public NodeSpecificType NodeSpecificType
        {
            get
            {
                switch (NodeType)
                {
                    case ParticleNodeType.Null:
                        return NodeSpecificType.Null;
                    case ParticleNodeType.Emission:
                        return EmissionNode.GetNodeType();
                    case ParticleNodeType.Emitter:
                        return EmitterNode.GetNodeType();
                }

                return NodeSpecificType.Null;
            }
        }

        public NodeFlags1 NodeFlags
        {
            get => _nodeFlags;
            set
            {
                if (value != _nodeFlags)
                {
                    _nodeFlags = value;
                    NotifyPropertyChanged(nameof(NodeFlags));
                    NotifyPropertyChanged(nameof(UndoableIsVisible));
                }
            }
        }
        public NodeFlags2 NodeFlags2 { get; set; }

        public byte StartTime { get; set; }
        public byte StartTime_Variance { get; set; }
        public short MaxInstances { get; set; }
        public ushort Lifetime { get; set; }
        public ushort Lifetime_Variance { get; set; }
        public byte BurstFrequency { get; set; }
        public byte BurstFrequency_Variance { get; set; }
        public ushort Burst { get; set; }
        public ushort Burst_Variance { get; set; }

        public KeyframedVector3Value Position { get; set; } = new KeyframedVector3Value(0, 0, 0, KeyframedValueType.Position);
        public KeyframedVector3Value Rotation { get; set; } = new KeyframedVector3Value(0, 0, 0, KeyframedValueType.Rotation);
        public CustomVector4 Position_Variance { get; set; } = new CustomVector4();
        public CustomVector4 Rotation_Variance { get; set; } = new CustomVector4();

        //Unknown values
        public ushort I_48 { get; set; } = 1; //always 1
        public ushort I_50 { get; set; } //always 0
        public ushort I_56 { get; set; } //always 0
        public ushort I_58 { get; set; } //always 0
        public ushort I_60 { get; set; } //always 0
        public ushort I_62 { get; set; } //always 0
        public ushort I_128 { get; set; } //0, 512
        public ushort I_130 { get; set; } //always 0
        public float F_132 { get; set; } //0, 1, 2

        public ParticleEmitter EmitterNode { get; set; } = new ParticleEmitter();
        public ParticleEmission EmissionNode { get; set; } = new ParticleEmission();

        public AsyncObservableCollection<EMP_KeyframedValue> KeyframedValues { get; set; } = new AsyncObservableCollection<EMP_KeyframedValue>();
        public AsyncObservableCollection<EMP_Modifier> Modifiers { get; set; } = new AsyncObservableCollection<EMP_Modifier>();
        public AsyncObservableCollection<ParticleNode> ChildParticleNodes { get; set; } = new AsyncObservableCollection<ParticleNode>();


        public ParticleNode Clone(bool ignoreChildren = false)
        {
            AsyncObservableCollection<ParticleNode> _children = new AsyncObservableCollection<ParticleNode>();

            if (!ignoreChildren && ChildParticleNodes != null)
            {
                foreach (var e in ChildParticleNodes)
                {
                    _children.Add(e.Clone());
                }
            }

            return new ParticleNode()
            {
                NodeFlags = NodeFlags,
                NodeFlags2 = NodeFlags2,
                MaxInstances = MaxInstances,
                Lifetime = Lifetime,
                Lifetime_Variance = Lifetime_Variance,
                StartTime = StartTime,
                StartTime_Variance = StartTime_Variance,
                BurstFrequency = BurstFrequency,
                BurstFrequency_Variance = BurstFrequency_Variance,
                I_48 = I_48,
                I_50 = I_50,
                Burst = Burst,
                Burst_Variance = Burst_Variance,
                I_56 = I_56,
                I_58 = I_58,
                I_60 = I_60,
                I_62 = I_62,
                Rotation = Rotation.Copy(),
                Rotation_Variance = Rotation_Variance.Copy(),
                I_128 = I_128,
                F_132 = F_132,
                Position = Position.Copy(),
                Position_Variance = Position_Variance.Copy(),
                Name = Utils.CloneString(Name),
                KeyframedValues = KeyframedValues.Copy(),
                Modifiers = Modifiers.Copy(),
                EmissionNode = EmissionNode.Clone(),
                EmitterNode = EmitterNode.Copy(),
                NodeType = NodeType,
                ChildParticleNodes = _children
            };
        }

        public static ParticleNode GetNew()
        {
            return new ParticleNode()
            {
                Name = "NewEmpty",
                NodeType = ParticleNodeType.Null
            };
        }

        public static ParticleNode GetNew(NewNodeType type, IList<ParticleNode> siblingNodes)
        {
            ParticleNode node = new ParticleNode();
            node.Lifetime = 60;
            node.Burst = 1;
            node.MaxInstances = 1;

            if(type == NewNodeType.Plane)
            {
                node.Name = NameHelper.GetUniqueName("Plane", siblingNodes);
                node.NodeType = ParticleNodeType.Emission;
                node.EmissionNode.BillboardType = ParticleBillboardType.Camera;
                node.EmissionNode.EmissionType = ParticleEmission.ParticleEmissionType.Plane;
            }
            else if (type == NewNodeType.Shape)
            {
                node.Name = NameHelper.GetUniqueName("Shape", siblingNodes);
                node.NodeType = ParticleNodeType.Emission;
                node.EmissionNode.BillboardType = ParticleBillboardType.None;
                node.EmissionNode.EmissionType = ParticleEmission.ParticleEmissionType.ShapeDraw;
            }
            else if (type == NewNodeType.Mesh)
            {
                node.Name = NameHelper.GetUniqueName("StaticMesh", siblingNodes);
                node.NodeType = ParticleNodeType.Emission;
                node.EmissionNode.BillboardType = ParticleBillboardType.None;
                node.EmissionNode.EmissionType = ParticleEmission.ParticleEmissionType.Mesh;
            }
            else if (type == NewNodeType.Extrude)
            {
                node.Name = NameHelper.GetUniqueName("ConeExtrude", siblingNodes);
                node.NodeType = ParticleNodeType.Emission;
                node.EmissionNode.BillboardType = ParticleBillboardType.None;
                node.EmissionNode.EmissionType = ParticleEmission.ParticleEmissionType.ConeExtrude;
            }
            else if (type == NewNodeType.EmitterCirlce)
            {
                node.Name = NameHelper.GetUniqueName("EmitterCircle", siblingNodes);
                node.NodeType = ParticleNodeType.Emitter;
                node.EmitterNode.Shape = ParticleEmitter.ParticleEmitterShape.Circle;
            }
            else if (type == NewNodeType.EmitterSquare)
            {
                node.Name = NameHelper.GetUniqueName("EmitterSquare", siblingNodes);
                node.NodeType = ParticleNodeType.Emitter;
                node.EmitterNode.Shape = ParticleEmitter.ParticleEmitterShape.Square;
            }
            else if (type == NewNodeType.EmitterSphere)
            {
                node.Name = NameHelper.GetUniqueName("EmitterSphere", siblingNodes);
                node.NodeType = ParticleNodeType.Emitter;
                node.EmitterNode.Shape = ParticleEmitter.ParticleEmitterShape.Sphere;
            }
            else if (type == NewNodeType.EmitterPoint)
            {
                node.Name = NameHelper.GetUniqueName("EmitterPoint", siblingNodes);
                node.NodeType = ParticleNodeType.Emitter;
                node.EmitterNode.Shape = ParticleEmitter.ParticleEmitterShape.Point;
            }
            else
            {
                node.Name = NameHelper.GetUniqueName("NewEmpty", siblingNodes);
                node.NodeType = ParticleNodeType.Null;
            }

            return node;
        }

        /// <summary>
        /// Add a new ParticleEffect entry.
        /// </summary>
        /// <param name="index">Where in the collection to insert the new entry. The default value of -1 will result in it being added to the end.</param>
        public void AddNew(int index = -1, List<IUndoRedo> undos = null)
        {
            if (index < -1 || index > ChildParticleNodes.Count() - 1) index = -1;

            var newEffect = GetNew();

            if (index == -1)
            {
                ChildParticleNodes.Add(newEffect);

                if (undos != null)
                    undos.Add(new UndoableListAdd<ParticleNode>(ChildParticleNodes, newEffect));
            }
            else
            {
                ChildParticleNodes.Insert(index, newEffect);

                if (undos != null)
                    undos.Add(new UndoableStateChange<ParticleNode>(ChildParticleNodes, index, ChildParticleNodes[index], newEffect));
            }
        }

        public void CopyValues(ParticleNode particleEffect, List<IUndoRedo> undos = null)
        {
            if (undos == null) undos = new List<IUndoRedo>();

            undos.AddRange(Utils.CopyValues(this, particleEffect));

            var emitter = particleEffect.EmitterNode.Copy();
            var emission = particleEffect.EmissionNode.Clone(); //Clone keeps texture and material references intact
            var type_0 = particleEffect.KeyframedValues.Copy();
            var type_1 = particleEffect.Modifiers.Copy();

            undos.Add(new UndoableProperty<ParticleNode>(nameof(EmitterNode), this, EmitterNode, emitter));
            undos.Add(new UndoableProperty<ParticleNode>(nameof(EmissionNode), this, EmissionNode, emission));
            undos.Add(new UndoableProperty<ParticleNode>(nameof(KeyframedValues), this, KeyframedValues, type_0));
            undos.Add(new UndoableProperty<ParticleNode>(nameof(Modifiers), this, Modifiers, type_1));
            undos.Add(new UndoActionPropNotify(this, true));

            EmitterNode = emitter;
            EmissionNode = emission;
            KeyframedValues = type_0;
            Modifiers = type_1;

            this.NotifyPropsChanged();
        }

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();

            RgbColor color1 = EmissionNode.Texture.Color1.GetAverageColor();
            RgbColor color2 = EmissionNode.Texture.Color2.GetAverageColor();

            if (!color1.IsWhiteOrBlack)
                colors.Add(color1);

            if (!color2.IsWhiteOrBlack)
                colors.Add(color2);


            if (ChildParticleNodes != null)
            {
                foreach(var child in ChildParticleNodes)
                {
                    colors.AddRange(child.GetUsedColors());
                }
            }

            return colors;
        }

        public void ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos, bool hueSet = false, int variance = 0)
        {
            if(NodeType == ParticleNodeType.Emission)
            {
                EmissionNode.Texture.Color1.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
                EmissionNode.Texture.Color2.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
                EmissionNode.Texture.Color_Variance.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
            }

            //Children
            if (ChildParticleNodes != null)
            {
                foreach (var child in ChildParticleNodes)
                {
                    child.ChangeHue(hue, saturation, lightness, undos, hueSet);
                }
            }
        }

        public void RemoveColorRandomRange(List<IUndoRedo> undos)
        {
            if(EmissionNode?.Texture != null)
            {
                if (EmissionNode.Texture.Color_Variance != 0f)
                {
                    undos.Add(new UndoableProperty<CustomColor>(nameof(EmissionNode.Texture.Color_Variance.R), EmissionNode.Texture.Color_Variance, EmissionNode.Texture.Color_Variance.R, 0f));
                    undos.Add(new UndoableProperty<CustomColor>(nameof(EmissionNode.Texture.Color_Variance.G), EmissionNode.Texture.Color_Variance, EmissionNode.Texture.Color_Variance.G, 0f));
                    undos.Add(new UndoableProperty<CustomColor>(nameof(EmissionNode.Texture.Color_Variance.B), EmissionNode.Texture.Color_Variance, EmissionNode.Texture.Color_Variance.B, 0f));

                    EmissionNode.Texture.Color_Variance.R = 0f;
                    EmissionNode.Texture.Color_Variance.G = 0f;
                    EmissionNode.Texture.Color_Variance.B = 0f;
                }
            }
        }

        public EMP_KeyframedValue[] GetKeyframedValues(int parameter, params int[] components)
        {
            EMP_KeyframedValue[] values = new EMP_KeyframedValue[components.Length];

            for(int i = 0; i < components.Length; i++)
            {
                EMP_KeyframedValue value = KeyframedValues.FirstOrDefault(x => x.Parameter == parameter && x.Component == components[i]);

                if (value != null)
                    values[i] = value;
                else
                    values[i] = EMP_KeyframedValue.Default;
            }

            return values;
        }

        internal void CompileAllKeyframes()
        {
            KeyframedValues.Clear();

            //Position and Rotation (XYZ) exist on all nodes, regardless of the type
            AddKeyframedValues(Position.CompileKeyframes());
            AddKeyframedValues(Rotation.CompileKeyframes());

            //Emission types always have Scale, Color1 and Color2 values (since they all have a ParticleTexture)
            if(NodeType == ParticleNodeType.Emission)
            {
                AddKeyframedValues(EmissionNode.Texture.Color1.CompileKeyframes());
                AddKeyframedValues(EmissionNode.Texture.Color2.CompileKeyframes());
                AddKeyframedValues(EmissionNode.Texture.Color1_Transparency.CompileKeyframes());
                AddKeyframedValues(EmissionNode.Texture.Color2_Transparency.CompileKeyframes());

                if (NodeFlags.HasFlag(NodeFlags1.EnableScaleXY))
                {
                    AddKeyframedValues(EmissionNode.Texture.ScaleXY.CompileKeyframes(true));
                    AddKeyframedValues(EmissionNode.Texture.ScaleBase.CompileKeyframes(true));
                }
                else
                {
                    AddKeyframedValues(EmissionNode.Texture.ScaleBase.CompileKeyframes(false));
                }
            }

            //Node-specific values
            switch (NodeSpecificType)
            {
                case NodeSpecificType.SphericalDistribution:
                    AddKeyframedValues(EmitterNode.Size.CompileKeyframes(isSphere: true));
                    AddKeyframedValues(EmitterNode.Velocity.CompileKeyframes());
                    break;
                case NodeSpecificType.VerticalDistribution:
                    AddKeyframedValues(EmitterNode.Position.CompileKeyframes());
                    AddKeyframedValues(EmitterNode.Velocity.CompileKeyframes());
                    AddKeyframedValues(EmitterNode.Angle.CompileKeyframes());
                    break;
                case NodeSpecificType.ShapeAreaDistribution:
                case NodeSpecificType.ShapePerimeterDistribution:
                    AddKeyframedValues(EmitterNode.Position.CompileKeyframes());
                    AddKeyframedValues(EmitterNode.Velocity.CompileKeyframes());
                    AddKeyframedValues(EmitterNode.Angle.CompileKeyframes());
                    AddKeyframedValues(EmitterNode.Size.CompileKeyframes());
                    AddKeyframedValues(EmitterNode.Size2.CompileKeyframes());
                    break;
                case NodeSpecificType.AutoOriented:
                case NodeSpecificType.Default:
                case NodeSpecificType.Mesh:
                case NodeSpecificType.ShapeDraw:
                    AddKeyframedValues(EmissionNode.ActiveRotation.CompileKeyframes());
                    break;
            }
        
            //Modifers
            foreach(EMP_Modifier modifier in Modifiers)
            {
                modifier.CompileEmp();
            }
        }

        internal void AddKeyframedValues(EMP_KeyframedValue[] values)
        {
            for(int i = 0; i < values.Length; i++)
            {
                if(values[i] != null)
                {
                    if(KeyframedValues.Any(x => x.Parameter == values[i].Parameter && x.Component == values[i].Component))
                    {
                        throw new Exception($"EMP_File: KeyframedValue already exists (parameter = {values[i].Parameter}, component = {values[i].Component})");
                    }

                    KeyframedValues.Add(values[i]);
                }
            }
        }
    }

    [Serializable]
    public class ParticleTexture : INotifyPropertyChanged, ITextureRef
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public const int ENTRY_SIZE = 112;

        #region References
        private EmmMaterial _materialRef = null;
        public EmmMaterial MaterialRef
        {
            get
            {
                return _materialRef;
            }
            set
            {
                if (_materialRef != value)
                    _materialRef = value;
                NotifyPropertyChanged(nameof(MaterialRef));
            }
        }
        public AsyncObservableCollection<TextureEntry_Ref> TextureEntryRef { get; set; } = new AsyncObservableCollection<TextureEntry_Ref>();

        #endregion

        public byte I_00 { get; set; }
        public byte I_01 { get; set; }
        public byte I_02 { get; set; }
        public byte I_03 { get; set; }

        public float RenderDepth { get; set; }
        public int I_08 { get; set; }
        public int I_12 { get; set; }
        public ushort MaterialID { get; set; }

        //Colors
        public KeyframedColorValue Color1 { get; set; } = new KeyframedColorValue(1, 1, 1, KeyframedValueType.Color1);
        public KeyframedColorValue Color2 { get; set; } = new KeyframedColorValue(1, 1, 1, KeyframedValueType.Color2);
        public CustomColor Color_Variance { get; set; } = new CustomColor();
        public KeyframedFloatValue Color1_Transparency { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.Color1_Transparency);
        public KeyframedFloatValue Color2_Transparency { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.Color2_Transparency);

        public KeyframedFloatValue ScaleBase { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.ScaleBase, isScale: true);
        public float ScaleBase_Variance { get; set; }
        public KeyframedVector2Value ScaleXY { get; set; } = new KeyframedVector2Value(1f, 1f, KeyframedValueType.ScaleXY, isScale: true);
        public CustomVector4 ScaleXY_Variance { get; set; } = new CustomVector4();
        public float F_96 { get; set; }
        public float F_100 { get; set; }
        public float F_104 { get; set; }
        public float F_108 { get; set; }

        internal static ParticleTexture Parse(byte[] rawBytes, int offset, Parser parser)
        {
            int particleNodeOffset = offset - ParticleNode.ENTRY_SIZE;
            ParticleTexture newTexture = new ParticleTexture();

            newTexture.I_00 = rawBytes[offset + 0];
            newTexture.I_01 = rawBytes[offset + 1];
            newTexture.I_02 = rawBytes[offset + 2];
            newTexture.I_03 = rawBytes[offset + 3];
            newTexture.I_08 = BitConverter.ToInt32(rawBytes, offset + 8);
            newTexture.I_12 = BitConverter.ToInt32(rawBytes, offset + 12);
            newTexture.MaterialID = BitConverter.ToUInt16(rawBytes, offset + 16);
            newTexture.RenderDepth = BitConverter.ToSingle(rawBytes, offset + 4);

            //Scales (scale by 2 when reading, divide by 2 when writing)
            newTexture.ScaleBase.Constant = BitConverter.ToSingle(rawBytes, offset + 24) * 2f;
            newTexture.ScaleBase_Variance = BitConverter.ToSingle(rawBytes, offset + 28) * 2f;
            newTexture.ScaleXY.Constant.X = BitConverter.ToSingle(rawBytes, offset + 32) * 2f;
            newTexture.ScaleXY.Constant.Y = BitConverter.ToSingle(rawBytes, offset + 40) * 2f;
            newTexture.ScaleXY_Variance.X = BitConverter.ToSingle(rawBytes, offset + 36) * 2f;
            newTexture.ScaleXY_Variance.Y = BitConverter.ToSingle(rawBytes, offset + 44) * 2f;

            //Colors
            newTexture.Color1.Constant = new CustomColor(BitConverter.ToSingle(rawBytes, offset + 48), BitConverter.ToSingle(rawBytes, offset + 52), BitConverter.ToSingle(rawBytes, offset + 56), 1f);
            newTexture.Color_Variance = new CustomColor(BitConverter.ToSingle(rawBytes, offset + 64), BitConverter.ToSingle(rawBytes, offset + 68), BitConverter.ToSingle(rawBytes, offset + 72), BitConverter.ToSingle(rawBytes, offset + 76));
            newTexture.Color2.Constant = new CustomColor(BitConverter.ToSingle(rawBytes, offset + 80), BitConverter.ToSingle(rawBytes, offset + 84), BitConverter.ToSingle(rawBytes, offset + 88), 1f);
            newTexture.Color1_Transparency.Constant = BitConverter.ToSingle(rawBytes, offset + 60);
            newTexture.Color2_Transparency.Constant = BitConverter.ToSingle(rawBytes, offset + 92);

            newTexture.F_96 = BitConverter.ToSingle(rawBytes, offset + 96);
            newTexture.F_100 = BitConverter.ToSingle(rawBytes, offset + 100);
            newTexture.F_104 = BitConverter.ToSingle(rawBytes, offset + 104);
            newTexture.F_108 = BitConverter.ToSingle(rawBytes, offset + 108);

            int textureSamplerCount = BitConverter.ToInt16(rawBytes, offset + 18);
            int texturePointerList = BitConverter.ToInt32(rawBytes, offset + 20) + particleNodeOffset;

            if (texturePointerList != particleNodeOffset)
            {
                int textureEntrySize = EMP_TextureSamplerDef.GetSize(parser.EmpFile.Version);

                for (int e = 0; e < textureSamplerCount; e++)
                {
                    int textureOffset = BitConverter.ToInt32(rawBytes, texturePointerList + (4 * e));

                    if(textureOffset != 0)
                    {
                        int texIdx = (textureOffset + particleNodeOffset - parser.TextureSamplersOffset) / textureEntrySize;
                        newTexture.TextureEntryRef.Add(new TextureEntry_Ref(parser.EmpFile.Textures[texIdx]));
                    }
                    else
                    {
                        newTexture.TextureEntryRef.Add(new TextureEntry_Ref(null));
                    }
                }
            }

            //Full out texture list to size 2
            while(newTexture.TextureEntryRef.Count < 2)
            {
                newTexture.TextureEntryRef.Add(new TextureEntry_Ref());
            }

            return newTexture;
        }

        internal byte[] Write()
        {
            List<byte> bytes = new List<byte>(ENTRY_SIZE);

            int textureCount = GetTextureCount();

            bytes.AddRange(new byte[4] { I_00, I_01, I_02, I_03 });
            bytes.AddRange(BitConverter.GetBytes(RenderDepth));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_12));
            bytes.AddRange(BitConverter.GetBytes(MaterialID));
            bytes.AddRange(BitConverter.GetBytes((ushort)textureCount));
            bytes.AddRange(BitConverter.GetBytes((int)0)); //Offset, fill with 0 for now

            bytes.AddRange(BitConverter.GetBytes(ScaleBase.Constant / 2));
            bytes.AddRange(BitConverter.GetBytes(ScaleBase_Variance / 2));
            bytes.AddRange(BitConverter.GetBytes(ScaleXY.Constant.X / 2));
            bytes.AddRange(BitConverter.GetBytes(ScaleXY_Variance.X / 2));
            bytes.AddRange(BitConverter.GetBytes(ScaleXY.Constant.Y / 2));
            bytes.AddRange(BitConverter.GetBytes(ScaleXY_Variance.Y / 2));
            bytes.AddRange(BitConverter.GetBytes(Color1.Constant.R));
            bytes.AddRange(BitConverter.GetBytes(Color1.Constant.G));
            bytes.AddRange(BitConverter.GetBytes(Color1.Constant.B));
            bytes.AddRange(BitConverter.GetBytes(Color1_Transparency.Constant));
            bytes.AddRange(BitConverter.GetBytes(Color_Variance.R));
            bytes.AddRange(BitConverter.GetBytes(Color_Variance.G));
            bytes.AddRange(BitConverter.GetBytes(Color_Variance.B));
            bytes.AddRange(BitConverter.GetBytes(Color_Variance.A));
            bytes.AddRange(BitConverter.GetBytes(Color2.Constant.R));
            bytes.AddRange(BitConverter.GetBytes(Color2.Constant.G));
            bytes.AddRange(BitConverter.GetBytes(Color2.Constant.B));
            bytes.AddRange(BitConverter.GetBytes(Color2_Transparency.Constant));
            bytes.AddRange(BitConverter.GetBytes(F_96));
            bytes.AddRange(BitConverter.GetBytes(F_100));
            bytes.AddRange(BitConverter.GetBytes(F_104));
            bytes.AddRange(BitConverter.GetBytes(F_108));

            return bytes.ToArray();
        }

        public ParticleTexture Clone()
        {
            AsyncObservableCollection<TextureEntry_Ref> textureRefs = new AsyncObservableCollection<TextureEntry_Ref>();

            if(TextureEntryRef != null)
            {
                foreach(var textRef in TextureEntryRef)
                {
                    textureRefs.Add(new TextureEntry_Ref() { TextureRef = textRef.TextureRef });
                }
            }

            return new ParticleTexture()
            {
                I_00 = I_00,
                I_01 = I_01,
                I_02 = I_02,
                I_03 = I_03,
                I_08 = I_08,
                I_12 = I_12,
                MaterialID = MaterialID,
                RenderDepth = RenderDepth,
                F_100 = F_100,
                F_104 = F_104,
                F_108 = F_108,
                ScaleBase = ScaleBase,
                ScaleBase_Variance = ScaleBase_Variance,
                ScaleXY = ScaleXY,
                Color1 = Color1.Copy(),
                Color_Variance = Color_Variance.Copy(),
                Color2 = Color2.Copy(),
                Color1_Transparency = Color1_Transparency.Copy(),
                Color2_Transparency = Color2_Transparency.Copy(),
                F_96 = F_96,
                MaterialRef = MaterialRef,
                TextureEntryRef = textureRefs
            };
        }

        public static ParticleTexture GetNew()
        {
            return new ParticleTexture()
            {
                TextureEntryRef = new AsyncObservableCollection<TextureEntry_Ref>()
                {
                    //These are created because EMP Editor relies on there always being 2 texture references. Technically there can be any number of these, but only 2 are ever used by the game EMP files, so thats all that the EMP Editor exposes.
                    new TextureEntry_Ref(), new TextureEntry_Ref()
                }
            };
        }

        /// <summary>
        /// Returns the amount of textures to write back to binary. Any nulls at the end will be removed, but nulls inbetween will be preserved.
        /// </summary>
        public int GetTextureCount()
        {
            for (int i = TextureEntryRef.Count - 1; i >= 0; i--)
            {
                if (TextureEntryRef[i].TextureRef != null)
                    return i + 1;
            }

            return 0;
        }
    }

    [Serializable]
    public class ParticleEmitter
    {
        public enum ParticleEmitterShape
        {
            Circle = 0, //"ShapePerimeterDistribution" or "ShapeAreaDistribution"
            Square = 1, //"ShapePerimeterDistribution" or "ShapeAreaDistribution"
            Sphere, //"SphericalDistribution"
            Point //"VerticalDistribution"
        }

        public ParticleEmitterShape Shape { get; set; }

        //Differentiates emitter type 2 and 3 ("ShapePerimeterDistribution" and "ShapeAreaDistribution")
        public bool EmitFromArea { get; set; }

        //Position along Up vector. Used by all except "Sphere"
        public KeyframedFloatValue Position { get; set; } = new KeyframedFloatValue(0f, KeyframedValueType.PositionY);
        public float Position_Variance { get; set; }

        //Velocity along up vector. Used for all shapes.
        public KeyframedFloatValue Velocity { get; set; } = new KeyframedFloatValue(0f, KeyframedValueType.Velocity);
        public float Velocity_Variance { get; set; }

        //Used for all except "Cone"
        public KeyframedFloatValue Size { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.Size1);
        public float Size_Variance { get; set; }

        //Used for "Square"
        public KeyframedFloatValue Size2 { get; set; } = new KeyframedFloatValue(1f, KeyframedValueType.Size2);
        public float Size2_Variance { get; set; }

        //Angles the shape. Used by all except "Sphere"
        public KeyframedFloatValue Angle { get; set; } = new KeyframedFloatValue(0f, KeyframedValueType.Angle);
        public float Angle_Variance { get; set; }

        //Two unknowns on "Cone", and one on "Cirlce" and "Square"
        public float F_1 { get; set; }
        public float F_2 { get; set; }

        internal static ParticleEmitter Parse(byte[] bytes, int offset)
        {
            ParticleEmitter emitter = new ParticleEmitter();
            byte emitterType = bytes[offset + 36];

            offset += ParticleNode.ENTRY_SIZE;

            if(emitterType == 0)
            {
                //Cone
                emitter.Shape = ParticleEmitterShape.Point;
                emitter.Position.Constant = BitConverter.ToSingle(bytes, offset + 0);
                emitter.Position_Variance = BitConverter.ToSingle(bytes, offset + 4);
                emitter.Velocity.Constant = BitConverter.ToSingle(bytes, offset + 8);
                emitter.Velocity_Variance = BitConverter.ToSingle(bytes, offset + 12);
                emitter.Angle.Constant = BitConverter.ToSingle(bytes, offset + 16);
                emitter.Angle_Variance = BitConverter.ToSingle(bytes, offset + 20);
                emitter.F_1 = BitConverter.ToSingle(bytes, offset + 24);
                emitter.F_2 = BitConverter.ToSingle(bytes, offset + 28);
            }
            else if(emitterType == 1)
            {
                //Sphere
                emitter.Shape = ParticleEmitterShape.Sphere;
                emitter.Size.Constant = BitConverter.ToSingle(bytes, offset + 0);
                emitter.Size_Variance = BitConverter.ToSingle(bytes, offset + 4);
                emitter.Velocity.Constant = BitConverter.ToSingle(bytes, offset + 8);
                emitter.Velocity_Variance = BitConverter.ToSingle(bytes, offset + 12);
            }
            else if(emitterType == 2 || emitterType == 3)
            {
                //Cirlce and Square
                int shape = BitConverter.ToInt32(bytes, offset + 40);

                if (shape != 0 && shape != 1)
                    throw new ArgumentException($"ParticleEmitter.Parse: Invalid Shape parameter on Square/Circle type (Read {shape}, expected 0 or 1).");

                emitter.Shape = (ParticleEmitterShape)shape;
                emitter.EmitFromArea = emitterType == 2;
                emitter.Position.Constant = BitConverter.ToSingle(bytes, offset + 0);
                emitter.Position_Variance = BitConverter.ToSingle(bytes, offset + 4);
                emitter.Velocity.Constant = BitConverter.ToSingle(bytes, offset + 8);
                emitter.Velocity_Variance = BitConverter.ToSingle(bytes, offset + 12);
                emitter.Angle.Constant = BitConverter.ToSingle(bytes, offset + 16);
                emitter.Angle_Variance = BitConverter.ToSingle(bytes, offset + 20);
                emitter.Size.Constant = BitConverter.ToSingle(bytes, offset + 24);
                emitter.Size_Variance = BitConverter.ToSingle(bytes, offset + 28);
                emitter.Size2.Constant = BitConverter.ToSingle(bytes, offset + 32);
                emitter.Size2_Variance = BitConverter.ToSingle(bytes, offset + 36);
                emitter.F_1 = BitConverter.ToSingle(bytes, offset + 44);
            }

            return emitter;
        }

        internal byte[] Write(ref byte emitterType)
        {
            List<byte> bytes = new List<byte>();

            if(Shape == ParticleEmitterShape.Point)
            {
                emitterType = 0;
                bytes.AddRange(BitConverter.GetBytes(Position.Constant));
                bytes.AddRange(BitConverter.GetBytes(Position_Variance));
                bytes.AddRange(BitConverter.GetBytes(Velocity.Constant));
                bytes.AddRange(BitConverter.GetBytes(Velocity_Variance));
                bytes.AddRange(BitConverter.GetBytes(Angle.Constant));
                bytes.AddRange(BitConverter.GetBytes(Angle_Variance));
                bytes.AddRange(BitConverter.GetBytes(F_1));
                bytes.AddRange(BitConverter.GetBytes(F_2));
            }
            else if(Shape == ParticleEmitterShape.Sphere)
            {
                emitterType = 1;
                bytes.AddRange(BitConverter.GetBytes(Size.Constant));
                bytes.AddRange(BitConverter.GetBytes(Size_Variance));
                bytes.AddRange(BitConverter.GetBytes(Velocity.Constant));
                bytes.AddRange(BitConverter.GetBytes(Velocity_Variance));
            }
            else if(Shape == ParticleEmitterShape.Circle || Shape == ParticleEmitterShape.Square)
            {
                emitterType = (byte)(EmitFromArea ? 2 : 3);
                bytes.AddRange(BitConverter.GetBytes(Position.Constant));
                bytes.AddRange(BitConverter.GetBytes(Position_Variance));
                bytes.AddRange(BitConverter.GetBytes(Velocity.Constant));
                bytes.AddRange(BitConverter.GetBytes(Velocity_Variance));
                bytes.AddRange(BitConverter.GetBytes(Angle.Constant));
                bytes.AddRange(BitConverter.GetBytes(Angle_Variance));
                bytes.AddRange(BitConverter.GetBytes(Size.Constant));
                bytes.AddRange(BitConverter.GetBytes(Size_Variance));
                bytes.AddRange(BitConverter.GetBytes(Size2.Constant));
                bytes.AddRange(BitConverter.GetBytes(Size2_Variance));
                bytes.AddRange(BitConverter.GetBytes((int)Shape));
                bytes.AddRange(BitConverter.GetBytes(F_1));
            }

            return bytes.ToArray();
        }

        internal NodeSpecificType GetNodeType()
        {
            switch (Shape)
            {
                case ParticleEmitterShape.Sphere:
                    return NodeSpecificType.SphericalDistribution;
                case ParticleEmitterShape.Point:
                    return NodeSpecificType.VerticalDistribution;
                case ParticleEmitterShape.Circle:
                case ParticleEmitterShape.Square:
                    return EmitFromArea ? NodeSpecificType.ShapePerimeterDistribution : NodeSpecificType.ShapeAreaDistribution;
            }

            return NodeSpecificType.Null;
        }
    }

    [Serializable]
    public class ParticleEmission
    {
        public enum ParticleEmissionType
        {
            Plane,
            ConeExtrude,
            ShapeDraw,
            Mesh
        }

        public ParticleEmissionType EmissionType { get; set; }
        public ParticleBillboardType BillboardType { get; set; }
        public ParticleTexture Texture { get; set; } = ParticleTexture.GetNew();

        //Default:
        public bool BillboardEnabled => BillboardType != ParticleBillboardType.None;
        public bool VisibleOnlyOnMotion { get; set; } //Requires AutoRotation

        //Particle starts at this angle
        public float StartRotation { get; set; }
        public float StartRotation_Variance { get; set; }

        //Active rotation (degrees/second)
        public KeyframedFloatValue ActiveRotation { get; set; } = new KeyframedFloatValue(0f, KeyframedValueType.ActiveRotation);
        public float ActiveRotation_Variance { get; set; }

        //Defines a RotationAxis to be used. Only used in Mesh or when AutoRotation is disabled
        public CustomVector4 RotationAxis { get; set; } = new CustomVector4();

        //Specialised types:
        public ConeExtrude ConeExtrude { get; set; } = new ConeExtrude();
        public ShapeDraw ShapeDraw { get; set; } = new ShapeDraw();
        public ParticleStaticMesh Mesh { get; set; } = new ParticleStaticMesh();

        internal static ParticleEmission Parse(byte[] bytes, int offset, Parser parser)
        {
            ParticleEmission emission = new ParticleEmission();
            emission.BillboardType = (ParticleBillboardType)bytes[offset + 35];
            byte emissionType = bytes[offset + 36];
            int nodeOffset = offset;

            //Load texture
            offset += ParticleNode.ENTRY_SIZE;
            emission.Texture = ParticleTexture.Parse(bytes, offset, parser);

            //Load emission type
            offset += ParticleTexture.ENTRY_SIZE;

            if(emissionType == 0)
            {
                //AutoOriented
                emission.EmissionType = ParticleEmissionType.Plane;
                emission.VisibleOnlyOnMotion = false;
                emission.StartRotation = BitConverter.ToSingle(bytes, offset + 0);
                emission.StartRotation_Variance = BitConverter.ToSingle(bytes, offset + 4);
                emission.ActiveRotation.Constant = BitConverter.ToSingle(bytes, offset + 8);
                emission.ActiveRotation_Variance = BitConverter.ToSingle(bytes, offset + 12);
            }
            else if(emissionType == 1)
            {
                emission.EmissionType = ParticleEmissionType.Plane;
                emission.VisibleOnlyOnMotion = true;
            }
            else if(emissionType == 2)
            {
                emission.EmissionType = ParticleEmissionType.Plane;
                emission.BillboardType = ParticleBillboardType.None;
                emission.VisibleOnlyOnMotion = false;
                emission.StartRotation = BitConverter.ToSingle(bytes, offset + 0);
                emission.StartRotation_Variance = BitConverter.ToSingle(bytes, offset + 4);
                emission.ActiveRotation.Constant = BitConverter.ToSingle(bytes, offset + 8);
                emission.ActiveRotation_Variance = BitConverter.ToSingle(bytes, offset + 12);
                emission.RotationAxis.X = BitConverter.ToSingle(bytes, offset + 16);
                emission.RotationAxis.Y = BitConverter.ToSingle(bytes, offset + 20);
                emission.RotationAxis.Z = BitConverter.ToSingle(bytes, offset + 24);
            }
            else if(emissionType == 3)
            {
                emission.EmissionType = ParticleEmissionType.ConeExtrude;
                emission.BillboardType = ParticleBillboardType.None;

                emission.ConeExtrude.Duration = BitConverter.ToUInt16(bytes, offset + 0);
                emission.ConeExtrude.Duration_Variance = BitConverter.ToUInt16(bytes, offset + 2);
                emission.ConeExtrude.StepDuration = BitConverter.ToUInt16(bytes, offset + 4);
                emission.ConeExtrude.I_08 = BitConverter.ToUInt16(bytes, offset + 8);
                emission.ConeExtrude.I_10 = BitConverter.ToUInt16(bytes, offset + 10);

                int count = BitConverter.ToInt16(bytes, offset + 6) + 1;
                int listOffset = BitConverter.ToInt32(bytes, offset + 12) + nodeOffset;

                emission.ConeExtrude.Points.Clear();

                for (int i = 0; i < count; i++)
                {
                    emission.ConeExtrude.Points.Add(new ConeExtrudePoint()
                    {
                        WorldScaleFactor = BitConverter.ToSingle(bytes, listOffset + 0),
                        WorldScaleAdd = BitConverter.ToSingle(bytes, listOffset + 4),
                        WorldOffsetFactor = BitConverter.ToSingle(bytes, listOffset + 8),
                        WorldOffsetFactor2 = BitConverter.ToSingle(bytes, listOffset + 12)
                    });

                    listOffset += 16;
                }

                //Validation of the first point. This must always be 1, 0, 0, 0
                if(!MathHelpers.FloatEquals(emission.ConeExtrude.Points[0].WorldScaleFactor, 1f) && !MathHelpers.FloatEquals(emission.ConeExtrude.Points[0].WorldScaleAdd, 0f) &&
                   !MathHelpers.FloatEquals(emission.ConeExtrude.Points[0].WorldOffsetFactor, 0f) && !MathHelpers.FloatEquals(emission.ConeExtrude.Points[0].WorldOffsetFactor2, 0f))
                {
                    emission.ConeExtrude.Points.Insert(0, new ConeExtrudePoint(1, 0, 0, 0));
                }
            }
            else if(emissionType == 4)
            {
                emission.EmissionType = ParticleEmissionType.Mesh;
                emission.BillboardType = ParticleBillboardType.None;

                emission.StartRotation = BitConverter.ToSingle(bytes, offset + 0);
                emission.StartRotation_Variance = BitConverter.ToSingle(bytes, offset + 4);
                emission.ActiveRotation.Constant = BitConverter.ToSingle(bytes, offset + 8);
                emission.ActiveRotation_Variance = BitConverter.ToSingle(bytes, offset + 12);

                emission.RotationAxis.X = BitConverter.ToSingle(bytes, offset + 16);
                emission.RotationAxis.Y = BitConverter.ToSingle(bytes, offset + 20);
                emission.RotationAxis.Z = BitConverter.ToSingle(bytes, offset + 24);
                emission.Mesh.I_32 = BitConverter.ToInt32(bytes, offset + 32);
                emission.Mesh.I_40 = BitConverter.ToInt32(bytes, offset + 40);
                emission.Mesh.I_44 = BitConverter.ToInt32(bytes, offset + 44);

                int emgOffset = BitConverter.ToInt32(bytes, offset + 36) + nodeOffset;

                if(BitConverter.ToInt32(bytes, emgOffset) == EMG_File.EMG_SIGNATURE)
                    emission.Mesh.EmgFile = EMG_File.Read(bytes, emgOffset);

                //For testing purposes, keep using the old EMG code path for now. Parsing and saving the EMG file will produce a different binary result, which isn't good for comparisons
                //int emgSize = parser.CalculateEmgSize(emgOffset - nodeOffset, nodeOffset);
                //emission.Mesh.EmgBytes = bytes.GetRange(emgOffset, emgSize);

            }
            else if (emissionType == 5)
            {
                emission.EmissionType = ParticleEmissionType.ShapeDraw;
                emission.StartRotation = BitConverter.ToSingle(bytes, offset + 0);
                emission.StartRotation_Variance = BitConverter.ToSingle(bytes, offset + 4);
                emission.ActiveRotation.Constant = BitConverter.ToSingle(bytes, offset + 8);
                emission.ActiveRotation_Variance = BitConverter.ToSingle(bytes, offset + 12);

                emission.BillboardType = (ParticleBillboardType)BitConverter.ToUInt16(bytes, offset + 18);
                emission.ShapeDraw.I_24 = BitConverter.ToUInt16(bytes, offset + 24);
                emission.ShapeDraw.I_26 = BitConverter.ToUInt16(bytes, offset + 26);
                emission.ShapeDraw.I_28 = BitConverter.ToUInt16(bytes, offset + 28);
                emission.ShapeDraw.I_30 = BitConverter.ToUInt16(bytes, offset + 30);

                int pointCount = BitConverter.ToInt16(bytes, offset + 16) + 1;
                int pointsOffset = BitConverter.ToInt32(bytes, offset + 20) + nodeOffset;

                for (int i = 0; i < pointCount; i++)
                {
                    emission.ShapeDraw.Points.Add(new ShapeDrawPoint()
                    {
                        X = BitConverter.ToSingle(bytes, pointsOffset + 0),
                        Y = BitConverter.ToSingle(bytes, pointsOffset + 4)
                    });

                    pointsOffset += 8;
                }
            }

            return emission;
        }

        internal byte[] Write(ref byte emissionType, int nodeStart, Deserializer writer)
        {
            int textureStart = nodeStart + ParticleNode.ENTRY_SIZE;
            int currentRelativeOffset = ParticleNode.ENTRY_SIZE;;
            List<byte> bytes = new List<byte>();

            //Write TexturePart
            bytes.AddRange(Texture.Write());
            currentRelativeOffset += ParticleTexture.ENTRY_SIZE;

            if(bytes.Count != ParticleTexture.ENTRY_SIZE)
            {
                throw new Exception("TexturePart invalid size!");
            }

            //Write emission data
            switch (EmissionType)
            {
                case ParticleEmissionType.Plane:
                    if(BillboardEnabled && VisibleOnlyOnMotion)
                    {
                        //"VisibleOnSpeed"
                        emissionType = 1;
                    }
                    else if (BillboardEnabled)
                    {
                        //"AutoOriented"
                        emissionType = 0;
                        bytes.AddRange(BitConverter.GetBytes(StartRotation));
                        bytes.AddRange(BitConverter.GetBytes(StartRotation_Variance));
                        bytes.AddRange(BitConverter.GetBytes(ActiveRotation.Constant));
                        bytes.AddRange(BitConverter.GetBytes(ActiveRotation_Variance));
                        currentRelativeOffset += 16;
                    }
                    else
                    {
                        //"Default"
                        emissionType = 2;
                        bytes.AddRange(BitConverter.GetBytes(StartRotation));
                        bytes.AddRange(BitConverter.GetBytes(StartRotation_Variance));
                        bytes.AddRange(BitConverter.GetBytes(ActiveRotation.Constant));
                        bytes.AddRange(BitConverter.GetBytes(ActiveRotation_Variance));
                        bytes.AddRange(BitConverter.GetBytes(RotationAxis.X));
                        bytes.AddRange(BitConverter.GetBytes(RotationAxis.Y));
                        bytes.AddRange(BitConverter.GetBytes(RotationAxis.Z));
                        bytes.AddRange(BitConverter.GetBytes(0f)); //W is never used
                        currentRelativeOffset += 32;
                    }

                    break;
                case ParticleEmissionType.ConeExtrude:
                    emissionType = 3;

                    byte[] coneStruct = ConeExtrude.Write(currentRelativeOffset);
                    bytes.AddRange(coneStruct);

                    currentRelativeOffset += coneStruct.Length;
                    break;
                case ParticleEmissionType.Mesh:
                    emissionType = 4;

                    bytes.AddRange(BitConverter.GetBytes(StartRotation));
                    bytes.AddRange(BitConverter.GetBytes(StartRotation_Variance));
                    bytes.AddRange(BitConverter.GetBytes(ActiveRotation.Constant));
                    bytes.AddRange(BitConverter.GetBytes(ActiveRotation_Variance));
                    bytes.AddRange(BitConverter.GetBytes(RotationAxis.X));
                    bytes.AddRange(BitConverter.GetBytes(RotationAxis.Y));
                    bytes.AddRange(BitConverter.GetBytes(RotationAxis.Z));
                    bytes.AddRange(BitConverter.GetBytes(0f));
                    bytes.AddRange(BitConverter.GetBytes(Mesh.I_32));
                    bytes.AddRange(new byte[4]);
                    bytes.AddRange(BitConverter.GetBytes(Mesh.I_40));
                    bytes.AddRange(BitConverter.GetBytes(Mesh.I_44));

                    currentRelativeOffset += 48;
                    break;
                case ParticleEmissionType.ShapeDraw:
                    emissionType = 5;

                    bytes.AddRange(BitConverter.GetBytes(StartRotation));
                    bytes.AddRange(BitConverter.GetBytes(StartRotation_Variance));
                    bytes.AddRange(BitConverter.GetBytes(ActiveRotation.Constant));
                    bytes.AddRange(BitConverter.GetBytes(ActiveRotation_Variance));

                    int indexCount = ShapeDraw.Points.Count - 1;

                    bytes.AddRange(BitConverter.GetBytes((ushort)indexCount));
                    bytes.AddRange(BitConverter.GetBytes((ushort)BillboardType));
                    bytes.AddRange(BitConverter.GetBytes(currentRelativeOffset + 32));
                    bytes.AddRange(BitConverter.GetBytes(ShapeDraw.I_24));
                    bytes.AddRange(BitConverter.GetBytes(ShapeDraw.I_26));
                    bytes.AddRange(BitConverter.GetBytes(ShapeDraw.I_28));
                    bytes.AddRange(BitConverter.GetBytes(ShapeDraw.I_30));
                    currentRelativeOffset += 32;

                    foreach (ShapeDrawPoint point in ShapeDraw.Points)
                    {
                        bytes.AddRange(BitConverter.GetBytes(point.X));
                        bytes.AddRange(BitConverter.GetBytes(point.Y));
                        currentRelativeOffset += 8;
                    }
                    break;
            }

            //Write texture offsets
            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count + ParticleNode.ENTRY_SIZE), 20);

            int textureCount = Texture.GetTextureCount();

            for (int i = 0; i < textureCount; i++)
            {
                if (Texture.TextureEntryRef[i].TextureRef != null)
                {
                    int textureIdx = writer.EmpFile.Textures.IndexOf(Texture.TextureEntryRef[i].TextureRef);
                    
                    //Just a safe-guard - this idealy shouldn't happen
                    if(textureIdx == -1)
                    {
                        textureIdx = writer.EmpFile.Textures.Count;
                        writer.EmpFile.Textures.Add(Texture.TextureEntryRef[i].TextureRef);

                        writer.EmbTextureOffsets.Add(new List<int>());
                        writer.EmbTextureOffsets_Minus.Add(new List<int>());
                    }

                    writer.EmbTextureOffsets[textureIdx].Add(nodeStart + currentRelativeOffset);
                    writer.EmbTextureOffsets_Minus[textureIdx].Add(nodeStart);
                }

                bytes.AddRange(new byte[4]);
                currentRelativeOffset += 4;
            }

            //Write mesh data
            if(EmissionType == ParticleEmissionType.Mesh)
            {
                byte[] emgBytes;

                if(Mesh.EmgFile != null)
                {
                    emgBytes = Mesh.EmgFile.Write();
                }
                else if(Mesh.EmgBytes != null)
                {
                    emgBytes = Mesh.EmgBytes;
                }
                else
                {
                    emgBytes = new EMG_File().Write();
                }

                if(emgBytes != null)
                {
                    int meshOffset = ParticleTexture.ENTRY_SIZE + 36;
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count + ParticleNode.ENTRY_SIZE), meshOffset);

                    bytes.AddRange(emgBytes);
                }
            }

            return bytes.ToArray();
        }

        public ParticleEmission Clone()
        {
            return new ParticleEmission()
            {
                EmissionType = EmissionType,
                VisibleOnlyOnMotion = VisibleOnlyOnMotion,
                StartRotation = StartRotation,
                StartRotation_Variance = StartRotation_Variance,
                ActiveRotation = ActiveRotation.Copy(),
                ActiveRotation_Variance = ActiveRotation_Variance,
                RotationAxis = RotationAxis.Copy(),
                ConeExtrude = ConeExtrude.Copy(),
                ShapeDraw = ShapeDraw.Copy(),
                Mesh = Mesh.Copy(),
                Texture = Texture.Clone()
            };
        }

        internal NodeSpecificType GetNodeType()
        {
            switch (EmissionType)
            {
                case ParticleEmissionType.Plane:
                    if (BillboardEnabled && VisibleOnlyOnMotion)
                    {
                        return NodeSpecificType.AutoOriented_VisibleOnSpeed;
                    }
                    else if (BillboardEnabled)
                    {
                        return NodeSpecificType.AutoOriented;
                    }
                    else
                    {
                        return NodeSpecificType.Default;
                    }
                case ParticleEmissionType.ConeExtrude:
                    return NodeSpecificType.ConeExtrude;
                case ParticleEmissionType.Mesh:
                    return NodeSpecificType.Mesh;
                case ParticleEmissionType.ShapeDraw:
                    return NodeSpecificType.ShapeDraw;
                default:
                    return NodeSpecificType.Null;

            }
        }
    }

    [Serializable]
    public class ConeExtrude
    {
        public ushort Duration { get; set; }
        public ushort Duration_Variance { get; set; }
        public ushort StepDuration { get; set; }
        public ushort I_08 { get; set; } //0, 1
        public ushort I_10 { get; set; } //always 0
        public AsyncObservableCollection<ConeExtrudePoint> Points { get; set; } = new AsyncObservableCollection<ConeExtrudePoint>();

        public ConeExtrude()
        {
            Points.Add(new ConeExtrudePoint(1, 0, 0, 0));
        }

        internal byte[] Write(int currentRelativeOffset)
        {
            List<byte> bytes = new List<byte>();

            int indexCount = Points.Count - 1;

            bytes.AddRange(BitConverter.GetBytes(Duration));
            bytes.AddRange(BitConverter.GetBytes(Duration_Variance));
            bytes.AddRange(BitConverter.GetBytes(StepDuration));
            bytes.AddRange(BitConverter.GetBytes((ushort)indexCount));
            bytes.AddRange(BitConverter.GetBytes(I_08));
            bytes.AddRange(BitConverter.GetBytes(I_10));
            bytes.AddRange(BitConverter.GetBytes(currentRelativeOffset + 16));

            foreach (ConeExtrudePoint entry in Points)
            {
                bytes.AddRange(BitConverter.GetBytes(entry.WorldScaleFactor));
                bytes.AddRange(BitConverter.GetBytes(entry.WorldScaleAdd));
                bytes.AddRange(BitConverter.GetBytes(entry.WorldOffsetFactor));
                bytes.AddRange(BitConverter.GetBytes(entry.WorldOffsetFactor2));
            }

            //Size = bytes.count
            return bytes.ToArray();
        }
    }

    [Serializable]
    public class ConeExtrudePoint : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private float _scaleFactor = 0;
        private float _scaleAdd = 0;
        private float _offsetFactor = 0;
        private float _offsetFactor2;

        public float WorldScaleFactor
        {
            get => _scaleFactor;
            set
            {
                if (value != _scaleFactor)
                {
                    _scaleFactor = value;
                    NotifyPropertyChanged(nameof(WorldScaleFactor));
                    NotifyPropertyChanged(nameof(UndoableWorldScaleFactor));
                }
            }
        }
        public float WorldScaleAdd
        {
            get => _scaleAdd;
            set
            {
                if (value != _scaleAdd)
                {
                    _scaleAdd = value;
                    NotifyPropertyChanged(nameof(WorldScaleAdd));
                    NotifyPropertyChanged(nameof(UndoableWorldScaleAdd));
                }
            }
        }
        public float WorldOffsetFactor
        {
            get => _offsetFactor;
            set
            {
                if (value != _offsetFactor)
                {
                    _offsetFactor = value;
                    NotifyPropertyChanged(nameof(WorldOffsetFactor));
                    NotifyPropertyChanged(nameof(UndoableWorldOffsetFactor));
                }
            }
        }
        public float WorldOffsetFactor2
        {
            get => _offsetFactor2;
            set
            {
                if (value != _offsetFactor2)
                {
                    _offsetFactor2 = value;
                    NotifyPropertyChanged(nameof(WorldOffsetFactor2));
                    NotifyPropertyChanged(nameof(UndoableWorldOffsetFactor2));
                }
            }
        }

        #region Undoable
        public float UndoableWorldScaleFactor
        {
            get => WorldScaleFactor;
            set
            {
                if (!MathHelpers.FloatEquals(value, WorldScaleFactor))
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(WorldScaleFactor), this, WorldScaleFactor, value, "Cone Extrude -> WorldScaleFactor"));
                    WorldScaleFactor = value;
                }
            }
        }
        public float UndoableWorldScaleAdd
        {
            get => WorldScaleAdd;
            set
            {
                if (!MathHelpers.FloatEquals(value, WorldScaleAdd))
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(WorldScaleAdd), this, WorldScaleAdd, value, "Cone Extrude -> WorldScaleAdd"));
                    WorldScaleAdd = value;
                }
            }
        }
        public float UndoableWorldOffsetFactor
        {
            get => WorldOffsetFactor;
            set
            {
                if (!MathHelpers.FloatEquals(value, WorldOffsetFactor))
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(WorldOffsetFactor), this, WorldOffsetFactor, value, "Cone Extrude -> WorldOffsetFactor"));
                    WorldOffsetFactor = value;
                }
            }
        }
        public float UndoableWorldOffsetFactor2
        {
            get => WorldOffsetFactor2;
            set
            {
                if (!MathHelpers.FloatEquals(value, WorldOffsetFactor2))
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(WorldOffsetFactor2), this, WorldOffsetFactor2, value, "Cone Extrude -> WorldOffsetFactor2"));
                    WorldOffsetFactor2 = value;
                }
            }
        }

        #endregion

        public ConeExtrudePoint() { }

        public ConeExtrudePoint(float worldScaleFactor, float worldScaleAdd, float worldOffsetFactor, float worldOffsetFactor2)
        {
            WorldScaleFactor = worldScaleFactor;
            WorldScaleAdd = worldScaleAdd;
            WorldOffsetFactor = worldOffsetFactor;
            WorldOffsetFactor2 = worldOffsetFactor2;
        }

        public ConeExtrudePoint(byte[] bytes, int offset)
        {
            WorldScaleFactor = BitConverter.ToSingle(bytes, offset);
            WorldScaleAdd = BitConverter.ToSingle(bytes, offset + 4);
            WorldOffsetFactor = BitConverter.ToSingle(bytes, offset + 8);
            WorldOffsetFactor2 = BitConverter.ToSingle(bytes, offset + 12);
        }

        public override string ToString()
        {
            return $"{WorldScaleFactor}, {WorldScaleAdd}, {WorldOffsetFactor}, {WorldOffsetFactor2}";
        }

    }

    [Serializable]
    public class ShapeDraw
    {
        public ushort I_24 { get; set; } //always 0
        public ushort I_26 { get; set; } //always 0
        public ushort I_28 { get; set; } //always 0
        public ushort I_30 { get; set; } //always 0

        public AsyncObservableCollection<ShapeDrawPoint> Points { get; set; } = new AsyncObservableCollection<ShapeDrawPoint>();

        /// <summary>
        /// Removes each second <see cref="ShapeDrawPoint"/>, cutting the total amount in half. Will always keep the last 4 points.
        /// </summary>
        public List<IUndoRedo> ReducePoints()
        {
            return ReducePoints(Points);
        }

        public static List<IUndoRedo> ReducePoints(IList<ShapeDrawPoint> Points)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            const int MinPoints = 4;

            if (Points.Count > MinPoints)
            {
                for (int i = Points.Count - 2; i >= 0; i -= 2)
                {
                    if (Points.Count <= MinPoints || i < 0) break;

                    undos.Add(new UndoableListRemove<ShapeDrawPoint>(Points, Points[i], i));
                    Points.RemoveAt(i);
                }
            }

            return undos;
        }
    }

    [Serializable]
    public class ShapeDrawPoint : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private float _x = 0f;
        private float _y = 0f;

        public float X
        {
            get => _x;
            set
            {
                if(value != _x)
                {
                    _x = value;
                    NotifyPropertyChanged(nameof(X));
                    NotifyPropertyChanged(nameof(UndoablePixelSpaceX));
                    NotifyPropertyChanged(nameof(UndoableX));
                }
            }
        }
        public float Y
        {
            get => _y;
            set
            {
                if (value != _y)
                {
                    _y = value;
                    NotifyPropertyChanged(nameof(Y));
                    NotifyPropertyChanged(nameof(UndoablePixelSpaceY));
                    NotifyPropertyChanged(nameof(UndoableY));
                }
            }
        }

        #region Undoable
        //Size of the shape draw control in the editor.
        public const int SHAPE_DRAW_CONTROL_SIZE = 500;

        public float UndoablePixelSpaceX
        {
            get => ConvertPointToPixelSpace(X);
            set
            {
                float val = ConvertPointFromPixelSpace(value);
                if (!MathHelpers.FloatEquals(val, X))
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(X), this, X, val, "Shape Draw -> X"));
                    X = val;
                }
            }
        }
        public float UndoablePixelSpaceY
        {
            get => ConvertPointToPixelSpace(Y);
            set
            {
                float val = ConvertPointFromPixelSpace(value);
                if (!MathHelpers.FloatEquals(val, Y))
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(Y), this, Y, val, "Shape Draw -> Y"));
                    Y = val;
                }
            }
        }
        public float UndoableX
        {
            get => X;
            set
            {
                if (!MathHelpers.FloatEquals(value, X))
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(X), this, X, value, "Shape Draw -> X"));
                    X = value;
                }
            }
        }
        public float UndoableY
        {
            get => Y;
            set
            {
                if (!MathHelpers.FloatEquals(value, Y))
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(Y), this, Y, value, "Shape Draw -> Y"));
                    Y = value;
                }
            }
        }
        #endregion

        public ShapeDrawPoint() { }

        public ShapeDrawPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static float ConvertPointToPixelSpace(float point)
        {
            return (point + 1) / 2 * SHAPE_DRAW_CONTROL_SIZE;
        }

        public static float ConvertPointFromPixelSpace(float point)
        {
            return (point / SHAPE_DRAW_CONTROL_SIZE * 2) - 1;
        }
    
        public void PasteValues(ShapeDrawPoint point, List<IUndoRedo> undos = null)
        {
            if(undos != null)
            {
                undos.Add(new UndoablePropertyGeneric(nameof(X), this, X, point.X));
                undos.Add(new UndoablePropertyGeneric(nameof(Y), this, Y, point.Y));
            }

            X = point.X;
            Y = point.Y;
        }

        public override string ToString()
        {
            return $"X: {X}, Y: {Y}";
        }
    }

    [Serializable]
    public class ParticleStaticMesh
    {
        public int I_32 { get; set; } //always 0
        public int I_40 { get; set; } //always 0
        public int I_44 { get; set; } //always 0

        public EMG_File EmgFile { get; set; }
        public byte[] EmgBytes { get; set; }

    }

    [Serializable]
    public class EMP_KeyframedValue
    {
        public const byte PARAMETER_POSITION = 0;
        public const byte PARAMETER_ROTATION = 1;
        public const byte PARAMETER_SCALE = 2;
        public const byte PARAMETER_COLOR1 = 3;
        public const byte PARAMETER_COLOR2 = 4;
        public const byte COMPONENT_R = 0;
        public const byte COMPONENT_G = 1;
        public const byte COMPONENT_B = 2;
        public const byte COMPONENT_A = 3;
        public const byte COMPONENT_X = 0;
        public const byte COMPONENT_Y = 1;
        public const byte COMPONENT_Z = 2;

        /// <summary>
        /// This is the default <see cref="EMP_KeyframedValue"/> instance. Do not use this for anything other than representing a "null" or "empty" keyframed value.
        /// </summary>
        public static EMP_KeyframedValue Default { get; private set; } = new EMP_KeyframedValue() { IsDefault = true };
        /// <summary>
        /// This is the default <see cref="EMP_KeyframedValue"/> instance. Do not use this for anything other than representing a "null" or "empty" keyframed value.
        /// </summary>
        public bool IsDefault { get; private set; }

        //Base:
        public bool Interpolate { get; set; }
        public bool Loop { get; set; }
        public byte I_03 { get; set; }
        public float DefaultValue { get; set; } //Only used in EMP KeyframeGroups. Missing entirely in ECF.
        public ushort Duration => (ushort)(Keyframes.Count > 1 ? Keyframes.Max(x => x.Time) + 1 : 0);
        public byte Parameter { get; set; }
        public byte Component { get; set; }
        public AsyncObservableCollection<EMP_Keyframe> Keyframes { get; set; } = new AsyncObservableCollection<EMP_Keyframe>();

        //ECF specific value. Defines how interpolation will be applied to the extruded part.
        public ETR.ETR_InterpolationType ETR_InterpolationType { get; set; }

        public void SetParameters(int parameter, int component)
        {
            Parameter = (byte)parameter;
            Component = (byte)component;
        }

        /// <summary>
        /// Returns the Parameter, Component and Looped value as an Int16 for writing a binary EMP file.
        /// </summary>
        public short GetParameters()
        {
            byte _I_00 = Parameter;
            byte _I_01_a = Component;
            byte _I_01 = 0;

            _I_01 = Int4Converter.GetByte(_I_01_a, BitConverter_Ex.GetBytes(Interpolate), "Type0_Animation: Component", "Type0_Animation: Interpolated");

            return BitConverter.ToInt16(new byte[2] { _I_00, _I_01 }, 0);
        }

        public EMP_KeyframedValue Clone()
        {
            return this.Copy();
        }

        public void SetValue(int time, float value, List<IUndoRedo> undos = null)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Time == time)
                {
                    float oldValue = keyframe.Value;
                    keyframe.Value = value;

                    if (undos != null)
                        undos.Add(new UndoableProperty<EMP_Keyframe>(nameof(keyframe.Value), keyframe, oldValue, value));

                    return;
                }
            }

            //Keyframe doesn't exist. Add it.
            Keyframes.Add(new EMP_Keyframe() { Time = (ushort)time, Value = value });
        }

        public static byte GetParameter(KeyframedValueType value, bool isSphere = false)
        {
            switch (value)
            {
                case KeyframedValueType.Position:
                case KeyframedValueType.ECF_DiffuseColor:
                case KeyframedValueType.ECF_DiffuseTransparency:
                case KeyframedValueType.ETR_Color1:
                case KeyframedValueType.ETR_Color1_Transparency:
                case KeyframedValueType.Modifier_Axis:
                case KeyframedValueType.Modifier_DragStrength:
                    return 0;
                case KeyframedValueType.Rotation:
                case KeyframedValueType.ActiveRotation:
                case KeyframedValueType.ECF_SpecularColor:
                case KeyframedValueType.ECF_SpecularTransparency:
                case KeyframedValueType.ETR_Color2:
                case KeyframedValueType.ETR_Color2_Transparency:
                case KeyframedValueType.Modifier_Axis2:
                case KeyframedValueType.Modifier_Factor:
                case KeyframedValueType.Modifier_Direction:
                    return 1;
                case KeyframedValueType.ScaleBase:
                case KeyframedValueType.ScaleXY:
                case KeyframedValueType.PositionY:
                case KeyframedValueType.Velocity:
                case KeyframedValueType.Angle:
                case KeyframedValueType.ECF_AmbientColor:
                case KeyframedValueType.ECF_AmbientTransparency:
                case KeyframedValueType.ETR_Scale:
                case KeyframedValueType.Modifier_Radial:
                case KeyframedValueType.Modifier_RotationRate:
                case KeyframedValueType.Modifier_Unk7_20:
                case KeyframedValueType.Modifier_Unk7_21:
                    return 2;
                case KeyframedValueType.Color1:
                case KeyframedValueType.Color1_Transparency:
                case KeyframedValueType.Size1:
                    //Radius for sphere is at a different component.
                    return (byte)(isSphere ? 2 : 3);
                case KeyframedValueType.Size2:
                case KeyframedValueType.ECF_BlendingFactor:
                    return 3;
                case KeyframedValueType.Color2:
                case KeyframedValueType.Color2_Transparency:
                    return 4;
                default:
                    return 0;
            }
        }

        public static byte[] GetComponent(KeyframedValueType value, bool isScaleXyEnabled = false)
        {
            switch (value)
            {
                case KeyframedValueType.Position:
                case KeyframedValueType.Rotation:
                case KeyframedValueType.Color1:
                case KeyframedValueType.Color2:
                case KeyframedValueType.ECF_DiffuseColor:
                case KeyframedValueType.ECF_SpecularColor:
                case KeyframedValueType.ECF_AmbientColor:
                case KeyframedValueType.ETR_Color1:
                case KeyframedValueType.ETR_Color2:
                case KeyframedValueType.Modifier_Axis:
                case KeyframedValueType.Modifier_Axis2:
                case KeyframedValueType.Modifier_Direction:
                    return new byte[] { 0, 1, 2 };
                case KeyframedValueType.ActiveRotation:
                case KeyframedValueType.ECF_AmbientTransparency:
                case KeyframedValueType.ECF_DiffuseTransparency:
                case KeyframedValueType.ECF_SpecularTransparency:
                case KeyframedValueType.Color1_Transparency:
                case KeyframedValueType.Color2_Transparency:
                case KeyframedValueType.ETR_Color1_Transparency:
                case KeyframedValueType.ETR_Color2_Transparency:
                    return new byte[] { 3 };
                case KeyframedValueType.ScaleBase:
                    return isScaleXyEnabled ? new byte[] { 2 } : new byte[] { 0 };
                case KeyframedValueType.ScaleXY:
                    return new byte[] { 0, 1 };
                case KeyframedValueType.PositionY:
                case KeyframedValueType.Size1:
                case KeyframedValueType.ECF_BlendingFactor:
                case KeyframedValueType.ETR_Scale:
                case KeyframedValueType.Modifier_Factor:
                case KeyframedValueType.Modifier_DragStrength:
                case KeyframedValueType.Modifier_Radial:
                case KeyframedValueType.Modifier_Unk7_20:
                    return new byte[] { 0 };
                case KeyframedValueType.Velocity:
                case KeyframedValueType.Size2:
                case KeyframedValueType.Modifier_RotationRate:
                case KeyframedValueType.Modifier_Unk7_21:
                    return new byte[] { 1 };
                case KeyframedValueType.Angle:
                    return new byte[] { 2 };
                default:
                    return new byte[] { 0 };
            }
        }

    }

    [Serializable]
    public class EMP_Keyframe : IKeyframe, ISortable
    {
        public int SortID { get { return Time; } }

        public ushort Time { get; set; }
        public float Value { get; set; }

        public EMP_Keyframe() { }

        public EMP_Keyframe(ushort time, float value)
        {
            Time = time;
            Value = value;
        }

        public static float GetInterpolatedKeyframe<T>(IList<T> keyframes, int time, bool interpolationEnabled) where T : IKeyframe
        {
            int prev = -1;
            int next = -1;

            foreach (var keyframe in keyframes.OrderBy(x => x.Time))
            {
                if (keyframe.Time > prev && prev < time)
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
                return keyframes.FirstOrDefault(x => x.Time == next).Value;
            }

            //Same, but for next keyframe. We will use the prev keyframe here.
            if (next == -1 || prev == next)
            {
                return keyframes.FirstOrDefault(x => x.Time == prev).Value;
            }

            float factor = (time - prev) / (next - prev);
            float prevKeyframe = keyframes.FirstOrDefault(x => x.Time == prev).Value;
            float nextKeyframe = keyframes.FirstOrDefault(x => x.Time == next).Value;

            return interpolationEnabled ? MathHelpers.Lerp(prevKeyframe, nextKeyframe, factor) : prevKeyframe;
        }

        public override string ToString()
        {
            return $"Time: {Time}, Value: {Value}";
        }
    }

    [Serializable]
    public class EMP_Modifier
    {
        public enum EmpModifierType : byte
        {
            Translation = 4,
            Acceleration = 5,
            AngleTranslation = 6,
            AngleAcceleration = 7,
            PointLoop = 8,
            Vortex = 9,
            Jitter = 10,
            Drag = 11,
            Attract = 12
        }

        public enum EtrModifierType : byte
        {
            Translation = 2,
            Acceleration = 3, //Guess
            InverseTranslation = 4,
            InverseAcceleration = 5,
            Unk7 = 7
        }

        [Flags]
        public enum ModifierFlags : byte
        {
            Unk1 = 0x1,
            Unk2 = 0x2
        }

        public bool IsEtr { get; private set; }
        public byte Type { get; set; }
        public EmpModifierType EmpType => (EmpModifierType)Type;
        public EtrModifierType EtrType => (EtrModifierType)Type;
        public string TypeStr => IsEtr ? EtrType.ToString() : EmpType.ToString();
        public ModifierFlags Flags { get; set; } //"PRESERVE_NODE_FLAG"?
        public AsyncObservableCollection<EMP_KeyframedValue> KeyframedValues { get; set; } = new AsyncObservableCollection<EMP_KeyframedValue>();

        public KeyframedVector3Value Axis { get; set; }
        public KeyframedVector3Value Direction { get; set; }
        public KeyframedFloatValue Factor { get; set; }
        public KeyframedFloatValue Radial { get; set; }
        public KeyframedFloatValue RotationRate { get; set; }
        public KeyframedFloatValue DragStrength { get; set; }

        //ETR:
        public KeyframedVector3Value Axis2 { get; set; }
        public KeyframedFloatValue Unk7_20 { get; set; }
        public KeyframedFloatValue Unk7_21 { get; set; }

        public EMP_Modifier(bool isEtr)
        {
            IsEtr = isEtr;
            if (EffectContainer.EepkToolInterlop.FullDecompile)
            {
                Axis = new KeyframedVector3Value(0, 0, 0, KeyframedValueType.Modifier_Axis, isEtr, true);
                Axis2 = new KeyframedVector3Value(0, 0, 0, KeyframedValueType.Modifier_Axis2, isEtr, true);
                Direction = new KeyframedVector3Value(0, 0, 0, KeyframedValueType.Modifier_Direction, isEtr, true);
                Factor = new KeyframedFloatValue(0, KeyframedValueType.Modifier_Factor, isEtr, true);
                Radial = new KeyframedFloatValue(0, KeyframedValueType.Modifier_Radial, isEtr, true);
                RotationRate = new KeyframedFloatValue(0, KeyframedValueType.Modifier_RotationRate, isEtr, true);
                DragStrength = new KeyframedFloatValue(0, KeyframedValueType.Modifier_DragStrength, isEtr, true);
                Unk7_20 = new KeyframedFloatValue(0, KeyframedValueType.Modifier_Unk7_20, isEtr, true);
                Unk7_21 = new KeyframedFloatValue(0, KeyframedValueType.Modifier_Unk7_21, isEtr, true);
            }
        }

        public EMP_Modifier(byte type, bool isEtr) : this(isEtr)
        {
            Type = type;
        }

        public void DecompileEmp()
        {
            switch (EmpType)
            {
                case EmpModifierType.Translation:
                case EmpModifierType.Acceleration:
                case EmpModifierType.AngleTranslation:
                case EmpModifierType.AngleAcceleration:
                case EmpModifierType.PointLoop:
                case EmpModifierType.Jitter:
                case EmpModifierType.Attract:
                    Axis.DecompileKeyframes(GetKeyframedValues(0, 0, 1, 2));
                    Factor.DecompileKeyframes(GetKeyframedValues(1, 0));
                    break;
                case EmpModifierType.Vortex:
                    Axis.DecompileKeyframes(GetKeyframedValues(0, 0, 1, 2));
                    Direction.DecompileKeyframes(GetKeyframedValues(1, 0, 1, 2));
                    Radial.DecompileKeyframes(GetKeyframedValues(2, 0));
                    RotationRate.DecompileKeyframes(GetKeyframedValues(2, 1));
                    break;
                case EmpModifierType.Drag:
                    DragStrength.DecompileKeyframes(GetKeyframedValues(0, 0));
                    break;
            }
        }

        public void CompileEmp()
        {
            KeyframedValues.Clear();

            switch (EmpType)
            {
                case EmpModifierType.Translation:
                case EmpModifierType.Acceleration:
                case EmpModifierType.AngleTranslation:
                case EmpModifierType.AngleAcceleration:
                case EmpModifierType.PointLoop:
                case EmpModifierType.Jitter:
                case EmpModifierType.Attract:
                    AddKeyframedValues(Axis.CompileKeyframes());
                    AddKeyframedValues(Factor.CompileKeyframes());
                    break;
                case EmpModifierType.Vortex:
                    AddKeyframedValues(Axis.CompileKeyframes());
                    AddKeyframedValues(Direction.CompileKeyframes());
                    AddKeyframedValues(Radial.CompileKeyframes());
                    AddKeyframedValues(RotationRate.CompileKeyframes());
                    break;
                case EmpModifierType.Drag:
                    AddKeyframedValues(DragStrength.CompileKeyframes());
                    break;
            }
        }

        public void DecompileEtr()
        {
            switch (EtrType)
            {
                case EtrModifierType.Translation:
                case EtrModifierType.InverseAcceleration:
                case EtrModifierType.Acceleration:
                case EtrModifierType.InverseTranslation:
                    Axis.DecompileKeyframes(GetKeyframedValues(0, 0, 1, 2));
                    Factor.DecompileKeyframes(GetKeyframedValues(1, 0));
                    break;
                case EtrModifierType.Unk7:
                    Axis2.DecompileKeyframes(GetKeyframedValues(1, 0, 1, 2));
                    Unk7_20.DecompileKeyframes(GetKeyframedValues(2, 0));
                    Unk7_21.DecompileKeyframes(GetKeyframedValues(2, 1));
                    break;
            }
        }

        public void CompileEtr()
        {
            KeyframedValues.Clear();

            switch (EtrType)
            {
                case EtrModifierType.Translation:
                case EtrModifierType.InverseAcceleration:
                case EtrModifierType.Acceleration:
                case EtrModifierType.InverseTranslation:
                    AddKeyframedValues(Axis.CompileKeyframes());
                    AddKeyframedValues(Factor.CompileKeyframes());
                    break;
                case EtrModifierType.Unk7:
                    AddKeyframedValues(Axis2.CompileKeyframes());
                    AddKeyframedValues(Unk7_20.CompileKeyframes());
                    AddKeyframedValues(Unk7_21.CompileKeyframes());
                    break;
            }
        }

        public EMP_KeyframedValue[] GetKeyframedValues(int parameter, params int[] components)
        {
            EMP_KeyframedValue[] values = new EMP_KeyframedValue[components.Length];

            for (int i = 0; i < components.Length; i++)
            {
                EMP_KeyframedValue value = KeyframedValues.FirstOrDefault(x => x.Parameter == parameter && x.Component == components[i]);

                if (value != null)
                    values[i] = value;
                else
                    values[i] = EMP_KeyframedValue.Default;
            }

            return values;
        }

        private void AddKeyframedValues(EMP_KeyframedValue[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                {
                    if (KeyframedValues.Any(x => x.Parameter == values[i].Parameter && x.Component == values[i].Component))
                    {
                        throw new Exception($"EMP_Modifier: KeyframedValue already exists (parameter = {values[i].Parameter}, component = {values[i].Component})");
                    }

                    KeyframedValues.Add(values[i]);
                }
            }
        }

    }

    [Serializable]
    public class EMP_TextureSamplerDef : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public enum TextureRepitition : byte
        {
            Wrap = 0,
            Mirror = 1,
            Clamp = 2,
            Border = 3
        }

        public enum TextureFiltering : byte
        {
            None = 0,
            Point = 1,
            Linear = 2
        }

        public string TextureName => _textureRef != null ? TextureRef.Name : "No Texture Assigned";

        private EmbEntry _textureRef = null;
        public EmbEntry TextureRef
        {
            get => _textureRef;
            set
            {
                if (value != _textureRef)
                {
                    _textureRef = value;
                    NotifyPropertyChanged(nameof(TextureRef));
                    NotifyPropertyChanged(nameof(TextureName));
                }
            }
        }

        public byte EmbIndex { get; set; } = byte.MaxValue;
        public byte I_00 { get; set; }
        public byte I_02 { get; set; }
        public byte I_03 { get; set; }
        public TextureFiltering FilteringMin { get; set; }
        public TextureFiltering FilteringMag { get; set; }
        public TextureRepitition RepetitionU { get; set; }
        public TextureRepitition RepetitionV { get; set; }
        public byte RandomSymetryU { get; set; }
        public byte RandomSymetryV { get; set; }

        public EMP_ScrollState ScrollState { get; set; } = new EMP_ScrollState();

        #region LoadSave
        public static EMP_TextureSamplerDef Read(byte[] rawBytes, int TextureSamplersOffset, int index, VersionEnum version)
        {

            int textureOffset = TextureSamplersOffset + (index * EMP_TextureSamplerDef.GetSize(version));

            EMP_TextureSamplerDef textureEntry = new EMP_TextureSamplerDef();
            textureEntry.ScrollState.ScrollType = (EMP_ScrollState.ScrollTypeEnum)BitConverter.ToInt16(rawBytes, textureOffset + 10);
            textureEntry.I_00 = rawBytes[textureOffset + 0];
            textureEntry.EmbIndex = rawBytes[textureOffset + 1];
            textureEntry.I_02 = rawBytes[textureOffset + 2];
            textureEntry.I_03 = rawBytes[textureOffset + 3];
            textureEntry.FilteringMin = (EMP_TextureSamplerDef.TextureFiltering)rawBytes[textureOffset + 4];
            textureEntry.FilteringMag = (EMP_TextureSamplerDef.TextureFiltering)rawBytes[textureOffset + 5];
            textureEntry.RepetitionU = (EMP_TextureSamplerDef.TextureRepitition)rawBytes[textureOffset + 6];
            textureEntry.RepetitionV = (EMP_TextureSamplerDef.TextureRepitition)rawBytes[textureOffset + 7];
            textureEntry.RandomSymetryU = rawBytes[textureOffset + 8];
            textureEntry.RandomSymetryV = rawBytes[textureOffset + 9];

            switch (textureEntry.ScrollState.ScrollType)
            {
                case EMP_ScrollState.ScrollTypeEnum.Static:
                    {
                        EMP_ScrollKeyframe staticKeyframe = new EMP_ScrollKeyframe()
                        {
                            Time = 100,
                            ScrollU = BitConverter.ToSingle(rawBytes, textureOffset + 12),
                            ScrollV = BitConverter.ToSingle(rawBytes, textureOffset + 16),
                            ScaleU = BitConverter.ToSingle(rawBytes, textureOffset + 20),
                            ScaleV = BitConverter.ToSingle(rawBytes, textureOffset + 24),
                        };

                        if (version == VersionEnum.SDBH)
                        {
                            staticKeyframe.I_20 = BitConverter.ToInt32(rawBytes, textureOffset + 28);
                            staticKeyframe.I_24 = BitConverter.ToInt32(rawBytes, textureOffset + 32);
                        }

                        textureEntry.ScrollState.Keyframes[0] = staticKeyframe;
                    }
                    break;
                case EMP_ScrollState.ScrollTypeEnum.Speed:
                    {
                        textureEntry.ScrollState.ScrollSpeed_U = BitConverter.ToSingle(rawBytes, textureOffset + 12);
                        textureEntry.ScrollState.ScrollSpeed_V = BitConverter.ToSingle(rawBytes, textureOffset + 16);
                    }
                    break;
                case EMP_ScrollState.ScrollTypeEnum.SpriteSheet:
                    {
                        textureEntry.ScrollState.Keyframes.Clear();
                        int keyframeCount = BitConverter.ToInt16(rawBytes, textureOffset + 22);
                        int keyframeOffset = BitConverter.ToInt32(rawBytes, textureOffset + 24) + textureOffset + 12;

                        for (int i = 0; i < keyframeCount; i++)
                        {
                            EMP_ScrollKeyframe keyframe = new EMP_ScrollKeyframe()
                            {
                                Time = BitConverter.ToInt32(rawBytes, keyframeOffset + 0),
                                ScrollU = BitConverter.ToSingle(rawBytes, keyframeOffset + 4),
                                ScrollV = BitConverter.ToSingle(rawBytes, keyframeOffset + 8),
                                ScaleU = BitConverter.ToSingle(rawBytes, keyframeOffset + 12),
                                ScaleV = BitConverter.ToSingle(rawBytes, keyframeOffset + 16)
                            };

                            if (version == VersionEnum.SDBH)
                            {
                                keyframe.I_20 = BitConverter.ToInt32(rawBytes, keyframeOffset + 20);
                                keyframe.I_24 = BitConverter.ToInt32(rawBytes, keyframeOffset + 24);
                            }

                            textureEntry.ScrollState.Keyframes.Add(keyframe);

                            keyframeOffset += EMP_ScrollKeyframe.GetSize(version);
                        }
                    }
                    break;
            }

            return textureEntry;
        }

        public static void Write(List<byte> bytes, IList<EMP_TextureSamplerDef> textures, VersionEnum version)
        {
            List<int> KeyframeOffsets_ToReplace = new List<int>();

            for (int i = 0; i < textures.Count; i++)
            {
                //Filling in offsets
                for (int a = 0; a < textures[i].OffsetsToReplace.Count; a++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - textures[i].OffsetsToReplace_Relative[a]), textures[i].OffsetsToReplace[a]);
                }

                bytes.AddRange(new byte[10] { textures[i].I_00, textures[i].EmbIndex, textures[i].I_02, textures[i].I_03, (byte)textures[i].FilteringMin, (byte)textures[i].FilteringMag, (byte)textures[i].RepetitionU, (byte)textures[i].RepetitionV, textures[i].RandomSymetryU, textures[i].RandomSymetryV });
                bytes.AddRange(BitConverter.GetBytes((ushort)textures[i].ScrollState.ScrollType));

                switch (textures[i].ScrollState.ScrollType)
                {
                    case EMP_ScrollState.ScrollTypeEnum.Static:
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].ScrollU));
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].ScrollV));
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].ScaleU));
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].ScaleV));

                        if (version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].I_20));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[0].I_24));
                        }

                        KeyframeOffsets_ToReplace.Add(bytes.Count());
                        break;
                    case EMP_ScrollState.ScrollTypeEnum.Speed:
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.ScrollSpeed_U));
                        bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.ScrollSpeed_V));
                        bytes.AddRange(new byte[8]);

                        if (version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(new byte[8]);
                        }

                        KeyframeOffsets_ToReplace.Add(bytes.Count());
                        break;
                    case EMP_ScrollState.ScrollTypeEnum.SpriteSheet:
                        bytes.AddRange(new byte[10]);
                        int animationCount = (textures[i].ScrollState.Keyframes != null) ? textures[i].ScrollState.Keyframes.Count() : 0;
                        bytes.AddRange(BitConverter.GetBytes((short)animationCount));

                        KeyframeOffsets_ToReplace.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);

                        if (version == VersionEnum.SDBH)
                        {
                            bytes.AddRange(new byte[8]);
                        }
                        break;
                    default:
                        throw new InvalidDataException("Unknown ScrollState.ScrollType: " + textures[i].ScrollState.ScrollType);
                }
            }

            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i].ScrollState != null)
                {
                    if (textures[i].ScrollState.ScrollType == EMP_ScrollState.ScrollTypeEnum.SpriteSheet)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - KeyframeOffsets_ToReplace[i] + 12), KeyframeOffsets_ToReplace[i]);

                        for (int a = 0; a < textures[i].ScrollState.Keyframes.Count; a++)
                        {
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].Time));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].ScrollU));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].ScrollV));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].ScaleU));
                            bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].ScaleV));

                            if (version == VersionEnum.SDBH)
                            {
                                bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].I_20));
                                bytes.AddRange(BitConverter.GetBytes(textures[i].ScrollState.Keyframes[a].I_24));
                            }
                        }
                    }
                }
            }
        }

        private List<int> OffsetsToReplace = new List<int>();
        private List<int> OffsetsToReplace_Relative = new List<int>();

        public void AddOffset(int offset, int relative)
        {
            OffsetsToReplace.Add(offset);
            OffsetsToReplace_Relative.Add(relative);
        }

        public void ClearOffsets()
        {
            OffsetsToReplace.Clear();
            OffsetsToReplace_Relative.Clear();
        }
        #endregion
        public void ReplaceValues(EMP_TextureSamplerDef newValues, List<IUndoRedo> undos = null)
        {
            if (undos == null) undos = new List<IUndoRedo>();

            //Utils.CopyValues only copies primitives - this must be copied manually.
            undos.Add(new UndoableProperty<EMP_TextureSamplerDef>(nameof(TextureRef), this, TextureRef, newValues.TextureRef));
            TextureRef = newValues.TextureRef;

            if(newValues.ScrollState != null && ScrollState != null)
            {
                undos.Add(new UndoableProperty<EMP_ScrollState>(nameof(ScrollState.ScrollSpeed_U), ScrollState, ScrollState.ScrollSpeed_U, newValues.ScrollState.ScrollSpeed_U));
                undos.Add(new UndoableProperty<EMP_ScrollState>(nameof(ScrollState.ScrollSpeed_V), ScrollState, ScrollState.ScrollSpeed_V, newValues.ScrollState.ScrollSpeed_V));
                undos.Add(new UndoableProperty<EMP_ScrollState>(nameof(ScrollState.Keyframes), ScrollState, ScrollState.Keyframes, newValues.ScrollState.Keyframes));
                undos.Add(new UndoableProperty<EMP_ScrollState>(nameof(ScrollState.ScrollType), ScrollState, ScrollState.ScrollType, newValues.ScrollState.ScrollType));

                ScrollState.ScrollSpeed_U = newValues.ScrollState.ScrollSpeed_U;
                ScrollState.ScrollSpeed_V = newValues.ScrollState.ScrollSpeed_V;
                ScrollState.Keyframes = newValues.ScrollState.Keyframes;
                ScrollState.ScrollType = newValues.ScrollState.ScrollType;
            }

            //Copy remaining values
            undos.AddRange(Utils.CopyValues(this, newValues));

            undos.Add(new UndoActionPropNotify(this, true));
            this.NotifyPropsChanged();
        }

        public EMP_TextureSamplerDef Clone()
        {
            return new EMP_TextureSamplerDef()
            {
                I_00 = I_00,
                EmbIndex = EmbIndex,
                I_02 = I_02,
                I_03 = I_03,
                FilteringMin = FilteringMin,
                FilteringMag = FilteringMag,
                RepetitionU = RepetitionU,
                RepetitionV = RepetitionV,
                RandomSymetryU = RandomSymetryU,
                RandomSymetryV = RandomSymetryV,
                ScrollState = ScrollState.Copy(),
                TextureRef = TextureRef
            };
        }

        public static EMP_TextureSamplerDef GetNew()
        {
            AsyncObservableCollection<EMP_ScrollKeyframe> keyframes = new AsyncObservableCollection<EMP_ScrollKeyframe>
            {
                new EMP_ScrollKeyframe()
            };

            return new EMP_TextureSamplerDef()
            {
                RepetitionU = TextureRepitition.Wrap,
                RepetitionV = TextureRepitition.Wrap,
                ScrollState = new EMP_ScrollState()
                {
                    Keyframes = keyframes,
                }
            };
        }

        public bool Compare(EMP_TextureSamplerDef obj2)
        {
            return Compare(this, obj2);
        }

        public static bool Compare(EMP_TextureSamplerDef obj1, EMP_TextureSamplerDef obj2)
        {
            if (obj1.I_00 != obj2.I_00) return false;
            if (obj1.EmbIndex != obj2.EmbIndex) return false;
            if (obj1.I_02 != obj2.I_02) return false;
            if (obj1.I_03 != obj2.I_03) return false;
            if (obj1.FilteringMin != obj2.FilteringMin) return false;
            if (obj1.FilteringMag != obj2.FilteringMag) return false;
            if (obj1.RandomSymetryU != obj2.RandomSymetryU) return false;
            if (obj1.RepetitionU != obj2.RepetitionU) return false;
            if (obj1.RepetitionV != obj2.RepetitionV) return false;
            if (obj1.RandomSymetryV != obj2.RandomSymetryV) return false;

            if(obj1.ScrollState != null)
            {
                if (!obj1.ScrollState.Compare(obj2.ScrollState)) return false;
            }

            if(obj1.TextureRef != null && obj2.TextureRef != null)
            {
                if (!obj1.TextureRef.Compare(obj2.TextureRef, true)) return false;
            }
            else if (obj1.TextureRef == null && obj2.TextureRef == null)
            {

            }
            else
            {
                return false;
            }

            return true;
        }

        public static bool IsRepeatingTexture(EmbEntry embEntry, EffectContainer.AssetContainerTool assetContainer)
        {
            foreach(EffectContainer.Asset emp in assetContainer.Assets)
            {
                if(emp.Files?.Count > 0)
                {
                    foreach (var textureDef in emp.Files[0].EmpFile.Textures)
                    {
                        if(textureDef.TextureRef == embEntry)
                        {
                            if(textureDef.ScrollState != null)
                            {
                                if (textureDef.ScrollState.ScrollType != EMP_ScrollState.ScrollTypeEnum.Speed)
                                {
                                    foreach(EMP_ScrollKeyframe keyframe in textureDef.ScrollState.Keyframes)
                                    {
                                        if (keyframe.ScaleU > 1f || keyframe.ScaleV > 1f) return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static int GetSize(VersionEnum version)
        {
            return version == VersionEnum.SDBH ? 36 : 28;
        }
    }

    [Serializable]
    public class EMP_ScrollState
    {
        public enum ScrollTypeEnum : ushort
        {
            Static = 0,
            Speed = 1,
            SpriteSheet = 2
        }

        public ScrollTypeEnum ScrollType { get; set; }
        public float ScrollSpeed_U { get; set; }
        public float ScrollSpeed_V { get; set; }

        public AsyncObservableCollection<EMP_ScrollKeyframe> Keyframes { get; set; }

        public EMP_ScrollState()
        {
            Keyframes = new AsyncObservableCollection<EMP_ScrollKeyframe>();
            Keyframes.Add(new EMP_ScrollKeyframe(100, 0, 0, 1, 1));
        }

        public bool Compare(EMP_ScrollState obj2)
        {
            return Compare(this, obj2);
        }

        public static bool Compare(EMP_ScrollState obj1, EMP_ScrollState obj2)
        {
            if (obj2 == null && obj1 == null) return true;
            if (obj1 != null && obj2 == null) return false;
            if (obj2 != null && obj1 == null) return false;

            if (obj1.ScrollSpeed_U != obj2.ScrollSpeed_U) return false;
            if (obj1.ScrollSpeed_V != obj2.ScrollSpeed_V) return false;
            if (obj1.ScrollType != obj2.ScrollType) return false;

            if (obj1.Keyframes != null && obj2.Keyframes != null)
            {
                if (obj1.Keyframes.Count != obj2.Keyframes.Count) return false;

                for(int i = 0; i < obj1.Keyframes.Count; i++)
                {
                    if (!obj1.Keyframes[i].Compare(obj2.Keyframes[i])) return false;
                }
            }
            else if(obj1.Keyframes == null && obj2.Keyframes == null)
            {
                //Both are null. OK
            }
            else
            {
                //Mismatch
                return false;
            }

            return true;
        }
    }

    [Serializable]
    public class EMP_ScrollKeyframe : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private int _time = 0;
        private float _scrollU = 0f;
        private float _scrollV = 0f;
        private float _scaleU = 0f;
        private float _scaleV = 0f;

        public int Time
        {
            get => _time;
            set
            {
                if(value != _time)
                {
                    _time = value;
                    NotifyPropertyChanged(nameof(Time));
                }
            }
        }
        public float ScrollU
        {
            get => _scrollU;
            set
            {
                if (value != _scrollU)
                {
                    _scrollU = value;
                    NotifyPropertyChanged(nameof(ScrollU));
                }
            }
        }
        public float ScrollV
        {
            get => _scrollV;
            set
            {
                if (value != _scrollV)
                {
                    _scrollV = value;
                    NotifyPropertyChanged(nameof(ScrollV));
                }
            }
        }
        public float ScaleU
        {
            get => _scaleU;
            set
            {
                if (value != _scaleU)
                {
                    _scaleU = value;
                    NotifyPropertyChanged(nameof(ScaleU));
                }
            }
        }
        public float ScaleV
        {
            get => _scaleV;
            set
            {
                if (value != _scaleV)
                {
                    _scaleV = value;
                    NotifyPropertyChanged(nameof(ScaleV));
                }
            }
        }

        //Added in newer versions of EMP (SDBH and Breakers)
        public int I_20 { get; set; } //0, 3
        public int I_24 { get; set; } //0, 60

        public EMP_ScrollKeyframe() { }

        public EMP_ScrollKeyframe(int time, float scrollX, float scrollY, float scaleX, float scaleY)
        {
            Time = time;
            ScrollU = scrollX;
            ScrollV = scrollY;
            ScaleU = scaleX;
            ScaleV = scaleY;
        }

        /// <summary>
        /// Create sprite sheet keyframes out of the specified columns and rows.
        /// </summary>
        /// <param name="max">The maximum amount of keyframes.</param>
        public static List<EMP_ScrollKeyframe> GenerateSpriteSheet(int columns, int rows, int max = -1, int defaultTime = 50)
        {
            List<EMP_ScrollKeyframe> keyframes = new List<EMP_ScrollKeyframe>();

            for(int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    if (keyframes.Count == max) return keyframes;

                    keyframes.Add(new EMP_ScrollKeyframe
                    {
                        Time = defaultTime,
                        ScrollU = column / columns,
                        ScrollV = row / rows,
                        ScaleU = 1f / columns,
                        ScaleV = 1f / rows
                    });
                }
            }

            return keyframes;
        }

        public EMP_ScrollKeyframe Clone()
        {
            return new EMP_ScrollKeyframe()
            {
                Time = Time,
                ScaleU = ScaleU,
                ScrollV = ScrollV,
                ScrollU = ScrollU,
                ScaleV = ScaleV
            };
        }

        public bool Compare(EMP_ScrollKeyframe obj2)
        {
            return Compare(this, obj2);
        }

        public static bool Compare(EMP_ScrollKeyframe obj1, EMP_ScrollKeyframe obj2)
        {
            if (obj1.Time != obj2.Time) return false;
            if (obj1.ScrollU != obj2.ScrollU) return false;
            if (obj1.ScrollV != obj2.ScrollV) return false;
            if (obj1.ScaleU != obj2.ScaleU) return false;
            if (obj1.ScaleV != obj2.ScaleV) return false;
            if (obj1.I_20 != obj2.I_20) return false;
            if (obj1.I_24 != obj2.I_24) return false;

            return true;
        }

        public static int GetSize(VersionEnum version)
        {
            return version == VersionEnum.SDBH ? 28 : 20;
        }

        public override string ToString()
        {
            return $"{Time}: {ScrollU}, {ScrollV}, {ScaleU}, {ScaleV}, {I_20}, {I_24}";
        }
    }

    [Serializable]
    public class TextureEntry_Ref : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private EMP_TextureSamplerDef _textureRef = null;
        public EMP_TextureSamplerDef TextureRef
        {
            get
            {
                return _textureRef;
            }
            set
            {
                if (value != _textureRef)
                {
                    _textureRef = value;
                    NotifyPropertyChanged(nameof(TextureRef));
                    NotifyPropertyChanged(nameof(UndoableTextureRef));
                }
            }
        }
    
        public EMP_TextureSamplerDef UndoableTextureRef
        {
            get
            {
                return TextureRef;
            }
            set
            {
                if(TextureRef != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<TextureEntry_Ref>(nameof(TextureRef), this, TextureRef, value, "Texture Ref"));
                    TextureRef = value;
                    NotifyPropertyChanged(nameof(TextureRef));
                    NotifyPropertyChanged(nameof(UndoableTextureRef));
                }
            }
        }
   
        public TextureEntry_Ref() { }

        public TextureEntry_Ref(EMP_TextureSamplerDef textureSampler)
        {
            _textureRef = textureSampler;
        }
    
    }

    //Interface
    public interface IKeyframe
    {
        ushort Time { get; set; }
        float Value { get; set; }
    }

    /// <summary>
    /// The exact node type. Includes all Emitters and Emissions.
    /// </summary>
    public enum NodeSpecificType
    {
        Null,
        VerticalDistribution,
        SphericalDistribution,
        ShapePerimeterDistribution,
        ShapeAreaDistribution,
        AutoOriented,
        AutoOriented_VisibleOnSpeed,
        Default,
        ConeExtrude,
        Mesh,
        ShapeDraw
    }

    /// <summary>
    /// Used by the <see cref="ParticleNode.GetNew(NewNodeType, IList{ParticleNode})"/> method for creating new nodes.
    /// </summary>
    public enum NewNodeType
    {
        Empty = 0,
        //Emissions:
        Plane = 1,
        Shape = 2,
        Extrude = 3,
        Mesh = 4,
        //Emitters:
        EmitterCirlce = 5,
        EmitterSquare = 6,
        EmitterSphere = 7,
        EmitterPoint = 8
    }

    public enum KeyframedValueType
    {
        //All known keyframed values and their associated Parameter/Component values. These apply to main KeyframedValues only
        //Formated where the first number is the Parameter, and any following are the Components

        Position, //0, X = 0, Y = 1, Z = 2 (All node types)
        Rotation, //1, X = 0, Y = 1, Z = 2 (All node types)
        ScaleBase, //When UseScale2, its 2, 2, otherwise its 2, 0 (All Emisions)
        ScaleXY, //When UseScale2, its 2, X = 0, Y = 1, otherwise its not used (All Emissions)
        Color1, //3, R=0, G=1, B=2 (All Emissions)
        Color2, //4, R=0, G=1, B=2, A=3 (All Emissions)
        Color1_Transparency, //3, 3 (All Emissions)
        Color2_Transparency, //4, 3 (All Emissions)
        ActiveRotation, //1, 3 (AutoOriented, Default, Mesh, ShapeDraw)
        PositionY, //2, 0 (ShapeAreaDist, ShapePerimeterDist, VerticalDist)
        Velocity, //2, 1 (ShapeAreaDist, ShapePerimeterDist, SphereDist, VerticalDist)
        Angle, //2, 2 (ShapeAreaDist, ShapePerimeterDist, VerticalDist)
        Size1, //3, 0 (ShapeAreaDist, ShapePerimeterDist) OR 2, 0 (SphereDist)
        Size2, //3, 1 (ShapeAreaDist, ShapePerimeterDist)

        ECF_DiffuseColor,
        ECF_SpecularColor,
        ECF_AmbientColor,
        ECF_DiffuseTransparency,
        ECF_SpecularTransparency,
        ECF_AmbientTransparency,
        ECF_BlendingFactor,

        ETR_Color1,
        ETR_Color2,
        ETR_Color1_Transparency,
        ETR_Color2_Transparency,
        ETR_Scale,

        //Modifiers (groups):
        Modifier_Axis, //0, vector3
        Modifier_Axis2, //1, vetor3
        Modifier_Factor, //1, float
        Modifier_Direction, //1, vector3
        Modifier_Radial, //2, 0 = float
        Modifier_RotationRate, //2, 1 = float
        Modifier_DragStrength, //0, float
        Modifier_Unk7_20, //2, 0 = float (unknown type in ETR)
        Modifier_Unk7_21, //2, 1 = float (unknown type in ETR)
    }

    public interface ITexture
    {
        AsyncObservableCollection<EMP_TextureSamplerDef> Textures { get; set; }

        List<ITextureRef> GetNodesThatUseTexture(EMP_TextureSamplerDef embEntryRef);
        void RefactorTextureRef(EMP_TextureSamplerDef oldTextureRef, EMP_TextureSamplerDef newTextureRef, List<IUndoRedo> undos);
        void RemoveTextureReferences(EMP_TextureSamplerDef textureRef, List<IUndoRedo> undos = null);
    }

    public interface ITextureRef
    {
        AsyncObservableCollection<TextureEntry_Ref> TextureEntryRef { get; set; }
    }
}
