using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    [YAXSerializeAs("BCS")]
    public class BCS_File : ISorting
    {
        [YAXAttributeFor("Race")]
        [YAXSerializeAs("value")]
        [YAXFormat("X02")]
        public byte Race { get; set; }
        [YAXAttributeFor("Female")]
        [YAXSerializeAs("value")]
        public bool Sex { get; set; }
        [YAXAttributeFor("F_48")]
        [YAXSerializeAs("values")]
        [YAXFormat("0.0###########")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public float[] F_48 { get; set; } // size 7
        
        [YAXDontSerializeIfNull]
        public List<PartSet> PartSets { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("PartColors")]
        public List<PartColor> Part_Colors { get; set; }
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

            if (Part_Colors != null)
            {
                Part_Colors.Sort((x, y) => x.SortID - y.SortID);

                foreach (var partColor in Part_Colors)
                {
                    partColor._Colors.Sort((x, y) => x.SortID - y.SortID);
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

        public PartColor GetPartColors(string id, string name)
        {
            if (Part_Colors == null) Part_Colors = new List<PartColor>();

            int index = Part_Colors.FindIndex(p => (p.Index == id));

            if(index != -1)
            {
                return Part_Colors[index];
            }
            else
            {
                PartColor newPartColor = new PartColor() { Index = id, Str_00 = name, _Colors = new List<Colors>() };
                Part_Colors.Add(newPartColor);
                return newPartColor;
            }

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
        [YAXDontSerializeIfNull]
        public Part FaceBase { get; set; }
        [YAXDontSerializeIfNull]
        public Part FaceForehead { get; set; }
        [YAXDontSerializeIfNull]
        public Part FaceEye { get; set; }
        [YAXDontSerializeIfNull]
        public Part FaceNose { get; set; }
        [YAXDontSerializeIfNull]
        public Part FaceEar { get; set; }
        [YAXDontSerializeIfNull]
        public Part Hair { get; set; }
        [YAXDontSerializeIfNull]
        public Part Bust { get; set; }
        [YAXDontSerializeIfNull]
        public Part Pants { get; set; }
        [YAXDontSerializeIfNull]
        public Part Rist { get; set; }
        [YAXDontSerializeIfNull]
        public Part Boots { get; set; }
    }

    public class Part
    {

        [YAXAttributeFor("Model")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAttributeFor("Model2")]
        [YAXSerializeAs("value")]
        public short I_02 { get; set; }
        [YAXAttributeFor("Texture")]
        [YAXSerializeAs("value")]
        public short I_04 { get; set; }
        [YAXAttributeFor("Shader")]
        [YAXSerializeAs("value")]
        public short I_16 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint I_24 { get; set; }

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
        public string Str_52 { get; set; }
        //Str
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMD")]
        [BindingString]
        public string Str_56 { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMM")]
        [BindingString]
        public string Str_60 { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMB")]
        [BindingString]
        public string Str_64 { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EAN")]
        [BindingString]
        public string Str_68 { get; set; }

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ColorSelector")]
        public List<ColorSelector> Color_Selectors { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PhysicsObject")]
        public List<PhysicsObject> Physics_Objects { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Unk3")]
        public List<Unk3> Unk_3 { get; set; }

        public string GetModelPath(PartType partType)
        {
            if ((Str_56 == "NULL" || String.IsNullOrWhiteSpace(Str_56)) && I_00 != -1)
            {
                //Use I_00
                switch (partType)
                {
                    case PartType.FaceBase:
                        return string.Format("chara/{0}/{0}_{1}_Face_base.emd", Str_52, I_00.ToString("D3"));
                    case PartType.FaceForehead:
                        return string.Format("chara/{0}/{0}_{1}_Face_forehead.emd", Str_52, I_00.ToString("D3"));
                    case PartType.FaceEye:
                        return string.Format("chara/{0}/{0}_{1}_Face_eye.emd", Str_52, I_00.ToString("D3"));
                    case PartType.FaceNose:
                        return string.Format("chara/{0}/{0}_{1}_Face_nose.emd", Str_52, I_00.ToString("D3"));
                    case PartType.FaceEar:
                        return string.Format("chara/{0}/{0}_{1}_Face_ear.emd", Str_52, I_00.ToString("D3"));
                    case PartType.Bust:
                        return string.Format("chara/{0}/{0}_{1}_Bust.emd", Str_52, I_00.ToString("D3"));
                    case PartType.Boots:
                        return string.Format("chara/{0}/{0}_{1}_Boots.emd", Str_52, I_00.ToString("D3"));
                    case PartType.Rist:
                        return string.Format("chara/{0}/{0}_{1}_Rist.emd", Str_52, I_00.ToString("D3"));
                    case PartType.Hair:
                        return string.Format("chara/{0}/{0}_{1}_Hair.emd", Str_52, I_00.ToString("D3"));
                    case PartType.Pants:
                        return string.Format("chara/{0}/{0}_{1}_Pants.emd", Str_52, I_00.ToString("D3"));
                    default:
                        throw new InvalidDataException("Invalid partType = " + partType);
                }
            }
            else if (Str_56 != "NULL" && !String.IsNullOrWhiteSpace(Str_56))
            {
                return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emd", Str_52, Str_56));
            }
            else
            {
                //Part has no model
                return null;
            }
        }

        
    }

    public struct ColorSelector
    {
        [YAXAttributeFor("PartColors")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("value")]
        public short I_02 { get; set; }
    }

    public class PhysicsObject
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public short I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public short I_02 { get; set; }
        [YAXAttributeFor("Texture")]
        [YAXSerializeAs("value")]
        public short I_04 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public uint I_24 { get; set; }


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
        public string Str_36 { get; set; }

        //Str
        [YAXAttributeFor("BoneToAttach")]
        [YAXSerializeAs("value")]
        [BindingString]
        public string BoneToAttach { get; set; }

        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMD")]
        [BindingString]
        public string Files_EMD { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMM")]
        [BindingString]
        public string Files_EMM { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EMB")]
        [BindingString]
        public string Files_EMB { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("EAN")]
        [BindingString]
        public string Files_EAN { get; set; }
        [YAXAttributeFor("Files")]
        [YAXSerializeAs("SCD")]
        [BindingString]
        public string Files_SCD { get; set; }

        public string GetModelPath(PartType partType)
        {
            return Utils.ResolveRelativePath(string.Format("chara/{0}/{1}.emd", Str_36, Files_EMD));
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
        public string Str_00 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Colors")]
        public List<Colors> _Colors { get; set; }

        public void AddColor(Colors color)
        {
            if (_Colors == null) _Colors = new List<Colors>();
            int idx = int.Parse(color.Index);

            int existingIndex = _Colors.FindIndex(p => p.Index == color.Index);

            if(existingIndex != -1)
            {
                _Colors[existingIndex] = color;
            }
            else
            {
                //Index doesnt exist. Pad entries until index is reached then add entry.

                while ((_Colors.Count - 1) < (idx - 1))
                {
                    _Colors.Add(new Colors() { Index = _Colors.Count.ToString() });
                }

                _Colors.Add(color);
            }
        }
        
    }

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
        public float F_00 { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Color1")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Color2")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Color3")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Color3")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Color3")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Color3")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Color4")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("Color4")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Color4")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Color4")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("Color5")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("Color5")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("Color5")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float F_72 { get; set; }
        [YAXAttributeFor("Color5")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float F_76 { get; set; }
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
        public float F_00 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Y")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Z")]
        public float F_08 { get; set; }
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
        public string Str_48 { get; set; }
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
