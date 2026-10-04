using LB_Common.Numbers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.EMP_XML
{
    //This EMP parser is obsolete and only kept here for XML serialization since the newer one is incapable of that (intentionally).

    public enum VersionEnum : ushort
    {
        DBXV2 = 37568,
        SDBH = 37632
    }

    [Serializable]
    [YAXSerializeAs("EMP")]
    [Obsolete("This EMP parser is obsolete and only used for XML now. Use Xv2CoreLib.EMP_NEW instead!")]
    public class EMP_File
    {
        public const int EMP_SIGNATURE = 1347241251;

        [YAXAttributeForClass]
        public VersionEnum Version { get; set; } = VersionEnum.DBXV2;

        [YAXDontSerializeIfNull]
        public AsyncObservableCollection<ParticleEffect> ParticleEffects { get; set; } = new AsyncObservableCollection<ParticleEffect>();
        [YAXDontSerializeIfNull]
        public AsyncObservableCollection<EMP_TextureDefinition> Textures { get; set; } = new AsyncObservableCollection<EMP_TextureDefinition>();

    }

    //Section1/MainEntry

    [Serializable]
    public class ParticleEffect
    {
        public enum ComponentType
        {
            None, //"null"
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

        [YAXDontSerialize]
        public IEnumerable<ComponentType> ComponentTypeValues
        {
            get
            {
                return Enum.GetValues(typeof(ComponentType))
                    .Cast<ComponentType>();
            }
        }


        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name { get; set; }

        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public ComponentType Component_Type { get; set; }

        //Start
        [YAXAttributeFor("StartTime")]
        [YAXSerializeAs("Base")]
        public byte I_44 { get; set; }
        [YAXAttributeFor("StartTime")]
        [YAXSerializeAs("AddedRandom")]
        public byte I_45 { get; set; }
        [YAXAttributeFor("Loop")]
        [YAXSerializeAs("value")]
        public bool I_32_1 { get; set; }
        [YAXAttributeFor("FlashOnGeneration")]
        [YAXSerializeAs("value")]
        public bool I_32_3 { get; set; }
        [YAXAttributeFor("NumberOfParticles")]
        [YAXSerializeAs("value")]
        public short I_38 { get; set; }
        [YAXAttributeFor("Particle_Lifetime")]
        [YAXSerializeAs("Base")]
        public short I_40 { get; set; }
        [YAXAttributeFor("Particle_Lifetime")]
        [YAXSerializeAs("AddedRandom")]
        public ushort I_42 { get; set; }
        [YAXAttributeFor("Hide")]
        [YAXSerializeAs("value")]
        public bool I_33_3 { get; set; }
        [YAXAttributeFor("UseScale2")]
        [YAXSerializeAs("value")]
        public bool I_33_4 { get; set; }
        [YAXAttributeFor("UseColor2")]
        [YAXSerializeAs("value")]
        public bool I_33_6 { get; set; }
        [YAXAttributeFor("AutoOrientationType")]
        [YAXSerializeAs("value")]
        public AutoOrientationType I_35 { get; set; } //int8
        [YAXAttributeFor("NumberOfFramesBetweenNewParticles")]
        [YAXSerializeAs("Base")]
        public byte I_46 { get; set; }
        [YAXAttributeFor("NumberOfFramesBetweenNewParticles")]
        [YAXSerializeAs("AddedRandom")]
        public byte I_47 { get; set; }
        [YAXAttributeFor("MaxParticlesPerFrame")]
        [YAXSerializeAs("Base")]
        public ushort I_52 { get; set; }
        [YAXAttributeFor("MaxParticlesPerFrame")]
        [YAXSerializeAs("AddedRandom")]
        public ushort I_54 { get; set; }

        [YAXAttributeFor("PositionOffset")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_64 { get; set; }
        [YAXAttributeFor("PositionOffset")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_68 { get; set; }
        [YAXAttributeFor("PositionOffset")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_72 { get; set; }
        [YAXAttributeFor("PositionOffset")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0###########")]
        public float F_76 { get; set; }
        [YAXAttributeFor("PositionOffset_AddedRandom")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_80 { get; set; }
        [YAXAttributeFor("PositionOffset_AddedRandom")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_84 { get; set; }
        [YAXAttributeFor("PositionOffset_AddedRandom")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_88 { get; set; }
        [YAXAttributeFor("PositionOffset_AddedRandom")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0###########")]
        public float F_92 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_100 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0###########")]
        public float F_108 { get; set; }
        [YAXAttributeFor("Rotation_AddedRandom")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_112 { get; set; }
        [YAXAttributeFor("Rotation_AddedRandom")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_116 { get; set; }
        [YAXAttributeFor("Rotation_AddedRandom")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_120 { get; set; }
        [YAXAttributeFor("Rotation_AddedRandom")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0###########")]
        public float F_124 { get; set; }
        [YAXAttributeFor("EnableRandomRotationDirection")]
        [YAXSerializeAs("value")]
        public bool I_34_4 { get; set; }
        [YAXAttributeFor("EnableRandomUpVectorOnVirtualCone")]
        [YAXSerializeAs("value")]
        public bool I_34_6 { get; set; }
        [YAXAttributeFor("F_128")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_128 { get; set; }
        [YAXAttributeFor("F_132")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float F_132 { get; set; }
        [YAXAttributeFor("I_136")]
        [YAXSerializeAs("value")]
        public ushort I_136 { get; set; }

        //Unknown values
        [YAXAttributeFor("Flags_32")]
        [YAXSerializeAs("Unk0")]
        public bool I_32_0 { get; set; }
        [YAXAttributeFor("Flags_32")]
        [YAXSerializeAs("Unk2")]
        public bool I_32_2 { get; set; }
        [YAXAttributeFor("Flags_32")]
        [YAXSerializeAs("Unk4")]
        public bool I_32_4 { get; set; }
        [YAXAttributeFor("Flags_32")]
        [YAXSerializeAs("Unk5")]
        public bool I_32_5 { get; set; }
        [YAXAttributeFor("Flags_32")]
        [YAXSerializeAs("Unk6")]
        public bool I_32_6 { get; set; }
        [YAXAttributeFor("Flags_32")]
        [YAXSerializeAs("Unk7")]
        public bool I_32_7 { get; set; }


        //I_33 test
        [YAXAttributeFor("Flags_33")]
        [YAXSerializeAs("Unk0")]
        public bool I_33_0 { get; set; }
        [YAXAttributeFor("Flags_33")]
        [YAXSerializeAs("Unk1")]
        public bool I_33_1 { get; set; }
        [YAXAttributeFor("Flags_33")]
        [YAXSerializeAs("Unk2")]
        public bool I_33_2 { get; set; }
        [YAXAttributeFor("Flags_33")]
        [YAXSerializeAs("Unk5")]
        public bool I_33_5 { get; set; }
        [YAXAttributeFor("Flags_33")]
        [YAXSerializeAs("Unk7")]
        public bool I_33_7 { get; set; }
        //I_34 test
        [YAXAttributeFor("Flags_34")]
        [YAXSerializeAs("Unk0")]
        public bool I_34_0 { get; set; }
        [YAXAttributeFor("Flags_34")]
        [YAXSerializeAs("Unk1")]
        public bool I_34_1 { get; set; }
        [YAXAttributeFor("Flags_34")]
        [YAXSerializeAs("Unk2")]
        public bool I_34_2 { get; set; }
        [YAXAttributeFor("Flags_34")]
        [YAXSerializeAs("Unk3")]
        public bool I_34_3 { get; set; }
        [YAXAttributeFor("Flags_34")]
        [YAXSerializeAs("Unk5")]
        public bool I_34_5 { get; set; }
        [YAXAttributeFor("Flags_34")]
        [YAXSerializeAs("Unk7")]
        public bool I_34_7 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public ushort I_48 { get; set; }
        [YAXAttributeFor("I_50")]
        [YAXSerializeAs("value")]
        public ushort I_50 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public ushort I_56 { get; set; }
        [YAXAttributeFor("I_58")]
        [YAXSerializeAs("value")]
        public ushort I_58 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public ushort I_60 { get; set; }
        [YAXAttributeFor("I_62")]
        [YAXSerializeAs("value")]
        public ushort I_62 { get; set; }

        //ExtraPart data

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("TexturePart")]
        public TexturePart Type_Texture { get; set; }

        [YAXDontSerializeIfNull]
        [YAXSerializeAs("VerticalDistribution_Component")]
        public FloatPart_0_1 FloatPart_00_01 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("SphericalDistribution_Component")]
        public FloatPart_1_1 FloatPart_01_01 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ShapePerimeterDistribution_Component")]
        public FloatPart_2_1 FloatPart_02_01 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ShapeAreaDistribution_Component")]
        public FloatPart_3_1 FloatPart_03_01 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("AutoOriented_Component")]
        public FloatPart_0_2 FloatPart_00_02 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Default_Component")]
        public FloatPart_2_2 FloatPart_02_02 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ConeExtrude_Component")]
        public Struct3 Type_Struct3 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ShapeDraw_Component")]
        public Struct5 Type_Struct5 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Mesh_Component")]
        public ModelStruct Type_Model { get; set; }

        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation")]
        public AsyncObservableCollection<Type0> Type_0 { get; set; }
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation_SubGroup")]
        public AsyncObservableCollection<Type1_Header> Type_1 { get; set; }

        //SubEntry
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "ParticleEffect")]
        public AsyncObservableCollection<ParticleEffect> ChildParticleEffects { get; set; }

        public enum AutoOrientationType
        {
            Camera = 0,
            Front = 1
        }

        public static ComponentType GetComponentType(int[] flags)
        {
            if (flags[1] == 2)
            {
                switch (flags[0])
                {
                    case 0:
                        return ComponentType.AutoOriented;
                    case 1:
                        return ComponentType.AutoOriented_VisibleOnSpeed;
                    case 2:
                        return ComponentType.Default;
                    case 3:
                        return ComponentType.ConeExtrude;
                    case 4:
                        return ComponentType.Mesh;
                    case 5:
                        return ComponentType.ShapeDraw;
                    default:
                        return ComponentType.None;

                }
            }
            else if (flags[1] == 1)
            {
                switch (flags[0])
                {
                    case 0:
                        return ComponentType.VerticalDistribution;
                    case 1:
                        return ComponentType.SphericalDistribution;
                    case 2:
                        return ComponentType.ShapePerimeterDistribution;
                    case 3:
                        return ComponentType.ShapeAreaDistribution;
                    default:
                        return ComponentType.None;

                }
            }
            else
            {
                return ComponentType.None;
            }
        }

        public string GetDataFlagsFromDeclaredType()
        {
            switch (Component_Type)
            {
                case ComponentType.AutoOriented:
                    return "00_02";
                case ComponentType.AutoOriented_VisibleOnSpeed:
                    return "01_02";
                case ComponentType.Default:
                    return "02_02";
                case ComponentType.ConeExtrude:
                    return "03_02";
                case ComponentType.Mesh:
                    return "04_02";
                case ComponentType.ShapeDraw:
                    return "05_02";
                case ComponentType.VerticalDistribution:
                    return "00_01";
                case ComponentType.SphericalDistribution:
                    return "01_01";
                case ComponentType.ShapePerimeterDistribution:
                    return "02_01";
                case ComponentType.ShapeAreaDistribution:
                    return "03_01";
                case ComponentType.None:
                    return "00_00";
                default:
                    return "00_00";
            }
        }

        public bool IsScale2Enabled()
        {
            return I_33_4;
        }

        public static ParticleEffect GetNew(bool initComponents = true)
        {
            if (initComponents)
            {
                return new ParticleEffect()
                {
                    Name = "New ParticleEffect",
                    Component_Type = ComponentType.None,
                    ChildParticleEffects = new AsyncObservableCollection<ParticleEffect>(),
                    Type_Texture = TexturePart.GetNew(),
                    FloatPart_00_01 = FloatPart_0_1.GetNew(),
                    FloatPart_00_02 = FloatPart_0_2.GetNew(),
                    FloatPart_01_01 = FloatPart_1_1.GetNew(),
                    FloatPart_02_01 = FloatPart_2_1.GetNew(),
                    FloatPart_02_02 = FloatPart_2_2.GetNew(),
                    FloatPart_03_01 = FloatPart_3_1.GetNew(),
                    Type_Model = ModelStruct.GetNew(),
                    Type_Struct3 = Struct3.GetNew(),
                    Type_Struct5 = Struct5.GetNew(),
                    I_35 = AutoOrientationType.Camera,
                    Type_0 = new AsyncObservableCollection<Type0>(),
                    Type_1 = new AsyncObservableCollection<Type1_Header>()
                };
            }
            else
            {
                return new ParticleEffect()
                {
                    Name = "New ParticleEffect",
                    Component_Type = ComponentType.None,
                    I_35 = AutoOrientationType.Camera
                };
            }
            
        }

        
    }

    //FLAG36/37 data
    [Serializable]
    [YAXSerializeAs("TexturePart")]
    public class TexturePart
    {
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("Texture_ID")]
        public AsyncObservableCollection<int> TextureIndex { get; set; } //XML Mode (TextureEntryRef for Tool mode)

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("int8")]
        public byte I_00 { get; set; }
        [YAXAttributeFor("I_01")]
        [YAXSerializeAs("int8")]
        public byte I_01 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("int8")]
        public byte I_02 { get; set; }
        [YAXAttributeFor("I_03")]
        [YAXSerializeAs("int8")]
        public byte I_03 { get; set; }


        [YAXAttributeFor("AddedDepth")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("EmmMaterial")]
        [YAXSerializeAs("Index")]
        public ushort I_16 { get; set; }

        //Colors
        public CustomColor Color1 { get; set; } = new CustomColor();
        public CustomColor Color2 { get; set; } = new CustomColor();
        public CustomColor Color_Variance { get; set; } = new CustomColor();

        //Remaining floats
        [YAXAttributeFor("Scale1")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Scale1")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Scale2")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Scale2_AddedRandom")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Scale2")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Scale2_AddedRandom")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0########")]
        public float F_44 { get; set; }
        [YAXAttributeFor("F_96")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_96 { get; set; }
        [YAXAttributeFor("F_100")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_100 { get; set; }
        [YAXAttributeFor("F_104")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_104 { get; set; }
        [YAXAttributeFor("F_108")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_108 { get; set; }

        public static TexturePart GetNew()
        {
            return new TexturePart()
            {
                TextureIndex = new AsyncObservableCollection<int>()
            };
        }
        
    }

    [Serializable]
    [YAXSerializeAs("VerticalDistribution_Component")]
    public class FloatPart_0_1 : IFloatSize8
    {
        [YAXAttributeFor("PositionOffset_Y")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("PositionOffset_Y")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("TranslationSpeed_Y")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("TranslationSpeed_Y")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("VirtualConeOpenAngle")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("VirtualConeOpenAngle")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("F_24")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("F_28")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }

        public static FloatPart_0_1 GetNew()
        {
            return new FloatPart_0_1();
        }

    }

    [Serializable]
    [YAXSerializeAs("SphericalDistribution_Component")]
    public class FloatPart_1_1 : IFloatSize4
    {
        [YAXAttributeFor("SphereRadius_ForRandomPositionOnSphere")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("SphereRadius_ForRandomPositionInSphere")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("TranslationSpeed_Y")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("TranslationSpeed_Y")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }

        public static FloatPart_1_1 GetNew()
        {
            return new FloatPart_1_1();
        }
    }

    [Serializable]
    [YAXSerializeAs("ShapePerimeterDistribution_Component")]
    public class FloatPart_2_1
    {
        [YAXAttributeFor("PositionOffset_Y")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("PositionOffset_Y")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("TranslationSpeed_Y")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("TranslationSpeed_Y")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Width1")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Width1")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Width2")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Width2")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Shape")]
        [YAXSerializeAs("type")]
        public Shape I_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_44 { get; set; }

        public enum Shape
        {
            Circle = 0,
            Square = 1
        }

        public static FloatPart_2_1 GetNew()
        {
            return new FloatPart_2_1()
            {
                I_40 = Shape.Circle
            };
        }
    }

    [Serializable]
    [YAXSerializeAs("ShapeAreaDistribution_Component")]
    public class FloatPart_3_1
    {
        [YAXAttributeFor("PositionOffset_Y")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("PositionOffset_Y")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("TranslationSpeed_Y")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("TranslationSpeed_Y")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("F_16")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("F_20")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Width1")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Width1")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Width2")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Width2")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Shape")]
        [YAXSerializeAs("type")]
        public Shape I_40 { get; set; }
        [YAXAttributeFor("F_44")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0########")]
        public float F_44 { get; set; }


        public enum Shape
        {
            Circle = 0,
            Square = 1
        }

        public static FloatPart_3_1 GetNew()
        {
            return new FloatPart_3_1()
            {
                I_40 = Shape.Circle
            };
        }

    }

    [Serializable]
    [YAXSerializeAs("AutoOriented_Component")]
    public class FloatPart_0_2 : IFloatSize4
    {
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("SpeedRotation")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("SpeedRotation")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }

        public static FloatPart_0_2 GetNew()
        {
            return new FloatPart_0_2();
        }
    }

    [Serializable]
    [YAXSerializeAs("Default_Component")]
    public class FloatPart_2_2 : IFloatSize8
    {
        [YAXAttributeFor("AngleRotation")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("AngleRotation")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("SpeedRotation")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("SpeedRotation")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("RotationAxis")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("RotationAxis")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("RotationAxis")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("RotationAxis")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0########")]
        public float F_28 { get; set; }

        public static FloatPart_2_2 GetNew()
        {
            return new FloatPart_2_2();
        }
    }

    [Serializable]
    [YAXSerializeAs("ConeExtrude_Component")]
    public class Struct3
    {
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("Base")]
        public UInt16 I_00 { get; set; }
        [YAXAttributeFor("Duration")]
        [YAXSerializeAs("AddedRandom")]
        public UInt16 I_02 { get; set; }
        [YAXAttributeFor("TimeBetweenTwoStep")]
        [YAXSerializeAs("value")]
        public UInt16 I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public UInt16 I_08 { get; set; }
        [YAXAttributeFor("I_10")]
        [YAXSerializeAs("value")]
        public UInt16 I_10 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Point")]
        [YAXSerializeAs("Points")]
        public AsyncObservableCollection<Struct3_Entries> FloatList { get; set; }

        public static Struct3 GetNew()
        {
            return new Struct3()
            {
                FloatList = AsyncObservableCollection<Struct3_Entries>.Create()
            };
        }
    }

    [Serializable]
    [YAXSerializeAs("Point")]
    public class Struct3_Entries
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("WorldScaleFactor")]
        [YAXFormat("0.0###########")]
        public float F_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("WorldScaleAdd")]
        [YAXFormat("0.0###########")]
        public float F_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("WorldOffsetFactor")]
        [YAXFormat("0.0###########")]
        public float F_08 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("WorldOffsetFactorb")]
        [YAXFormat("0.0###########")]
        public float F_12 { get; set; }

    }

    [Serializable]
    [YAXSerializeAs("ShapeDraw_Component")]
    public class Struct5
    {
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("AngularSpeed")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("AngularSpeed")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("AutoOrientation")]
        [YAXSerializeAs("Z_Axis")]
        public AutoOrientation I_18 { get; set; } //uint16
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public UInt16 I_24 { get; set; }
        [YAXAttributeFor("I_26")]
        [YAXSerializeAs("value")]
        public UInt16 I_26 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public UInt16 I_28 { get; set; }
        [YAXAttributeFor("I_30")]
        [YAXSerializeAs("value")]
        public UInt16 I_30 { get; set; }


        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Point")]
        [YAXSerializeAs("BaseLine")]
        public AsyncObservableCollection<Struct5_Entries> FloatList { get; set; }

        public enum AutoOrientation
        {
            Camera = 0,
            Front = 1,
            None = 2
        }

        public static Struct5 GetNew()
        {
            return new Struct5()
            {
                I_18 = AutoOrientation.Camera,
                FloatList = AsyncObservableCollection<Struct5_Entries>.Create()
            };
        }
    }

    [Serializable]
    [YAXSerializeAs("Point")]
    public class Struct5_Entries
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_04 { get; set; }



    }

    [Serializable]
    [YAXSerializeAs("Mesh_Component")]
    public class ModelStruct : IFloatSize4
    {

        [YAXAttributeFor("RotateAngle")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("RotateAngle")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("SpeedRotation")]
        [YAXSerializeAs("Base")]
        [YAXFormat("0.0########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("SpeedRotation")]
        [YAXSerializeAs("AddedRandom")]
        [YAXFormat("0.0########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("RotationAxis")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0###########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("RotationAxis")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0###########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("RotationAxis")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0###########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("RotationAxis")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0###########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public UInt32 I_32 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public UInt32 I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public UInt32 I_44 { get; set; }
        [YAXAttributeFor("EmgFile")]
        [YAXSerializeAs("bytes")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<byte> emgBytes { get; set; }

        public static ModelStruct GetNew()
        {
            return new ModelStruct()
            {
                emgBytes = new List<byte>()
            };
        }
    }

    [Serializable]
    [YAXSerializeAs("Animation")]
    public class Type0 : INotifyPropertyChanged
    {
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

        [YAXDontSerialize]
        public string Name
        {
            get
            {
                switch (SelectedParameterValue)
                {
                    case Parameter.Position:
                        return String.Format("{0}, {1}", SelectedParameterValue.ToString(), SelectedComponentPositionValue.ToString());
                    case Parameter.Rotation:
                        return String.Format("{0}, {1}", SelectedParameterValue.ToString(), SelectedComponentRotationValue.ToString());
                    case Parameter.Scale:
                        return String.Format("{0}, {1}", SelectedParameterValue.ToString(), SelectedComponentScaleValue.ToString());
                    case Parameter.Color1:
                        return String.Format("{0}, {1}", SelectedParameterValue.ToString(), SelectedComponentColor1Value.ToString());
                    case Parameter.Color2:
                        return String.Format("{0}, {1}", SelectedParameterValue.ToString(), SelectedComponentColor2Value.ToString());
                    default:
                        return String.Format("{0}", SelectedParameterValue.ToString());

                };
            }
        }

        [YAXDontSerialize]
        public bool IsAlpha
        {
            get
            {
                switch (SelectedParameter)
                {
                    case Parameter.Color1:
                        return (SelectedComponentColor1 == ComponentColor1.A);
                    case Parameter.Color2:
                        return (SelectedComponentColor2 == ComponentColor2.A);
                    default:
                        return false;
                }
            }
        }

        private Parameter SelectedParameterValue = Parameter.Position;
        private ComponentPosition SelectedComponentPositionValue = ComponentPosition.X;
        private ComponentRotation SelectedComponentRotationValue = ComponentRotation.X;
        private ComponentScale SelectedComponentScaleValue = ComponentScale.X;
        private ComponentColor1 SelectedComponentColor1Value = ComponentColor1.R;
        private ComponentColor2 SelectedComponentColor2Value = ComponentColor2.R;

        [YAXAttributeForClass]
        [YAXSerializeAs("Parameter")]
        public Parameter SelectedParameter
        {
            get
            {
                return this.SelectedParameterValue;
            }

            set
            {
                if (value != this.SelectedParameterValue)
                {
                    this.SelectedParameterValue = value;
                    NotifyPropertyChanged("SelectedParameter");
                    NotifyPropertyChanged("Name");
                    NotifyPropertyChanged("IsPositionVisible");
                    NotifyPropertyChanged("IsRotationVisible");
                    NotifyPropertyChanged("IsScaleVisible");
                    NotifyPropertyChanged("IsColor1Visible");
                    NotifyPropertyChanged("IsColor2Visible");
                }
            }
        }

        [YAXDontSerialize]
        public ComponentPosition SelectedComponentPosition
        {
            get
            {
                return this.SelectedComponentPositionValue;
            }

            set
            {
                if (value != this.SelectedComponentPositionValue)
                {
                    this.SelectedComponentPositionValue = value;
                    NotifyPropertyChanged("SelectedComponentPosition");
                    NotifyPropertyChanged("Name");
                }
            }
        }
        [YAXDontSerialize]
        public ComponentRotation SelectedComponentRotation
        {
            get
            {
                return this.SelectedComponentRotationValue;
            }

            set
            {
                if (value != this.SelectedComponentRotationValue)
                {
                    this.SelectedComponentRotationValue = value;
                    NotifyPropertyChanged("SelectedComponentRotation");
                    NotifyPropertyChanged("Name");
                }
            }
        }
        [YAXDontSerialize]
        public ComponentScale SelectedComponentScale
        {
            get
            {
                return this.SelectedComponentScaleValue;
            }

            set
            {
                if (value != this.SelectedComponentScaleValue)
                {
                    this.SelectedComponentScaleValue = value;
                    NotifyPropertyChanged("SelectedComponentScale");
                    NotifyPropertyChanged("Name");
                }
            }
        }
        [YAXDontSerialize]
        public ComponentColor1 SelectedComponentColor1
        {
            get
            {
                return this.SelectedComponentColor1Value;
            }

            set
            {
                if (value != this.SelectedComponentColor1Value)
                {
                    this.SelectedComponentColor1Value = value;
                    NotifyPropertyChanged("SelectedComponentColor1");
                    NotifyPropertyChanged("Name");
                }
            }
        }
        [YAXDontSerialize]
        public ComponentColor2 SelectedComponentColor2
        {
            get
            {
                return this.SelectedComponentColor2Value;
            }

            set
            {
                if (value != this.SelectedComponentColor2Value)
                {
                    this.SelectedComponentColor2Value = value;
                    NotifyPropertyChanged("SelectedComponentColor2");
                    NotifyPropertyChanged("Name");
                }
            }
        }

        //This is where the SelectedComponent is saved to and read from xml. For all other purposes, use the above properties.
        [YAXAttributeForClass]
        [YAXSerializeAs("Component")]
        public string SelectedComponent
        {
            get
            {
                switch (SelectedParameter)
                {
                    case Parameter.Color1:
                        return SelectedComponentColor1.ToString();
                    case Parameter.Color2:
                        return SelectedComponentColor2.ToString();
                    case Parameter.Scale:
                        return SelectedComponentScale.ToString();
                    case Parameter.Rotation:
                        return SelectedComponentRotation.ToString();
                    case Parameter.Position:
                        return SelectedComponentPosition.ToString();
                    default:
                        throw new InvalidOperationException("Cannot get SelectedComponent for Parameter: " + SelectedParameter);
                }
            }
            set
            {
                switch (SelectedParameter)
                {
                    case Parameter.Color1:
                        SelectedComponentColor1 = (ComponentColor1)Enum.Parse(typeof(ComponentColor1), value);
                        break;
                    case Parameter.Color2:
                        SelectedComponentColor2 = (ComponentColor2)Enum.Parse(typeof(ComponentColor2), value);
                        break;
                    case Parameter.Scale:
                        SelectedComponentScale = (ComponentScale)Enum.Parse(typeof(ComponentScale), value);
                        break;
                    case Parameter.Position:
                        SelectedComponentPosition = (ComponentPosition)Enum.Parse(typeof(ComponentPosition), value);
                        break;
                    case Parameter.Rotation:
                        SelectedComponentRotation = (ComponentRotation)Enum.Parse(typeof(ComponentRotation), value);
                        break;
                    default:
                        throw new InvalidOperationException("Cannot set SelectedComponent for Parameter: " + SelectedParameter);
                }
            }
        }

        [YAXAttributeFor("Properties")]
        [YAXSerializeAs("Interpolated")]
        public bool I_01_b { get; set; }
        [YAXAttributeFor("Properties")]
        [YAXSerializeAs("Looped")]
        public bool I_02 { get; set; }
        [YAXAttributeFor("I_03")]
        [YAXSerializeAs("int8")]
        public byte I_03 { get; set; }
        [YAXAttributeFor("F_04")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0#######")]
        public float F_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Duration")]
        public short I_08 { get; set; }

        [YAXDontSerialize]
        private AsyncObservableCollection<Type0_Keyframe> _keyframes = null;

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Keyframe")]
        public AsyncObservableCollection<Type0_Keyframe> Keyframes
        {
            get
            {
                return this._keyframes;
            }
            set
            {
                if (value != this._keyframes)
                {
                    this._keyframes = value;
                    NotifyPropertyChanged(nameof(Keyframes));
                }
            }
        }

        //Parameter > Component (changes based on selected Parameter)
        //There are only 5 Parameters (tested all files)
        //There are only 4 unique values for Component (tested all files)
        //Type1 Parameter values = 0, 1, 2
        //Type1 Component values = 0, 1, 2
        public enum Parameter
        {
            Position = 0,
            Rotation = 1,
            Scale = 2,
            Color1 = 3,
            Color2 = 4
        }

        public enum ComponentPosition
        {
            X = 0,
            Y = 1,
            Z = 2,
            W = 3
        }

        public enum ComponentRotation
        {
            X = 0,
            Y = 1,
            Z = 2,
            W = 3
        }

        public enum ComponentScale
        {
            X = 0,
            Y = 1,
            Z = 2,
            W = 3
        }

        public enum ComponentColor1
        {
            R = 0,
            G = 1,
            B = 2,
            A = 3
        }

        public enum ComponentColor2
        {
            R = 0,
            G = 1,
            B = 2,
            A = 3
        }

        public void SetParameters(int parameter, int component, bool scale2)
        {
            switch (parameter)
            {
                case 0:
                    SelectedParameter = Parameter.Position;
                    SelectedComponentPosition = (ComponentPosition)component;
                    break;
                case 1:
                    SelectedParameter = Parameter.Rotation;
                    SelectedComponentRotation = (ComponentRotation)component;
                    break;
                case 2:
                    SelectedParameter = Parameter.Scale;
                    SelectedComponentScale = (ComponentScale)component;
                    break;
                case 3:
                    SelectedParameter = Parameter.Color1;
                    SelectedComponentColor1 = (ComponentColor1)component;
                    break;
                case 4:
                    SelectedParameter = Parameter.Color2;
                    SelectedComponentColor2 = (ComponentColor2)component;
                    break;
            }
        }

        /// <summary>
        /// Returns the Parameter, Component and Looped value as an Int16 for writing a binary EMP file.
        /// </summary>
        public short GetParameters(bool scale2)
        {
            byte _I_00 = (byte)SelectedParameter;
            byte _I_01_a = 0;
            byte _I_01 = 0;

            switch (SelectedParameter)
            {
                case Parameter.Position:
                    _I_01_a = (byte)SelectedComponentPosition;
                    break;
                case Parameter.Rotation:
                    _I_01_a = (byte)SelectedComponentRotation;
                    break;
                case Parameter.Scale:
                    _I_01_a = (byte)SelectedComponentScale;
                    break;
                case Parameter.Color1:
                    _I_01_a = (byte)SelectedComponentColor1;
                    break;
                case Parameter.Color2:
                    _I_01_a = (byte)SelectedComponentColor2;
                    break;
            }

            _I_01 = Int4Converter.GetByte(_I_01_a, BitConverter_Ex.GetBytes(I_01_b), "Type0_Animation: Component", "Type0_Animation: Interpolated");

            return BitConverter.ToInt16(new byte[2] { _I_00, _I_01 }, 0);
        }

        public float GetValue(int time)
        {
            foreach(var keyframe in Keyframes)
            {
                if (keyframe.Index == time) return keyframe.Float;
            }

            //Keyframe doesn't exist. Calculate the value.
            return CalculateKeyframeValue(time);

            //return (Keyframes.Count > 0) ? Keyframes[0].Float : 0f;//Rework this to calculate the value
        }

        public void SetValue(int time, float value, List<IUndoRedo> undos = null)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index == time)
                {
                    float oldValue = keyframe.Float;
                    keyframe.Float = value;

                    if (undos != null)
                        undos.Add(new UndoableProperty<Type0_Keyframe>(nameof(keyframe.Float), keyframe, oldValue, value));

                    return;
                }
            }

            //Keyframe doesn't exist. Add it.
            Keyframes.Add(new Type0_Keyframe() { Index = (short)time, Float = value });
        }

        public float CalculateKeyframeValue(int time)
        {
            Type0_Keyframe before = GetKeyframeBefore(time);
            Type0_Keyframe after = GetKeyframeAfter(time);

            if(after == null)
            {
                after = new Type0_Keyframe() { Index = short.MaxValue, Float = before.Float };
            }

            if(before == null)
            {
                throw new Exception("CalculateKeyframeValue: could not locate a \"before\" keyframe to use as reference.");
            }

            //Frame difference between previous frame and the current frame (current frame is AFTER the frame we want)
            int diff = after.Index - before.Index;

            //Keyframe value difference
            float keyframe2 = after.Float - before.Float;

            //Difference between the frame we WANT and the previous frame
            int diff2 = time - before.Index;

            //Divide keyframe value difference by the keyframe time difference, and then multiply it by diff2, then add the previous keyframe value
            return (keyframe2 / diff) * diff2 + before.Float;
        }

        /// <summary>
        /// Returns the keyframe that appears just before the specified frame
        /// </summary>
        /// <returns></returns>
        public Type0_Keyframe GetKeyframeBefore(int time)
        {
            SortKeyframes();
            Type0_Keyframe prev = null;

            foreach(var keyframe in Keyframes)
            {
                if (keyframe.Index >= time) break;
                prev = keyframe;
            }

            return prev;
        }

        /// <summary>
        /// Returns the keyframe that appears just after the specified frame
        /// </summary>
        /// <returns></returns>
        public Type0_Keyframe GetKeyframeAfter(int time)
        {
            SortKeyframes();
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index > time) return keyframe;
            }

            return null;
        }

        public void SortKeyframes()
        {
            Sorting.SortEntries2(Keyframes);
        }
    
    }

    [Serializable]
    [YAXSerializeAs("Keyframe")]
    public class Type0_Keyframe : IKeyframe, ISortable
    {
        [YAXDontSerialize]
        public int SortID { get { return Index; } }

        [YAXAttributeForClass]
        public short Index { get; set; }
        [YAXAttributeForClass]
        [YAXFormat("0.0######")]
        public float Float { get; set; }
    }

    //Type 1 data

    [Serializable]
    [YAXSerializeAs("Animation_SubGroup")]
    public class Type1_Header
    {
        [YAXDontSerialize]
        public string Name
        {
            get
            {
                return "Entry";
            }
        }

        [YAXAttributeForClass]
        public byte I_00 { get; set; }
        [YAXAttributeForClass]
        public byte I_01 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Animation")]
        public AsyncObservableCollection<Type0> Entries { get; set; }

    }


    //Section 2: EMB
    [Serializable]
    [YAXSerializeAs("Texture")]
    public class EMP_TextureDefinition
    {
        private EMB_CLASS.EmbEntry _textureRef = null;
        [YAXDontSerialize]
        public EMB_CLASS.EmbEntry TextureRef
        {
            get
            {
                return this._textureRef;
            }

            set
            {
                if (value != this._textureRef)
                {
                    this._textureRef = value;
                }
            }
        }

        [YAXDontSerialize]
        public string ToolName
        {
            get
            {
                if(TextureRef == null) return String.Format("Texture {0} (No Assigned Texture)", EntryIndex);
                return String.Format("Texture {0} ({1})", EntryIndex, TextureRef.Name);
            }
        }

        private TextureAnimationType TextureTypeValue = TextureAnimationType.Static;
        [YAXDontSerialize]
        public TextureAnimationType TextureType
        {
            get
            {
                return this.TextureTypeValue;
            }

            set
            {
                if (value != this.TextureTypeValue)
                {
                    this.TextureTypeValue = value;
                }
            }
        }

        private int _entryIndex = 0;
        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        public int EntryIndex
        {
            get
            {
                return this._entryIndex;
            }

            set
            {
                if (value != this._entryIndex)
                {
                    this._entryIndex = value;
                }
            }
        }
        
        [YAXAttributeForClass]
        [YAXSerializeAs("TextureIndex")]
        public byte I_01 { get; set; } = byte.MaxValue;

        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public byte I_00 { get; set; }
        [YAXAttributeFor("I_02")]
        [YAXSerializeAs("value")]
        public byte I_02 { get; set; }
        [YAXAttributeFor("I_03")]
        [YAXSerializeAs("value")]
        public byte I_03 { get; set; }
        [YAXAttributeFor("Filtering")]
        [YAXSerializeAs("Min")]
        public byte I_04 { get; set; } //int8
        [YAXAttributeFor("Filtering")]
        [YAXSerializeAs("Max")]
        public byte I_05 { get; set; } //int8
        [YAXAttributeFor("TextureRepetition")]
        [YAXSerializeAs("X")]
        public TextureRepitition I_06_byte { get; set; } //int8
        [YAXAttributeFor("TextureRepetition")]
        [YAXSerializeAs("Y")]
        public TextureRepitition I_07_byte { get; set; } //int8
        [YAXAttributeFor("EnableRandomSymetry")]
        [YAXSerializeAs("X")]
        public byte I_08 { get; set; } //int8
        [YAXAttributeFor("EnableRandomSymetry")]
        [YAXSerializeAs("Y")]
        public byte I_09 { get; set; } //int8

        private ScrollAnimation _subData2 = null;
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ScrollAnimationType")]
        public ScrollAnimation SubData2
        {
            get
            {
                return this._subData2;
            }

            set
            {
                if (value != this._subData2)
                {
                    this._subData2 = value;
                }
            }
        }

        public enum TextureAnimationType
        {
            Static = 0,
            Speed = 1,
            SpriteSheet = 2
        }

        public enum TextureRepitition
        {
            Wrap = 0,
            Mirror = 1,
            Clamp = 2,
            Border = 3
        }

        public TextureAnimationType CalculateTextureType()
        {
            if (SubData2.useSpeedInsteadOfKeyFrames == true)
            {
                return EMP_TextureDefinition.TextureAnimationType.Speed;
            }
            if (SubData2.Keyframes.Count() == 1 && SubData2.Keyframes[0].I_00 == -1)
            {
                return EMP_TextureDefinition.TextureAnimationType.Static;
            }
            else
            {
                return EMP_TextureDefinition.TextureAnimationType.SpriteSheet;
            }
        }
        
        public static EMP_TextureDefinition GetNew()
        {
            var keyframes = AsyncObservableCollection<SubData_2_Entry>.Create();
            keyframes.Add(new SubData_2_Entry());

            return new EMP_TextureDefinition()
            {
                EntryIndex = byte.MaxValue,
                TextureType = TextureAnimationType.Static,
                I_06_byte = TextureRepitition.Wrap,
                I_07_byte = TextureRepitition.Wrap,
                SubData2 = new ScrollAnimation()
                {
                    Keyframes = keyframes,
                    useSpeedInsteadOfKeyFrames = false
                }
            };
        }
        
    }

    [Serializable]
    [YAXSerializeAs("AnimationScrollType")]
    public class ScrollAnimation
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("UseSpeedInsteadKeyFrames")]
        public bool useSpeedInsteadOfKeyFrames { get; set; }
        [YAXAttributeFor("ScrollSpeed")]
        [YAXSerializeAs("U")]
        [YAXFormat("0.0###########")]
        public float ScrollSpeed_U { get; set; }
        [YAXAttributeFor("ScrollSpeed")]
        [YAXSerializeAs("V")]
        [YAXFormat("0.0###########")]
        public float ScrollSpeed_V { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Keyframe")]
        public AsyncObservableCollection<SubData_2_Entry> Keyframes { get; set; }

    }

    [Serializable]
    [YAXSerializeAs("Keyframe")]
    public class SubData_2_Entry
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Time")]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Scroll_X")]
        [YAXFormat("0.0###########")]
        public float ScrollX { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Scroll_Y")]
        [YAXFormat("0.0###########")]
        public float ScrollY { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Scale_X")]
        [YAXFormat("0.0###########")]
        public float ScaleX { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Scale_Y")]
        [YAXFormat("0.0###########")]
        public float ScaleY { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("F_20")]
        public string F_20 { get; set; } //float
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("F_24")]
        public string F_24 { get; set; } //float

        public void SetDefaultValuesForSDBH()
        {
            if (F_20 == null)
                F_20 = "0.0";
            if (F_24 == null)
                F_24 = "0.0";
        }

        public bool Compare(SubData_2_Entry obj2)
        {
            return Compare(this, obj2);
        }

        public static bool Compare(SubData_2_Entry obj1, SubData_2_Entry obj2)
        {
            if (obj1.I_00 != obj2.I_00) return false;
            if (obj1.ScrollX != obj2.ScrollX) return false;
            if (obj1.ScrollY != obj2.ScrollY) return false;
            if (obj1.ScaleX != obj2.ScaleX) return false;
            if (obj1.ScaleY != obj2.ScaleY) return false;
            if (obj1.F_20 != obj2.F_20) return false;
            if (obj1.F_24 != obj2.F_24) return false;

            return true;
        }
    }

    //Interface
    public interface IKeyframe
    {
        short Index { get; set; }
        float Float { get; set; }
    }

    public interface IFloatSize4
    {
        float F_16 { get; set; }
        float F_20 { get; set; }
        float F_24 { get; set; }
        float F_28 { get; set; }
    }

    public interface IFloatSize8
    {
        float F_00 { get; set; }
        float F_04 { get; set; }
        float F_08 { get; set; }
        float F_12 { get; set; }
        float F_16 { get; set; }
        float F_20 { get; set; }
        float F_24 { get; set; }
        float F_28 { get; set; }
    }

    public interface IFloatSize12Type0
    {
        float F_00 { get; set; }
        float F_04 { get; set; }
        float F_08 { get; set; }
        float F_12 { get; set; }
        float F_16 { get; set; }
        float F_20 { get; set; }
        float F_24 { get; set; }
        float F_28 { get; set; }
        float F_32 { get; set; }
        float F_36 { get; set; }
        float F_40 { get; set; }
        float F_44 { get; set; }
    }

    public interface IFloatSize12Type1
    {
        float F_00 { get; set; }
        float F_04 { get; set; }
        float F_08 { get; set; }
        float F_12 { get; set; }
        float F_16 { get; set; }
        float F_20 { get; set; }
        float F_24 { get; set; }
        float F_28 { get; set; }
        float F_32 { get; set; }
        float F_36 { get; set; }
        string I_40 { get; set; }
        float F_44 { get; set; }

        int GetShape();

    }



}
