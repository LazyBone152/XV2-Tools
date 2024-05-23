using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.IDB
{
    public class Deserializer
    {
        private bool isSkillIdb = false;
        private string saveLocation;
        private IDB_File idbFile;
        public List<byte> bytes = new List<byte>() { 35, 73, 68, 66, 254, 255, 7, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(IDB_File), YAXExceptionHandlingPolicies.DoNotThrow, YAXExceptionTypes.Error, YAXSerializationOptions.DontSerializeNullObjects);
            idbFile = (IDB_File)serializer.DeserializeFromFile(location);
            Validation();
            Write();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(IDB_File _idbFile, string _saveLocation)
        {
            saveLocation = _saveLocation;
            idbFile = _idbFile;
            Validation();
            Write();
            File.WriteAllBytes(this.saveLocation, bytes.ToArray());
        }

        public Deserializer(IDB_File _idbFile, bool isSkillIdb)
        {
            this.isSkillIdb = isSkillIdb;
            idbFile = _idbFile;
            Validation();
            Write();
        }

        private void Validation()
        {
            //Sorting logic:
            //skill_item = separate by type, sort by SortID, rejoin
            //Everything else = ignore type, just sort by SortID

            if (Path.GetFileNameWithoutExtension(saveLocation) == "skill_item" || isSkillIdb)
            {
                idbFile.SortEntries();
            }
            else
            {
                idbFile.Entries.Sort((x, y) => x.SortID - y.SortID);
            }
        }

        private void Write()
        {
            int count = (idbFile.Entries != null) ? idbFile.Entries.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(16));

            switch (idbFile.Version)
            {
                case 0:
                    WriteEntriesOld(count);
                    break;
                case 1:
                case 2:
                    WriteEntriesNew(count, idbFile.Version);
                    break;
            }
        }

        private void WriteEntriesOld(int count)
        {
            for (int i = 0; i < count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].ID));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_02));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].NameMsgID));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].DescMsgID));
                bytes.AddRange(BitConverter.GetBytes((ushort)idbFile.Entries[i].Type));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_10));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_12));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_14));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_16));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_20));
                bytes.AddRange(BitConverter.GetBytes((int)idbFile.Entries[i].RaceLock));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_28));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_32));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_36));
                bytes.AddRange(BitConverter.GetBytes((ushort)idbFile.Entries[i].I_38));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_40));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_42));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_44));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_46));

                if (idbFile.Entries[i].Effects.Count() != 3)
                    throw new InvalidDataException(String.Format("Effect entry count mismatch. There must be 3. (ID: {0})", idbFile.Entries[i].Index));

                WriteEffectOld(idbFile.Entries[i].Effects[0]);
                WriteEffectOld(idbFile.Entries[i].Effects[1]);
                WriteEffectOld(idbFile.Entries[i].Effects[2]);
            }
        }

        private void WriteEffectOld(IBD_Effect effect)
        {
            bytes.AddRange(BitConverter.GetBytes(effect.I_00));
            bytes.AddRange(BitConverter.GetBytes(effect.I_04));
            bytes.AddRange(BitConverter.GetBytes(effect.I_08));
            bytes.AddRange(BitConverter.GetBytes(effect.F_12));
            Assertion.AssertArraySize(effect.F_16, 6, "Effect", "Ability_Values");
            bytes.AddRange(BitConverter_Ex.GetBytes(effect.F_16));
            bytes.AddRange(BitConverter.GetBytes(effect.I_40));
            bytes.AddRange(BitConverter.GetBytes(effect.I_44));
            Assertion.AssertArraySize(effect.F_48, 6, "Effect", "Multipliers");
            bytes.AddRange(BitConverter_Ex.GetBytes(effect.F_48));
            Assertion.AssertArraySize(effect.I_72, 6, "Effect", "I_72");
            bytes.AddRange(BitConverter_Ex.GetBytes(effect.I_72));
            bytes.AddRange(BitConverter.GetBytes(effect.F_96));
            bytes.AddRange(BitConverter.GetBytes(effect.F_100));
            bytes.AddRange(BitConverter.GetBytes(effect.F_104));
            bytes.AddRange(BitConverter.GetBytes(effect.F_108));
            bytes.AddRange(BitConverter.GetBytes(effect.F_112));
            bytes.AddRange(BitConverter.GetBytes(effect.F_116));
            bytes.AddRange(BitConverter.GetBytes(effect.F_120));
            bytes.AddRange(BitConverter.GetBytes(effect.F_124));
            bytes.AddRange(BitConverter.GetBytes(effect.F_128));
            bytes.AddRange(BitConverter.GetBytes(effect.F_132));
            bytes.AddRange(BitConverter.GetBytes(effect.F_136));
            bytes.AddRange(BitConverter.GetBytes(effect.F_140));
            bytes.AddRange(BitConverter.GetBytes(effect.F_144));
            bytes.AddRange(BitConverter.GetBytes(effect.F_148));
            bytes.AddRange(BitConverter.GetBytes(effect.F_152));
            Assertion.AssertArraySize(effect.F_156, 17, "Effect", "F_156");
            bytes.AddRange(BitConverter_Ex.GetBytes(effect.F_156));
        }

        private void WriteEntriesNew(int count, int version)
        {
            if (version != 1 && version != 2)
                throw new InvalidDataException($"IDB: This IDB version is not supported (Version: {version}).");

                for (int i = 0; i < count; i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].ID));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].NameMsgID));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].DescMsgID));
                    if (version >= 2)
                    {
                        bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].NEW_I_08));
                        bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].NEW_I_10));
                    }
                    bytes.AddRange(BitConverter.GetBytes((ushort)idbFile.Entries[i].Type));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_10));

                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].NEW_I_12));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].NEW_I_14));

                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_12));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_14));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_16));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_20));
                    bytes.AddRange(BitConverter.GetBytes((int)idbFile.Entries[i].RaceLock));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_28));
                    if (version >= 2)
                    {
                        bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].NEW_I_32));
                        bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].NEW_I_36));
                    }
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_32));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_36));
                    bytes.AddRange(BitConverter.GetBytes((ushort)idbFile.Entries[i].I_38));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_40));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_42));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_44));
                    bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_46));

                    if (idbFile.Entries[i].Effects.Count() != 3)
                        throw new InvalidDataException(String.Format("Effect entry count mismatch. There must be 3. (ID: {0})", idbFile.Entries[i].Index));

                    WriteEffectNew(idbFile.Entries[i].Effects[0], version);
                    WriteEffectNew(idbFile.Entries[i].Effects[1], version);
                    WriteEffectNew(idbFile.Entries[i].Effects[2], version);
                }

        }

        private void WriteEffectNew(IBD_Effect effect, int version)
        {
            if (version != 1 && version != 2)
                throw new InvalidDataException($"IDB: This IDB version is not supported (Version: {version}).");

            bytes.AddRange(BitConverter.GetBytes(effect.I_00));
            bytes.AddRange(BitConverter.GetBytes(effect.I_04));
            bytes.AddRange(BitConverter.GetBytes(effect.I_08));

            if (version == 2)
                bytes.AddRange(BitConverter.GetBytes(effect.NEW_I_12));

            bytes.AddRange(BitConverter.GetBytes(effect.F_12));
            Assertion.AssertArraySize(effect.F_16, 6, "Effect", "Ability_Values");
            bytes.AddRange(BitConverter_Ex.GetBytes(effect.F_16));
            bytes.AddRange(BitConverter.GetBytes(effect.I_40));
            bytes.AddRange(BitConverter.GetBytes(effect.I_44));

            bytes.AddRange(BitConverter.GetBytes(effect.NEW_I_48));
            bytes.AddRange(BitConverter.GetBytes(effect.NEW_I_52));

            Assertion.AssertArraySize(effect.F_48, 6, "Effect", "Multipliers");
            bytes.AddRange(BitConverter_Ex.GetBytes(effect.F_48));
            Assertion.AssertArraySize(effect.I_72, 6, "Effect", "I_72");
            bytes.AddRange(BitConverter_Ex.GetBytes(effect.I_72));
            bytes.AddRange(BitConverter.GetBytes(effect.F_96));
            bytes.AddRange(BitConverter.GetBytes(effect.F_100));
            bytes.AddRange(BitConverter.GetBytes(effect.F_104));
            bytes.AddRange(BitConverter.GetBytes(effect.F_108));
            bytes.AddRange(BitConverter.GetBytes(effect.F_112));
            bytes.AddRange(BitConverter.GetBytes(effect.F_116));
            bytes.AddRange(BitConverter.GetBytes(effect.F_120));
            bytes.AddRange(BitConverter.GetBytes(effect.F_124));
            bytes.AddRange(BitConverter.GetBytes(effect.F_128));
            bytes.AddRange(BitConverter.GetBytes(effect.F_132));
            bytes.AddRange(BitConverter.GetBytes(effect.F_136));
            bytes.AddRange(BitConverter.GetBytes(effect.F_140));
            bytes.AddRange(BitConverter.GetBytes(effect.F_144));
            bytes.AddRange(BitConverter.GetBytes(effect.F_148));
            bytes.AddRange(BitConverter.GetBytes(effect.F_152));
            Assertion.AssertArraySize(effect.F_156, 17, "Effect", "F_156");
            bytes.AddRange(BitConverter_Ex.GetBytes(effect.F_156));
        }

    }
}
