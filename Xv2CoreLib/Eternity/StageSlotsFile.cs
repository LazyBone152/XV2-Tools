using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.Eternity
{
    public class StageSlotsFile
    {
        public const string FILE_NAME_BIN = "XV2P_SLOTS_STAGE.x2s";
        public const string FILE_NAME_XML = "XV2P_SLOTS_STAGE.x2s.xml";
        public const string FILE_NAME_LOCAL_BIN = "XV2P_SLOTS_STAGE_LOCAL.x2s";
        public const string FILE_NAME_LOCAL_XML = "XV2P_SLOTS_STAGE_LOCAL.x2s.xml";

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "StageSlot")]
        public List<StageSlot> StageSlots { get; set; } = new List<StageSlot>();

        #region XmlLoadSave
        public static void CreateXml(string path)
        {
            var file = Load(path);

            YAXSerializer serializer = new YAXSerializer(typeof(StageSlotsFile));
            serializer.SerializeToFile(file, path + ".xml");
        }

        public static void ConvertFromXml(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));

            YAXSerializer serializer = new YAXSerializer(typeof(StageSlotsFile), YAXSerializationOptions.DontSerializeNullObjects);
            var file = (StageSlotsFile)serializer.DeserializeFromFile(xmlPath);

            file.SaveFile(saveLocation);
        }
        #endregion


        #region LoadSave
        public static StageSlotsFile Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static StageSlotsFile Load(byte[] bytes)
        {
            string rawText = Encoding.ASCII.GetString(bytes);

            StageSlotsFile stageFile = new StageSlotsFile();

            rawText = rawText.Replace("[", "");
            rawText = rawText.Replace(" ", "");
            string[] stageSlotsText = rawText.Split(']');

            foreach (var stage in stageSlotsText)
            {
                if (string.IsNullOrWhiteSpace(stage)) continue;

                StageSlot stageSlot = new StageSlot();

                string[] parameters = stage.Split(',');
                if (parameters.Length != 2) throw new InvalidDataException($"Invalid number of StageSlot parameters. Expected 2, found {parameters.Length}.");

                stageSlot.StageIndex = uint.Parse(parameters[0]);
                stageSlot.DLC = int.Parse(parameters[1]);

                stageFile.StageSlots.Add(stageSlot);
            }

            return stageFile;
        }

        public byte[] SaveToBytes()
        {
            StringBuilder strBuilder = new StringBuilder();

            foreach (var costume in StageSlots)
            {
                strBuilder.Append("[");

                strBuilder.Append(costume.StageIndex).Append(",");
                strBuilder.Append(costume.DLC);

                strBuilder.Append("]");
            }

            return Encoding.ASCII.GetBytes(strBuilder.ToString());
        }

        public void SaveFile(string path)
        {
            byte[] bytes = SaveToBytes();
            File.WriteAllBytes(path, bytes);
        }

        #endregion


        #region Install

        #endregion
    }

    public class StageSlot
    {
        [YAXAttributeForClass]
        public uint StageIndex { get; set; }
        [YAXAttributeForClass]
        public int DLC { get; set; }
    }
}
