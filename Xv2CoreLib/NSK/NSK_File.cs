using System;
using System.Collections.Generic;
using System.IO;
using Xv2CoreLib.EMD;
using Xv2CoreLib.ESK;
using YAXLib;

namespace Xv2CoreLib.NSK
{
    public class NSK_File
    {
        public ESK_File EskFile { get; set; }
        public EMD_File EmdFile { get; set; }

        #region LoadSave
        public static NSK_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static NSK_File Load(byte[] bytes)
        {
            int eskAddress = LB_Common.Utils.ArraySearch.IndexOf(bytes, "#ESK");
            int emdAddress = LB_Common.Utils.ArraySearch.IndexOf(bytes, "#EMD");

            if (eskAddress == -1) throw new InvalidDataException("NSK_File.Load: Could not locate \"#ESK\".");
            if (emdAddress == -1) throw new InvalidDataException("NSK_File.Load: Could not locate \"#EMD\".");
            if (eskAddress != 0) throw new InvalidDataException("NSK_File.Load: #ESK not at the expected address.");

            NSK_File nskFile = new NSK_File();
            nskFile.EskFile = ESK_File.Load(bytes);
            nskFile.EmdFile = new EMD.Parser(bytes, emdAddress).emdFile;

            return nskFile;
        }

        public void SaveFile(string path)
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(EskFile.SaveToBytes());
            bytes.AddRange(EmdFile.SaveToBytes());

            File.WriteAllBytes(path, bytes.ToArray());
        }

        #endregion

        #region XmlLoadSave
        public static void CreateXml(string path)
        {
            var file = Load(path);

            YAXSerializer serializer = new YAXSerializer(typeof(NSK_File));
            serializer.SerializeToFile(file, path + ".xml");
        }

        public static void ConvertFromXml(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));

            YAXSerializer serializer = new YAXSerializer(typeof(NSK_File), YAXSerializationOptions.DontSerializeNullObjects);
            var file = (NSK_File)serializer.DeserializeFromFile(xmlPath);

            file.SaveFile(saveLocation);
        }
        #endregion
    }
}
