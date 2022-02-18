using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.IDB
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        IDB_File idbFile = new IDB_File();


        public Parser(string location, bool _writeXml = false)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            Parse();
            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(IDB_File));
                serializer.SerializeToFile(idbFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            if (rawBytes != null)
            {
                Parse();
            }
            else
            {
                idbFile = null;
            }
        }

        public IDB_File GetIdbFile()
        {
            return idbFile;
        }

        private void Parse()
        {
            int count = BitConverter.ToInt32(rawBytes, 8);
            int offset = BitConverter.ToInt32(rawBytes, 12);

            if(count > 0)
            {
                idbFile.Entries = new List<IDB_Entry>();

                for(int i = 0; i < count; i++)
                {
                    idbFile.Entries.Add(new IDB_Entry()
                    {
                        ID = BitConverter.ToUInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        NameMsgID = BitConverter.ToUInt16(rawBytes, offset + 4),
                        DescMsgID = BitConverter.ToUInt16(rawBytes, offset + 6),
                        Type = BitConverter.ToUInt16(rawBytes, offset + 8),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                        I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                        I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),
                        I_16 = BitConverter.ToInt32(rawBytes, offset + 16),
                        I_20 = BitConverter.ToInt32(rawBytes, offset + 20),
                        RaceLock = (RaceLock)BitConverter.ToInt32(rawBytes, offset + 24),
                        I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                        I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                        I_36 = BitConverter.ToUInt16(rawBytes, offset + 36),
                        I_38 = (LB_Color)BitConverter.ToUInt16(rawBytes, offset + 38),
                        I_40 = BitConverter.ToUInt16(rawBytes, offset + 40),
                        I_42 = BitConverter.ToUInt16(rawBytes, offset + 42),
                        I_44 = BitConverter.ToUInt16(rawBytes, offset + 44),
                        I_46 = BitConverter.ToUInt16(rawBytes, offset + 46),
                        Effects = ParseEffect(offset + 48)
                    });

                    offset += 720;
                }
            }
        }

        private List<IBD_Effect> ParseEffect(int offset)
        {
            List<IBD_Effect> effects = new List<IBD_Effect>();

            for(int i = 0; i < 3; i++)
            {
                effects.Add(new IBD_Effect()
                {
                    I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                    I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                    I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                    F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                    F_16 = BitConverter_Ex.ToFloat32Array(rawBytes, offset + 16, 6),
                    I_40 = BitConverter.ToInt32(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                    F_48 = BitConverter_Ex.ToFloat32Array(rawBytes, offset + 48, 6),
                    I_72 = BitConverter_Ex.ToInt32Array(rawBytes, offset + 72, 6),
                    F_96 = BitConverter.ToSingle(rawBytes, offset + 96),
                    F_100 = BitConverter.ToSingle(rawBytes, offset + 100),
                    F_104 = BitConverter.ToSingle(rawBytes, offset + 104),
                    F_108 = BitConverter.ToSingle(rawBytes, offset + 108),
                    F_112 = BitConverter.ToSingle(rawBytes, offset + 112),
                    F_116 = BitConverter.ToSingle(rawBytes, offset + 116),
                    F_120 = BitConverter.ToSingle(rawBytes, offset + 120),
                    F_124 = BitConverter.ToSingle(rawBytes, offset + 124),
                    F_128 = BitConverter.ToSingle(rawBytes, offset + 128),
                    F_132 = BitConverter.ToSingle(rawBytes, offset + 132),
                    F_136 = BitConverter.ToSingle(rawBytes, offset + 136),
                    F_140 = BitConverter.ToSingle(rawBytes, offset + 140),
                    F_144 = BitConverter.ToSingle(rawBytes, offset + 144),
                    F_148 = BitConverter.ToSingle(rawBytes, offset + 148),
                    F_152 = BitConverter.ToSingle(rawBytes, offset + 152),
                    F_156 = BitConverter_Ex.ToFloat32Array(rawBytes, offset + 156, 17)
                });
                offset += 224;
            }

            return effects;
        }

    }
}
