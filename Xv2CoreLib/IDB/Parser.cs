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

        public static int ENTRY_SIZE_OLD = IDB_Entry.OLD_ENTRY_SIZE + (IBD_Effect.OLD_ENTRY_SIZE * 3);
        public static int ENTRY_SIZE_V1 = IDB_Entry.ENTRY_SIZE_V1 + (IBD_Effect.ENTRY_SIZE_V1 * 3);
        public static int ENTRY_SIZE_V2 = IDB_Entry.ENTRY_SIZE_V2 + (IBD_Effect.ENTRY_SIZE_V2 * 3);
        public static int ENTRY_SIZE_V3 = IDB_Entry.ENTRY_SIZE_V2 + (IBD_Effect.ENTRY_SIZE_V3 * 3);


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

            //Determine the version of the IDB file
            if((rawBytes.Length - 16) == ENTRY_SIZE_OLD * count)
            {
                idbFile.Version = 0;
            }
            else if ((rawBytes.Length - 16) == ENTRY_SIZE_V1 * count)
            {
                idbFile.Version = 1;
            }
            else if ((rawBytes.Length - 16) == ENTRY_SIZE_V2 * count)
            {
                idbFile.Version = 2;
            }
            else if ((rawBytes.Length - 16) == ENTRY_SIZE_V3 * count)
            {
                idbFile.Version = 3;
            }
            else
            {
                throw new InvalidDataException("IDB version not supported.");
            }

            if (count > 0)
            {
                idbFile.Entries = new List<IDB_Entry>();

                switch (idbFile.Version)
                {
                    case 0:
                        ReadEntryOld(count, offset);
                        break;
                    case 1:
                    case 2:
                    case 3:
                        ReadEntryNew(count, offset, idbFile.Version);
                        break;
                }
            }
        }


        
        private void ReadEntryOld(int count, int offset)
        {
            for (int i = 0; i < count; i++)
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
                    RaceLock = (IdbRaceLock)BitConverter.ToInt32(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                    I_36 = BitConverter.ToUInt16(rawBytes, offset + 36),
                    I_38 = (LB_Color)BitConverter.ToUInt16(rawBytes, offset + 38),
                    I_40 = BitConverter.ToUInt16(rawBytes, offset + 40),
                    I_42 = BitConverter.ToUInt16(rawBytes, offset + 42),
                    I_44 = BitConverter.ToUInt16(rawBytes, offset + 44),
                    I_46 = BitConverter.ToUInt16(rawBytes, offset + 46),
                    Effects = ReadEffectOld(offset + 48)
                });

                offset += ENTRY_SIZE_OLD;
            }
        }

        private List<IBD_Effect> ReadEffectOld(int offset)
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



        private void ReadEntryNew(int count, int offset, int version)
        {
            for (int i = 0; i < count; i++)
            {
                // I wanted to do a similar version check like what is done with the CST files
                // But I couldn't exactly figure out how I would do that in this case

                if (version != 1 && version != 2 && version != 3)
                    throw new InvalidDataException($"IDB: This IDB version is not supported (Version: {version}).");

                if (version == 1)
                {
                    idbFile.Entries.Add(new IDB_Entry()
                    {
                        ID = BitConverter.ToUInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        NameMsgID = BitConverter.ToUInt16(rawBytes, offset + 4),
                        DescMsgID = BitConverter.ToUInt16(rawBytes, offset + 6),
                        Type = BitConverter.ToUInt16(rawBytes, offset + 8),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                        NEW_I_12 = BitConverter.ToUInt16(rawBytes, offset + 12),
                        NEW_I_14 = BitConverter.ToUInt16(rawBytes, offset + 14),

                        I_12 = BitConverter.ToUInt16(rawBytes, offset + 16),
                        I_14 = BitConverter.ToUInt16(rawBytes, offset + 18),
                        I_16 = BitConverter.ToInt32(rawBytes, offset + 20),
                        I_20 = BitConverter.ToInt32(rawBytes, offset + 24),
                        RaceLock = (IdbRaceLock)BitConverter.ToInt32(rawBytes, offset + 28),
                        I_28 = BitConverter.ToInt32(rawBytes, offset + 32),
                        I_32 = BitConverter.ToInt32(rawBytes, offset + 36),
                        I_36 = BitConverter.ToUInt16(rawBytes, offset + 40),
                        I_38 = (LB_Color)BitConverter.ToUInt16(rawBytes, offset + 42),
                        I_40 = BitConverter.ToUInt16(rawBytes, offset + 44),
                        I_42 = BitConverter.ToUInt16(rawBytes, offset + 46),
                        I_44 = BitConverter.ToUInt16(rawBytes, offset + 48),
                        I_46 = BitConverter.ToUInt16(rawBytes, offset + 50),
                        Effects = ReadEffectNew(offset + 52, version)
                    });

                    offset += ENTRY_SIZE_V1;
                }

                if (version >= 2)
                {
                    idbFile.Entries.Add(new IDB_Entry()
                    {
                        ID = BitConverter.ToUInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToUInt16(rawBytes, offset + 2),
                        NameMsgID = BitConverter.ToUInt16(rawBytes, offset + 4),
                        DescMsgID = BitConverter.ToUInt16(rawBytes, offset + 6),
                        HowMsgID = BitConverter.ToUInt16(rawBytes, offset + 8),
                        NEW_I_10 = BitConverter.ToUInt16(rawBytes, offset + 10),
                        Type = BitConverter.ToUInt16(rawBytes, offset + 12),
                        I_10 = BitConverter.ToUInt16(rawBytes, offset + 14),
                        NEW_I_12 = BitConverter.ToUInt16(rawBytes, offset + 16),
                        NEW_I_14 = BitConverter.ToUInt16(rawBytes, offset + 18),

                        I_12 = BitConverter.ToUInt16(rawBytes, offset + 20),
                        I_14 = BitConverter.ToUInt16(rawBytes, offset + 22),
                        I_16 = BitConverter.ToInt32(rawBytes, offset + 24),
                        I_20 = BitConverter.ToInt32(rawBytes, offset + 28),
                        RaceLock = (IdbRaceLock)BitConverter.ToInt32(rawBytes, offset + 32),
                        I_28 = BitConverter.ToInt32(rawBytes, offset + 36),
                        NEW_I_32 = BitConverter.ToInt32(rawBytes, offset + 40),
                        NEW_I_36 = BitConverter.ToInt32(rawBytes, offset + 44),
                        I_32 = BitConverter.ToInt32(rawBytes, offset + 48),
                        I_36 = BitConverter.ToUInt16(rawBytes, offset + 52),
                        I_38 = (LB_Color)BitConverter.ToUInt16(rawBytes, offset + 54),
                        I_40 = BitConverter.ToUInt16(rawBytes, offset + 56),
                        I_42 = BitConverter.ToUInt16(rawBytes, offset + 58),
                        I_44 = BitConverter.ToUInt16(rawBytes, offset + 60),
                        I_46 = BitConverter.ToUInt16(rawBytes, offset + 62),
                        Effects = ReadEffectNew(offset + 64, version)
                    });

                    offset += (version >= 3) ? ENTRY_SIZE_V3 : ENTRY_SIZE_V2;
                }
            }
        }


        private List<IBD_Effect> ReadEffectNew(int offset, int version)
        {
            List<IBD_Effect> effects = new List<IBD_Effect>();

            if (version != 1 && version != 2 && version != 3)
                throw new InvalidDataException($"IDB: This IDB version is not supported (Version: {version}).");

            if (version == 1)
            {
                for (int i = 0; i < 3; i++)
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
                        NEW_I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                        NEW_I_52 = BitConverter.ToInt32(rawBytes, offset + 52),


                        F_48 = BitConverter_Ex.ToFloat32Array(rawBytes, offset + 56, 6),
                        I_72 = BitConverter_Ex.ToInt32Array(rawBytes, offset + 80, 6),
                        F_96 = BitConverter.ToSingle(rawBytes, offset + 104),
                        F_100 = BitConverter.ToSingle(rawBytes, offset + 108),
                        F_104 = BitConverter.ToSingle(rawBytes, offset + 112),
                        F_108 = BitConverter.ToSingle(rawBytes, offset + 116),
                        F_112 = BitConverter.ToSingle(rawBytes, offset + 120),
                        F_116 = BitConverter.ToSingle(rawBytes, offset + 124),
                        F_120 = BitConverter.ToSingle(rawBytes, offset + 128),
                        F_124 = BitConverter.ToSingle(rawBytes, offset + 132),
                        F_128 = BitConverter.ToSingle(rawBytes, offset + 136),
                        F_132 = BitConverter.ToSingle(rawBytes, offset + 140),
                        F_136 = BitConverter.ToSingle(rawBytes, offset + 144),
                        F_140 = BitConverter.ToSingle(rawBytes, offset + 148),
                        F_144 = BitConverter.ToSingle(rawBytes, offset + 152),
                        F_148 = BitConverter.ToSingle(rawBytes, offset + 156),
                        F_152 = BitConverter.ToSingle(rawBytes, offset + 160),
                        F_156 = BitConverter_Ex.ToFloat32Array(rawBytes, offset + 164, 17)
                    });
                    offset += 232;
                }
            }

            if (version >= 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    int statOffset = (version >= 3) ? offset + 112 : offset + 108;
                    int f156Offset = (version >= 3) ? offset + 172 : offset + 168;
                    int[] i72 = (version >= 3)
                        ? new int[]
                        {
                            BitConverter.ToInt32(rawBytes, offset + 84),
                            BitConverter.ToInt32(rawBytes, offset + 88),
                            BitConverter.ToInt32(rawBytes, offset + 92),
                            BitConverter.ToInt32(rawBytes, offset + 96),
                            BitConverter.ToInt32(rawBytes, offset + 100),
                            BitConverter.ToInt32(rawBytes, offset + 108)
                        }
                        : BitConverter_Ex.ToInt32Array(rawBytes, offset + 84, 6);

                    effects.Add(new IBD_Effect()
                    {
                        I_00 = BitConverter.ToInt32(rawBytes, offset + 0),
                        I_04 = BitConverter.ToInt32(rawBytes, offset + 4),
                        I_08 = BitConverter.ToInt32(rawBytes, offset + 8),
                        NEW_I_12 = BitConverter.ToInt32(rawBytes, offset + 12),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 16),
                        F_16 = BitConverter_Ex.ToFloat32Array(rawBytes, offset + 20, 6),
                        I_40 = BitConverter.ToInt32(rawBytes, offset + 44),
                        I_44 = BitConverter.ToInt32(rawBytes, offset + 48),
                        NEW_I_48 = BitConverter.ToInt32(rawBytes, offset + 52),
                        NEW_I_52 = BitConverter.ToInt32(rawBytes, offset + 56),


                        F_48 = BitConverter_Ex.ToFloat32Array(rawBytes, offset + 60, 6),
                        I_72 = i72,
                        NEW_I_104 = (version >= 3) ? BitConverter.ToInt32(rawBytes, offset + 104) : -1,
                        F_96 = BitConverter.ToSingle(rawBytes, statOffset + 0),
                        F_100 = BitConverter.ToSingle(rawBytes, statOffset + 4),
                        F_104 = BitConverter.ToSingle(rawBytes, statOffset + 8),
                        F_108 = BitConverter.ToSingle(rawBytes, statOffset + 12),
                        F_112 = BitConverter.ToSingle(rawBytes, statOffset + 16),
                        F_116 = BitConverter.ToSingle(rawBytes, statOffset + 20),
                        F_120 = BitConverter.ToSingle(rawBytes, statOffset + 24),
                        F_124 = BitConverter.ToSingle(rawBytes, statOffset + 28),
                        F_128 = BitConverter.ToSingle(rawBytes, statOffset + 32),
                        F_132 = BitConverter.ToSingle(rawBytes, statOffset + 36),
                        F_136 = BitConverter.ToSingle(rawBytes, statOffset + 40),
                        F_140 = BitConverter.ToSingle(rawBytes, statOffset + 44),
                        F_144 = BitConverter.ToSingle(rawBytes, statOffset + 48),
                        F_148 = BitConverter.ToSingle(rawBytes, statOffset + 52),
                        F_152 = BitConverter.ToSingle(rawBytes, statOffset + 56),
                        F_156 = BitConverter_Ex.ToFloat32Array(rawBytes, f156Offset, 17)
                    });
                    offset += (version >= 3) ? IBD_Effect.ENTRY_SIZE_V3 : IBD_Effect.ENTRY_SIZE_V2;
                }
            }

            return effects;
        }
    }
}
