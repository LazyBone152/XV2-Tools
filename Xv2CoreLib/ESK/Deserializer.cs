using System;
using System.Collections.Generic;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.ESK
{
    public class Deserializer
    {
        string saveLocation;
        ESK_File eskFile;
        public List<byte> bytes = new List<byte>() { 35, 69, 83, 75, 254, 255, 28, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(ESK_File), YAXSerializationOptions.DontSerializeNullObjects);
            eskFile = (ESK_File)serializer.DeserializeFromFile(location);
            
            WriteFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(ESK_File _eskFile)
        {
            eskFile = _eskFile;
            WriteFile();
        }

        public Deserializer(ESK_File _eskFile, string path)
        {
            eskFile = _eskFile;
            WriteFile();
            File.WriteAllBytes(path, bytes.ToArray());
        }

        private void WriteFile()
        {
            //Header
            bytes.AddRange(BitConverter.GetBytes(eskFile.Version)); //8
            bytes.AddRange(BitConverter.GetBytes(eskFile.I_10)); //10
            bytes.AddRange(BitConverter.GetBytes(eskFile.I_12)); //12
            bytes.AddRange(BitConverter.GetBytes((int)32)); //Offset to skeleton, 16
            bytes.AddRange(BitConverter.GetBytes((int)0)); //20 (Next part in NSKs)
            bytes.AddRange(BitConverter.GetBytes(eskFile.I_24)); //24
            bytes.AddRange(new byte[4]);

            //Skeleton
            bytes.AddRange(eskFile.Skeleton.Write());
        }



    }
}
