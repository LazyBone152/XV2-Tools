using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.EMM
{
    public class Deserializer
    {
        string saveLocation;
        EMM_File emmFile;
        public List<byte> bytes = new List<byte>() { 35, 69, 77, 77, 254, 255 };

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

            if (EffectContainer.EepkToolInterlop.FullDecompile)
                emmFile.CompileMaterials();

            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(EMM_File _emmFile)
        {
            emmFile = _emmFile;
            
            if (EffectContainer.EepkToolInterlop.FullDecompile)
                emmFile.CompileMaterials();

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
            bytes.AddRange(BitConverter.GetBytes(emmFile.Version));
            bytes.AddRange(BitConverter.GetBytes(headerSize));

            //Extended header
            if(headerSize > 16)
            {
                bytes.AddRange(new byte[16]);
            }

            int count = (emmFile.Materials != null) ? emmFile.Materials.Count : 0;
            bytes.AddRange(BitConverter.GetBytes(count));

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
                        else if (a == emmFile.Materials.Count - 1)
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

                bytes = StringWriter.WriteFixedLengthString(emmFile.Materials[i].Name, 32, "Name", bytes);
                bytes = StringWriter.WriteFixedLengthString(emmFile.Materials[i].ShaderProgram, 32, "Shader", bytes);

                int paramCount = (emmFile.Materials[i].Parameters != null) ? emmFile.Materials[i].Parameters.Count : 0;

                bytes.AddRange(BitConverter.GetBytes((short)paramCount));
                bytes.AddRange(BitConverter.GetBytes(emmFile.Materials[i].I_66));

                for (int a = 0; a < paramCount; a++)
                {
                    bytes = StringWriter.WriteFixedLengthString(emmFile.Materials[i].Parameters[a].Name, 32, "Name", bytes);
                    bytes.AddRange(BitConverter.GetBytes((int)emmFile.Materials[i].Parameters[a].Type));

                    switch (emmFile.Materials[i].Parameters[a].Type)
                    {
                        case Parameter.ParameterType.Float:
                        case Parameter.ParameterType.Float2:
                            bytes.AddRange(BitConverter.GetBytes(emmFile.Materials[i].Parameters[a].FloatValue));
                            break;
                        case Parameter.ParameterType.Int:
                        case Parameter.ParameterType.Bool:
                        default:
                            bytes.AddRange(BitConverter.GetBytes(emmFile.Materials[i].Parameters[a].IntValue));
                            break;
                    }
                }

            }

            //Unknown Data
            if (emmFile.Unknown_Data != null)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 16);

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
    }
}
