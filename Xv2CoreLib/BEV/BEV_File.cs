using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BEV
{
    [YAXSerializeAs("BEV")]
    public class BEV_File : ISorting
    {
        [YAXSerializeAs("BEV_Entries")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BEV_Entry")]
        public List<Entry> Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public void SortEntries()
        {
            Entries.Sort((x, y) => x.SortID - y.SortID);
        }

        public void AddEntry(int id, Entry entry)
        {
            for(int i = 0; i < Entries.Count; i++)
            {
                if(int.Parse(Entries[i].Index) == id)
                {
                    Entries[i] = entry;
                    return;
                }
            }

            Entries.Add(entry);
        }

        public void SaveBinary(string path)
        {
            new Deserializer(this, path);
        }

        public static BEV_File Load(byte[] bytes)
        {
            return new Parser(bytes).bevFile;
        }

    }

    [YAXSerializeAs("BEV_Entry")]
    public class Entry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        [BindingAutoId]
        public string Index { get; set; } //int32
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int I_12 { get; set; } //int32
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }



        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ActorControl")]
        public List<Type_0> Type0 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Camera")]
        public List<Type_1> Type1 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Effect")]
        public List<Type_2> Type2 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Sound")]
        public List<Type_3> Type3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SceneControl")]
        public List<Type_4> Type4 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BEV_Type5")]
        public List<Type_5> Type5 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BEV_Type6")]
        public List<Type_6> Type6 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BEV_Type7")]
        public List<Type_7> Type7 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BEV_Type8")]
        public List<Type_8> Type8 { get; set; }
    }
    
    //Data Types below

    [YAXSerializeAs("ActorControl")]
    public class Type_0 : TypeDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Layer")]
        public int Idx { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("ID")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("Parameter")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("I_18")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
        [YAXAttributeFor("Flag")]
        [YAXSerializeAs("value")]
        public bool I_20 { get; set; } //int16
        [YAXAttributeFor("I_22")]
        [YAXSerializeAs("value")]
        public ushort I_22 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public ushort I_24 { get; set; }
        [YAXAttributeFor("I_26")]
        [YAXSerializeAs("value")]
        public ushort I_26 { get; set; }



        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_48 { get; set; }
        

    }

    [YAXSerializeAs("Camera")]
    public class Type_1 : TypeDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Layer")]
        public int Idx { get; set; }

        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }


        [YAXAttributeFor("StartTargetPosition")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("StartTargetPosition")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("StartTargetPosition")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("EndTargetPosition")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("EndTargetPosition")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("EndTargetPosition")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("StartCameraPosition")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("StartCameraPosition")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("StartCameraPosition")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("EndCameraPosition")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("EndCameraPosition")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("EndCameraPosition")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("F_64")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("F_68")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("F_72")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_72 { get; set; }
        [YAXAttributeFor("FOV")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_76 { get; set; }

    }

    [YAXSerializeAs("Effect")]
    public class Type_2 : TypeDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Layer")]
        public int Idx { get; set; }

        [YAXAttributeFor("Actor")]
        [YAXSerializeAs("ID")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("BoneIndex")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("EffectID")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }

        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
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
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }



    }

    [YAXSerializeAs("Sound")]
    public class Type_3 : TypeDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Layer")]
        public int Idx { get; set; }
        [YAXAttributeFor("Acb_To_Use")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("Cue_ID")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
    }

    [YAXSerializeAs("SceneControl")]
    public class Type_4 : TypeDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Layer")]
        public int Idx { get; set; }
        [YAXAttributeFor("Function")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("Parameter1")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("Parameter2")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("Parameter3")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXAttributeFor("Parameter4")]
        [YAXSerializeAs("value")]
        public ushort I_16 { get; set; }
        [YAXAttributeFor("Parameter5")]
        [YAXSerializeAs("value")]
        public ushort I_18 { get; set; }
    }

    [YAXSerializeAs("BEV_Type5")]
    public class Type_5 : TypeDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Layer")]
        public int Idx { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public ushort I_04 { get; set; }
        [YAXAttributeFor("I_06")]
        [YAXSerializeAs("value")]
        public ushort I_06 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public ushort I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public ushort I_10 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public ushort I_12 { get; set; }
        [YAXAttributeFor("I_14")]
        [YAXSerializeAs("value")]
        public ushort I_14 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("values")]
        public int[] I_16 { get; set; } //Size = 12
    }

    [YAXSerializeAs("BEV_Type6")]
    public class Type_6 : TypeDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Layer")]
        public int Idx { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("F_08")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("F_12")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_28 { get; set; }
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
    }

    [YAXSerializeAs("BEV_Type7")]
    public class Type_7 : TypeDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Layer")]
        public int Idx { get; set; }
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
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        public float F_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("F_32")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("F_48")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("F_56")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("F_60")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public int I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public int I_72 { get; set; }
        [YAXAttributeFor("F_76")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_76 { get; set; }
        [YAXAttributeFor("F_80")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.#########")]
        public float F_80 { get; set; }
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
        [YAXAttributeFor("I_124")]
        [YAXSerializeAs("value")]
        public int I_124 { get; set; }
        [YAXAttributeFor("I_128")]
        [YAXSerializeAs("value")]
        public int I_128 { get; set; }
        [YAXAttributeFor("I_132")]
        [YAXSerializeAs("value")]
        public int I_132 { get; set; }
        [YAXAttributeFor("I_136")]
        [YAXSerializeAs("value")]
        public int I_136 { get; set; }
        [YAXAttributeFor("I_140")]
        [YAXSerializeAs("value")]
        public int I_140 { get; set; }
    }

    [YAXSerializeAs("BEV_Type8")]
    public class Type_8 : TypeDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("StartTime")]
        public ushort I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Layer")]
        public int Idx { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public uint I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public uint I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public uint I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public uint I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public uint I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public uint I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public uint I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public uint I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public uint I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public uint I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public uint I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public uint I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public uint I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public uint I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public uint I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public uint I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public uint I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public uint I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public uint I_76 { get; set; }
    }

    public interface TypeDef
    {
        int Idx { get; set; }
        ushort I_00 { get; set; } //Start time
        ushort I_02 { get; set; } //Duration
    }

}
