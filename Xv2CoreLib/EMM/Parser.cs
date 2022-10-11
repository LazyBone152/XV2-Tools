using System;
using System.Collections.Generic;
using System.IO;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.EMM
{
    public class Parser
    {
        private const int EMM_SIGNATURE = 1296909603;

        string saveLocation { get; set; }
        public EMM_File emmFile { get; private set; } = new EMM_File();
        byte[] rawBytes { get; set; }

        public Parser(string location, bool writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            SignatureValidation();
            ParseEmm();

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EMM_File));
                serializer.SerializeToFile(emmFile, saveLocation + ".xml");
            }
            else if (EffectContainer.EepkToolInterlop.FullDecompile)
            {
                emmFile.DecompileMaterials();
            }
        }

        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            SignatureValidation();
            ParseEmm();

            if(EffectContainer.EepkToolInterlop.FullDecompile)
                emmFile.DecompileMaterials();
        }

        public EMM_File GetEmmFile()
        {
            return emmFile;
        }

        private void SignatureValidation()
        {
            if(BitConverter.ToInt32(rawBytes, 0) != EMM_SIGNATURE)
            {
                throw new InvalidDataException($"EMM Validation failed. Could not locate the file signature... {StringEx.GetString(rawBytes, 0, false, StringEx.EncodingType.ASCII, 4)} was found instead. This is most likely not a EMM file.");
            }
        }

        private void ParseEmm()
        {
            int headerSize = BitConverter.ToInt16(rawBytes, 12);
            int offset = BitConverter.ToInt32(rawBytes, 12) + 4;
            int count = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, 12));
            emmFile.Version = BitConverter.ToInt32(rawBytes, 8);
            int unkOffset = (headerSize == 32) ? BitConverter.ToInt32(rawBytes, 16) : 0;
            int unkCount = rawBytes.Length - unkOffset;


            emmFile.Materials = AsyncObservableCollection<EmmMaterial>.Create();

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    emmFile.Materials.Add(ParseMaterial(BitConverter.ToInt32(rawBytes, offset), i, headerSize));
                    offset += 4;
                }
            }
            
            if(unkCount == 68)
            {
                emmFile.Unknown_Data = new UnknownData()
                {
                    I_00 = BitConverter.ToInt32(rawBytes, unkOffset + 0),
                    I_04 = BitConverter.ToInt32(rawBytes, unkOffset + 4),
                    I_08 = BitConverter.ToInt32(rawBytes, unkOffset + 8),
                    I_12 = BitConverter.ToInt32(rawBytes, unkOffset + 12),
                    I_16 = BitConverter.ToInt32(rawBytes, unkOffset + 16),
                    I_20 = BitConverter.ToInt32(rawBytes, unkOffset + 20),
                    I_24 = BitConverter.ToInt32(rawBytes, unkOffset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, unkOffset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, unkOffset + 32),
                    I_36 = BitConverter.ToInt32(rawBytes, unkOffset + 36),
                    I_40 = BitConverter.ToInt32(rawBytes, unkOffset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, unkOffset + 44),
                    I_48 = BitConverter.ToInt32(rawBytes, unkOffset + 48),
                    I_52 = BitConverter.ToInt32(rawBytes, unkOffset + 52),
                    I_56 = BitConverter.ToInt32(rawBytes, unkOffset + 56),
                    I_60 = BitConverter.ToInt32(rawBytes, unkOffset + 60),
                    I_64 = BitConverter.ToInt32(rawBytes, unkOffset + 64)
                };
            }

        }

        private EmmMaterial ParseMaterial(int offset, int index, int headerSize)
        {
            if(offset != 0)
            {
                offset += headerSize;

                return new EmmMaterial()
                {
                    Index = index,
                    Name = StringEx.GetString(rawBytes, offset + 0, false, StringEx.EncodingType.ASCII, 32),
                    ShaderProgram = StringEx.GetString(rawBytes, offset + 32, false, StringEx.EncodingType.ASCII, 32),
                    I_66 = BitConverter.ToUInt16(rawBytes, offset + 66),
                    Parameters = ParseParameters(offset + 68, BitConverter.ToInt16(rawBytes, offset + 64))
                };
            }
            else
            {
                return null;
            }
        }

        private List<Parameter> ParseParameters(int offset, int count)
        {
            List<Parameter> paramaters = new List<Parameter>();

            if (count > 0)
            {
                for(int i = 0; i < count; i++)
                {
                    paramaters.Add(new Parameter()
                    {
                        Name = StringEx.GetString(rawBytes, offset + 0, false, StringEx.EncodingType.ASCII, 32),
                        Type = (Parameter.ParameterType)BitConverter.ToInt32(rawBytes, offset + 32),
                        Value = GetValue((Parameter.ParameterType)BitConverter.ToInt32(rawBytes, offset + 32), offset + 36)
                    });
                    offset += 40;
                }
            }

            return paramaters;
        }

        //Value conversion
        private string GetValue(Parameter.ParameterType type, int offset)
        {
            switch (type)
            {
                case Parameter.ParameterType.Float:
                    return BitConverter.ToSingle(rawBytes, offset).ToString();
                case Parameter.ParameterType.Int:
                    return BitConverter.ToInt32(rawBytes, offset).ToString();
                case Parameter.ParameterType.Float2:
                    return BitConverter.ToSingle(rawBytes, offset).ToString();
                case Parameter.ParameterType.Bool:
                    int val = BitConverter.ToInt32(rawBytes, offset);

                    if (val == 0 || val == 1)
                        return val == 0 ? "false" : "true";
                    else
                        return val.ToString();
                default:
                    //Unknown value type. Read as int.
                    return BitConverter.ToInt32(rawBytes, offset).ToString();
            }
        }
    }
}
