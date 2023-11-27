using LB_Common.Numbers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.BCS
{
    public enum Version
    {
        XV1 = 1,
        XV2 = 0
    }

    public enum PartType
    {
        FaceBase,
        FaceForehead,
        FaceEye,
        FaceNose,
        FaceEar,
        Hair,
        Bust,
        Pants,
        Rist,
        Boots
    }

    [Flags]
    public enum PartTypeFlags : int
    {
        None = 0x0,
        FaceBase = 0x1,
        FaceForehead = 0x2,
        FaceEye = 0x4,
        FaceNose = 0x8,
        FaceEar = 0x10,
        Hair = 0x20,
        Bust = 0x40,
        Pants = 0x80,
        Rist = 0x100,
        Boots = 0x200,
        AllParts = FaceBase | FaceForehead | FaceEye | FaceNose | FaceEar | Hair | Bust | Pants | Rist | Boots
    }

    public enum Race : byte
    {
        Human = 0,
        Saiyan = 1,
        Namekian = 2,
        FriezaRace = 3,
        Majin = 4,
        Other = 5
    }

    public enum Gender : byte
    {
        Male = 0,
        Female = 1
    }

    [YAXSerializeAs("BCS")]
    [Serializable]
    public class BCS_File : ISorting
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Error, DefaultValue = Version.XV2)]
        public Version Version { get; set; } = Version.XV2;

        [YAXAttributeFor("Race")]
        [YAXSerializeAs("value")]
        public Race Race { get; set; }
        [YAXAttributeFor("Gender")]
        [YAXSerializeAs("value")]
        public Gender Gender { get; set; }
        [YAXAttributeFor("F_48")]
        [YAXSerializeAs("values")]
        [YAXFormat("0.0###########")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_48 { get; set; } = new float[7]; // size 7

        [YAXDontSerializeIfNull]
        public AsyncObservableCollection<PartSet> PartSets { get; set; } = new AsyncObservableCollection<PartSet>();
        [YAXDontSerializeIfNull]
        public AsyncObservableCollection<PartColor> PartColors { get; set; } = new AsyncObservableCollection<PartColor>();
        [YAXDontSerializeIfNull]
        public AsyncObservableCollection<Body> Bodies { get; set; } = new AsyncObservableCollection<Body>();
        [YAXDontSerializeIfNull]
        public SkeletonData SkeletonData1 { get; set; } = new SkeletonData();
        [YAXDontSerializeIfNull]
        public SkeletonData SkeletonData2 { get; set; } = new SkeletonData();



        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            if (PartSets != null)
                PartSets.Sort((x, y) => x.SortID - y.SortID);

            if (PartColors != null)
            {
                PartColors.Sort((x, y) => x.SortID - y.SortID);

                foreach (var partColor in PartColors)
                {
                    if (partColor.ColorsList != null)
                        partColor.ColorsList.Sort((x, y) => x.SortID - y.SortID);
                }
            }

            if (Bodies != null)
                Bodies.Sort((x, y) => x.SortID - y.SortID);
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public static BCS_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetBcsFile();
        }

        public static BCS_File Load(string path)
        {
            return new Parser(File.ReadAllBytes(path)).GetBcsFile();
        }

        public PartColor GetPartColors(string id, string name, bool addNewIfMissing = true)
        {
            if (PartColors == null) PartColors = new AsyncObservableCollection<PartColor>();

            int index = PartColors.IndexOf(PartColors.FirstOrDefault(x => x.Index == id));

            if (index != -1)
            {
                return PartColors[index];
            }
            else if(addNewIfMissing)
            {
                PartColor newPartColor = new PartColor() { Index = id, Name = name, ColorsList = new AsyncObservableCollection<Colors>() };
                PartColors.Add(newPartColor);
                return newPartColor;
            }
            else
            {
                return null;
            }

        }

        public void AddPartColorGroup(PartColor colorGroup)
        {
            if (PartColors == null) PartColors = new AsyncObservableCollection<PartColor>();

            int index = PartColors.IndexOf(PartColors.FirstOrDefault(x => x.Index == colorGroup.Index));

            if(index != -1)
            {
                PartColors[index] = colorGroup;
            }
            else
            {
                PartColors.Add(colorGroup);
            }

        }

        public Colors GetColor(int groupId, int colorIndex)
        {
            PartColor partColor = PartColors.FirstOrDefault(x => x.ID == groupId);

            if (partColor != null)
            {
                return partColor.ColorsList.FirstOrDefault(x => x.ID == colorIndex);
            }

            return null;
        }

        public static string GetBcsFilePath(Race race, Gender gender = Gender.Male)
        {
            switch (race)
            {
                case Race.Human:
                case Race.Saiyan:
                    return gender == Gender.Male ? "chara/HUM/HUM.bcs" : "chara/HUF/HUF.bcs";
                case Race.Majin:
                    return gender == Gender.Male ? "chara/MAM/MAM.bcs" : "chara/MAF/MAF.bcs";
                case Race.FriezaRace:
                    return "chara/FRI/FRI.bcs";
                case Race.Namekian:
                    return "chara/NMC/NMC.bcs";
            }

            return null;
        }

        public int NewPartColorGroupID(int min = 0)
        {
            int id = min;

            while (PartColors.Any(x => x.SortID == id))
            {
                id++;
            }

            return id;
        }

        public int NewBodyID()
        {
            int id = 0;

            while (Bodies.Any(x => x.ID == id))
            {
                id++;
            }

            return id;
        }

        public int NewPartSetID()
        {
            int id = 0;

            while (PartSets.Any(x => x.ID == id))
            {
                id++;
            }

            return id;
        }

        public PartSet GetParentPartSet(PhysicsPart physicsPart)
        {
            foreach (var partSet in PartSets)
            {
                foreach (var _part in partSet.Parts)
                {
                    foreach (var _physicsPart in _part.PhysicsParts)
                    {
                        if (_physicsPart == physicsPart) return partSet;
                    }
                }
            }

            return null;
        }

        public PartSet GetParentPartSet(Part part)
        {
            foreach (var partSet in PartSets)
            {
                foreach (var _part in partSet.Parts)
                {
                    if (_part == part) return partSet;
                }
            }

            return null;
        }

        public PartSet GetParentPartSet(ColorSelector colSel)
        {
            foreach (var partSet in PartSets)
            {
                foreach (var part in partSet.Parts)
                {
                    foreach (var colorSel in part.ColorSelectors)
                    {
                        if (colorSel == colSel) return partSet;
                    }
                }
            }

            return null;
        }

        public Part GetParentPart(PhysicsPart physicsPart)
        {
            foreach (var partSet in PartSets)
            {
                foreach (var part in partSet.Parts)
                {
                    foreach (var _physicsPart in part.PhysicsParts)
                    {
                        if (_physicsPart == physicsPart) return part;
                    }
                }
            }

            return null;
        }

        public Part GetParentPart(ColorSelector colSel)
        {
            foreach (var partSet in PartSets)
            {
                foreach (var part in partSet.Parts)
                {
                    foreach (var colorSel in part.ColorSelectors)
                    {
                        if (colorSel == colSel) return part;
                    }
                }
            }

            return null;
        }

        public Part GetPartWithEmdPath(string emdPath)
        {
            foreach (var partSet in PartSets)
            {
                foreach (var part in partSet.Parts)
                {
                    if (Path.GetFileName(part.GetModelPath(part.PartType)) == emdPath) return part;
                }
            }

            return null;
        }

        public PhysicsPart GetPhysicsPartWithEmdPath(string emdPath)
        {
            foreach (var partSet in PartSets)
            {
                foreach (var part in partSet.Parts)
                {
                    foreach (var physicsPart in part.PhysicsParts)
                    {
                        if (Path.GetFileNameWithoutExtension(physicsPart.GetEmbPath()) == emdPath) return physicsPart;
                    }
                }
            }

            return null;
        }
    }

    //PartSet
    [Serializable]
    public class PartSet : IInstallable, INotifyPropertyChanged
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

        #region WrappedProps
        [YAXDontSerialize]
        public int SortID
        {
            get
            {
                return int.Parse(Index);
            }
        }
        [YAXDontSerialize]
        public int ID { get { return int.Parse(Index); } set { Index = value.ToString(); } }
        #endregion

        [YAXAttributeForClass]
        [BindingAutoId()]
        public string Index { get; set; } //uint16

        //Parts
        [YAXDontSerialize]
        public AsyncObservableCollection<Part> Parts { get; set; } = new AsyncObservableCollection<Part>();

        [YAXDontSerializeIfNull]
        public Part FaceBase { get { return GetPart(PartType.FaceBase); } set { SetPart(value, PartType.FaceBase); } }
        [YAXDontSerializeIfNull]
        public Part FaceForehead { get { return GetPart(PartType.FaceForehead); } set { SetPart(value, PartType.FaceForehead); } }
        [YAXDontSerializeIfNull]
        public Part FaceEye { get { return GetPart(PartType.FaceEye); } set { SetPart(value, PartType.FaceEye); } }
        [YAXDontSerializeIfNull]
        public Part FaceNose { get { return GetPart(PartType.FaceNose); } set { SetPart(value, PartType.FaceNose); } }
        [YAXDontSerializeIfNull]
        public Part FaceEar { get { return GetPart(PartType.FaceEar); } set { SetPart(value, PartType.FaceEar); } }
        [YAXDontSerializeIfNull]
        public Part Hair { get { return GetPart(PartType.Hair); } set { SetPart(value, PartType.Hair); } }
        [YAXDontSerializeIfNull]
        public Part Bust { get { return GetPart(PartType.Bust); } set { SetPart(value, PartType.Bust); } }
        [YAXDontSerializeIfNull]
        public Part Pants { get { return GetPart(PartType.Pants); } set { SetPart(value, PartType.Pants); } }
        [YAXDontSerializeIfNull]
        public Part Rist { get { return GetPart(PartType.Rist); } set { SetPart(value, PartType.Rist); } }
        [YAXDontSerializeIfNull]
        public Part Boots { get { return GetPart(PartType.Boots); } set { SetPart(value, PartType.Boots); } }

        public Part GetPart(int index)
        {
            switch (index)
            {
                case 0:
                    return FaceBase;
                case 1:
                    return FaceForehead;
                case 2:
                    return FaceEye;
                case 3:
                    return FaceNose;
                case 4:
                    return FaceEar;
                case 5:
                    return Hair;
                case 6:
                    return Bust;
                case 7:
                    return Pants;
                case 8:
                    return Rist;
                case 9:
                    return Boots;
                default:
                    throw new ArgumentException($"PartSet.GetPart: part index out of range ({index})");
            }
        }

        public void SetPart(Part part, PartType type, List<IUndoRedo> undos = null)
        {
            if (part == null) return;
            if (Parts == null) Parts = new AsyncObservableCollection<Part>();

            part.PartType = type;
            var entry = GetPart(type);

            if (entry == null)
            {
                int insetIdx = (int)type;

                if (insetIdx <= Parts.Count)
                {
                    Parts.Insert(insetIdx, part);

                    if (undos != null)
                        undos.Add(new UndoableListInsert<Part>(Parts, insetIdx, part));
                }
                else
                {
                    Parts.Add(part);

                    if (undos != null)
                        undos.Add(new UndoableListAdd<Part>(Parts, part));
                }
            }
            else
            {
                int replaceIdx = Parts.IndexOf(entry);

                if (undos != null)
                {
                    undos.Add(new UndoableListRemove<Part>(Parts, Parts[replaceIdx], replaceIdx));
                    undos.Add(new UndoableListInsert<Part>(Parts, replaceIdx, part));
                }

                Parts.RemoveAt(replaceIdx);
                Parts.Insert(replaceIdx, part);
                //Parts[replaceIdx] = part;
            }
        }

        public Part GetPart(PartType type)
        {
            if (Parts == null) return null;
            return Parts.FirstOrDefault(x => x.PartType == type);
        }

        public bool HasPart(PartType type)
        {
            return GetPart(type) != null;
        }

        public Part GetParentPart(PhysicsPart physicsPart)
        {
            foreach (var part in Parts)
            {
                if (part.PhysicsParts != null)
                {
                    if (part.PhysicsParts.Contains(physicsPart)) return part;
                }
            }

            return null;
        }

        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(ID));
        }

    }

    [Serializable]
    public class Part
    {
        [Flags]
        public enum PartFlags : int
        {
            Unk1 = 0x1, //Seemingly does nothing
            DytFromTextureEmb = 0x2, //Takes DYT path from the main EMB (Model2 and Files_EMB)
            DytRampsFromTextureEmb = 0x4, //Untested
            GreenScouterOverlay = 0x8,
            RedScouterOverlay = 0x10,
            BlueScouterOverlay = 0x20,
            PurpleScouterOverlay = 0x40,
            Unk8 = 0x80,
            Unk9 = 0x100,
            OrangeScouterOverlay = 0x200,
        }

        [YAXDontSerialize]
        public PartType PartType { get; set; }

        [YAXAttributeFor("Model")]
        [YAXSerializeAs("value")]
        public short Model { get; set; }
        [YAXAttributeFor("Model2")]
        [YAXSerializeAs("value")]
        public short Model2 { get; set; }
        [YAXAttributeFor("Texture")]
        [YAXSerializeAs("value")]
        public short Texture { get; set; }
        [YAXAttributeFor("Shader")]
        [YAXSerializeAs("value")]
        public short Shader { get; set; }
        [YAXAttributeFor("PartFlags")]
        [YAXSerializeAs("value")]
        public PartFlags Flags { get; set; }

        //Flags
        [YAXDontSerialize]
        public PartTypeFlags HideFlags { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("FaceBase")]
        public bool Hide_FaceBase { get => HideFlags.HasFlag(PartTypeFlags.FaceBase); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceBase, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Forehead")]
        public bool Hide_Forehead { get => HideFlags.HasFlag(PartTypeFlags.FaceForehead); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceForehead, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Eye")]
        public bool Hide_Eye { get => HideFlags.HasFlag(PartTypeFlags.FaceEye); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceEye, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Nose")]
        public bool Hide_Nose { get => HideFlags.HasFlag(PartTypeFlags.FaceNose); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceNose, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Ear")]
        public bool Hide_Ear { get => HideFlags.HasFlag(PartTypeFlags.FaceEar); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceEar, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Hair")]
        public bool Hide_Hair { get => HideFlags.HasFlag(PartTypeFlags.Hair); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Hair, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Bust")]
        public bool Hide_Bust { get => HideFlags.HasFlag(PartTypeFlags.Bust); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Bust, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Pants")]
        public bool Hide_Pants { get => HideFlags.HasFlag(PartTypeFlags.Pants); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Pants, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Rist")]
        public bool Hide_Rist { get => HideFlags.HasFlag(PartTypeFlags.Rist); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Rist, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Boots")]
        public bool Hide_Boots { get => HideFlags.HasFlag(PartTypeFlags.Boots); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Boots, value); }

        [YAXDontSerialize]
        public PartTypeFlags HideMatFlags { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("FaceBase")]
        public bool HideMat_FaceBase { get => HideMatFlags.HasFlag(PartTypeFlags.FaceBase); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceBase, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Forehead")]
        public bool HideMat_Forehead { get => HideMatFlags.HasFlag(PartTypeFlags.FaceForehead); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceForehead, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Eye")]
        public bool HideMat_Eye { get => HideMatFlags.HasFlag(PartTypeFlags.FaceEye); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceEye, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Nose")]
        public bool HideMat_Nose { get => HideMatFlags.HasFlag(PartTypeFlags.FaceNose); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceNose, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Ear")]
        public bool HideMat_Ear { get => HideMatFlags.HasFlag(PartTypeFlags.FaceEar); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceEar, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Hair")]
        public bool HideMat_Hair { get => HideMatFlags.HasFlag(PartTypeFlags.Hair); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Hair, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Bust")]
        public bool HideMat_Bust { get => HideMatFlags.HasFlag(PartTypeFlags.Bust); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Bust, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Pants")]
        public bool HideMat_Pants { get => HideMatFlags.HasFlag(PartTypeFlags.Pants); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Pants, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Rist")]
        public bool HideMat_Rist { get => HideMatFlags.HasFlag(PartTypeFlags.Rist); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Rist, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Boots")]
        public bool HideMat_Boots { get => HideMatFlags.HasFlag(PartTypeFlags.Boots); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Boots, value); }


        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string CharaCode { get; set; }

        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMD")]
        public string EmdPath { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMM")]
        public string EmmPath { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMB")]
        public string EmbPath { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EAN")]
        public string EanPath { get; set; }

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ColorSelector")]
        public AsyncObservableCollection<ColorSelector> ColorSelectors { get; set; } = new AsyncObservableCollection<ColorSelector>();
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PhysicsObject")]
        public AsyncObservableCollection<PhysicsPart> PhysicsParts { get; set; } = new AsyncObservableCollection<PhysicsPart>();
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Unk3")]
        public List<Unk3> Unk_3 { get; set; }


        //XenoKit
        [field: NonSerialized]
        private CompositeReadOnlyAsyncObservableCollection _mergedList = null;
        [YAXDontSerialize]
        public CompositeReadOnlyAsyncObservableCollection MergedList
        {
            get
            {
                if (_mergedList == null)
                {
                    _mergedList = new CompositeReadOnlyAsyncObservableCollection();
                    _mergedList.AddList(PhysicsParts, PhysicsParts);
                    _mergedList.AddList(ColorSelectors, ColorSelectors);
                    _mergedList.SyncLists();
                }
                return _mergedList;
            }
        }

        public string GetModelPath(PartType partType)
        {
            if (!string.IsNullOrWhiteSpace(EmdPath))
            {
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emd", CharaCode, EmdPath));
            }
            else if (Model != -1)
            {
                //Use Model1
                return string.Format("{0}.emd", GetPath(Model, partType));
            }
            else
            {
                //Part has no model
                return null;
            }
        }

        public string GetEmmPath(PartType partType)
        {
            if (!String.IsNullOrWhiteSpace(EmmPath))
            {
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emm", CharaCode, EmmPath));
            }
            else if (Model != -1)
            {
                return string.Format("{0}.emm", GetPath(Model, partType));
            }
            else
            {
                return null;
            }
        }

        public string GetEmbPath(PartType partType)
        {
            if (!String.IsNullOrWhiteSpace(EmbPath))
            {
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emb", CharaCode, EmbPath));
            }
            else if (Model2 != -1)
            {
                return string.Format("{0}.emb", GetPath(Model2, partType));
            }
            else
            {
                return null;
            }
        }

        public string GetDytPath(PartType partType)
        {
            if (Flags.HasFlag(PartFlags.DytFromTextureEmb))
            {
                //Uses Model2/EmbPath
                string embPath = GetEmbPath(partType);
                if (string.IsNullOrWhiteSpace(embPath)) return null;

                return string.Format("{0}/{1}.dyt.emb", Path.GetDirectoryName(embPath), Path.GetFileNameWithoutExtension(embPath));
            }
            else if (Model != -1)
            {
                //Uses Model1
                return string.Format("{0}.dyt.emb", GetPath(Model, partType));
            }
            else if (!string.IsNullOrWhiteSpace(EmdPath))
            {
                string emdPath = GetModelPath(partType);
                return string.Format("{0}/{1}.dyt.emb", Path.GetDirectoryName(emdPath), Path.GetFileNameWithoutExtension(emdPath));
            }

            return null;
        }

        public string GetEanPath()
        {
            if (string.IsNullOrWhiteSpace(EanPath)) return null;
            return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.ean", CharaCode, EanPath));
        }

        private string GetPath(int modelNumber, PartType partType)
        {
            switch (partType)
            {
                case PartType.FaceBase:
                    return string.Format("chara/{0}/{0}_{1}_Face_base", CharaCode, modelNumber.ToString("D3"));
                case PartType.FaceForehead:
                    return string.Format("chara/{0}/{0}_{1}_Face_forehead", CharaCode, modelNumber.ToString("D3"));
                case PartType.FaceEye:
                    return string.Format("chara/{0}/{0}_{1}_Face_eye", CharaCode, modelNumber.ToString("D3"));
                case PartType.FaceNose:
                    return string.Format("chara/{0}/{0}_{1}_Face_nose", CharaCode, modelNumber.ToString("D3"));
                case PartType.FaceEar:
                    return string.Format("chara/{0}/{0}_{1}_Face_ear", CharaCode, modelNumber.ToString("D3"));
                case PartType.Bust:
                    return string.Format("chara/{0}/{0}_{1}_Bust", CharaCode, modelNumber.ToString("D3"));
                case PartType.Boots:
                    return string.Format("chara/{0}/{0}_{1}_Boots", CharaCode, modelNumber.ToString("D3"));
                case PartType.Rist:
                    return string.Format("chara/{0}/{0}_{1}_Rist", CharaCode, modelNumber.ToString("D3"));
                case PartType.Hair:
                    return string.Format("chara/{0}/{0}_{1}_Hair", CharaCode, modelNumber.ToString("D3"));
                case PartType.Pants:
                    return string.Format("chara/{0}/{0}_{1}_Pants", CharaCode, modelNumber.ToString("D3"));
                default:
                    throw new InvalidDataException("Invalid partType = " + partType);
            }
        }

        public PartTypeFlags GetPartTypeFlags()
        {
            return GetPartTypeFlags(PartType);
        }

        public static PartTypeFlags GetPartTypeFlags(PartType type)
        {
            switch (type)
            {
                case PartType.FaceBase:
                    return PartTypeFlags.FaceBase;
                case PartType.FaceEar:
                    return PartTypeFlags.FaceEar;
                case PartType.FaceEye:
                    return PartTypeFlags.FaceEye;
                case PartType.FaceForehead:
                    return PartTypeFlags.FaceForehead;
                case PartType.FaceNose:
                    return PartTypeFlags.FaceNose;
                case PartType.Hair:
                    return PartTypeFlags.Hair;
                case PartType.Bust:
                    return PartTypeFlags.Bust;
                case PartType.Rist:
                    return PartTypeFlags.Rist;
                case PartType.Boots:
                    return PartTypeFlags.Boots;
                case PartType.Pants:
                    return PartTypeFlags.Pants;
                default:
                    return 0;
            }
        }

        public static PartType GetPartType(PartTypeFlags type)
        {
            switch (type)
            {
                case PartTypeFlags.FaceBase:
                    return PartType.FaceBase;
                case PartTypeFlags.FaceEar:
                    return PartType.FaceEar;
                case PartTypeFlags.FaceEye:
                    return PartType.FaceEye;
                case PartTypeFlags.FaceForehead:
                    return PartType.FaceForehead;
                case PartTypeFlags.FaceNose:
                    return PartType.FaceNose;
                case PartTypeFlags.Hair:
                    return PartType.Hair;
                case PartTypeFlags.Bust:
                    return PartType.Bust;
                case PartTypeFlags.Rist:
                    return PartType.Rist;
                case PartTypeFlags.Boots:
                    return PartType.Boots;
                case PartTypeFlags.Pants:
                    return PartType.Pants;
                default:
                    return 0;
            }
        }
    }

    [Serializable]
    public class ColorSelector
    {
        [YAXAttributeFor("PartColors")]
        [YAXSerializeAs("value")]
        public ushort PartColorGroup { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("value")]
        public ushort ColorIndex { get; set; }

    }

    [Serializable]
    public class PhysicsPart
    {
        [YAXAttributeFor("Model1")]
        [YAXSerializeAs("value")]
        public short Model1 { get; set; }
        [YAXAttributeFor("Model2")]
        [YAXSerializeAs("value")]
        public short Model2 { get; set; }
        [YAXAttributeFor("Texture")]
        [YAXSerializeAs("value")]
        public short Texture { get; set; }
        [YAXAttributeFor("Flags")]
        [YAXSerializeAs("value")]
        public Part.PartFlags Flags { get; set; }


        //Flags
        [YAXDontSerialize]
        public PartTypeFlags HideFlags { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("FaceBase")]
        public bool Hide_FaceBase { get => HideFlags.HasFlag(PartTypeFlags.FaceBase); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceBase, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Forehead")]
        public bool Hide_Forehead { get => HideFlags.HasFlag(PartTypeFlags.FaceForehead); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceForehead, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Eye")]
        public bool Hide_Eye { get => HideFlags.HasFlag(PartTypeFlags.FaceEye); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceEye, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Nose")]
        public bool Hide_Nose { get => HideFlags.HasFlag(PartTypeFlags.FaceNose); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceNose, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Ear")]
        public bool Hide_Ear { get => HideFlags.HasFlag(PartTypeFlags.FaceEar); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.FaceEar, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Hair")]
        public bool Hide_Hair { get => HideFlags.HasFlag(PartTypeFlags.Hair); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Hair, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Bust")]
        public bool Hide_Bust { get => HideFlags.HasFlag(PartTypeFlags.Bust); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Bust, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Pants")]
        public bool Hide_Pants { get => HideFlags.HasFlag(PartTypeFlags.Pants); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Pants, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Rist")]
        public bool Hide_Rist { get => HideFlags.HasFlag(PartTypeFlags.Rist); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Rist, value); }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Boots")]
        public bool Hide_Boots { get => HideFlags.HasFlag(PartTypeFlags.Boots); set => HideFlags = HideFlags.SetFlag(PartTypeFlags.Boots, value); }

        [YAXDontSerialize]
        public PartTypeFlags HideMatFlags { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("FaceBase")]
        public bool HideMat_FaceBase { get => HideMatFlags.HasFlag(PartTypeFlags.FaceBase); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceBase, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Forehead")]
        public bool HideMat_Forehead { get => HideMatFlags.HasFlag(PartTypeFlags.FaceForehead); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceForehead, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Eye")]
        public bool HideMat_Eye { get => HideMatFlags.HasFlag(PartTypeFlags.FaceEye); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceEye, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Nose")]
        public bool HideMat_Nose { get => HideMatFlags.HasFlag(PartTypeFlags.FaceNose); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceNose, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Ear")]
        public bool HideMat_Ear { get => HideMatFlags.HasFlag(PartTypeFlags.FaceEar); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.FaceEar, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Hair")]
        public bool HideMat_Hair { get => HideMatFlags.HasFlag(PartTypeFlags.Hair); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Hair, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Bust")]
        public bool HideMat_Bust { get => HideMatFlags.HasFlag(PartTypeFlags.Bust); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Bust, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Pants")]
        public bool HideMat_Pants { get => HideMatFlags.HasFlag(PartTypeFlags.Pants); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Pants, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Rist")]
        public bool HideMat_Rist { get => HideMatFlags.HasFlag(PartTypeFlags.Rist); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Rist, value); }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Boots")]
        public bool HideMat_Boots { get => HideMatFlags.HasFlag(PartTypeFlags.Boots); set => HideMatFlags = HideMatFlags.SetFlag(PartTypeFlags.Boots, value); }

        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string CharaCode { get; set; }

        //Str
        [YAXAttributeFor("BoneToAttach")]
        [YAXSerializeAs("value")]
        public string BoneToAttach { get; set; }

        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMD")]
        public string EmdPath { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMM")]
        public string EmmPath { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMB")]
        public string EmbPath { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EAN")]
        public string EanPath { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("SCD")]
        public string ScdPath { get; set; }

        public string GetModelPath()
        {
            return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emd", CharaCode, EmdPath));
        }

        public string GetEmbPath()
        {
            if (string.IsNullOrWhiteSpace(EmbPath))
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emb", CharaCode, EmdPath));
            else
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emb", CharaCode, EmbPath));
        }

        public string GetEmmPath()
        {
            if (string.IsNullOrWhiteSpace(EmmPath))
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emm", CharaCode, EmdPath));
            else
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emm", CharaCode, EmmPath));
        }

        public string GetDytPath()
        {
            if (string.IsNullOrWhiteSpace(EmbPath))
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.dyt.emb", CharaCode, EmdPath));
            else
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.dyt.emb", CharaCode, EmbPath));
        }

        public string GetEanPath()
        {
            return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.ean", CharaCode, EanPath));
        }

        public string GetScdPath()
        {
            return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.scd", CharaCode, ScdPath));
        }

        public string GetEskPath()
        {
            string emd = GetModelPath();
            return string.Format("{0}/{1}.esk", Path.GetDirectoryName(emd), Path.GetFileNameWithoutExtension(emd));
        }
    }

    [Serializable]
    public class Unk3
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public short[] I_00 { get; set; } //size 6
    }

    //Color
    [Serializable]
    public class PartColor : IInstallable, INotifyPropertyChanged
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

        [YAXDontSerialize]
        public int SortID
        {
            get
            {
                return int.Parse(Index);
            }
        }
        [YAXDontSerialize]
        public ushort ID
        {
            get => (ushort)Utils.TryParseInt(Index);
            set => Index = value.ToString();
        }

        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } //int16
        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }

        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ReplaceAll")]
        public string Overwrite_XmlBinding { get; set; }
        [YAXDontSerialize]
        public bool Overwrite => !string.IsNullOrWhiteSpace(Overwrite_XmlBinding) ? Overwrite_XmlBinding.Equals("true", StringComparison.OrdinalIgnoreCase) : false;


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Colors")]
        public AsyncObservableCollection<Colors> ColorsList { get; set; } = new AsyncObservableCollection<Colors>();

        public int NewColorID()
        {
            int id = 0;

            while (ColorsList.Any(x => x.ID == id))
            {
                id++;
            }

            return id;
        }

        public void AddColor(Colors color)
        {
            if (ColorsList == null)
                ColorsList = new AsyncObservableCollection<Colors>();

            int idx = color.SortID;

            int existingIndex = ColorsList.IndexOf(ColorsList.FirstOrDefault(p => p.Index == color.Index));

            if (existingIndex != -1)
            {
                ColorsList[existingIndex] = color;
            }
            else
            {
                ColorsList.Add(color);
            }
        }

        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(Index));
            NotifyPropertyChanged(nameof(Name));

            if (ColorsList != null)
            {
                foreach (var color in ColorsList)
                {
                    color.RefreshValues();
                }
            }
        }

        public Brush GetPreview(int color)
        {
            Colors colorEntry = ColorsList.FirstOrDefault(x => x.ID == color);

            return colorEntry != null ? colorEntry.ColorPreview : Brushes.White;
        }
    }

    [YAXSerializeAs("Colors")]
    [Serializable]
    public class Colors : IInstallable, INotifyPropertyChanged
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

        [YAXDontSerialize]
        public int SortID => Utils.TryParseInt(Index);
        [YAXDontSerialize]
        public int ID
        {
            get => Utils.TryParseInt(Index);
            set
            {
                Index = value.ToString();
                NotifyPropertyChanged(nameof(ID));
                NotifyPropertyChanged(nameof(ToolTip));
            }
        }

        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; }

        public CustomColor Color1 { get; set; } = new CustomColor();
        public CustomColor Color2 { get; set; } = new CustomColor();
        public CustomColor Color3 { get; set; } = new CustomColor();
        public CustomColor Color4 { get; set; } = new CustomColor();

        //Present for older XMLs only. Doesn't actually do anything.
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public CustomColor Color5 { get; set; }

        public CustomColor GetColor(int colorIndex)
        {
            switch (colorIndex)
            {
                case 0:
                    return Color1;
                case 1:
                    return Color2;
                case 2:
                    return Color3;
                case 3:
                    return Color4;
                default:
                    return null;
            }
        }

        public bool IsNull()
        {
            return Color1.R == 0f && Color1.G == 0f && Color1.B == 0f && Color1.A == 0f &&
                   Color2.R == 0f && Color2.G == 0f && Color2.B == 0f && Color2.A == 0f &&
                   Color3.R == 0f && Color3.G == 0f && Color3.B == 0f && Color3.A == 0f &&
                   Color4.R == 0f && Color4.G == 0f && Color4.B == 0f && Color4.A == 0f;
        }

        public List<IUndoRedo> PasteValues(Colors color)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.Add(new UndoableProperty<Colors>(nameof(Color1.R), this, Color1.R, color.Color1.R));
            undos.Add(new UndoableProperty<Colors>(nameof(Color1.G), this, Color1.G, color.Color1.G));
            undos.Add(new UndoableProperty<Colors>(nameof(Color1.B), this, Color1.B, color.Color1.B));
            undos.Add(new UndoableProperty<Colors>(nameof(Color1.A), this, Color1.A, color.Color1.A));
            undos.Add(new UndoableProperty<Colors>(nameof(Color2.R), this, Color1.R, color.Color2.R));
            undos.Add(new UndoableProperty<Colors>(nameof(Color2.G), this, Color1.G, color.Color2.G));
            undos.Add(new UndoableProperty<Colors>(nameof(Color2.B), this, Color1.B, color.Color2.B));
            undos.Add(new UndoableProperty<Colors>(nameof(Color2.A), this, Color1.A, color.Color2.A));
            undos.Add(new UndoableProperty<Colors>(nameof(Color3.R), this, Color1.R, color.Color3.R));
            undos.Add(new UndoableProperty<Colors>(nameof(Color3.G), this, Color1.G, color.Color3.G));
            undos.Add(new UndoableProperty<Colors>(nameof(Color3.B), this, Color1.B, color.Color3.B));
            undos.Add(new UndoableProperty<Colors>(nameof(Color3.A), this, Color1.A, color.Color3.A));
            undos.Add(new UndoableProperty<Colors>(nameof(Color4.R), this, Color1.R, color.Color4.R));
            undos.Add(new UndoableProperty<Colors>(nameof(Color4.G), this, Color1.G, color.Color4.G));
            undos.Add(new UndoableProperty<Colors>(nameof(Color4.B), this, Color1.B, color.Color4.B));
            undos.Add(new UndoableProperty<Colors>(nameof(Color4.A), this, Color1.A, color.Color4.A));

            Color1.R = color.Color1.R;
            Color1.G = color.Color1.G;
            Color1.B = color.Color1.B;
            Color1.A = color.Color1.A;
            Color2.R = color.Color2.R;
            Color2.G = color.Color2.G;
            Color2.B = color.Color2.B;
            Color2.A = color.Color2.A;
            Color3.R = color.Color3.R;
            Color3.G = color.Color3.G;
            Color3.B = color.Color3.B;
            Color3.A = color.Color3.A;
            Color4.R = color.Color4.R;
            Color4.G = color.Color4.G;
            Color4.B = color.Color4.B;
            Color4.A = color.Color4.A;

            RefreshColorValues();

            return undos;
        }

        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(ID));
            NotifyPropertyChanged(nameof(ToolTip));
            NotifyPropertyChanged(nameof(ColorPreview));
        }

        public void RefreshColorValues()
        {
            NotifyPropertyChanged(nameof(Color1));
            NotifyPropertyChanged(nameof(Color2));
            NotifyPropertyChanged(nameof(Color3));
            NotifyPropertyChanged(nameof(Color4));
        }

        #region Preview
        [YAXDontSerialize]
        public Brush ColorPreview => CreateColorPreview();

        [YAXDontSerialize]
        public string ToolTip => $"#{Index}";

        private SolidColorBrush CreateColorPreview()
        {
            if (!Color1.IsBlack())
                return new SolidColorBrush(Color.FromArgb(255, RgbConverter.ConvertToByte(Color1.R), RgbConverter.ConvertToByte(Color1.G), RgbConverter.ConvertToByte(Color1.B)));

            if (!Color4.IsBlack())
                return new SolidColorBrush(Color.FromArgb(255, RgbConverter.ConvertToByte(Color4.R), RgbConverter.ConvertToByte(Color4.G), RgbConverter.ConvertToByte(Color4.B)));

            if (!Color2.IsBlack())
                return new SolidColorBrush(Color.FromArgb(255, RgbConverter.ConvertToByte(Color2.R), RgbConverter.ConvertToByte(Color2.G), RgbConverter.ConvertToByte(Color2.B)));

            return new SolidColorBrush(Color.FromArgb(255, RgbConverter.ConvertToByte(Color3.R), RgbConverter.ConvertToByte(Color3.G), RgbConverter.ConvertToByte(Color3.B)));
        }

        public void RefreshPreview()
        {
            NotifyPropertyChanged(nameof(ColorPreview));
        }

        #endregion
    }

    //BCS Body
    [Serializable]
    public class Body : IInstallable, INotifyPropertyChanged
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

        [YAXDontSerialize]
        public int SortID
        {
            get
            {
                return int.Parse(Index);
            }
        }
        [YAXDontSerialize]
        public int ID
        {
            get => Utils.TryParseInt(Index);
            set => Index = value.ToString();
        }

        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } //int16
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BodyScale")]
        public AsyncObservableCollection<BoneScale> BodyScales { get; set; } = new AsyncObservableCollection<BoneScale>();

        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(ID));
        }
    }

    [YAXSerializeAs("BodyScale")]
    [Serializable]
    public class BoneScale : INotifyPropertyChanged
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

        [YAXDontSerialize]
        public string DisplayName => $"{BoneName} ({ScaleX}, {ScaleY}, {ScaleZ})";

        [YAXAttributeForClass]
        [YAXSerializeAs("Bone")]
        public string BoneName { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("X")]
        public float ScaleX { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Y")]
        public float ScaleY { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Z")]
        public float ScaleZ { get; set; }

        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(DisplayName));
        }
    }

    //Skeleton
    [Serializable]
    public class SkeletonData
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        public AsyncObservableCollection<Bone> Bones { get; set; } = new AsyncObservableCollection<Bone>();
    }

    [YAXSerializeAs("Bone")]
    [Serializable]
    public class Bone : INotifyPropertyChanged
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

        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string BoneName { get; set; }
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("F_36")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("F_40")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_44 { get; set; }

        public void RefreshValues()
        {
            NotifyPropertyChanged(nameof(BoneName));
        }

    }

}
