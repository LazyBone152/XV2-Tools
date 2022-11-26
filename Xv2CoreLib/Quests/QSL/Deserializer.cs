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
        private string saveLocation;
        private QSL_File qsl_File;
        public List<byte> bytes { get; private set; } = new List<byte>() { 35, 81, 83, 76, 254, 255, 24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 24, 0, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(QSL_File), YAXSerializationOptions.DontSerializeNullObjects);
            qsl_File = (QSL_File)serializer.DeserializeFromFile(location);
            WriteBinaryFile();

            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(QSL_File qslFile)
        {
            qsl_File = qslFile;
            WriteBinaryFile();
        }

        private void WriteBinaryFile()
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qsl_File.I_10), 10);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qsl_File.Stages.Count), 12);

            List<int> pointerListOffsets = new List<int>();
            List<int> offsetToEntries = new List<int>();
            List<int> offsetToStageInfo = new List<int>();

            for (int i = 0; i < qsl_File.Stages.Count; i++)
            {
                pointerListOffsets.Add(bytes.Count);
                bytes.AddRange(new byte[4]);
            }

            for (int i = 0; i < qsl_File.Stages.Count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), pointerListOffsets[i]);
                offsetToStageInfo.Add(bytes.Count);
                bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].StageID));
                bytes.AddRange(BitConverter_Ex.GetBytes(qsl_File.Stages[i].I_04, 3));

                bytes.AddRange(BitConverter.GetBytes((short)qsl_File.Stages[i].SubEntries.Count));
                offsetToEntries.Add(bytes.Count);
                bytes.AddRange(new byte[4]);
            }

            for (int i = 0; i < qsl_File.Stages.Count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - offsetToStageInfo[i]), offsetToStageInfo[i] + 12);

                for (int a = 0; a < qsl_File.Stages[i].SubEntries.Count; a++)
                {
                    int size = bytes.Count;

                    bytes.AddRange(StringEx.WriteFixedSizeString(qsl_File.Stages[i].SubEntries[a].Position, 32));
                    bytes.AddRange(BitConverter.GetBytes((ushort)qsl_File.Stages[i].SubEntries[a].Type)); //32
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].SubEntries[a].ID)); //34
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].SubEntries[a].ChanceDialogue)); //36
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].SubEntries[a].I_38)); //38
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].SubEntries[a].QML_Change)); //40
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].SubEntries[a].DefaultPose)); //42
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].SubEntries[a].TalkingPose)); //44
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].SubEntries[a].EffectPose)); //46
                    bytes.AddRange(BitConverter.GetBytes(qsl_File.Stages[i].SubEntries[a].I_48));
                    bytes.AddRange(BitConverter_Ex.GetBytes(qsl_File.Stages[i].SubEntries[a].I_50, 7));

                    if (bytes.Count - size != 64)
                        throw new InvalidDataException("QSL.Save: QSL entry is an invalid size!");
                }
            }
        }
    }
}
