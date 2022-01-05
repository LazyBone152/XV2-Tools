using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.EMM
{
    public class Deserializer
    {
        const UInt16 FILE_VERSION = 37568;

        string saveLocation;
        EMM_File emmFile;
        public List<byte> bytes = new List<byte>() { 35, 69, 77, 77, 254, 255 };

        //Strings
        List<StringWriter.StringInfo> stringInfo = new List<StringWriter.StringInfo>();

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(EMM_File), YAXSerializationOptions.DontSerializeNullObjects);
            emmFile = (EMM_File)serializer.DeserializeFromFile(location);
            
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(string location, EMM_File _emmFile)
        {
            saveLocation = location;
            emmFile = _emmFile;

            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EMM_File _emmFile)
        {
            emmFile = _emmFile;
            Write();
        }



        private void SetIndex()
        {
            for(int i = 0; i < emmFile.Materials.Count; i++)
            {
                emmFile.Materials[i].Index = i;
            }
        }

        private void Write()
        {
            SetIndex();

            int headerSize = (emmFile.Unknown_Data != null) ? 32 : 16;
            List<int> Table = new List<int>();
            bytes.AddRange(BitConverter.GetBytes((short)headerSize));
            bytes.AddRange(BitConverter.GetBytes(emmFile.I_08));
            bytes.AddRange(BitConverter.GetBytes(headerSize));

            //Extended header
            if(headerSize > 16)
            {
                bytes.AddRange(new byte[16]);
            }

            int count = (emmFile.Materials != null) ? emmFile.Materials.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));

            //Validate materials
            emmFile.Validate();

            //Table
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int a = 0; a < count; a++)
                    {
                        if (emmFile.Materials[i].Index == i)
                        {
                            Table.Add(bytes.Count());
                            bytes.AddRange(new byte[4]);
                            break;
                        }
                        else if (a == emmFile.Materials.Count() - 1)
                        {
                            //Null entry
                            bytes.AddRange(new byte[4]);
                            break;
                        }
                    }

                }
            }

            for(int i = 0; i < count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - headerSize), Table[i]);

                //emmFile.Materials[i].Str_00 = AddUniqueIdToName(emmFile.Materials[i].Str_00, i);

                bytes = StringWriter.WriteFixedLengthString(emmFile.Materials[i].Name, 32, "Name", bytes);
                bytes = StringWriter.WriteFixedLengthString(emmFile.Materials[i].ShaderProgram, 32, "Shader", bytes);
                int paramCount = (emmFile.Materials[i].Parameters != null) ? emmFile.Materials[i].Parameters.Count() : 0;
                bytes.AddRange(BitConverter.GetBytes((short)paramCount));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Materials[i].I_66));

                for (int a = 0; a < paramCount; a++)
                {
                    bytes = StringWriter.WriteFixedLengthString(emmFile.Materials[i].Parameters[a].Name, 32, "Name", bytes);
                    bytes.AddRange(BitConverter.GetBytes(GetValueType(emmFile.Materials[i].Parameters[a].Type)));
                    switch (GetValueType(emmFile.Materials[i].Parameters[a].Type))
                    {
                        case 0:
                            bytes.AddRange(BitConverter.GetBytes(GetType_Float(emmFile.Materials[i].Parameters[a].value)));
                            break;
                        case 65536:
                            bytes.AddRange(BitConverter.GetBytes(GetType_Float(emmFile.Materials[i].Parameters[a].value)));
                            break;
                        case 65537:
                            bytes.AddRange(BitConverter.GetBytes(GetType_Int32(emmFile.Materials[i].Parameters[a].value)));
                            break;
                        case 1:
                            bytes.AddRange(BitConverter.GetBytes(GetType_Bool(emmFile.Materials[i].Parameters[a].value)));
                            break;
                        default:
                            bytes.AddRange(BitConverter.GetBytes(GetType_Hex(emmFile.Materials[i].Parameters[a].value)));
                            break;
                    }
                }

            }

            //Unknown Data
            if (emmFile.Unknown_Data != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 16);

                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_00));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_04));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_08));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_12));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_16));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_20));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_24));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_28));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_32));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_36));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_40));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_44));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_48));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_52));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_56));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_60));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Unknown_Data.I_64));
            }

        }

        

        private int GetValueType(string type)
        {
            switch (type)
            {
                case "Float":
                    return 0;
                case "Float2":
                    return 65536;
                case "Int":
                    return 65537;
                case "Bool":
                    return 1;
                default:
                    Assertion.ValidateNumericString(type, "Parameter", "Type");
                    return Int16.Parse(type);
            }
        }

        private Single GetType_Float(string value)
        {
            return Single.Parse(value);
        }

        private Int32 GetType_Int32(string value)
        {
            Assertion.ValidateNumericString(value, "Parameter", "Value");
            return Int32.Parse(value);
        }

        private Int32 GetType_Bool(string value)
        {
            if(value == "true")
            {
                return 1;
            } else
            {
                return 0;
            }
        }
        
        private Int32 GetType_Hex(string value)
        {
            return HexConverter.ToInt32(value);
        }
    }
}
