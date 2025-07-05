using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Xv2CoreLib.FMP;
using Xv2CoreLib.Resource;
using static Xv2CoreLib.BAC.BAC_Entry;
using MIConvexHull;
using YAXLib;

namespace Xv2CoreLib.Havok
{
    [Flags]
    public enum HavokTagSubType
    {
        TST_Void = 0x0,
        TST_Invalid = 0x1,
        TST_Bool = 0x2,
        TST_String = 0x3,
        TST_Int = 0x4,
        TST_Float = 0x5,
        TST_Pointer = 0x6,
        TST_Class = 0x7,
        TST_Array = 0x8,
        TST_Tuple = 0x28,
        TST_TypeMask = 0xff,
        TST_IsSigned = 0x200,
        TST_Float32 = 0x1746,
        TST_Int8 = 0x2000,
        TST_Int16 = 0x4000,
        TST_Int32 = 0x8000,
        TST_Int64 = 0x10000,
    };

    [Flags]
    public enum HavokTagFlag
    {
        TF_SubType = 0x1,
        TF_Pointer = 0x2,
        TF_Version = 0x4,
        TF_ByteSize = 0x8,
        TF_AbstractValue = 0x10,
        TF_Members = 0x20,
        TF_Interfaces = 0x40,
        TF_Unknown = 0x80,
    };

    public class HavokTagFile
    {
        internal static readonly string[] AllowedSignatures = { Havok_TAG0_SIGNATURE, Havok_SDKV_SIGNATURE, Havok_DATA_SIGNATURE, Havok_TYPE_SIGNATURE,
                                                                Havok_TSTR_SIGNATURE, Havok_TNAM_SIGNATURE, Havok_FSTR_SIGNATURE, Havok_TBOD_SIGNATURE,
                                                                Havok_TPAD_SIGNATURE, Havok_INDX_SIGNATURE, Havok_ITEM_SIGNATURE };
        internal const string Havok_TAG0_SIGNATURE = "TAG0";
        internal const string Havok_SDKV_SIGNATURE = "SDKV";
        internal const string Havok_v2015_SIGNATURE = "2015";
        internal const string Havok_major01_SIGNATURE = "01";
        internal const string Havok_minor00_SIGNATURE = "00";
        internal const string Havok_SUPPORTED_VERSION = "2015.01.00";
        internal const string Havok_DATA_SIGNATURE = "DATA";
        internal const string Havok_TYPE_SIGNATURE = "TYPE";
        internal const string Havok_TSTR_SIGNATURE = "TSTR";
        internal const string Havok_TNAM_SIGNATURE = "TNAM";
        internal const string Havok_FSTR_SIGNATURE = "FSTR";
        internal const string Havok_TBOD_SIGNATURE = "TBOD";
        internal const string Havok_TPAD_SIGNATURE = "TPAD";
        internal const string Havok_INDX_SIGNATURE = "INDX";
        internal const string Havok_ITEM_SIGNATURE = "ITEM";

        [YAXAttributeForClass]
        public string Version { get; set; }

        [YAXSerializeAs("Object")]
        public HavokTagObject RootObject { get; set; }
        public List<HavokTagType> TagTypes { get; set; }
        public List<HavokTagItem> TagItems { get; set; }

        public HavokPartHeader Headers { get; set; }

        #region LoadSave
        public static void SerializeToXml(string path)
        {
            HavokTagFile havokFile = Load(File.ReadAllBytes(path));

            foreach (var type in havokFile.TagTypes)
            {
                type.SerializeReferences(havokFile.TagTypes);
            }

            YAXSerializer serializer = new YAXSerializer(typeof(HavokTagFile));
            serializer.SerializeToFile(havokFile, path + ".xml");
        }

        public static void DeserializeFromXml(string path)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            YAXSerializer serializer = new YAXSerializer(typeof(HavokTagFile), YAXSerializationOptions.DontSerializeNullObjects);
            HavokTagFile hvkFile = (HavokTagFile)serializer.DeserializeFromFile(path);
            hvkFile.ResolveReferences();
            File.WriteAllBytes(saveLocation, hvkFile.Write());
        }

        public static HavokTagFile Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static HavokTagFile Load(byte[] bytes)
        {
            string sig = StringEx.GetString(bytes, 4, false, StringEx.EncodingType.UTF8, 4, false);
            if (sig != Havok_TAG0_SIGNATURE)
                throw new InvalidDataException($"HavokTagFile.Load: Unknown root tag signature \"{sig}\" (expected {Havok_TAG0_SIGNATURE}), parse failed.");

            HavokTagFile hvkFile = new HavokTagFile();
            HavokPartHeader tagHeader = HavokPartHeader.Parse(bytes, 0);
            HavokPartHeader dataHeader = null;
            hvkFile.Headers = tagHeader;

            List<string> listTSTR = new List<string>();
            List<string> listFSTR = new List<string>();
            List<HavokTagType> tagTypeList = new List<HavokTagType>();
            List<HavokTagItem> tagItemList = new List<HavokTagItem>();

            foreach (HavokPartHeader partHeader in tagHeader.Parts)
            {
                if (partHeader.Signature == Havok_SDKV_SIGNATURE)
                {
                    string verYear = StringEx.GetString(bytes, partHeader.Offset + 8, false, StringEx.EncodingType.UTF8, 4, false);
                    string verMajor = StringEx.GetString(bytes, partHeader.Offset + 12, false, StringEx.EncodingType.UTF8, 2, false);
                    string verMinor = StringEx.GetString(bytes, partHeader.Offset + 14, false, StringEx.EncodingType.UTF8, 2, false);
                    string version = $"{verYear}.{verMajor}.{verMinor}";

                    if (version != Havok_SUPPORTED_VERSION)
                        throw new InvalidDataException($"HavokTagFile.Load: Unsupported version \"{version}\" (only {Havok_SUPPORTED_VERSION} is supported), parse failed.");

                    hvkFile.Version = version;
                }
                else if (partHeader.Signature == Havok_DATA_SIGNATURE)
                {
                    dataHeader = partHeader;
                }
                else if (partHeader.Signature == Havok_TYPE_SIGNATURE)
                {
                    foreach (HavokPartHeader typePartHeader in partHeader.Parts)
                    {
                        if (typePartHeader.Signature == Havok_TSTR_SIGNATURE)
                        {
                            listTSTR.AddRange(ReadStrings(bytes, typePartHeader));
                        }
                        else if (typePartHeader.Signature == Havok_FSTR_SIGNATURE)
                        {
                            listFSTR.AddRange(ReadStrings(bytes, typePartHeader));
                        }
                        else if (typePartHeader.Signature == Havok_TNAM_SIGNATURE)
                        {
                            int offset = typePartHeader.Offset + 8;
                            int numTypes = ReadPacked(bytes, ref offset);

                            for (int i = 0; i < numTypes; i++)
                                tagTypeList.Add(new HavokTagType(i, "None"));

                            for (int i = 1; i < numTypes; i++)
                            {
                                int nameIdx = ReadPacked(bytes, ref offset);
                                int numValues = ReadPacked(bytes, ref offset);

                                HavokTagType tagType = tagTypeList[i];
                                tagType.Name = listTSTR[nameIdx];

                                for (int a = 0; a < numValues; a++)
                                {
                                    int valueNameIdx = ReadPacked(bytes, ref offset);
                                    int value = ReadPacked(bytes, ref offset);

                                    HavokTagTemplate template = new HavokTagTemplate(listTSTR[valueNameIdx], value);
                                    tagType.Templates.Add(template);

                                    if (template.IsType())
                                    {
                                        template.ValueType = tagTypeList[template.Value];
                                    }
                                }
                            }
                        }
                        else if (typePartHeader.Signature == Havok_TBOD_SIGNATURE)
                        {
                            int offset = typePartHeader.Offset + 8;
                            int lastTypeIdx = -1;

                            while (offset < typePartHeader.PartEndOffset)
                            {
                                int typeIdx = ReadPacked(bytes, ref offset);

                                if (typeIdx == 0)
                                    continue;

                                HavokTagType tagType = tagTypeList[typeIdx];

                                int typeParentIdx = ReadPacked(bytes, ref offset);
                                tagType.Parent = tagTypeList[typeParentIdx];
                                tagType.Flags = (HavokTagFlag)ReadPacked(bytes, ref offset);

                                lastTypeIdx = typeIdx;
                                if (tagType.Flags.HasFlag(HavokTagFlag.TF_SubType))
                                {
                                    tagType.SubTypeFlags = (HavokTagSubType)ReadPacked(bytes, ref offset);
                                }
                                if (tagType.Flags.HasFlag(HavokTagFlag.TF_Pointer) && (((int)tagType.SubTypeFlags & 0xF) >= 6))
                                {
                                    tagType.Pointer = tagTypeList[ReadPacked(bytes, ref offset)];
                                }
                                if (tagType.Flags.HasFlag(HavokTagFlag.TF_Version))
                                {
                                    tagType.Version = ReadPacked(bytes, ref offset);
                                }
                                if (tagType.Flags.HasFlag(HavokTagFlag.TF_ByteSize))
                                {
                                    tagType.ByteSize = ReadPacked(bytes, ref offset);
                                    tagType.Alignment = ReadPacked(bytes, ref offset);
                                }
                                if (tagType.Flags.HasFlag(HavokTagFlag.TF_AbstractValue))
                                {
                                    tagType.AbstractValue = ReadPacked(bytes, ref offset);
                                }
                                if (tagType.Flags.HasFlag(HavokTagFlag.TF_Members))
                                {
                                    int numMembers = ReadPacked(bytes, ref offset);

                                    for (int i = 0; i < numMembers; i++)
                                    {
                                        HavokTagMember tagMember = new HavokTagMember();
                                        tagMember.Name = listFSTR[ReadPacked(bytes, ref offset)];
                                        tagMember.Flags = ReadPacked(bytes, ref offset);
                                        tagMember.ByteSize = ReadPacked(bytes, ref offset);
                                        tagMember.TagType = tagTypeList[ReadPacked(bytes, ref offset)];

                                        tagType.Members.Add(tagMember);
                                    }
                                }
                                if (tagType.Flags.HasFlag(HavokTagFlag.TF_Interfaces))
                                {
                                    int numTypeArray = ReadPacked(bytes, ref offset);

                                    for (int i = 0; i < numTypeArray; i++)
                                    {
                                        HavokTagInterface tagInterface = new HavokTagInterface();
                                        tagInterface.TagType = tagTypeList[ReadPacked(bytes, ref offset)];
                                        tagInterface.Value = ReadPacked(bytes, ref offset);

                                        tagType.Interfaces.Add(tagInterface);
                                    }
                                }
                                if (tagType.Flags.HasFlag(HavokTagFlag.TF_Unknown))
                                {
                                    throw new Exception($"Unknown HavokTagFlag 0x80 exists.");
                                }
                            }


                        }
                    }
                }
                else if (partHeader.Signature == Havok_INDX_SIGNATURE)
                {
                    foreach (HavokPartHeader indxPartHeader in partHeader.Parts)
                    {

                        if (indxPartHeader.Signature == Havok_ITEM_SIGNATURE)
                        {
                            int offset = indxPartHeader.Offset + 8;

                            while (offset < indxPartHeader.PartEndOffset)
                            {
                                int flag = BitConverter.ToInt32(bytes, offset);
                                int offsetValue = BitConverter.ToInt32(bytes, offset + 4);
                                int count = BitConverter.ToInt32(bytes, offset + 8);
                                offset += 12;

                                HavokTagItem tagItem = new HavokTagItem();
                                tagItemList.Add(tagItem);

                                tagItem.TagType = tagTypeList[flag & 0xFFFFFF];
                                tagItem.IsPtr = ((flag & 0x10000000) != 0);
                                tagItem.Offset = dataHeader.Offset + 8 + offsetValue;
                                tagItem.Count = count;
                            }
                        }
                        else
                        {
                            throw new InvalidDataException($"HavokTagFile.Load: Unknown header in INDX ({indxPartHeader.Signature})");
                        }
                    }

                }
            }

            //Create the root object from the first item. Children objects will be created recursively.
            if(tagItemList.Count > 1 && dataHeader != null)
            {
                if (tagItemList[1].Count != 1)
                    throw new Exception("Havok.Load: Unexpected count on first item");

                HavokTagItem item = tagItemList[1];

                hvkFile.RootObject = ReadObject(bytes, item.Offset, item.TagType, tagItemList, tagTypeList, item, null);
                item.Objects.Add(hvkFile.RootObject);
            }
            /*
            if (dataHeader != null && tagItemList.Count > 0)
            {
                foreach (HavokTagItem item in tagItemList)
                {
                    if (item.TagType == null || item.TagType?.Name == "None")
                        continue;

                    for (int a = 0; a < item.Count; a++)
                    {
                        int offset = item.Offset + (a * item.TagType.SuperType().ByteSize);

                        item.Objects.Add(ReadObject(bytes, offset, item.TagType, tagItemList, tagTypeList, item, null));

                        if (hvkFile.RootObject == null)
                            hvkFile.RootObject = item.Objects[0];
                    }

                    break;
                }
            }
            */
            
            hvkFile.TagTypes = tagTypeList;
            hvkFile.TagItems = tagItemList;

            return hvkFile;
        }

        public byte[] Write()
        {
            //Create string lists
            List<string> TSTRs = new List<string>();
            List<string> FSTRs = new List<string>();

            for(int i = 1; i < TagTypes.Count; i++)
            {
                HavokTagType type = TagTypes[i];

                if (!TSTRs.Contains(type.Name))
                    TSTRs.Add(type.Name);

                type._NameIdx = TSTRs.IndexOf(type.Name);

                foreach (HavokTagTemplate template in type.Templates)
                {
                    if (!TSTRs.Contains(template.Name))
                        TSTRs.Add(template.Name);

                    template._NameIdx = TSTRs.IndexOf(template.Name);
                }

                foreach (HavokTagMember member in type.Members)
                {
                    if (!FSTRs.Contains(member.Name))
                        FSTRs.Add(member.Name);

                    member._NameIdx = FSTRs.IndexOf(member.Name);
                }
            }

            List<byte> TSTR_bytes = WriteStrings(TSTRs);
            List<byte> FSTR_bytes = WriteStrings(FSTRs);

            //TNAM
            List<byte> tnamBytes = new List<byte>();
            WritePacked(tnamBytes, TagTypes.Count);

            foreach(HavokTagType havokTagType in TagTypes)
            {
                if (havokTagType.Name == "None" || havokTagType.ID == 0) continue; //Skip dummy type

                WritePacked(tnamBytes, havokTagType._NameIdx);
                WritePacked(tnamBytes, havokTagType.Templates.Count);

                foreach(HavokTagTemplate template in havokTagType.Templates)
                {
                    WritePacked(tnamBytes, template._NameIdx);
                    WritePacked(tnamBytes, template.Value);
                }
            }

            //TBOD
            List<byte> tbodBytes = new List<byte>();

            foreach(HavokTagType type in TagTypes)
            {
                if (type.Name == "None" || type.ID == 0) continue; //Skip dummy type

                WritePacked(tbodBytes, type.ID);
                WritePacked(tbodBytes, type.ParentID);
                WritePacked(tbodBytes, (int)type.Flags);

                if (type.Flags.HasFlag(HavokTagFlag.TF_SubType))
                {
                    WritePacked(tbodBytes, (int)type.SubTypeFlags);
                }
                if (type.Flags.HasFlag(HavokTagFlag.TF_Pointer) && (((int)type.SubTypeFlags & 0xF) >= 6))
                {
                    WritePacked(tbodBytes, type.PointerID);
                }
                if (type.Flags.HasFlag(HavokTagFlag.TF_Version))
                {
                    WritePacked(tbodBytes, type.Version);
                }
                if (type.Flags.HasFlag(HavokTagFlag.TF_ByteSize))
                {
                    WritePacked(tbodBytes, type.ByteSize);
                    WritePacked(tbodBytes, type.Alignment);
                }
                if (type.Flags.HasFlag(HavokTagFlag.TF_AbstractValue))
                {
                    WritePacked(tbodBytes, type.AbstractValue);
                }
                if (type.Flags.HasFlag(HavokTagFlag.TF_Members))
                {
                    WritePacked(tbodBytes, type.Members.Count);

                    for (int i = 0; i < type.Members.Count; i++)
                    {
                        WritePacked(tbodBytes, type.Members[i]._NameIdx);
                        WritePacked(tbodBytes, type.Members[i].Flags);
                        WritePacked(tbodBytes, type.Members[i].ByteSize);
                        WritePacked(tbodBytes, type.Members[i].TypeID);
                    }
                }
                if (type.Flags.HasFlag(HavokTagFlag.TF_Interfaces))
                {
                    WritePacked(tbodBytes, type.Interfaces.Count);

                    for (int i = 0; i < type.Interfaces.Count; i++)
                    {
                        WritePacked(tbodBytes, type.Interfaces[i].TypeID);
                        WritePacked(tbodBytes, type.Interfaces[i].Value);
                    }
                }
            }

            //ITEM
            TagItems = HavokTagItem.CreateItems(RootObject, TagTypes, TagItems);
            List<byte> dataBytes = new List<byte>();
            List<byte> itemBytes = new List<byte>();

            for(int i = 0; i < TagItems.Count; i++)
            {
                //Write Item
                int flag = (TagItems[i].TagType.ID & 0xFFFFFF) + (TagItems[i].TagType.ID != 0 ? ((TagItems[i].IsPtr) ? 0x10000000 : 0x20000000) : 0);
                itemBytes.AddRange(BitConverter.GetBytes(flag));
                itemBytes.AddRange(BitConverter.GetBytes(dataBytes.Count));
                itemBytes.AddRange(BitConverter.GetBytes(TagItems[i].Count));

                //Write Data
                for(int a = 0; a < TagItems[i].Objects.Count; a++)
                {
                    WriteObject(TagItems[i].Objects[a], dataBytes, TagItems, TagTypes);
                }
            }

            //Create type parts
            List<byte> typeBytes = new List<byte>();
            typeBytes.AddRange(HavokPartHeader.WritePart(TSTR_bytes, Havok_TSTR_SIGNATURE));
            typeBytes.AddRange(HavokPartHeader.WritePart(tnamBytes, Havok_TNAM_SIGNATURE));
            typeBytes.AddRange(HavokPartHeader.WritePart(FSTR_bytes, Havok_FSTR_SIGNATURE));
            typeBytes.AddRange(HavokPartHeader.WritePart(tbodBytes, Havok_TBOD_SIGNATURE));
            typeBytes.AddRange(HavokPartHeader.WritePart(new List<byte>(), Havok_TPAD_SIGNATURE));

            //Create indx part
            List<byte> indxBytes = new List<byte>();
            indxBytes.AddRange(HavokPartHeader.WritePart(itemBytes, Havok_ITEM_SIGNATURE));

            //Write main parts
            List<byte> mainPartBytes = new List<byte>();

            mainPartBytes.AddRange(HavokPartHeader.WritePart(StringEx.WriteFixedSizeString("20150100", 8).ToList(), Havok_SDKV_SIGNATURE));
            mainPartBytes.AddRange(HavokPartHeader.WritePart(dataBytes, Havok_DATA_SIGNATURE));
            mainPartBytes.AddRange(HavokPartHeader.WritePart(typeBytes, Havok_TYPE_SIGNATURE));
            mainPartBytes.AddRange(HavokPartHeader.WritePart(indxBytes, Havok_INDX_SIGNATURE));

            //Assemble final file
            return HavokPartHeader.WritePart(mainPartBytes, Havok_TAG0_SIGNATURE);
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        internal void ResolveReferences()
        {
            foreach(var type in TagTypes)
            {
                type.DeserializeReferences(TagTypes);
            }

            RootObject.DeserializeReferences(TagTypes);
        }

        private static List<string> ReadStrings(byte[] bytes, HavokPartHeader partHeader)
        {
            List<string> strings = new List<string>();
            int offset = partHeader.Offset + 8;
            int partEnd = partHeader.PartEndOffset;

            if (partEnd > bytes.Length)
                throw new IndexOutOfRangeException($"HavokTagFile.ReadStrings: partEnd exceeds file size");

            while (offset < partEnd)
            {
                string _str = StringEx.GetString(bytes, offset, false, StringEx.EncodingType.UTF8);
                strings.Add(_str);
                offset += _str.Length + 1;
            }

            return strings;
        }

        private static List<byte> WriteStrings(List<string> strings)
        {
            List<byte> bytes = new List<byte>();

            foreach(string str in strings)
            {
                bytes.AddRange(Encoding.UTF8.GetBytes(str));
                bytes.Add(0);
            }

            return bytes;
        }

        internal static int ReadPacked(byte[] bytes, ref int offset)
        {
	        int value = bytes[offset];
            int ret;

            if ((value & 0x80) == 0) //uint8
            {
                offset += 1;
                return value;
            }

	        if ((value & 0x40) == 0) //uint16
            {
                ret = (((value << 8) | (bytes[offset + 1])) & 0x3fff);
                offset += 2;
                return ret;
	        }

	        if ((value & 0x20) == 0) //uint24
	        {
                ret = (((value << 16) | (bytes[offset + 1] << 8) | (bytes[offset + 2])) & 0x1fffff);
                offset += 3;
                return ret;
	        }

            //uint32
            ret = (((value << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | (bytes[offset + 3])) & 0x1fffffff);
            offset += 4;
            return ret;
        }

        internal static void WritePacked(List<byte> bytes, int value)
        {
            WritePacked(bytes, (uint)value);
        }

        internal static void WritePacked(List<byte> bytes, uint value)
        {
            // Clamp value to 29 bits (clear the top 3 bits)
            if (value > 0x1FFFFFFF)
                value &= 0x1FFFFFFF;

            if (value < 0x80) // 1 byte: 0xxxxxxx
            {
                bytes.Add((byte)value);
                return;
            }

            if (value < 0x4000) // 2 bytes: 10xxxxxx xxxxxxxx
            {
                bytes.Add((byte)(((value >> 8) & 0xFF) | 0x80));
                bytes.Add((byte)(value & 0xFF));
                return;
            }

            if (value < 0x200000) // 3 bytes: 110xxxxx xxxxxxxx xxxxxxxx
            {
                bytes.Add((byte)(((value >> 16) & 0xFF) | 0x80 | 0x40));
                bytes.Add((byte)((value >> 8) & 0xFF));
                bytes.Add((byte)(value & 0xFF));
                return;
            }

            // 4 bytes: 1110xxxx xxxxxxxx xxxxxxxx xxxxxxxx
            bytes.Add((byte)(((value >> 24) & 0xFF) | 0x80 | 0x40 | 0x20));
            bytes.Add((byte)((value >> 16) & 0xFF));
            bytes.Add((byte)((value >> 8) & 0xFF));
            bytes.Add((byte)(value & 0xFF));
        }

        internal static void WritePackedOld(List<byte> listBytesTNam, int value)
        {
            if (value > 0x1fffffff)
                value = value & 0x1fffffff;

            if (value < 0x80)                       //need a uin8
            {
                listBytesTNam.Add((byte)value);
                return;
            }

            if (value < 0x4000)                     //need a uin16
            {
                listBytesTNam.Add((byte)(((value >> 8) & 0xFF) | 0x80));
                listBytesTNam.Add((byte)(value & 0xFF));
                return;
            }

            if (value < 0x200000)                   //need a uin24
            {
                listBytesTNam.Add((byte)(((value >> 16) & 0xFF) | 0x80 | 0x40));
                listBytesTNam.Add((byte)((value >> 8) & 0xFF));
                listBytesTNam.Add((byte)(value & 0xFF));
                return;
            }

            //need a uin32
            listBytesTNam.Add((byte)(((value >> 24) & 0xFF) | 0x80 | 0x40 | 0x20));
            listBytesTNam.Add((byte)((value >> 16) & 0xFF));
            listBytesTNam.Add((byte)((value >> 8) & 0xFF));
            listBytesTNam.Add((byte)(value & 0xFF));
        }
        
        internal static long ReadFormat(byte[] bytes, int offset, HavokTagSubType flags, ref string type_str, bool bigEndian = false)
        {
            bool isSigned = flags.HasFlag(HavokTagSubType.TST_IsSigned);
            long value = 0;

            if (flags.HasFlag(HavokTagSubType.TST_Int8))
            {
                type_str = (isSigned ? "i" : "u") + "8";
                return (isSigned) ? (long)((sbyte)bytes[offset]) : bytes[offset];

	        }
	        else if (flags.HasFlag(HavokTagSubType.TST_Int16))
            {
		        type_str = (isSigned? "i" : "u") + "16";

		        value = (isSigned) ? (long)BitConverter.ToInt16(bytes, offset) : (long)BitConverter.ToUInt16(bytes, offset);

	        }
            else if (flags.HasFlag(HavokTagSubType.TST_Int32))
            {
                type_str = (isSigned ? "i" : "u") + "32";
                value = (isSigned) ? (long)BitConverter.ToInt32(bytes, offset) : (long)BitConverter.ToUInt32(bytes, offset);

            }
            else if (flags.HasFlag(HavokTagSubType.TST_Int64))
            {
                type_str = (isSigned ? "i" : "u") + "64";
                value = (isSigned) ? (long)BitConverter.ToInt64(bytes, offset) : (long)BitConverter.ToUInt64(bytes, offset);
            }

            if (bigEndian)
            {
                byte[] _temp = BitConverter.GetBytes(value);
                value = BigEndianConverter.ReadInt64(_temp, 0);
            }

            return value;
        }

        internal static void WriteFormat(long value, List<byte> listBytesData, HavokTagSubType flags)
        {
            bool isSigned = flags.HasFlag(HavokTagSubType.TST_IsSigned);

            if (flags.HasFlag(HavokTagSubType.TST_Int8))
            {
                listBytesData.Add((byte)value);

            }
            else if (flags.HasFlag(HavokTagSubType.TST_Int16))
            {
                ushort value_tmp = (ushort)value;

                listBytesData.Add((byte)(value_tmp & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 8) & 0xFF));

            }
            else if (flags.HasFlag(HavokTagSubType.TST_Int32))
            {
                uint value_tmp = (uint)value;

                listBytesData.Add((byte)(value_tmp & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 8) & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 16) & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 24) & 0xFF));

            }
            else if (flags.HasFlag(HavokTagSubType.TST_Int64))
            {
                ulong value_tmp = (ulong)value;

                listBytesData.Add((byte)(value_tmp & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 8) & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 16) & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 24) & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 32) & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 40) & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 48) & 0xFF));
                listBytesData.Add((byte)((value_tmp >> 56) & 0xFF));
            }
        }

        internal static List<HavokTagObject> ReadItemPtr(byte[] bytes, int offset, List<HavokTagItem> listItem, List<HavokTagType> listType, bool indexInversed = false)
        {
            List<HavokTagObject> ret = null;
            string str = string.Empty;
            int index = (int)ReadFormat(bytes, offset, HavokTagSubType.TST_Int32, ref str, indexInversed);

            if ((index == 0) || (index >= listItem.Count))
                return ret;

            HavokTagItem item = listItem[index];

            if (item.Objects.Count == 0)
            {
                for (int i = 0; i < item.Count; i++)
                {
                    int offset_tmp = item.Offset + i * item.TagType.SuperType().ByteSize;
                    item.Objects.Add(ReadObject(bytes, offset_tmp, item.TagType, listItem, listType, item, null));
                }
            }

            return item.Objects;
        }

        internal static HavokTagObject ReadObject(byte[] bytes, int offset, HavokTagType type, List<HavokTagItem> items, List<HavokTagType> types, HavokTagItem parentAttachement, HavokTagMember member)
        {
            HavokTagType typeOrigin = type;
            type = type.SuperType();

            HavokTagObject obj = new HavokTagObject();
            obj.TagType = typeOrigin;
            obj.ParentAttachment = parentAttachement;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].TagType.ID == typeOrigin.ID)
                {
                    obj.ParentAttachment = items[i];
                    break;
                }
            }

            obj.Name = member?.Name;

            string type_str = "";
            if (type.SubType() == HavokTagSubType.TST_Bool)
            {
                obj.BoolValue = (ReadFormat(bytes, offset, type.SubTypeFlags, ref type_str) > 0);
            }
            else if (type.SubType() == HavokTagSubType.TST_Int)
            {
                obj.IntValue = (int)ReadFormat(bytes, offset, type.SubTypeFlags, ref type_str);
            }
            else if (type.SubType() == HavokTagSubType.TST_Float)
            {
                obj.FloatValue = BitConverter.ToSingle(bytes, offset);
            }
            else if (type.SubType() == HavokTagSubType.TST_String)
            {
                throw new NotImplementedException("HavokTagFile.ReadObject: String type not implemented!");
            }
            else if (type.SubType() == HavokTagSubType.TST_Pointer)
            {
                List<HavokTagObject> listObj = ReadItemPtr(bytes, offset, items, types);

                if (listObj?.Count == 1)
                {
                    obj.Objects.Add(listObj[0]);
                }
            }
            else if (type.SubType() == HavokTagSubType.TST_Class)
            {
                obj.TName = type.Name;
                List<HavokTagMember> allMembers = type.AllMembers();

                int offset_tmp = 0;
                for (int i = 0; i < allMembers.Count; i++)
                {
                    offset_tmp = offset + allMembers[i].ByteSize;
                    obj.Objects.Add(ReadObject(bytes, offset_tmp, allMembers[i].TagType, items, types, obj.ParentAttachment, allMembers[i]));
                }
            }
            else if (type.SubType() == HavokTagSubType.TST_Array)
            {
                var arrayObjects = ReadItemPtr(bytes, offset, items, types);

                if(arrayObjects != null)
                {
                    for(int i = 0; i < arrayObjects.Count; i++)
                    {
                        arrayObjects[i].XML_ObjectIndex = i.ToString();
                        obj.Objects.Add(arrayObjects[i]);
                    }
                }
            }
            else if (type.SubType() == HavokTagSubType.TST_Tuple)
            {
                int numTuple = type.TupleSize();

                for (int i = 0; i < numTuple; i++)
                {
                    int offset_tmp = offset + i * type.Pointer.SuperType().ByteSize;

                    obj.Objects.Add(ReadObject(bytes, offset_tmp, type.Pointer, items, types, obj.ParentAttachment, null));
                }
            }

            return obj;
        }

        internal static void WriteObject(HavokTagObject obj, List<byte> dataBytes, List<HavokTagItem> items, List<HavokTagType> types)
        {
            HavokTagType originType = obj.TagType;
            HavokTagType type = originType.SuperType();
            HavokTagSubType subType = type.SubType();

            int dataStart = dataBytes.Count;

            if(subType == HavokTagSubType.TST_Bool)
            {
                WriteFormat((obj.BoolValue ? 1 : 0), dataBytes, type.SubTypeFlags);
            }
            else if (subType == HavokTagSubType.TST_Int)
            {
                WriteFormat(obj.IntValue, dataBytes, type.SubTypeFlags);
            }
            else if (subType == HavokTagSubType.TST_Float)
            {
                dataBytes.AddRange(BitConverter.GetBytes(obj.FloatValue));
            }
            else if (subType == HavokTagSubType.TST_String)
            {
                throw new NotImplementedException("Havok.WriteObject: String type not implemented!");
            }
            else if (subType == HavokTagSubType.TST_Pointer || subType == HavokTagSubType.TST_Array)
            {
                WriteFormat(obj._SaveItemPointerIdx, dataBytes, HavokTagSubType.TST_Int32);
            }
            else if(subType == HavokTagSubType.TST_Class)
            {
                foreach(HavokTagMember member in type.AllMembers().OrderBy(x => x.ByteSize))
                {
                    if (dataBytes.Count - dataStart < member.ByteSize) //Add padding to ensure proper byte alignment for the values if nessecary
                        dataBytes.AddRange(new byte[(member.ByteSize + dataStart) - dataBytes.Count]);

                    HavokTagObject memberObj = obj.GetChild(member.Name);

                    if (memberObj == null)
                        throw new ArgumentNullException($"Havok.WriteObject: member \"{member.Name}\" of class \"{obj.TName}\" is missing.");

                    WriteObject(memberObj, dataBytes, items, types);
                }
            }
            else if(subType == HavokTagSubType.TST_Tuple)
            {
                int numTuple = type.TupleSize();

                if (numTuple != obj.Objects.Count)
                    throw new ArgumentException("Havok.WriteObject: TupleSize does not match number of child objects");

                for (int i = 0; i < numTuple; i++)
                {
                    WriteObject(obj.Objects[i], dataBytes, items, types);
                }
            }

            if (dataBytes.Count - dataStart != type.ByteSize)
            {
                dataBytes.AddRange(new byte[(type.ByteSize + dataStart) - dataBytes.Count]);
                //throw new Exception("Havok.WriteObject: data size does not match up");
            }

        }

        #endregion

        #region CollisionImport
        /// <summary>
        /// Replace the mesh in this <see cref="HavokTagFile"/> with a convex mesh. NOTE: Only works on hknpConvexShape havok files.
        /// </summary>
        /// <param name="vertices">List of convex vertices</param>
        /// <returns>true if replace was a success</returns>
        public bool ReplaceMesh(Vector3[] vertices)
        {
            if (RootObject.TName == "hknpConvexShape")
            {
                HavokTagObject verticesObject = RootObject.GetChild("vertices");

                if (vertices != null)
                {
                    HavokTagObject vertexReferenceItem = verticesObject.Objects[0].Clone();
                    verticesObject.Objects.Clear();

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        HavokTagObject newVertex = vertexReferenceItem.Clone();
                        newVertex.Objects[0].FloatValue = vertices[i].X;
                        newVertex.Objects[1].FloatValue = vertices[i].Y;
                        newVertex.Objects[2].FloatValue = vertices[i].Z;
                        newVertex.Objects[3].FloatValue = 0.5f;

                        verticesObject.Objects.Add(newVertex);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Replace the mesh in this <see cref="HavokTagFile"/> with either a concave or convex mesh (determined by type of havok file passed in; supports hknpExternMeshShape and hknpConvexShape)
        /// NOTE: No validation is done on convex meshes. Ensure that a mesh is actually convex before passing it into this method with a convex havok file!
        /// </summary>
        /// <returns>true if replace was a success</returns>
        public bool ReplaceMesh(HavokCollisionMesh mesh)
        {
            if (RootObject.TName == "hknpExternMeshShape")
            {
                HavokTagObject geometry = RootObject.GetChild("geometry")?.GetChild("hknpDefaultExternMeshShapeGeometry", true)?.GetChild("geometry")?.GetChild("hkGeometry", true);

                if (geometry != null)
                {
                    HavokTagObject triangles = geometry.GetChild("triangles");
                    HavokTagObject vertices = geometry.GetChild("vertices");

                    if (triangles == null || vertices == null) return false;

                    HavokTagObject vertexReferenceItem = vertices.Objects[0].Clone();
                    HavokTagObject triangleReferenceItem = triangles.Objects[0].Clone();

                    vertices.Objects.Clear();
                    triangles.Objects.Clear();

                    for (int i = 0; i < mesh.Indices.Length; i += 3)
                    {
                        HavokTagObject triangle = triangleReferenceItem.Clone();
                        triangles.Objects.Add(triangle);

                        triangle.Objects[0].IntValue = mesh.Indices[i];
                        triangle.Objects[1].IntValue = mesh.Indices[i + 1];
                        triangle.Objects[2].IntValue = mesh.Indices[i + 2];
                    }

                    for (int i = 0; i < mesh.Vertices.Length; i++)
                    {
                        HavokTagObject vertex = vertexReferenceItem.Clone();
                        vertices.Objects.Add(vertex);

                        vertex.Objects[0].FloatValue = mesh.Vertices[i].X;
                        vertex.Objects[1].FloatValue = mesh.Vertices[i].Y;
                        vertex.Objects[2].FloatValue = mesh.Vertices[i].Z;
                        vertex.Objects[3].FloatValue = 0f;
                    }

                    //Create BVH
                    HavokTagObject aabbTree = RootObject.GetChild("boundingVolumeData").GetChild("hknpExternMeshShapeData", true).GetChild("aabbTree");
                    HavokTagObject simdTree = RootObject.GetChild("boundingVolumeData").GetChild("hknpExternMeshShapeData", true).GetChild("simdTree").GetChild("nodes");
                    BVH_Node[] bvhNodes = BoundingVolumeHierarchy.CreateBVH(mesh);

                    CreateAabbTree(bvhNodes, aabbTree);
                    CreateSimdTree(bvhNodes, simdTree);

                    //Set numShapeKeyBits based on new triangle count
                    //This is a value used internally by Havok to determine how many bits to use to address triangles. There needs to be enough bits to address the whole range of triangles, or the game will crash
                    HavokTagObject numShapeKeyBits = RootObject.GetChild("numShapeKeyBits");
                    numShapeKeyBits.IntValue = (int)Math.Ceiling(Math.Log(triangles.Objects.Count, 2));

                    if (numShapeKeyBits.IntValue > 24) return false; //Too many triangles, will crash

                    return true;
                }
            }
            else if (RootObject.TName == "hknpConvexShape")
            {
                HavokTagObject verticesObject = RootObject.GetChild("vertices");

                if (mesh?.Vertices?.Length > 0)
                {
                    HavokTagObject vertexReferenceItem = verticesObject.Objects[0].Clone();
                    verticesObject.Objects.Clear();

                    for (int i = 0; i < mesh.Vertices.Length; i++)
                    {
                        HavokTagObject newVertex = vertexReferenceItem.Clone();
                        newVertex.Objects[0].FloatValue = mesh.Vertices[i].X;
                        newVertex.Objects[1].FloatValue = mesh.Vertices[i].Y;
                        newVertex.Objects[2].FloatValue = mesh.Vertices[i].Z;
                        newVertex.Objects[3].FloatValue = 0.5f;

                        verticesObject.Objects.Add(newVertex);
                    }

                    return true;
                }
            }

            return false;
        }

        private static void CreateAabbTree(BVH_Node[] bvhNodes, HavokTagObject aabbTree)
        {
            HavokTagObject nodes = aabbTree.GetChild("nodes");
            HavokTagObject aabbDomainMin = aabbTree.GetChild("domain").GetChild("min");
            HavokTagObject aabbDomainMax = aabbTree.GetChild("domain").GetChild("max");

            //This tree is not required, as SimdTree is used instead.
            //Perhaps used for older PC's that lack SIMD support - but the min spec for DBXV2 should support it just fine, so not an issue here
            nodes.Objects.Clear();

            //Set domain AABB values
            //Even though we aren't using the AabbTree, these values must still be set as they are still internally used by Havok even when SimdTree is in use
            bvhNodes[0].CalculateNodeAabb(out Vector3 min, out Vector3 max);
            aabbDomainMin.Objects[0].FloatValue = min.X;
            aabbDomainMin.Objects[1].FloatValue = min.Y;
            aabbDomainMin.Objects[2].FloatValue = min.Z;
            aabbDomainMax.Objects[0].FloatValue = max.X;
            aabbDomainMax.Objects[1].FloatValue = max.Y;
            aabbDomainMax.Objects[2].FloatValue = max.Z;
        }

        private static void CreateSimdTree(BVH_Node[] bvhNodes, HavokTagObject simdTree)
        {
            HavokTagObject referenceItem = simdTree.Objects[0].Clone();
            simdTree.Objects.Clear();
            simdTree.Objects.Add(referenceItem.Clone());

            for (int i = 0; i < bvhNodes.Length; i++)
            {
                HavokTagObject node = referenceItem.Clone();
                HavokTagObject lx = node.GetChild("lx");
                HavokTagObject hx = node.GetChild("hx");
                HavokTagObject ly = node.GetChild("ly");
                HavokTagObject hy = node.GetChild("hy");
                HavokTagObject lz = node.GetChild("lz");
                HavokTagObject hz = node.GetChild("hz");
                HavokTagObject data = node.GetChild("data");

                for (int a = 0; a < bvhNodes[i].Indices.Length; a++)
                {
                    if (bvhNodes[i].Indices[a] == -1) break;

                    lx.Objects[a].FloatValue = bvhNodes[i].AabbMin[a].X;
                    ly.Objects[a].FloatValue = bvhNodes[i].AabbMin[a].Y;
                    lz.Objects[a].FloatValue = bvhNodes[i].AabbMin[a].Z;
                    hx.Objects[a].FloatValue = bvhNodes[i].AabbMax[a].X;
                    hy.Objects[a].FloatValue = bvhNodes[i].AabbMax[a].Y;
                    hz.Objects[a].FloatValue = bvhNodes[i].AabbMax[a].Z;

                    //The way SimdTree node linking works is:
                    //-If Data is an even number: This is an internal node, and data references a child SIMD node. Divide by 2 to get the real index (so we need to multiply by 2; the 1 addition is for the root dummy entry in the simdTree)
                    //-If it is an odd number: This is a leaf node, and data references a triangle
                    //-In this case, the value is read as: TriangleIndex = (data - aabbCount + 1) / 2)
                    //-So we encode it as the opposite: (value * 2) + 1
                    if (bvhNodes[i].isLeaf)
                    {
                        data.Objects[a].IntValue = (bvhNodes[i].Indices[a] * 2) + 1;
                    }
                    else
                    {
                        data.Objects[a].IntValue = (bvhNodes[i].Indices[a] + 1) * 2;
                    }
                }

                simdTree.Objects.Add(node);
            }
        }

        #endregion

        #region CollisionExport
        public HavokCollisionMesh ExtractMesh()
        {
            HavokCollisionMesh mesh = new HavokCollisionMesh();

            if (RootObject.TName == "hknpExternMeshShape")
            {
                HavokTagObject geometry = RootObject.GetChild("geometry")?.GetChild("hknpDefaultExternMeshShapeGeometry", true)?.GetChild("geometry")?.GetChild("hkGeometry", true);

                if (geometry != null)
                {
                    HavokTagObject triangles = geometry.GetChild("triangles");
                    HavokTagObject vertices = geometry.GetChild("vertices");

                    if (triangles == null || vertices == null) return null;

                    List<ushort> faces = new List<ushort>();

                    foreach (var triangle in triangles.Objects)
                    {
                        if (triangle.Objects.Count != 4) return null;

                        faces.Add((ushort)triangle.Objects[0].IntValue);
                        faces.Add((ushort)triangle.Objects[1].IntValue);
                        faces.Add((ushort)triangle.Objects[2].IntValue);
                    }

                    List<Vector3> _verts = new List<Vector3>();

                    foreach (var vert in vertices.Objects)
                    {
                        if (vert.Objects.Count != 4) return null;

                        _verts.Add(new Vector3(vert.Objects[0].FloatValue, vert.Objects[1].FloatValue, vert.Objects[2].FloatValue));
                    }

                    mesh.Indices = ArrayConvert.ConvertToIntArray(faces.ToArray());
                    mesh.Vertices = _verts.ToArray();
                    return mesh;
                }
            }
            else if (IsConvexMesh())
            {
                Vector3[] vertices = ExtractConvexPoints();

                var verts = vertices.Select(v => new ConvexVertex(v)).ToList();

                var hull = ConvexHull.Create(verts);
                // Extract triangle indices
                List<int> indices = new List<int>();
                List<Vector3> convexVertices = new List<Vector3>();

                var vertToIndex = new Dictionary<Vector3, int>();

                if (hull.Outcome == ConvexHullCreationResultOutcome.Success)
                {
                    foreach (var face in hull.Result.Faces)
                    {
                        foreach (var vert in face.Vertices)
                        {
                            Vector3 pos = ((ConvexVertex)vert).Original;
                            if (!vertToIndex.TryGetValue(pos, out int index))
                            {
                                index = convexVertices.Count;
                                convexVertices.Add(pos);
                                vertToIndex[pos] = index;
                            }
                            indices.Add((short)index);
                        }
                    }

                    mesh.Vertices = convexVertices.ToArray();
                    mesh.Indices = indices.ToArray();
                    return mesh;
                }

            }

            return null;
        }

        public List<int[]> ExtractTriangles()
        {
            List<int[]> triangleList = new List<int[]>();

            if (RootObject.TName == "hknpExternMeshShape")
            {
                HavokTagObject geometry = RootObject.GetChild("geometry")?.GetChild("hknpDefaultExternMeshShapeGeometry", true)?.GetChild("geometry")?.GetChild("hkGeometry", true);

                if (geometry != null)
                {
                    HavokTagObject triangles = geometry.GetChild("triangles");

                    if (triangles == null) return null;

                    foreach (var triangle in triangles.Objects)
                    {
                        if (triangle.Objects.Count != 4) return null;

                        triangleList.Add(new int[3]
                        {
                            triangle.Objects[0].IntValue,
                            triangle.Objects[1].IntValue,
                            triangle.Objects[2].IntValue
                        });
                    }
                }
            }

            return triangleList;
        }

        public Vector3[] ExtractConvexPoints()
        {
            List<Vector3> vertices = new List<Vector3>();

            if (RootObject.TName == "hknpConvexShape" || RootObject.TName == "hknpConvexPolytopeShape")
            {
                HavokTagObject verticesObject = RootObject.GetChild("vertices");

                if (vertices != null)
                {
                    foreach (var vert in verticesObject.Objects)
                    {
                        vertices.Add(new Vector3(vert.Objects[0].FloatValue, vert.Objects[1].FloatValue, vert.Objects[2].FloatValue));
                    }

                }
            }
            else if (RootObject.TName == "hknpExternMeshShape")
            {
                HavokTagObject geometry = RootObject.GetChild("geometry")?.GetChild("hknpDefaultExternMeshShapeGeometry", true)?.GetChild("geometry")?.GetChild("hkGeometry", true);

                HavokTagObject _vertices = geometry.GetChild("vertices");

                if (_vertices == null) return null;

                List<FMP_Vertex> fmpVertices = new List<FMP_Vertex>();

                foreach (var vert in _vertices.Objects)
                {
                    if (vert.Objects.Count != 4) return null;
                    vertices.Add(new Vector3(vert.Objects[0].FloatValue, vert.Objects[1].FloatValue, vert.Objects[2].FloatValue));
                }
            }

            return vertices.ToArray();
        }

        #endregion

        public bool IsConvexMesh()
        {
            return RootObject.TName == "hknpConvexShape" || RootObject.TName == "hknpConvexPolytopeShape";
        }
    
    }

    public class HavokTagType
    {
        internal int _NameIdx = -1;

        [YAXAttributeForClass]
        public int ID { get; set; }
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int Version { get; set; }
        [YAXAttributeForClass]
        public HavokTagFlag Flags { get; set; }
        [YAXAttributeForClass]
        public HavokTagSubType SubTypeFlags { get; set; }

        [YAXAttributeForClass]
        public int ByteSize { get; set; }
        [YAXAttributeForClass]
        public int Alignment { get; set; }
        [YAXAttributeForClass]
        public int AbstractValue { get; set; }
        [YAXAttributeForClass]
        public int ParentID
        {
            get => Parent != null ? Parent.ID : _parentTypeId;
            set
            {
                _parentTypeId = value;
            }
        }
        [YAXAttributeForClass]
        public int PointerID
        {
            get => Pointer != null ? Pointer.ID : _pointerTypeId;
            set
            {
                _pointerTypeId = value;
                if (value != Pointer?.ID)
                    Pointer = null;
            }
        }

        private int _parentTypeId = -1;
        private int _pointerTypeId = -1;
        [YAXDontSerialize]
        public HavokTagType Parent { get; set; }
        [YAXDontSerialize]
        public HavokTagType Pointer { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Template")]
        [YAXDontSerializeIfNull]
        public List<HavokTagTemplate> Templates { get; set; } = new List<HavokTagTemplate>();
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Member")]
        [YAXDontSerializeIfNull]
        public List<HavokTagMember> Members { get; set; } = new List<HavokTagMember>();
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Interface")]
        [YAXDontSerializeIfNull]
        public List<HavokTagInterface> Interfaces { get; set; } = new List<HavokTagInterface>();

        public HavokTagType() { }

        public HavokTagType(int id, string name)
        {
            ID = id;
            Name = name;
        }

        public void SerializeReferences(List<HavokTagType> tagTypes)
        {
            _parentTypeId = tagTypes.IndexOf(Parent);
            _pointerTypeId = tagTypes.IndexOf(Pointer);

            foreach(var template in Templates)
            {
                template.SerializeReferences(tagTypes);
            }

            foreach (var member in Members)
            {
                member.SerializeReferences(tagTypes);
            }
        }

        public void DeserializeReferences(List<HavokTagType> tagTypes)
        {
            if (_parentTypeId != -1 && _parentTypeId < tagTypes.Count)
                Parent = tagTypes[_parentTypeId];

            if (_pointerTypeId != -1 && _pointerTypeId < tagTypes.Count)
                Pointer = tagTypes[_pointerTypeId];

            if (Pointer == this || Parent == this)
                throw new Exception("HavokTagType: Recursion detected");

            if (Templates == null)
                Templates = new List<HavokTagTemplate>();

            if(Members == null)
                Members = new List<HavokTagMember>();

            if(Interfaces == null)
                Interfaces = new List<HavokTagInterface>();

            foreach (var template in Templates)
            {
                template.DeserializeReferences(tagTypes);
            }

            foreach (var member in Members)
            {
                member.DeserializeReferences(tagTypes);
            }

            foreach(var _interface in Interfaces)
            {
                _interface.TagType = tagTypes[_interface.TypeID];
            }
        }

        public List<HavokTagMember> AllMembers()
        {
            List<HavokTagMember> members = new List<HavokTagMember>();

            if(Parent != null)
                members.AddRange(Parent.AllMembers());

            members.AddRange(Members);

            return members;
        }

        public HavokTagType SuperType()
        {
            return Flags.HasFlag(HavokTagFlag.TF_SubType) ? this : Parent.SuperType();
        }

        public HavokTagSubType SubType()
        {
            return (HavokTagSubType)((int)SubTypeFlags & (int)HavokTagSubType.TST_TypeMask);
        }
    
        public int TupleSize() 
        { 
            return ((int)SubTypeFlags >> 8); 
        }
    }

    public class HavokTagTemplate
    {
        internal int _NameIdx = -1;

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int Value { get; set; }

        [YAXDontSerialize]
        public HavokTagType ValueType { get; set; }

        public HavokTagTemplate() { }

        public HavokTagTemplate(string name, int value, HavokTagType valueType = null)
        {
            Name = name;
            Value = value;
            ValueType = valueType;
        }

        public void SerializeReferences(List<HavokTagType> tagTypes)
        {
            if (IsType())
                Value = tagTypes.IndexOf(ValueType);
        }

        public void DeserializeReferences(List<HavokTagType> tagTypes)
        {
            if (IsType() && Value != -1)
                ValueType = tagTypes[Value];
        }

        public bool IsInt() { return (Name.Substring(0, 1) == "v"); }
        public bool IsType() { return (Name.Substring(0, 1) == "t"); }
    }

    public class HavokTagMember
    {
        internal int _NameIdx = -1;

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int Flags { get; set; }
        [YAXAttributeForClass]
        public int ByteSize { get; set; } //"offset"
        [YAXAttributeForClass]
        public int TypeID
        {
            get => TagType != null ? TagType.ID : _typeIdx;
            set
            {
                _typeIdx = value;
                if (value != TagType?.ID)
                    TagType = null;
            }
        }

        private int _typeIdx = -1;
        [YAXDontSerialize]
        public HavokTagType TagType { get; set; }

        public HavokTagMember() { }

        public HavokTagMember(string name = "v", int flags = 0, int byteSize = 0, HavokTagType tagType = null)
        {
            Name = name;
            Flags = flags;
            ByteSize = byteSize;
            TagType = tagType;
        }

        public void SerializeReferences(List<HavokTagType> tagTypes)
        {
            _typeIdx = tagTypes.IndexOf(TagType);
        }

        public void DeserializeReferences(List<HavokTagType> tagTypes)
        {
            if (_typeIdx != -1 && _typeIdx < tagTypes.Count)
                TagType = tagTypes[_typeIdx];
        }

        public override string ToString()
        {
            return $"Name: {Name}, ByteSize: {ByteSize}, TypeID: {TypeID}";
        }
    }

    public class HavokTagInterface
    {
        [YAXAttributeForClass]
        public int Value { get; set; }
        [YAXAttributeForClass]
        public int TypeID
        {
            get => TagType != null ? TagType.ID : _typeID;
            set
            {
                _typeID = value;
                if (value != TagType?.ID)
                    TagType = null;
            }
        }

        private int _typeID = -1;
        [YAXDontSerializeIfNull]
        public HavokTagType TagType { get; set; }
        
    }

    public class HavokTagItem
    {
        [YAXAttributeForClass]
        public int Offset { get; set; }
        [YAXAttributeForClass]
        public int Count { get; set; }
        [YAXAttributeForClass]
        public bool IsPtr { get; set; }
        [YAXAttributeForClass]
        public int TypeID
        {
            get => TagType != null ? TagType.ID : _typeID;
            set => _typeID = value;
        }

        private int _typeID = -1;
        [YAXDontSerialize]
        public HavokTagType TagType { get; set; }

        //[YAXDontSerialize]
        public List<HavokTagObject> Objects { get; set; } = new List<HavokTagObject>();
    
        public HavokTagItem() { }

        public HavokTagItem(int count, bool isPtr, HavokTagType type)
        {
            Count = count;
            IsPtr = isPtr;
            TagType = type;
        }

        internal static List<HavokTagItem> CreateItems(HavokTagObject rootObject, List<HavokTagType> types, List<HavokTagItem> originalRefList)
        {
            List<HavokTagItem> items = new List<HavokTagItem>();

            items.Add(new HavokTagItem(0, false, types[0])); //dummy
            items.Add(new HavokTagItem(1, true, rootObject.TagType)); //root
            items[1].Objects.Add(rootObject);

            CreateItem(rootObject, items);

            return items;
        }

        private static void CreateItem(HavokTagObject obj, List<HavokTagItem> items)
        {
            HavokTagSubType subType = obj.TagType.SubType();
            bool isPtr = subType == HavokTagSubType.TST_Pointer;

            if (isPtr || subType == HavokTagSubType.TST_Array)
            {
                if (isPtr && obj.Objects.Count > 1)
                    throw new Exception("HavokTagItem.CreateItem: Pointer type count must be 1!");

                if (obj.Objects.Count != 0)
                {
                    obj._SaveItemPointerIdx = items.Count;

                    HavokTagItem item = new HavokTagItem(obj.Objects.Count, isPtr, obj.GetChildType());
                    item.Objects.AddRange(obj.Objects);
                    items.Add(item);
                }
                else
                {
                    obj._SaveItemPointerIdx = 0;
                }
            }

            foreach(var _childObj in obj.Objects)
            {
                CreateItem(_childObj, items);
            }

        }
        
    }

    public class HavokTagObject
    {
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string XML_ObjectIndex { get; set; }

        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("TypeID")]
        public int XML_TypeID
        {
            get => TagType != null ? TagType.ID : _typeID;
            set => _typeID = value;
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        [YAXDontSerializeIfNull]
        public string XML_Type
        {
            get
            {
                switch (TagType.SuperType().SubType())
                {
                    case HavokTagSubType.TST_Int:
                        return "int";
                    case HavokTagSubType.TST_Float:
                        return "float";
                    case HavokTagSubType.TST_Bool:
                        return "bool";
                    case HavokTagSubType.TST_String:
                        return "string";
                    case HavokTagSubType.TST_Class:
                        return "Class";
                    case HavokTagSubType.TST_Tuple:
                        return "Tuple";
                    case HavokTagSubType.TST_Pointer:
                        return "Pointer";
                    case HavokTagSubType.TST_Array:
                        return "Array";
                    default:
                        return null;
                }
            }
        }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string TName { get; set; }

        [YAXAttributeForClass]
        [YAXSerializeAs("Value")]
        [YAXDontSerializeIfNull]
        public string XML_Value
        {
            get
            {
                switch (TagType.SuperType().SubType())
                {
                    case HavokTagSubType.TST_Int:
                        return IntValue.ToString();
                    case HavokTagSubType.TST_Float:
                        return FloatValue.ToString();
                    case HavokTagSubType.TST_Bool:
                        return BoolValue ? "true" : "false";
                    case HavokTagSubType.TST_String:
                        return StringValue;
                    default:
                        return null;
                }
            }
            set
            {
                _xmlValue = value;
            }
        }
        private string _xmlValue = string.Empty;

        public int IntValue;
        public float FloatValue;
        public bool BoolValue;
        public string StringValue;

        private int _typeID = -1;
        public HavokTagType TagType;
        public HavokTagItem ParentAttachment;

        internal int _SaveItemPointerIdx = -1;


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Object")]
        public List<HavokTagObject> Objects { get; set; } = new List<HavokTagObject>();


        public void DeserializeReferences(List<HavokTagType> tagTypes)
        {
            if (_typeID != -1 && _typeID < tagTypes.Count)
                TagType = tagTypes[_typeID];

            if(Objects == null)
                Objects = new List<HavokTagObject>();

            foreach(var obj in Objects)
            {
                obj.DeserializeReferences(tagTypes);
            }

            switch (TagType.SuperType().SubType())
            {
                case HavokTagSubType.TST_Int:
                    int.TryParse(_xmlValue, out IntValue);
                    break;
                case HavokTagSubType.TST_Float:
                    float.TryParse(_xmlValue, out FloatValue);
                    break;
                case HavokTagSubType.TST_Bool:
                    BoolValue = _xmlValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case HavokTagSubType.TST_String:
                    StringValue = _xmlValue;
                    break;
            }
        }

        internal HavokTagType GetChildType()
        {
            if(Objects?.Count > 0)
            {
                return Objects[0].TagType;
            }

            return null;
        }
    
        public HavokTagObject GetChild(string name, bool useTName = false)
        {
            foreach(var obj in Objects)
            {
                if (useTName)
                {
                    if (obj.TName == name)
                        return obj;
                }
                else
                {
                    if (obj.Name == name)
                        return obj;
                }
            }
            return null;
        }
    
        public HavokTagObject Clone()
        {
            List<HavokTagObject> objects = new List<HavokTagObject>();

            foreach (var child in Objects)
                objects.Add(child.Clone());

            return new HavokTagObject()
            {
                Name = Name,
                TName = TName,
                IntValue = IntValue,
                StringValue = StringValue,
                FloatValue = FloatValue,
                BoolValue = BoolValue,
                _typeID = _typeID,
                TagType = TagType,
                ParentAttachment = ParentAttachment,
                Objects = objects
            };
        }
    }

    public class HavokPartHeader
    {

        [YAXAttributeForClass]
        public int Offset { get; private set; }
        [YAXAttributeForClass]
        public int Flags { get; private set; }
        [YAXAttributeForClass]
        public int Size { get; private set; }
        [YAXDontSerialize]
        public int PartEndOffset => Offset + Size;
        [YAXAttributeForClass]
        public string Signature { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PartHeader")]
        public List<HavokPartHeader> Parts { get; set; }

        public static HavokPartHeader Parse(byte[] bytes, int offset)
        {
            int u0 = BigEndianConverter.ReadInt32(bytes, offset);
            int size = u0 & 0x3FFFFFFF;
            int flag = (int)(u0 & 0xC0000000);
            string sig = StringEx.GetString(bytes, offset + 4, false, StringEx.EncodingType.UTF8, 4, false);

            if (!HavokTagFile.AllowedSignatures.Contains(sig))
            {
                throw new Exception($"HavokPartHeader: unknown signature \"{sig}\" encountered, parse failed.");
            }

            int child_offset = 8;
            int hdr_size = size;
            List<HavokPartHeader> parts = new List<HavokPartHeader>();

            if (CanHaveChildParts(sig))
            {
                while (child_offset < hdr_size)
                {
                    var part = HavokPartHeader.Parse(bytes, child_offset + offset);

                    parts.Add(part);
                    child_offset += part.Size;
                }
            }

            return new HavokPartHeader() { Parts = parts, Offset = offset, Size = size, Flags = flag, Signature = sig };
        }

        private static bool CanHaveChildParts(string signature)
        {
            return signature == HavokTagFile.Havok_TAG0_SIGNATURE || signature == HavokTagFile.Havok_TYPE_SIGNATURE || signature == HavokTagFile.Havok_INDX_SIGNATURE;
        }

        public override string ToString()
        {
            return $"{Signature}";
        }
    
        internal static byte[] WritePart(List<byte> partBytes, string signature)
        {
            int padding = Utils.CalculatePadding(partBytes.Count, 4);

            List<byte> bytes = new List<byte>(8 + padding + partBytes.Count);

            bool flag = !CanHaveChildParts(signature);
            int packedValue = partBytes.Count + 8 + padding + (flag ? 0x40000000 : 0);

            bytes.AddRange(BigEndianConverter.GetBytes(packedValue));
            bytes.AddRange(StringEx.WriteFixedSizeString(signature, 4));
            bytes.AddRange(partBytes);

            if(padding > 0)
                bytes.AddRange(new byte[padding]);

            return bytes.ToArray();
        }
    }
}