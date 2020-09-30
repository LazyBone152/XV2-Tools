using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.TNL
{
    [YAXSerializeAs("TNL")]
    public class TNL_File
    {
        public List<TNL_Character> Characters { get; set; } = new List<TNL_Character>();
        public List<TNL_Teacher> Teachers { get; set; } = new List<TNL_Teacher>();
        public List<TNL_Object> Objects { get; set; } = new List<TNL_Object>();
        public List<TNL_Action> Actions { get; set; } = new List<TNL_Action>();

        public List<string> GetAllUsedIDs()
        {
            List<string> usedIds = new List<string>();

            if(Characters != null)
            {
                foreach (var chara in Characters)
                {
                    usedIds.Add(chara.I32_1);
                }
            }
            if(Teachers != null)
            {
                foreach (var master in Teachers)
                {
                    usedIds.Add(master.I32_1);
                }
            }
            if(Objects != null)
            {
                foreach (var objects in Objects)
                {
                    usedIds.Add(objects.Index);
                }
            }

            return usedIds;
        }

        public static TNL_File Load(byte[] bytes)
        {
            return new Parser(bytes).tnl_File;
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }
    }

    [YAXSerializeAs("Character")]
    public class TNL_Character : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }
        [YAXDontSerialize]
        public string Index 
        { 
            get 
            { 
                return string.Format("{0}_{1}", I32_1, I8_1);
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 2)
                {
                    I32_1 = split[0];
                    I8_1 = byte.Parse(split[1]);
                }
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string I32_1 { get; set; } //Int32
        [YAXSerializeAs("SubID")]
        [YAXAttributeForClass]
        public byte I8_1 { get; set; }//Int8
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I8_2")]
        public byte I8_2 { get; set; }//Int8
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I8_3")]
        public byte I8_3 { get; set; }//Int8
        [YAXSerializeAs("value")]
        [YAXAttributeFor("CMS")]
        public string Str1 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Costume")]
        public short I16_1 { get; set; }//Int16
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I16_2")]
        public short I16_2 { get; set; } //Int16
        [YAXSerializeAs("value")]
        [YAXAttributeFor("LobbyName")]
        [BindingString]
        public string Str2 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I32_2")]
        public string I32_2 { get; set; } //Int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Position")]
        [BindingString]
        public string Str3 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Zone")]
        public string Str4 { get; set; } 
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Action_ID")]
        public string I32_3 { get; set; } //Int32
    }

    [YAXSerializeAs("Teacher")]
    public class TNL_Teacher : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }
        [YAXDontSerialize]
        public string Index 
        { 
            get
            { 
                return string.Format("{0}_{1}", I32_1, I8_1);
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 2)
                {
                    I32_1 = split[0];
                    I8_1 = byte.Parse(split[1]);
                }
            }
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string I32_1 { get; set; } //Int32
        [YAXSerializeAs("SubID")]
        [YAXAttributeForClass]
        public byte I8_1 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I8_2")]
        public byte I8_2 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I8_3")]
        public byte I8_3 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("CMS")]
        public string Str1 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Costume")]
        public short I16_1 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I16_2")]
        public short I16_2 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("LobbyName")]
        [BindingString]
        public string Str2 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I32_2")]
        public string I32_2 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Position")]
        [BindingString]
        public string Str3 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Zone")]
        [BindingString]
        public string Str4 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Action_ID")]
        public string I32_3 { get; set; } //Int32
    }

    [YAXSerializeAs("Object")]
    public class TNL_Object : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //Int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("LobbyName")]
        [BindingString]
        public string Str1 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I32_2")]
        public string I32_2 { get; set; } //Int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Position1")]
        [BindingString]
        public string Str2 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Position2")]
        [BindingString]
        public string Str3 { get; set; }
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Str4")]
        [BindingString]
        public string Str4 { get; set; } 
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I32_3")]
        [BindingString]
        public string I32_3 { get; set; } //Int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I32_4")]
        public string I32_4 { get; set; } //Int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("I32_5")]
        public string I32_5 { get; set; } //Int32
    }

    [YAXSerializeAs("ScriptEntry")]
    public class TNL_Action : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXSerializeAs("ID")]
        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; } //int32
        [YAXSerializeAs("value")]
        [YAXAttributeFor("Path")]
        [BindingString]
        public string Str1 { get; set; }
        [YAXSerializeAs("name")]
        [YAXAttributeFor("Script")]
        [BindingString]
        public string Str2 { get; set; }
        [YAXSerializeAs("name")]
        [YAXAttributeFor("Function")]
        [BindingString]
        public string Str3 { get; set; }

        [BindingSubClass]
        public EventArguments Arguments { get; set; }
    }

    [BindingSubClass]
    public class EventArguments
    {
        [YAXAttributeFor("v0")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v0 { get; set; }
        [YAXAttributeFor("v1")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v1 { get; set; }
        [YAXAttributeFor("v2")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v2 { get; set; }
        [YAXAttributeFor("v3")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v3 { get; set; }
        [YAXAttributeFor("v4")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v4 { get; set; }
        [YAXAttributeFor("v5")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v5 { get; set; }
        [YAXAttributeFor("v6")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v6 { get; set; }
        [YAXAttributeFor("v7")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v7 { get; set; }
        [YAXAttributeFor("v8")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v8 { get; set; }
        [YAXAttributeFor("v9")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v9 { get; set; }
        [YAXAttributeFor("v10")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v10 { get; set; }
        [YAXAttributeFor("v11")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v11 { get; set; }
        [YAXAttributeFor("v12")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v12 { get; set; }
        [YAXAttributeFor("v13")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v13 { get; set; }
        [YAXAttributeFor("v14")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v14 { get; set; }
        [YAXAttributeFor("v15")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v15 { get; set; }
        [YAXAttributeFor("v16")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v16 { get; set; }
        [YAXAttributeFor("v17")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v17 { get; set; }
        [YAXAttributeFor("v18")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v18 { get; set; }
        [YAXAttributeFor("v19")]
        [YAXSerializeAs("arg")]
        [BindingString]
        public string v19 { get; set; }

        public static EventArguments Read(string args)
        {
            string[] splitStr = args.Split(new[] { "," }, StringSplitOptions.None);

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
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19}", v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19);
        }
    }

    public interface ICharacterInfo
    {
        string Index { get; set; } //Int32
        byte I8_1 { get; set; }
        byte I8_2 { get; set; }
        byte I8_3 { get; set; }
        string Str1 { get; set; }
        short I16_1 { get; set; }
        short I16_2 { get; set; }
        string Str2 { get; set; }
        int I32_2 { get; set; }
        string Str3 { get; set; }
        string Str4 { get; set; }
        string I32_3 { get; set; } //Int32
    }
}
