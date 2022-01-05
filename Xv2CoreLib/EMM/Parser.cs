using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.EMM
{
    public class Parser
    { 
        const UInt16 FILE_VERSION = 37568;

        string saveLocation { get; set; }
        public EMM_File emmFile { get; private set; } = new EMM_File();
        byte[] rawBytes { get; set; }
        List<byte> bytes { get; set; }

        public Parser(string location, bool writeXml)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(saveLocation);
            bytes = rawBytes.ToList();
            if (Validation(location))
            {
                ParseEmm();

                if (writeXml)
                {
                    YAXSerializer serializer = new YAXSerializer(typeof(EMM_File));
                    serializer.SerializeToFile(emmFile, saveLocation + ".xml");
                }
            }
            
        }

        public Parser(byte[] _rawBytes)
        {
            rawBytes = _rawBytes;
            bytes = rawBytes.ToList();
            ParseEmm();
        }

        public EMM_File GetEmmFile()
        {
            return emmFile;
        }

        private bool Validation(string path)
        {
            if(Utils.GetString(bytes, 0, 4) == "#EMB")
            {
                Console.WriteLine(String.Format("\"{0}\" is actually a incorrectly named emb file, and therefore cannot be parsed as a emm file.", path));
                Console.ReadLine();
                return false;
            }

            return true;
        }

        private void ParseEmm()
        {
            int headerSize = BitConverter.ToInt16(rawBytes, 12);
            int offset = BitConverter.ToInt32(rawBytes, 12) + 4;
            int count = BitConverter.ToInt32(rawBytes, BitConverter.ToInt32(rawBytes, 12));
            emmFile.I_08 = BitConverter.ToUInt32(rawBytes, 8);
            int unkOffset = (headerSize == 32) ? BitConverter.ToInt32(rawBytes, 16) : 0;
            int unkCount = bytes.Count() - unkOffset;


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
            } else if (unkCount != 0 && unkCount != bytes.Count())
            {
                Console.WriteLine(String.Format("Unknown extended data size: {0}\nSkipping...", unkCount));
                Console.ReadLine();
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
                    Name = Utils.GetString(bytes, offset + 0, 32),
                    ShaderProgram = Utils.GetString(bytes, offset + 32, 32),
                    I_66 = BitConverter.ToUInt16(rawBytes, offset + 66),
                    Parameters = ParseParameters(offset + 68, BitConverter.ToInt16(rawBytes, offset + 64))
                };
            }
            else
            {
                return null;
            }
        }

        private AsyncObservableCollection<Parameter> ParseParameters(int offset, int count)
        {
            if(count > 0)
            {
                AsyncObservableCollection<Parameter> paramaters = AsyncObservableCollection<Parameter>.Create();

                for(int i = 0; i < count; i++)
                {
                    paramaters.Add(new Parameter()
                    {
                        Name = Utils.GetString(bytes, offset + 0, 32),
                        Type = GetValueType(BitConverter.ToInt32(rawBytes, offset + 32), offset + 36),
                        value = GetValue(BitConverter.ToInt32(rawBytes, offset + 32), offset + 36)
                    });
                    offset += 40;
                }

                return paramaters;
            }
            else
            {
                return AsyncObservableCollection<Parameter>.Create();
            }
            
        }

        //Value conversion

        private string GetValueType(int type, int offset)
        {
            string ret = null;
            if (!EMM_File.EmmValueTypes.TryGetValue(type, out ret))
            {
                EMM_File.EmmValueTypes.Add(type, string.Format("{0}", type));
            }

            return ret;
        }

        private string GetValue(int type, int offset)
        {
            switch (type)
            {
                case 0:
                    return BitConverter.ToSingle(rawBytes, offset).ToString();
                case 65537:
                    return BitConverter.ToInt32(rawBytes, offset).ToString();
                case 65536:
                    return BitConverter.ToSingle(rawBytes, offset).ToString();
                case 1:
                    if(BitConverter.ToInt32(rawBytes, offset) == 0)
                    {
                        return "false";
                    } else
                    {
                        return "true";
                    }
                default:
                    //Unknown value type
                    return BitConverter.ToInt32(rawBytes, offset).ToString();
            }
        }
    }
}
