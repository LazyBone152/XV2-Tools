using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.TSD
{
    [YAXSerializeAs("TSD")]
    public class TSD_File
    {
        public List<TSD_Trigger> Triggers { get; set; } = new List<TSD_Trigger>();
        public List<TSD_Event> Events { get; set; } = new List<TSD_Event>();
        public List<TSD_Global> Globals { get; set; } = new List<TSD_Global>();
        public List<TSD_Constant> Constants { get; set; } = new List<TSD_Constant>();
        public List<TSD_Zone> Zones { get; set; } = new List<TSD_Zone>();

        public static TSD_File Load(byte[] bytes)
        {
            return new Parser(bytes).tsd_File;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

    }

    [YAXSerializeAs("Trigger")]
    public class TSD_Trigger : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXSerializeAs("ID")]
        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } //Int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("DLC_Flag")]
        public int I_04 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Icon")]
        public int I_08 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Type")]
        public int I_12 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("TNL_ID")]
        public string I_16 { get; set; } //Int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("TNL_SubID")]
        public int I_20 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Event_ID")]
        public string I_24 { get; set; } //Int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I_28")]
        public int I_28 { get; set; }

        [YAXSerializeAs("value")]
        [YAXAttributeFor("Conditions")]
        public string Condition { get; set; }
    }

    [YAXSerializeAs("Event")]
    public class TSD_Event : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }


        [YAXSerializeAs("ID")]
        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } //Int32
        [YAXSerializeAs("Name")]
        [YAXAttributeForClass]
        public string Str1 { get; set; }

        [YAXSerializeAs("value")]
        [YAXAttributeFor("DLC_Flag")]
        public int I_04 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Path")]
        public string Str2 { get; set; }
        [YAXSerializeAs("name")]
        [YAXAttributeFor("Script")]
        public string Str3 { get; set; }
        [YAXSerializeAs("name")]
        [YAXAttributeFor("Function")]
        public string Str4 { get; set; }

        [BindingSubClass]
        public EventArguments Arguments { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("values")]
        [YAXAttributeFor("TNL_IDS")]
        public List<string> TNL_IDs { get; set; } // size = varaible


    }

    [YAXSerializeAs("Global")]
    public class TSD_Global : ISec_3_4, IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return 0; } } //No sorting is done for this type, but we must define a SortID regardless

        [YAXSerializeAs("Name")]
        [YAXAttributeForClass]
        public string Index { get; set; }
        [YAXSerializeAs("Type")]
        [YAXAttributeForClass]
        public int Type { get; set; }
        [YAXSerializeAs("Initial_Value")]
        [YAXAttributeForClass]
        public string Str { get; set; }
    }

    [YAXSerializeAs("Constant")]
    public class TSD_Constant : ISec_3_4, IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return 0; } } //No sorting is done for this type, but we must define a SortID regardless

        [YAXSerializeAs("Name")]
        [YAXAttributeForClass]
        public string Index { get; set; }
        [YAXSerializeAs("Type")]
        [YAXAttributeForClass]
        public int Type { get; set; }
        [YAXSerializeAs("Value")]
        [YAXAttributeForClass]
        public string Str { get; set; }
    }

    [YAXSerializeAs("Zone")]
    public class TSD_Zone : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXSerializeAs("ID")]
        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } //Int32
        [YAXSerializeAs("Type")]
        [YAXAttributeForClass]
        public int I_04 { get; set; }
        [YAXSerializeAs("Name")]
        [YAXAttributeForClass]
        public string Str { get; set; }
    }

    [BindingSubClass]
    public class EventArguments
    {
        [YAXAttributeFor("v0")]
        [YAXSerializeAs("arg")]
        public string v0 { get; set; }
        [YAXAttributeFor("v1")]
        [YAXSerializeAs("arg")]
        public string v1 { get; set; }
        [YAXAttributeFor("v2")]
        [YAXSerializeAs("arg")]
        public string v2 { get; set; }
        [YAXAttributeFor("v3")]
        [YAXSerializeAs("arg")]
        public string v3 { get; set; }
        [YAXAttributeFor("v4")]
        [YAXSerializeAs("arg")]
        public string v4 { get; set; }
        [YAXAttributeFor("v5")]
        [YAXSerializeAs("arg")]
        public string v5 { get; set; }
        [YAXAttributeFor("v6")]
        [YAXSerializeAs("arg")]
        public string v6 { get; set; }
        [YAXAttributeFor("v7")]
        [YAXSerializeAs("arg")]
        public string v7 { get; set; }
        [YAXAttributeFor("v8")]
        [YAXSerializeAs("arg")]
        public string v8 { get; set; }
        [YAXAttributeFor("v9")]
        [YAXSerializeAs("arg")]
        public string v9 { get; set; }
        [YAXAttributeFor("v10")]
        [YAXSerializeAs("arg")]
        public string v10 { get; set; }
        [YAXAttributeFor("v11")]
        [YAXSerializeAs("arg")]
        public string v11 { get; set; }
        [YAXAttributeFor("v12")]
        [YAXSerializeAs("arg")]
        public string v12 { get; set; }
        [YAXAttributeFor("v13")]
        [YAXSerializeAs("arg")]
        public string v13 { get; set; }
        [YAXAttributeFor("v14")]
        [YAXSerializeAs("arg")]
        public string v14 { get; set; }
        [YAXAttributeFor("v15")]
        [YAXSerializeAs("arg")]
        public string v15 { get; set; }
        [YAXAttributeFor("v16")]
        [YAXSerializeAs("arg")]
        public string v16 { get; set; }
        [YAXAttributeFor("v17")]
        [YAXSerializeAs("arg")]
        public string v17 { get; set; }
        [YAXAttributeFor("v18")]
        [YAXSerializeAs("arg")]
        public string v18 { get; set; }
        [YAXAttributeFor("v19")]
        [YAXSerializeAs("arg")]
        public string v19 { get; set; }

        public static EventArguments Read(string args)
        {
            string[] splitStr = args.Split(new[] { ",," }, StringSplitOptions.None);

            if (splitStr.Length != 20)
            {
                throw new InvalidDataException(string.Format("The following EventArgument is invalid: \"{0}\". Parse failed.", args));
            }

            return new EventArguments()
            {
                v0 = splitStr[0],
                v1 = splitStr[1],
                v2 = splitStr[2],
                v3 = splitStr[3],
                v4 = splitStr[4],
                v5 = splitStr[5],
                v6 = splitStr[6],
                v7 = splitStr[7],
                v8 = splitStr[8],
                v9 = splitStr[9],
                v10 = splitStr[10],
                v11 = splitStr[11],
                v12 = splitStr[12],
                v13 = splitStr[13],
                v14 = splitStr[14],
                v15 = splitStr[15],
                v16 = splitStr[16],
                v17 = splitStr[17],
                v18 = splitStr[18],
                v19 = splitStr[19]
            };
        }

        public string Write()
        {
            return string.Format("{0},,{1},,{2},,{3},,{4},,{5},,{6},,{7},,{8},,{9},,{10},,{11},,{12},,{13},,{14},,{15},,{16},,{17},,{18},,{19}", v0, v1, v2, v3, v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19);
        }
    }

    public interface ISec_3_4
    {
        string Index { get; set; }
        int Type { get; set; }
        string Str { get; set; }
    }

}
