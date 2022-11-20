using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using System.IO;
using Xv2CoreLib.EffectContainer;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.Resource;

namespace Xv2CoreLib.EEPK
{
    [Serializable]
    public enum AssetType : ushort
    {
        EMO = 0,
        PBIND = 1,
        TBIND = 2,
        LIGHT = 3,
        CBIND = 4
    }

    [Serializable]
    [YAXSerializeAs("EEPK")]
    public class EEPK_File : ISorting
    {
        public const int EEPK_SIGNATURE = 1263551779;

        public int Version = 37568;
        [YAXSerializeAs("Containers")]
        [YAXDontSerializeIfNull]
        public List<AssetContainer> Assets { get; set; } = new List<AssetContainer>();
        [YAXDontSerializeIfNull]
        public List<Effect> Effects { get; set; } = new List<Effect>();

        public void SortEntries()
        {
            Effects = Sorting.SortEntries(Effects);
        }

        /// <summary>
        /// Loads the specified eepk file. It can be in either binary or xml format. 
        /// 
        /// If a file can not be found at the specified location, then a empty one will be returned.
        /// </summary>
        public static EEPK_File LoadEepk(string path, bool returnEmptyIfNotValid = true)
        {
            if (Path.GetExtension(path) == ".eepk")
            {
                return new Xv2CoreLib.EEPK.Parser(path, false).GetEepkFile();
            }
            else if (Path.GetExtension(path) == ".xml" && Path.GetExtension(Path.GetFileNameWithoutExtension(path)) == ".eepk")
            {
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.EMM.EMM_File), YAXSerializationOptions.DontSerializeNullObjects);
                return (Xv2CoreLib.EEPK.EEPK_File)serializer.DeserializeFromFile(path);
            }
            else
            {
                if (returnEmptyIfNotValid)
                {
                    return new EEPK_File()
                    {
                        Assets = new List<AssetContainer>(),
                        Effects = new List<Effect>()
                    };
                }
                else
                {
                    throw new FileNotFoundException("An .eppk could not be found at the specified location.");
                }

            }
        }

        public static EEPK_File LoadEepk(byte[] bytes)
        {
            return new Parser(bytes).eepkFile;
        }


        public int IndexOfContainer(AssetType _containerType)
        {
            for (int i = 0; i < Assets.Count(); i++)
            {
                if (Assets[i].I_16 == _containerType)
                {
                    return i;
                }
            }

            return -1;
        }

        public void SaveXmlEepkFile(string saveLocation)
        {
            if (!Directory.Exists(Path.GetDirectoryName(saveLocation)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveLocation));
            }

            YAXSerializer serializer = new YAXSerializer(typeof(EEPK_File));
            serializer.SerializeToFile(this, saveLocation);
        }

        public void SaveBinaryEepkFile(string saveLocation)
        {
            if (!Directory.Exists(Path.GetDirectoryName(saveLocation)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveLocation));
            }
            new Deserializer(this, saveLocation);
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public AssetContainer GetContainer(AssetType type)
        {
            if (Assets == null) throw new Exception("Assets was null.");

            //Check if it exists, and return it if it does.
            foreach (var container in Assets)
            {
                if (container.I_16 == type) return container;
            }

            return null;
        }

        public void SetContainer(AssetType type, AssetContainer container)
        {
            if (Assets == null) throw new Exception("Assets was null.");

            for (int i = 0; i < Assets.Count; i++)
            {
                if (Assets[i].I_16 == type)
                {
                    Assets[i] = container;
                    return;
                }
            }

            //A container of this type wasn't found in the eepk, so we must add it.
            Assets.Add(container);
        }

        public int NextFreeId(int minId = 100)
        {
            int id = minId;
            while (Effects.Any(x => x.SortID == id))
                id++;
            return id;
        }

    }

    [Serializable]
    [YAXSerializeAs("Container")]
    public class AssetContainer
    {

        [YAXAttributeFor("AssetSpawnLimit")]
        [YAXSerializeAs("value")]
        public int AssetSpawnLimit { get; set; } //Limits how many assets of this type that can be spawned. The number doesn't equal an exact amount of assets, and multiple users of an EEPK increase the limit. But generally, higher number = more assets.

        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte I_04 { get; set; } // int8
        [YAXAttributeFor("I_05")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte I_05 { get; set; } // int8
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte I_06 { get; set; } // int8
        [YAXAttributeFor("I_07")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public byte I_07 { get; set; } // int8

        [YAXAttributeFor("AssetListLimit")]
        [YAXSerializeAs("value")]
        public int AssetListLimit { get; set; }  // Limits the amount of assets that can be loaded by the game before crashing.
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }  // int32
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public AssetType I_16 { get; set; }  // int16
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("values")]
        public string[] FILES { get; set; } //the container files, made into an Array now
        [YAXSerializeAs("Asset Entries")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Container_Entry")]
        public List<Asset_Entry> AssetEntries { get; set; } = new List<Asset_Entry>();

        //I_30 = Num of Asset Entries
        //I_32 = Offset to Data Block start (Asset_Entry, relative)
        //I_36 = Offset to Asset Container string 1
        //I_40 = Offset to Asset Container string 2
        //I_44 = Offset to Asset Container string 3

        /// <summary>
        /// Creates a duplicate of the Asset Container, but without the Asset_Entries
        /// </summary>
        public AssetContainer Clone()
        {
            return new AssetContainer()
            {
                AssetSpawnLimit = AssetSpawnLimit,
                I_04 = I_04,
                I_05 = I_05,
                I_06 = I_06,
                I_07 = I_07,
                AssetListLimit = AssetListLimit,
                I_12 = I_12,
                I_16 = I_16,
                FILES = FILES,
                AssetEntries = new List<Asset_Entry>()
            };
        }

        public int IndexOf(string file)
        {
            if (AssetEntries != null)
            {
                for (int i = 0; i < AssetEntries.Count(); i++)
                {
                    if (AssetEntries[i].FILES[0].Path == file)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public static AssetContainer Default()
        {
            return new AssetContainer()
            {
                AssetListLimit = 0x9c40,
                AssetEntries = new List<Asset_Entry>(),
                FILES = new string[3] { "NULL", "NULL", "NULL" }
            };

        }
    }

    [Serializable]
    [YAXSerializeAs("Container_Entry")]
    public class Asset_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public int ReadOnly_Index { get; set; }

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        //I_03 (byte) = Num of Strings
        //Equalize I_03 to Number of File Strings that are NOT = "NULL"
        //Also, if Number of File Strings that are not = "NULL" is greater than UNK_NUMs that are not = "NULL", then automatically add the next number to keep them in sync.
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "File")]
        public List<Asset_File> FILES { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public string[] UNK_NUMBERS;
        //0 = emo, 1 = emm, 2 = emb, 3 = ema, 4 = emp, 5 = etr, 6 = ???, 7 = ecf, 255 = no file
    }

    [YAXSerializeAs("File")]
    public class Asset_File
    {
        [YAXAttributeForClass]
        public string Path { get; set; }
    }

    [Serializable]
    [YAXSerializeAs("Effect")]
    public class Effect : IInstallable, INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region IInstallable
        [YAXDontSerialize]
        public int SortID { get { return IndexNum; } set { IndexNum = (ushort)value; } }
        [YAXDontSerialize]
        public string Index { get { return IndexNum.ToString(); } set { IndexNum = ushort.Parse(value); } }
        #endregion

        #region EepkOrganiser
        //Namelist
        private string _nameList = null;
        [YAXDontSerialize]
        public string NameList
        {
            get
            {
                return this._nameList;
            }
            set
            {
                if (value != this._nameList)
                {
                    this._nameList = value;
                    NotifyPropertyChanged("NameList");
                }
            }
        }

        //UI Code.
        private ushort _index = 0;
        [NonSerialized]
        private bool _IdIncreaseEqualized = false;
        [NonSerialized]
        private ushort _viewIdIncrease = 0;
        [YAXDontSerialize]
        public ushort ImportIdIncrease
        {
            get
            {
                if (!_IdIncreaseEqualized)
                {
                    _IdIncreaseEqualized = true;
                    _viewIdIncrease = _index;
                }
                return this._viewIdIncrease;
            }
            set
            {
                if (value != this._viewIdIncrease)
                {
                    this._viewIdIncrease = value;
                    NotifyPropertyChanged("ImportIdIncrease");
                }
            }
        }
        private bool _isSelected = true;
        [YAXDontSerialize]
        public bool IsSelected
        {
            get
            {
                return this._isSelected;
            }
            set
            {
                if (value != this._isSelected)
                {
                    this._isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }
        #endregion

        //Props
        [YAXSerializeAs("ID")]
        [YAXAttributeForClass]
        public ushort IndexNum
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
                    NotifyPropertyChanged(nameof(IndexNum));
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_02")]
        public ushort I_02 { get; set; }
        [YAXSerializeAs("EffectParts")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EffectPart")]
        public AsyncObservableCollection<EffectPart> EffectParts
        {
            get
            {
                return this._effectParts;
            }
            set
            {
                if (value != this._effectParts)
                {
                    this._effectParts = value;
                    NotifyPropertyChanged(nameof(EffectParts));
                }
            }
        }
        private AsyncObservableCollection<EffectPart> _effectParts = null;

        //ViewModel
        [NonSerialized]
        private ObservableCollection<EffectPart> _selectedEffectParts = new ObservableCollection<EffectPart>();
        [YAXDontSerialize]
        public ObservableCollection<EffectPart> SelectedEffectParts
        {
            get
            {
                return this._selectedEffectParts;
            }
        }
        [NonSerialized]
        private EffectPart _selectedEffectPart = null;
        [YAXDontSerialize]
        public EffectPart SelectedEffectPart
        {
            get
            {
                return this._selectedEffectPart;
            }
            set
            {
                if (value != this._selectedEffectPart)
                {
                    this._selectedEffectPart = value;
                    NotifyPropertyChanged(nameof(SelectedEffectPart));
                }
            }
        }

        //Installer
        [YAXDontSerialize]
        public VfxPackageExtendedEffect ExtendedEffectData { get; set; }

        #region Undoable
        [YAXDontSerialize]
        public ushort UndoableId
        {
            get { return IndexNum; }
            set
            {
                if (value != IndexNum)
                {
                    UndoManager.Instance.AddUndo(new CompositeUndo(new List<IUndoRedo>() { new UndoableProperty<Effect>(nameof(IndexNum), this, IndexNum, value), new UndoActionDelegate(this, nameof(RefreshProperties), true) }, "Effect ID"));
                    IndexNum = value;
                }
            }
        }

        public void RefreshProperties()
        {
            NotifyPropertyChanged(nameof(UndoableId));
        }
        #endregion

        public Effect Clone()
        {
            Effect newEffect = new Effect();
            newEffect.EffectParts = AsyncObservableCollection<EffectPart>.Create();
            newEffect.IndexNum = IndexNum;
            newEffect.I_02 = I_02;

            if (EffectParts != null)
            {
                foreach (var effectPart in EffectParts)
                {
                    newEffect.EffectParts.Add(effectPart.Clone());
                }
            }

            return newEffect;
        }

        public void RemoveNulls()
        {
        start:
            for (int i = 0; i < EffectParts.Count; i++)
            {
                if (EffectParts[i].AssetRef == null)
                {
                    EffectParts.RemoveAt(i);
                    goto start;
                }
            }
        }

        public void AssetRefDetailsRefresh(Asset asset)
        {
            foreach (var part in EffectParts)
            {
                part.AssetRefDetailsRefreash(asset);
            }
        }
    }

    [Serializable]
    public class EffectPart : INotifyPropertyChanged
    {
        #region INotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region EEPK_Organiser
        [YAXDontSerialize]
        public static List<string> CommonBones { get; private set; } = new List<string>()
        {
            "b_C_Base",
            "b_C_Pelvis",
            "g_C_Pelvis",
            "b_C_Head",
            "g_C_Head",
            "b_C_Neck1",
            "b_C_Chest",
            "b_C_Spine1",
            "b_C_Spine2",
            "b_L_Shoulder",
            "b_L_Arm1",
            "b_L_Arm2",
            "b_L_Elbow",
            "b_L_Hand",
            "g_L_Hand",
            "b_R_Shoulder",
            "b_R_Arm1",
            "b_R_Arm2",
            "b_R_Elbow",
            "b_R_Hand",
            "g_R_Hand",
            "b_L_Leg1",
            "b_L_Leg2",
            "b_L_Knee",
            "b_L_Foot",
            "g_L_Foot",
            "b_L_Toe",
            "b_R_Leg1",
            "b_R_Leg2",
            "b_R_Knee",
            "b_R_Foot",
            "g_R_Foot",
            "b_R_Toe",
            "g_x_LND",
            "TRS",
            "SCENE_ROOT",
            "f_L_Eye",
            "f_R_Eye",
        };

        [YAXDontSerialize]
        public string AssetRefDetails
        {
            get
            {
                if (AssetRef == null) return "Unassigned";
                return String.Format("[{1}] {0}", AssetRef.FileNamesPreview, AssetType);
            }
        }
        [YAXDontSerialize]
        public string EffectPartDetails
        {
            get
            {
                if (AssetRef == null) return "Unassigned";
                return !string.IsNullOrWhiteSpace(ESK) ? String.Format("[{1}] {0}  |  {2}", AssetRef.FileNamesPreview, AssetType, ESK) : AssetRefDetails;
            }
        }
        private Asset _assetRef = null;
        [YAXDontSerialize]
        public Asset AssetRef
        {
            get
            {
                return this._assetRef;
            }
            set
            {
                if (value != this._assetRef)
                {
                    this._assetRef = value;
                    AssetRefDetailsRefreash(value);
                }
            }
        }
        #endregion


        [YAXAttributeForClass]
        [YAXSerializeAs("Container_Type")]
        public AssetType AssetType { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Container_Index")]
        public ushort AssetIndex { get; set; }
        [YAXAttributeFor("StartTime")]
        [YAXSerializeAs("Frames")]
        public ushort StartTime { get; set; }
        [YAXAttributeFor("Attachment")]
        [YAXSerializeAs("value")]
        public Attachment AttachementType { get; set; }
        [YAXAttributeFor("RotateOnMovement")]
        [YAXSerializeAs("value")]
        public byte RotateMovement { get; set; }
        [YAXAttributeFor("Deactivation")]
        [YAXSerializeAs("Mode")]
        public DeactivationMode Deactivation { get; set; }//int8
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public byte I_06 { get; set; }
        [YAXAttributeFor("I_07")]
        [YAXSerializeAs("value")]
        public byte I_07 { get; set; }
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
        [YAXAttributeFor("AvoidSphere")]
        [YAXSerializeAs("Diameter")]
        [YAXFormat("0.0#######")]
        public float AvoidSphere { get; set; }


        [YAXAttributeFor("AttachFlags")]
        [YAXSerializeAs("PositionUpdate")]
        public bool PositionUpdate { get; set; } //I_32_0
        [YAXAttributeFor("AttachFlags")]
        [YAXSerializeAs("RotateUpdate")]
        public bool RotateUpdate { get; set; } //I_32_1
        [YAXAttributeFor("AttachFlags")]
        [YAXSerializeAs("InstantUpdate")]
        public bool InstantUpdate { get; set; } //I_32_2
        [YAXAttributeFor("AttachFlags")]
        [YAXSerializeAs("OnGroundOnly")]
        public bool OnGroundOnly { get; set; } //I_32_3
        [YAXAttributeFor("AttachFlags")]
        [YAXSerializeAs("UseTimeScale")]
        public bool UseTimeScale { get; set; } //I_32_4
        [YAXAttributeFor("StartEffectPosition")]
        [YAXSerializeAs("UseBoneDirection")]
        public bool UseBoneDirection { get; set; } //I_32_5
        [YAXAttributeFor("StartEffectPosition")]
        [YAXSerializeAs("UseBoneToCameraDirection")]
        public bool UseBoneToCameraDirection { get; set; } //I_32_6
        [YAXAttributeFor("StartEffectPosition")]
        [YAXSerializeAs("UseSceneCenterToBoneDirection")]
        public bool UseScreenCenterToBoneDirection { get; set; } //I_32_7


        [YAXAttributeFor("I_34")]
        [YAXSerializeAs("value")]
        public short I_34 { get; set; }


        [YAXAttributeFor("Flag_36")]
        [YAXSerializeAs("Unk1")]
        public bool I_36_1 { get; set; }
        [YAXAttributeFor("Flag_36")]
        [YAXSerializeAs("Unk2")]
        public bool I_36_2 { get; set; }
        [YAXAttributeFor("Flag_36")]
        [YAXSerializeAs("Unk3")]
        public bool I_36_3 { get; set; }
        [YAXAttributeFor("Flag_36")]
        [YAXSerializeAs("Unk4")]
        public bool I_36_4 { get; set; }
        [YAXAttributeFor("Flag_36")]
        [YAXSerializeAs("Unk5")]
        public bool I_36_5 { get; set; }
        [YAXAttributeFor("Flag_36")]
        [YAXSerializeAs("Unk6")]
        public bool I_36_6 { get; set; }
        [YAXAttributeFor("Flag_36")]
        [YAXSerializeAs("Unk7")]
        public bool I_36_7 { get; set; }
        [YAXAttributeFor("Flag_37")]
        [YAXSerializeAs("Unk0")]
        public bool I_37_0 { get; set; }
        [YAXAttributeFor("Flag_37")]
        [YAXSerializeAs("Unk1")]
        public bool I_37_1 { get; set; }
        [YAXAttributeFor("Flag_37")]
        [YAXSerializeAs("Unk2")]
        public bool I_37_2 { get; set; }
        [YAXAttributeFor("Flag_37")]
        [YAXSerializeAs("Unk3")]
        public bool I_37_3 { get; set; }
        [YAXAttributeFor("Flag_37")]
        [YAXSerializeAs("Unk4")]
        public bool I_37_4 { get; set; }
        [YAXAttributeFor("Flag_37")]
        [YAXSerializeAs("Unk5")]
        public bool I_37_5 { get; set; }
        [YAXAttributeFor("Flag_37")]
        [YAXSerializeAs("Unk6")]
        public bool I_37_6 { get; set; }
        [YAXAttributeFor("Flag_37")]
        [YAXSerializeAs("Unk7")]
        public bool I_37_7 { get; set; }


        [YAXAttributeFor("Flag_38")]
        [YAXSerializeAs("a")]
        [YAXHexValue]
        public byte I_38_a { get; set; } //int4
        [YAXAttributeFor("Flag_38")]
        [YAXSerializeAs("b")]
        [YAXHexValue]
        public byte I_38_b { get; set; } //int4

        //Flag_39
        [YAXAttributeFor("Flag_39")]
        [YAXSerializeAs("NoGlare")]
        public bool NoGlare { get; set; } //I_39_0
        [YAXAttributeFor("Flag_39")]
        [YAXSerializeAs("InverseTransparentDrawOrder")]
        public bool InverseTransparentDrawOrder { get; set; } //I_39_2
        [YAXAttributeFor("Flag_39")]
        [YAXSerializeAs("Unk1")]
        public bool I_39_1 { get; set; }
        [YAXAttributeFor("Flag_39")]
        [YAXSerializeAs("Unk5")]
        public bool I_39_5 { get; set; }
        [YAXAttributeFor("Flag_39")]
        [YAXSerializeAs("Unk6")]
        public bool I_39_6 { get; set; }
        [YAXAttributeFor("LinkFlags")]
        [YAXSerializeAs("RelativePositionZ_To_AbsolutePositionZ")]
        public bool RelativePositionZ_To_AbsolutePositionZ { get; set; } //I_39_3
        [YAXAttributeFor("LinkFlags")]
        [YAXSerializeAs("ScaleZ_To_BonePositionZ")]
        public bool ScaleZ_To_BonePositionZ { get; set; } //I_39_4
        [YAXAttributeFor("LinkFlags")]
        [YAXSerializeAs("ObjectOrientation_To_XXXX")]
        public bool ObjectOrientation_To_XXXX { get; set; } //I_39_7


        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0#######")]
        public float PositionX { get; set; } //F_40
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0#######")]
        public float PositionY { get; set; } //F_44
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0#######")]
        public float PositionZ { get; set; } //F_48
        [YAXAttributeFor("Orientation_X")]
        [YAXSerializeAs("Min")]
        [YAXFormat("0.0#######")]
        public float RotationX_Min { get; set; }
        [YAXAttributeFor("Orientation_X")]
        [YAXSerializeAs("Max")]
        [YAXFormat("0.0#######")]
        public float RotationX_Max { get; set; }
        [YAXAttributeFor("Orientation_Y")]
        [YAXSerializeAs("Min")]
        [YAXFormat("0.0#######")]
        public float RotationY_Min { get; set; }
        [YAXAttributeFor("Orientation_Y")]
        [YAXSerializeAs("Max")]
        [YAXFormat("0.0#######")]
        public float RotationY_Max { get; set; }
        [YAXAttributeFor("Orientation_Z")]
        [YAXSerializeAs("Min")]
        [YAXFormat("0.0#######")]
        public float RotationZ_Min { get; set; }
        [YAXAttributeFor("Orientation_Z")]
        [YAXSerializeAs("Max")]
        [YAXFormat("0.0#######")]
        public float RotationZ_Max { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Min")]
        [YAXFormat("0.0#######")]
        public float ScaleMin { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Max")]
        [YAXFormat("0.0#######")]
        public float ScaleMax { get; set; }
        [YAXAttributeFor("NearFadeDistance")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float NearFadeDistance { get; set; }
        [YAXAttributeFor("FarFadeDistance")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float FarFadeDistance { get; set; }
        [YAXAttributeFor("EMA_Animation")]
        [YAXSerializeAs("Index")]
        public ushort EMA_AnimationIndex { get; set; }
        [YAXAttributeFor("EMA_Animation")]
        [YAXSerializeAs("LoopStartFrame")]
        public ushort EMA_LoopStartFrame { get; set; }
        [YAXAttributeFor("EMA_Animation")]
        [YAXSerializeAs("LoopEndFrame")]
        public ushort EMA_LoopEndFrame { get; set; }
        [YAXAttributeFor("EMA_Animation")]
        [YAXSerializeAs("Loop")]
        public bool EMA_Loop { get; set; } //I_36_0
        [YAXAttributeFor("BoneToAttach")]
        [YAXSerializeAs("name")]
        public string ESK { get; set; } //if NULL, then make it 4 zero bytes instead of an offset

        public enum DeactivationMode : byte
        {
            Never = 0,
            Always = 1,
            AfterAnimLoop = 2
        }

        public enum Attachment : byte
        {
            External = 0,
            Unk1 = 1,
            Bone = 2,
            Camera = 3
        }


        public EffectPart Clone()
        {
            return new EffectPart()
            {
                AssetRef = AssetRef,
                ScaleMax = ScaleMax,
                ScaleMin = ScaleMin,
                ESK = ESK,
                RotationZ_Min = RotationZ_Min,
                RotationZ_Max = RotationZ_Max,
                PositionX = PositionX,
                PositionY = PositionY,
                PositionZ = PositionZ,
                AvoidSphere = AvoidSphere,
                RotationX_Min = RotationX_Min,
                RotationX_Max = RotationX_Max,
                RotationY_Min = RotationY_Min,
                RotationY_Max = RotationY_Max,
                NearFadeDistance = NearFadeDistance,
                FarFadeDistance = FarFadeDistance,
                AssetIndex = AssetIndex,
                AssetType = AssetType,
                AttachementType = AttachementType,
                RotateMovement = RotateMovement,
                Deactivation = Deactivation,
                I_06 = I_06,
                I_07 = I_07,
                I_08 = I_08,
                I_12 = I_12,
                I_16 = I_16,
                I_20 = I_20,
                StartTime = StartTime,
                EMA_AnimationIndex = EMA_AnimationIndex,
                PositionUpdate = PositionUpdate,
                RotateUpdate = RotateUpdate,
                InstantUpdate = InstantUpdate,
                OnGroundOnly = OnGroundOnly,
                UseTimeScale = UseTimeScale,
                UseBoneDirection = UseBoneDirection,
                UseBoneToCameraDirection = UseBoneToCameraDirection,
                UseScreenCenterToBoneDirection = UseScreenCenterToBoneDirection,
                I_34 = I_34,
                EMA_Loop = EMA_Loop,
                I_36_1 = I_36_1,
                I_36_2 = I_36_2,
                I_36_3 = I_36_3,
                I_36_4 = I_36_4,
                I_36_5 = I_36_5,
                I_36_6 = I_36_6,
                I_36_7 = I_36_7,
                I_37_0 = I_37_0,
                I_37_1 = I_37_1,
                I_37_2 = I_37_2,
                I_37_3 = I_37_3,
                I_37_4 = I_37_4,
                I_37_5 = I_37_5,
                I_37_6 = I_37_6,
                I_37_7 = I_37_7,
                I_38_a = I_38_a,
                I_38_b = I_38_b,
                NoGlare = NoGlare,
                I_39_1 = I_39_1,
                InverseTransparentDrawOrder = InverseTransparentDrawOrder,
                RelativePositionZ_To_AbsolutePositionZ = RelativePositionZ_To_AbsolutePositionZ,
                ScaleZ_To_BonePositionZ = ScaleZ_To_BonePositionZ,
                I_39_5 = I_39_5,
                I_39_6 = I_39_6,
                ObjectOrientation_To_XXXX = ObjectOrientation_To_XXXX,
                EMA_LoopStartFrame = EMA_LoopStartFrame,
                EMA_LoopEndFrame = EMA_LoopEndFrame
            };
        }

        public static EffectPart NewEffectPart()
        {
            return new EffectPart();
        }

        public void AssetRefDetailsRefreash(Asset asset)
        {
            if (AssetRef == asset)
            {
                RefreshDetails();
            }
        }

        public void RefreshDetails()
        {
            NotifyPropertyChanged(nameof(AssetRefDetails));
            NotifyPropertyChanged(nameof(AssetRef));
            NotifyPropertyChanged(nameof(EffectPartDetails));
        }

        public void CopyValues(EffectPart effectPart, List<IUndoRedo> undos)
        {
            undos.Add(new UndoableProperty<EffectPart>(nameof(AssetRef), this, AssetRef, effectPart.AssetRef));
            AssetRef = effectPart.AssetRef;

            undos.AddRange(Utils.CopyValues(this, effectPart));

            ObjectExtensions.NotifyPropsChanged(this);
            undos.Add(new UndoActionPropNotify(this, true));
        }
    }


}
