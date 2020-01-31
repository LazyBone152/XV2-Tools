using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using System.IO;

namespace Xv2CoreLib.QSL
{
    public class Deserializer
    {
        string saveLocation;
        QSL_File qsl_File;
        List<byte> bytes = new List<byte>() { 35,81,83,76,254,255,24,0,  0,0,0,0, 0, 0, 0, 0, 0, 0, 0, 0,24, 0, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(QSL_File), YAXSerializationOptions.DontSerializeNullObjects);
            qsl_File = (QSL_File)serializer.DeserializeFromFile(location);
            Validation();
            WriteBinaryFile();
        }

        /// <summary>
        /// Create a QSL file from a QSL_File object.
        /// </summary>
        public Deserializer(QSL_File _qslFile, string _saveLocation)
        {
            this.saveLocation = _saveLocation;
            qsl_File = _qslFile;
            WriteBinaryFile();
        }

        private void Validation()
        {
            if(qsl_File.Stages != null)
            {
                foreach(var e in qsl_File.Stages)
                {
                    Assertion.AssertArraySize(e.I_04, 3, "Stage", "I_04");
                    if(e.Entries != null)
                    {
                        foreach(var qslEntry in e.Entries)
                        {
                            Assertion.AssertStringSize(qslEntry.MapString, 32, "QSL_Entry", "Position");
                            Assertion.AssertArraySize(qslEntry.I_38, 13, "QSL_Entry", "I_38");
                        }
                    }
                }
            }
        }

        void WriteBinaryFile() {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qsl_File.I_10), 10);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qsl_File.Stages.Count()), 12);

            List<int> pointerListOffsets = new List<int>();
            List<int> offsetToEntries = new List<int>();
            List<int> offsetToStageInfo = new List<int>();

            for (int i = 0; i < qsl_File.Stages.Count(); i++) {
                pointerListOffsets.Add(bytes.Count());
                bytes.AddRange(new byte[4]);
            }

            for (int i = 0; i < qsl_File.Stages.Count(); i++) {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), pointerListOffsets[i]);
                offsetToStageInfo.Add(bytes.Count());
                bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].StageID));

                for (int a = 0; a < 3; a++) {
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].I_04[a]));
                }

                bytes.AddRange(BitConverter.GetBytes((short)qsl_File.Stages[i].Entries.Count()));
                offsetToEntries.Add(bytes.Count());
                bytes.AddRange(new byte[4]);
            }

            for (int i = 0; i < qsl_File.Stages.Count(); i++) {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count() - offsetToStageInfo[i]), offsetToStageInfo[i] + 12);

                for (int a = 0; a < qsl_File.Stages[i].Entries.Count(); a++) {
                    bytes.AddRange(Encoding.ASCII.GetBytes(qsl_File.Stages[i].Entries[a].MapString));

                    int remainingSpace = 32 - qsl_File.Stages[i].Entries[a].MapString.Count();

                    for (int e = 0; e < remainingSpace; e++) {
                        bytes.Add(0);
                    }

                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].Entries[a].I_32));
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].Entries[a].I_34));

                    for (int e = 0; e < 13; e++) {
                        bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].Entries[a].I_38[e]));
                    }
                }

            }

            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }
    }
}
