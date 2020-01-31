using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using System.IO;

namespace Xv2CoreLib.BCM_XML
{
    [Flags]
    public enum ButtonInput : uint
    {
        [YAXEnum("Light")]
        light = 1, //Square
        [YAXEnum("Heavy")]
        heavy = 2, //Trianlge
        [YAXEnum("Blast")]
        blast = 4, //Circle
        [YAXEnum("Jump")]
        jump = 8, //X
        [YAXEnum("SkillMenu")]
        skillmenu = 16, //R2
        [YAXEnum("Boost")]
        boost = 32, //L2
        [YAXEnum("Guard")]
        guard = 64, //L1
        [YAXEnum("LockOn")]
        unk8 = 128,
        [YAXEnum("SuperSkill1")]
        superskill1 = 256,
        [YAXEnum("SuperSkill2")]
        superskill2 = 512,
        [YAXEnum("SuperSkill3")]
        superskill3 = 1024,
        [YAXEnum("SuperSkill4")]
        superskill4 = 2048,
        [YAXEnum("UltimateSkill1")]
        ultimateskill1 = 4096,
        [YAXEnum("UltimateSkill2")]
        ultimateskill2 = 8192,
        [YAXEnum("AwokenSkill")]
        awokenskill = 16384,
        [YAXEnum("EvasiveSkill")]
        evasiveskill = 32768,
        [YAXEnum("AdditionalInput")]
        additionalinput = 65536,
        [YAXEnum("SuperMenu_Duplicate")]
        supermenu_duplicate = 131072,
        [YAXEnum("UltimateMenu")]
        ultimatemenu = 262144,
        unk20 = 524288,
        [YAXEnum("LockOn")]
        lockon = 1048576, //unk21
        [YAXEnum("Descend")]
        descend = 2097152,
        [YAXEnum("DragonRadar")]
        dragonradar = 4194304,
        unk24 = 8388608,
        unk25 = 16777216,
        unk26 = 33554432,
        unk27 = 67108864,
        unk28 = 134217728,
        [YAXEnum("UltimateMenu_Duplicate")]
        ultimatemenu_duplicate = 268435456,
        unk30 = 536870912,
        unk31 = 1073741824,
        unk32 = 2147483648
    }

    [Flags]
    public enum DirectionalInput : uint
    {
        [YAXEnum("Forward")]
        forward = 1,
        [YAXEnum("Backwards")]
        backwards = 2,
        [YAXEnum("LeftRelative")]
        leftrelative = 4,
        [YAXEnum("RightRelative")]
        rightrelative = 8,
        [YAXEnum("SingleActivation")]
        singleactivation = 16,
        [YAXEnum("Up")]
        up = 32,
        [YAXEnum("Down")]
        down = 64,
        [YAXEnum("Right")]
        right = 128,
        [YAXEnum("Left")]
        left = 256,
        dirunk10 = 512,
        dirunk11 = 1024,
        dirunk12 = 2048,
        dirunk13 = 4096,
        dirunk14 = 8192,
        dirunk15 = 16384,
        dirunk16 = 32768,
        dirunk17 = 65536,
        dirunk18 = 131072,
        dirunk19 = 262144,
        dirunk20 = 524288,
        dirunk21 = 1048576,
        dirunk22 = 2097152,
        dirunk23 = 4194304,
        dirunk24 = 8388608,
        dirunk25 = 16777216,
        dirunk26 = 33554432,
        dirunk27 = 67108864,
        dirunk28 = 134217728,
        dirunk29 = 268435456,
        dirunk30 = 536870912,
        dirunk31 = 1073741824,
        dirunk32 = 2147483648
    }


    [Flags]
    public enum BacCases
    {
        Case1 = 1,
        Case2 = 2,
        Case3 = 4,
        Case4 = 8,
        Case5 = 16,
        Case6 = 32,
        Case7 = 64,
        Case8 = 128,
    }

    [Flags]
    public enum ActivatorState : uint
    {
        [YAXEnum("Idle")]
        idle = 1,
        [YAXEnum("Attacking")]
        attacking = 2,
        [YAXEnum("Boosting")]
        boosting = 4,
        [YAXEnum("Guarding")]
        guarding = 8,
        [YAXEnum("ReceiveDamage")]
        receivedamage = 16,
        [YAXEnum("Jumping")]
        jumping = 32,
        unk7 = 64,
        [YAXEnum("TargetAttacking")]
        targetattacking = 128,
        unk9 = 256,
        unk10 = 512,
        unk11 = 1024,
        unk12 = 2048,
        unk13 = 4096,
        unk14 = 8192,
        unk15 = 16384,
        unk16 = 32768,
        unk17 = 65536,
        unk18 = 131072,
        unk19 = 262144,
        unk20 = 524288,
        unk21 = 1048576,
        unk22 = 2097152,
        unk23 = 4194304,
        unk24 = 8388608,
        unk25 = 16777216,
        unk26 = 33554432,
        unk27 = 67108864,
        unk28 = 134217728,
        unk29 = 268435456,
        unk30 = 536870912,
        unk31 = 1073741824,
        unk32 = 2147483648
    }
    
    [YAXSerializeAs("BCM")]
    public class BCM_File
    {
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "BcmEntry")]
        public List<BCM_Entry> BCMEntries { get; set; }

        public static BCM_File Load(string path)
        {
            if(Path.GetExtension(path) == ".bcm")
            {
                return new Parser(path, false).GetBcmFile();
            }
            else if(Path.GetExtension(path) == ".xml" && Path.GetExtension(Path.GetFileNameWithoutExtension(path)) == ".bcm")
            {
                YAXSerializer serializer = new YAXSerializer(typeof(BCM_File), YAXSerializationOptions.DontSerializeNullObjects);
                return (BCM_File)serializer.DeserializeFromFile(path);
            }
            else
            {
                return null;
            }
        }

        public static BCM_File Load(byte[] rawBytes)
        {
            return new Parser(rawBytes).bcmFile;
        }

        public void Save(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            new Deserializer(this, path);
        }

    }

    [YAXSerializeAs("BcmEntry")]
    public class BCM_Entry
    {
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("ID")]
        public string Index { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Child_GoTo_ID")]
        public string LoopAsChild { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("Sibling_GoTo_ID")]
        public string LoopAsSibling { get; set; }

        [YAXSerializeAs("Parameters")]
        public Parameters BcmParameters { get; set; }

        
        [YAXDontSerializeIfNull]
        [YAXCollection(YAXCollectionSerializationTypes.Recursive)]
        public List<BCM_Entry> ChildBcmEntries { get; set; }
        
    }

    public class Parameters
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public string I_00 { get; set; } //int32
        [YAXAttributeFor("DirectionalInput")]
        [YAXSerializeAs("value")]
        public string I_04 { get; set; } //int32
        [YAXAttributeFor("ButtonInput")]
        [YAXSerializeAs("value")]
        public string I_08 { get; set; } //int32
        [YAXAttributeFor("HoldDownConditions")]
        [YAXSerializeAs("value")]
        public string I_12 { get; set; } //int32
        [YAXAttributeFor("OpponentSizeConditions")]
        [YAXSerializeAs("value")]
        public string I_16 { get; set; } //int32
        [YAXAttributeFor("HoldDownDelayTime")]
        [YAXSerializeAs("value")]
        public UInt16 I_20 { get; set; } //uint16
        [YAXAttributeFor("MaximumLoopDuration")]
        [YAXSerializeAs("value")]
        public UInt16 I_22 { get; set; } //uint16
        [YAXAttributeFor("PrimaryActivatorConditions")]
        [YAXSerializeAs("value")]
        public string I_24 { get; set; } //int32
        [YAXAttributeFor("ActivatorState")]
        [YAXSerializeAs("value")]
        public string I_28 { get; set; } //int32
        [YAXAttributeFor("BacEntryPrimary")]
        [YAXSerializeAs("value")]
        public Int16 I_32 { get; set; }
        [YAXAttributeFor("BacEntryAirborne")]
        [YAXSerializeAs("value")]
        public Int16 I_42 { get; set; }
        [YAXAttributeFor("BacEntryCharge")]
        [YAXSerializeAs("value")]
        public Int16 I_34 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public Int16 I_36 { get; set; }
        [YAXAttributeFor("BacEntryUserConnect")]
        [YAXSerializeAs("value")]
        public Int16 I_38 { get; set; }
        [YAXAttributeFor("BacEntryVictimConnect")]
        [YAXSerializeAs("value")]
        public Int16 I_40 { get; set; }
        [YAXAttributeFor("BacEntryUnknown")]
        [YAXSerializeAs("value")]
        public UInt16 I_44 { get; set; }
        [YAXAttributeFor("RandomFlag")]
        [YAXSerializeAs("value")]
        public UInt16 I_46 { get; set; }
        [YAXAttributeFor("KiCost")]
        [YAXSerializeAs("value")]
        public UInt32 I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public UInt32 I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public UInt32 I_72 { get; set; }
        [YAXAttributeFor("ReceiverLinkId")] //Reciever
        [YAXSerializeAs("value")]
        public string I_76 { get; set; } //int32
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        public UInt32 I_80 { get; set; }
        [YAXAttributeFor("StaminaCost")]
        [YAXSerializeAs("value")]
        public UInt32 I_84 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        public UInt32 I_88 { get; set; }
        [YAXAttributeFor("KiRequired")]
        [YAXSerializeAs("value")]
        public UInt32 I_92 { get; set; }
        [YAXAttributeFor("HealthRequired")]
        [YAXFormat("0.0#######")]
        [YAXSerializeAs("value")]
        public float F_96 { get; set; }
        [YAXAttributeFor("TransStage")]
        [YAXSerializeAs("value")]
        public Int16 I_100 { get; set; }
        [YAXAttributeFor("CUS_AURA")]
        [YAXSerializeAs("value")]
        public Int16 I_102 { get; set; }
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        public UInt32 I_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        public UInt32 I_108 { get; set; }
    }
    

}
