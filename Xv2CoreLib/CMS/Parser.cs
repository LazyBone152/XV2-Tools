using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.CMS
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        CMS_File cmsFile = new CMS_File();
        
        public Parser(string location, bool _writeXml = false)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            Parse();
            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(CMS_File));
                serializer.SerializeToFile(cmsFile, saveLocation + ".xml");
            }
        }

        /// <summary>
        /// Parse only. No XML saving.
        /// </summary>
        /// <param name="_bytes"></param>
        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            if (rawBytes != null)
            {
                Parse();
            }
            else
            {
                cmsFile = null;
            }
        }


        public CMS_File GetCmsFile() 
        {
            return cmsFile;
        }

        private void Parse() 
        {
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);
            cmsFile.CMS_Entries = new List<CMS_Entry>();


            for (int i = 0; i < count; i++)
            {

                cmsFile.CMS_Entries.Add(new CMS_Entry()
                {
                    Index = BitConverter.ToInt32(rawBytes, offset + 0).ToString(),
                    Str_04 = StringEx.GetString(rawBytes, offset + 4, false, StringEx.EncodingType.ASCII, 4),
                    I_08 = BitConverter.ToInt64(rawBytes, offset + 8),
                    I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                    I_20 = BitConverter.ToUInt16(rawBytes, offset + 20),
                    I_22 = BitConverter.ToUInt16(rawBytes, offset + 22),
                    I_24 = BitConverter.ToUInt16(rawBytes, offset + 24),
                    I_26 = BitConverter.ToUInt16(rawBytes, offset + 26),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                    Str_32 = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 32), false),
                    Str_36 = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 36), false),
                    Str_44 = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 44), false),
                    Str_48 = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 48), false),
                    Str_56 = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 56), false),
                    Str_60 = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 60), false),
                    Str_64 = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 64), false),
                    Str_68 = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 68), false),
                    Str_80 = StringEx.GetString(rawBytes, BitConverter.ToInt32(rawBytes, offset + 80), false)

                });

                offset += 84;
            }
        }
    }
}
