using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BPE
{
    [YAXSerializeAs("BPE")]
    public class BPE_File : ISorting
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BPE_Entry")]
        public List<BPE_Entry> Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static BPE_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetBpeFile();
        }

        public void SaveBinary(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

        public void SortEntries()
        {
            Entries.Sort((x, y) => x.SortID - y.SortID);
        }

    }
    
    public class BPE_Entry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } //int32
        [YAXAttributeForClass]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeForClass]
        public UInt16 I_06 { get; set; }
        [YAXAttributeForClass]
        public UInt16 I_08 { get; set; }

        
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BPE_SubEntry")]
        public List<BPE_SubEntry> SubEntries { get; set; }

        

    }

    [YAXSerializeAs("BPE_SubEntry")]
    public class BPE_SubEntry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public string I_00 { get; set; } //Int32
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeFor("End_Time")]
        [YAXSerializeAs("value")]
        public UInt16 I_06 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public UInt16 I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public UInt16 I_10 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0##########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public UInt16 I_40 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Blur")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type0> Type0 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "WhiteShine")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type1> Type1 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BPE_Type2")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type2> Type2 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BPE_Type3")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type3> Type3 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Disorientation")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type4> Type4 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BPE_Type5")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type5> Type5 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Zoom")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type6> Type6 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BPE_Type7")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type7> Type7 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Hue")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type8> Type8 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BodyOutline")]
        [YAXDontSerializeIfNull]
        public List<BPE_Type9> Type9 { get; set; }


        public static string GetBpeType(int type)
        {
            switch (type)
            {
                case 0:
                    return "Blur";
                case 1:
                    return "WhiteShine";
                case 4:
                    return "Disorientation";
                case 6:
                    return "Zoom";
                case 8:
                    return "Hue";
                case 9:
                    return "BodyOutline";
                default:
                    return type.ToString();
            }
        }

        public int GetBpeType()
        {
            switch (I_00)
            {
                case "Blur":
                    return 0;
                case "WhiteShine":
                    return 1;
                case "Disorientation":
                    return 4;
                case "Zoom":
                    return 6;
                case "Hue":
                    return 8;
                case "BodyOutline":
                    return 9;
                default:
                    return Int32.Parse(I_00);
            }
        }

    }

    [YAXSerializeAs("Blur")]
    public class BPE_Type0
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("Amount")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_04 { get; set; }
    }

    [YAXSerializeAs("WhiteShine")]
    public class BPE_Type1
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Intensity")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_12 { get; set; }
    }

    [YAXSerializeAs("BPE_Type2")]
    public class BPE_Type2
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
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
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("B")]
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
        [YAXAttributeFor("F_48")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_48 { get; set; }
    }
    
    [YAXSerializeAs("BPE_Type3")]
    public class BPE_Type3
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
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
    }

    [YAXSerializeAs("Disorientation")]
    public class BPE_Type4
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
    }

    [YAXSerializeAs("BPE_Type5")]
    public class BPE_Type5
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
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

    [YAXSerializeAs("Zoom")]
    public class BPE_Type6
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("EffectArea")]
        [YAXSerializeAs("Size")]
        [YAXFormat("0.0###########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("A")]
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
        [YAXAttributeFor("ZoomLevel")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_44 { get; set; }
    }

    [YAXSerializeAs("BPE_Type7")]
    public class BPE_Type7
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_04 { get; set; }
    }

    [YAXSerializeAs("Hue")]
    public class BPE_Type8
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("BlendMode")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("A")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
    }

    [YAXSerializeAs("BodyOutline")]
    public class BPE_Type9
    {
        [YAXAttributeFor("Start_Time")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("NearFadeDistance")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("FarFadeDistance")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Transparency")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("R")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("G")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Color")]
        [YAXSerializeAs("B")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
    }


}
