using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.BCS
{
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
    public class BCS_File : ISorting
    {

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
        public List<PartSet> PartSets { get; set; }
        [YAXDontSerializeIfNull]
        public List<PartColor> PartColors { get; set; }
        [YAXDontSerializeIfNull]
        public List<Body> Bodies { get; set; }
        [YAXDontSerializeIfNull]
        public SkeletonData SkeletonData1 { get; set; }
        [YAXDontSerializeIfNull]
        public SkeletonData SkeletonData2 { get; set; }



        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            if(PartSets != null)
                PartSets.Sort((x, y) => x.SortID - y.SortID);

            if (PartColors != null)
            {
                PartColors.Sort((x, y) => x.SortID - y.SortID);

                foreach (var partColor in PartColors)
                {
                    if(partColor.ColorsList != null)
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

        public PartColor GetPartColors(string id, string name)
        {
            if (PartColors == null) PartColors = new List<PartColor>();

            int index = PartColors.FindIndex(p => (p.Index == id));

            if(index != -1)
            {
                return PartColors[index];
            }
            else
            {
                PartColor newPartColor = new PartColor() { Index = id, Name = name, ColorsList = new List<Colors>() };
                PartColors.Add(newPartColor);
                return newPartColor;
            }

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
    }

    //PartSet
    public class PartSet : IInstallable
    {
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
        [BindingAutoId(999)]
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

        public void SetPart(Part part, PartType type)
        {
            if (part == null) return;
            if (Parts == null) Parts = new AsyncObservableCollection<Part>();

            part.PartType = type;
            var entry = GetPart(type);

            if (entry == null)
            {
                int insetIdx = (int)type;

                if (insetIdx <= Parts.Count)
                    Parts.Insert(insetIdx, part);
                else
                    Parts.Add(part);
            }
            else
            {
                Parts[Parts.IndexOf(entry)] = part;
            }
        }

        public Part GetPart(PartType type)
        {
            if (Parts == null) return null;
            return Parts.FirstOrDefault(x => x.PartType == type);
        }

#if DEBUG
        public List<Part> GetAllParts_DEBUG()
        {
            List<Part> parts = new List<Part>();

            if (FaceBase != null) parts.Add(FaceBase);
            if (FaceForehead != null) parts.Add(FaceForehead);
            if (FaceEye != null) parts.Add(FaceEye);
            if (FaceNose != null) parts.Add(FaceNose);
            if (FaceEar != null) parts.Add(FaceEar);
            if (Hair != null) parts.Add(Hair);
            if (Bust != null) parts.Add(Bust);
            if (Pants != null) parts.Add(Pants);
            if (Rist != null) parts.Add(Rist);
            if (Boots != null) parts.Add(Boots);

            return parts;
        }
#endif
    }

    public class Part
    {
        [Flags]
        public enum PartFlags : int
        {
            Unk1 = 0x1, //Seemingly does nothing
            DytFromTextureEmb = 0x2, //Both Model2 and Files_EMB
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
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("FaceBase")]
        public bool Hide_FaceBase { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Forehead")]
        public bool Hide_Forehead { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Eye")]
        public bool Hide_Eye { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Nose")]
        public bool Hide_Nose { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Ear")]
        public bool Hide_Ear { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Hair")]
        public bool Hide_Hair { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Bust")]
        public bool Hide_Bust { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Pants")]
        public bool Hide_Pants { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Rist")]
        public bool Hide_Rist { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Boots")]
        public bool Hide_Boots { get; set; }

        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("FaceBase")]
        public bool HideMat_FaceBase { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Forehead")]
        public bool HideMat_Forehead { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Eye")]
        public bool HideMat_Eye { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Nose")]
        public bool HideMat_Nose { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Ear")]
        public bool HideMat_Ear { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Hair")]
        public bool HideMat_Hair { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Bust")]
        public bool HideMat_Bust { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Pants")]
        public bool HideMat_Pants { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Rist")]
        public bool HideMat_Rist { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Boots")]
        public bool HideMat_Boots { get; set; }

        
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
        public List<ColorSelector> ColorSelectors { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PhysicsObject")]
        public AsyncObservableCollection<PhysicsPart> PhysicsParts { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Unk3")]
        public List<Unk3> Unk_3 { get; set; }

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
            else if(Model != -1)
            {
                //Uses Model1
                return string.Format("{0}.dyt.emb", GetPath(Model, partType));
            }

            return null;
        }

        public string GetEanPath()
        {
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

    }

    public class ColorSelector
    {
        [YAXAttributeFor("PartColors")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("value")]
        public short I_02 { get; set; }
    }

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
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("FaceBase")]
        public bool Hide_FaceBase { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Forehead")]
        public bool Hide_Forehead { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Eye")]
        public bool Hide_Eye { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Nose")]
        public bool Hide_Nose { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Ear")]
        public bool Hide_Ear { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Hair")]
        public bool Hide_Hair { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Bust")]
        public bool Hide_Bust { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Pants")]
        public bool Hide_Pants { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Rist")]
        public bool Hide_Rist { get; set; }
        [YAXAttributeFor("HideParts")]
        [YAXSerializeAs("Boots")]
        public bool Hide_Boots { get; set; }

        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("FaceBase")]
        public bool HideMat_FaceBase { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Forehead")]
        public bool HideMat_Forehead { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Eye")]
        public bool HideMat_Eye { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Nose")]
        public bool HideMat_Nose { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Ear")]
        public bool HideMat_Ear { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Hair")]
        public bool HideMat_Hair { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Bust")]
        public bool HideMat_Bust { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Pants")]
        public bool HideMat_Pants { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Rist")]
        public bool HideMat_Rist { get; set; }
        [YAXAttributeFor("HideMats")]
        [YAXSerializeAs("Boots")]
        public bool HideMat_Boots { get; set; }

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
            if(string.IsNullOrWhiteSpace(EmbPath))
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

    public class Unk3
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public short[] I_00 { get; set; } //size 6
    }

    //Color
    public class PartColor : IInstallable
    {
        [YAXDontSerialize]
        public int SortID
        {
            get
            {
                return int.Parse(Index);
            }
        }

        [YAXAttributeForClass]
        public string Index { get; set; } //int16
        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Colors")]
        public List<Colors> ColorsList { get; set; }

        public void AddColor(Colors color)
        {
            if (ColorsList == null) ColorsList = new List<Colors>();
            int idx = int.Parse(color.Index);

            int existingIndex = ColorsList.FindIndex(p => p.Index == color.Index);

            if(existingIndex != -1)
            {
                ColorsList[existingIndex] = color;
            }
            else
            {
                //Index doesnt exist. Pad entries until index is reached then add entry.

                while ((ColorsList.Count - 1) < (idx - 1))
                {
                    ColorsList.Add(new Colors() { Index = ColorsList.Count.ToString() });
                }

                ColorsList.Add(color);
            }
        }
        
    }

    [YAXSerializeAs("Colors")]
    public class Colors : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { int value; if (int.TryParse(Index, out value)) return value; return 0; } }

        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float Color1_R { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float Color1_G { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float Color1_B { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float Color1_A { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float Color2_R { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float Color2_G { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float Color2_B { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float Color2_A { get; set; }
        [YAXAttributeFor("Color3")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float Color3_R { get; set; }
        [YAXAttributeFor("Color3")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float Color3_G { get; set; }
        [YAXAttributeFor("Color3")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float Color3_B { get; set; }
        [YAXAttributeFor("Color3")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float Color3_A { get; set; }
        [YAXAttributeFor("Color4")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float Color4_R { get; set; }
        [YAXAttributeFor("Color4")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float Color4_G { get; set; }
        [YAXAttributeFor("Color4")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float Color4_B { get; set; }
        [YAXAttributeFor("Color4")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float Color4_A { get; set; }
        [YAXAttributeFor("Color5")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float Color5_R { get; set; }
        [YAXAttributeFor("Color5")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float Color5_G { get; set; }
        [YAXAttributeFor("Color5")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float Color5_B { get; set; }
        [YAXAttributeFor("Color5")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float Color5_A { get; set; }
    }

    //BCS Body
    public class Body : IInstallable
    {
        [YAXDontSerialize]
        public int SortID
        {
            get
            {
                return int.Parse(Index);
            }
        }

        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } //int16
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BodyScale")]
        public List<BoneScale> BodyScales { get; set; }
    }

    [YAXSerializeAs("BodyScale")]
    public class BoneScale
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Bone")]
        public string Str_12 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("X")]
        public float ScaleX { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Y")]
        public float ScaleY { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Z")]
        public float ScaleZ { get; set; }
    }

    //Skeleton
    public class SkeletonData
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        public List<Bone> Bones { get; set; }
    }

    [YAXSerializeAs("Bone")]
    public class Bone
    {
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
    }

}
