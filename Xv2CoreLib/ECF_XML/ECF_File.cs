using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.ECF_XML
{
    //This is the old ECF parser. It only exists now as solely an XML parser, since the new ECF parser doesn't support that.

    [Serializable]
    [YAXSerializeAs("ECF")]
    public class ECF_File
    {
        [YAXAttributeForClass]
        public ushort I_12 { get; set; }
        //followed by 12 zero bytes

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ColorEffect")]
        public List<ECF_Entry> Entries { get; set; }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static ECF_File Load(string path)
        {
            return new Parser(path, false).GetEcfFile();
        }

        public static ECF_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetEcfFile();
        }

    }

    [Serializable]
    [YAXSerializeAs("ColorEffect")]
    public class ECF_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("MaterialLink")]
        public string MaterialLink { get; set; } //Material linkage. "Node" seems to be the default value, applying to all materials.
        [YAXSerializeAs("StartFrame")]
        [YAXAttributeFor("Time")]
        public ushort I_56 { get; set; }
        [YAXSerializeAs("EndFrame")]
        [YAXAttributeFor("Time")]
        public ushort I_58 { get; set; }

        [YAXSerializeAs("R")]
        [YAXAttributeFor("DiffuseColor")]
        [YAXFormat("0.0######")]
        public float F_00 { get; set; }
        [YAXSerializeAs("G")]
        [YAXAttributeFor("DiffuseColor")]
        [YAXFormat("0.0######")]
        public float F_04 { get; set; }
        [YAXSerializeAs("B")]
        [YAXAttributeFor("DiffuseColor")]
        [YAXFormat("0.0######")]
        public float F_08 { get; set; }
        [YAXSerializeAs("A")]
        [YAXAttributeFor("DiffuseColor")]
        [YAXFormat("0.0######")]
        public float F_12 { get; set; }
        [YAXSerializeAs("R")]
        [YAXAttributeFor("SpecularColor")]
        [YAXFormat("0.0######")]
        public float F_16 { get; set; }
        [YAXSerializeAs("G")]
        [YAXAttributeFor("SpecularColor")]
        [YAXFormat("0.0######")]
        public float F_20 { get; set; }
        [YAXSerializeAs("B")]
        [YAXAttributeFor("SpecularColor")]
        [YAXFormat("0.0######")]
        public float F_24 { get; set; }
        [YAXSerializeAs("A")]
        [YAXAttributeFor("SpecularColor")]
        [YAXFormat("0.0######")]
        public float F_28 { get; set; }
        [YAXSerializeAs("R")]
        [YAXAttributeFor("AmbientColor")]
        [YAXFormat("0.0######")]
        public float F_32 { get; set; }
        [YAXSerializeAs("G")]
        [YAXAttributeFor("AmbientColor")]
        [YAXFormat("0.0######")]
        public float F_36 { get; set; }
        [YAXSerializeAs("B")]
        [YAXAttributeFor("AmbientColor")]
        [YAXFormat("0.0######")]
        public float F_40 { get; set; }
        [YAXSerializeAs("A")]
        [YAXAttributeFor("AmbientColor")]
        [YAXFormat("0.0######")]
        public float F_44 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("BlendingFactor")]
        [YAXFormat("0.0######")]
        public float F_48 { get; set; }
        [YAXSerializeAs("Mode")]
        [YAXAttributeFor("Loop")]
        public PlayMode I_52 { get; set; } //uint16
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_54")]
        public ushort I_54 { get; set; } //always 0
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_60")]
        public ushort I_60 { get; set; } //always 0
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_62")]
        public ushort I_62 { get; set; } //always 0
        [YAXSerializeAs("uint16")]
        [YAXAttributeFor("I_64")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public ushort[] I_64 { get; set; } // size = 14, all always 0
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_96")]
        public ushort I_96 { get; set; } //always 0

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation")]
        public List<Type0> Animations { get; set; } = new List<Type0>();


        public enum PlayMode : ushort
        {
            NoLoop = 2,
            Loop = 3
        }

    }

    [Serializable]
    [YAXSerializeAs("Animation")]
    public class Type0
    {
        [YAXDontSerialize]
        public bool IsAlpha
        {
            get
            {
                return (Component == ComponentEnum.A);
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Parameter")]
        public ParameterEnum Parameter { get; set; } //int8
        [YAXAttributeForClass]
        [YAXSerializeAs("Component")]
        public ComponentEnum Component { get; set; } //int4
        [YAXAttributeForClass]
        [YAXSerializeAs("Interpolated")]
        public bool Interpolated { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Looped")]
        public bool Loop { get; set; }
        [YAXAttributeFor("I_03")]
        [YAXSerializeAs("int8")]
        public byte I_03 { get; set; }

        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public ushort I_04 { get; set; }

        public AsyncObservableCollection<Type0_Keyframe> Keyframes { get; set; }

        public enum ParameterEnum
        {
            DiffuseColor = 0,
            SpecularColor = 1,
            AmbientColor = 2,
            BlendingFactor = 3
        }

        public enum ComponentEnum
        {
            R = 0,
            G = 1,
            B = 2,
            A = 3,
            Base = 4
        }

        public static ComponentEnum GetComponent(ParameterEnum parameter, int component)
        {
            switch (parameter)
            {
                case ParameterEnum.BlendingFactor:
                    return ComponentEnum.Base;
                default:
                    return (ComponentEnum)component;
            }
        }

        public byte GetComponent()
        {
            switch (Parameter)
            {
                case ParameterEnum.BlendingFactor:
                    return 0;
                default:
                    return (byte)Component;
            }
        }

    }


    [Serializable]
    [YAXSerializeAs("Keyframe")]
    public class Type0_Keyframe : ISortable
    {
        [YAXDontSerialize]
        public int SortID { get { return Index; } }

        [YAXAttributeForClass]
        public ushort Index { get; set; }
        [YAXAttributeForClass]
        [YAXFormat("0.0######")]
        public float Float { get; set; }
    }

}
