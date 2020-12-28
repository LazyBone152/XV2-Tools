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
        string saveLocation;
        IDB_File idbFile;
        public List<byte> bytes = new List<byte>() { 35, 73, 68, 66, 254, 255, 7, 0 };

        //Offset lists
        int EntryCount { get; set; }
        List<int> MainEntryOffsets = new List<int>();

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(IDB_File), YAXSerializationOptions.DontSerializeNullObjects);
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

        public Deserializer(IDB_File _idbFile)
        {
            idbFile = _idbFile;
            Validation();
            Write();
        }

        private void Validation()
        {
            idbFile.SortEntries();
        }

        private void Write()
        {
            int count = (idbFile.Entries != null) ? idbFile.Entries.Count() : 0;
            bytes.AddRange(BitConverter.GetBytes(count));
            bytes.AddRange(BitConverter.GetBytes(16));
            
            for (int i = 0; i < count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(ushort.Parse(idbFile.Entries[i].I_00)));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_02));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_06));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_08));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_10));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_12));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_14));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_16));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_20));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_24));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_28));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(idbFile.Entries[i].I_32)));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_36));
                bytes.AddRange(BitConverter.GetBytes((UInt16)idbFile.Entries[i].I_38));
                bytes.AddRange(BitConverter.GetBytes(idbFile.Entries[i].I_40));
                bytes.AddRange(BitConverter.GetBytes(ushort.Parse(idbFile.Entries[i].I_42)));
                bytes.AddRange(BitConverter.GetBytes(ushort.Parse(idbFile.Entries[i].I_44)));
                bytes.AddRange(BitConverter.GetBytes(ushort.Parse(idbFile.Entries[i].I_46)));
                if (idbFile.Entries[i].Effects.Count() != 3)
                {
                    Console.WriteLine(String.Format("Effect entry count mismatch. There must be 3. (ID: {0})", idbFile.Entries[i].Index));
                    Utils.WaitForInputThenQuit();
                }
                WriteEffect(idbFile.Entries[i].Effects[0]);
                WriteEffect(idbFile.Entries[i].Effects[1]);
                WriteEffect(idbFile.Entries[i].Effects[2]);
            }
        }

        private void WriteEffect(IBD_Effect effect)
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
    }
}
