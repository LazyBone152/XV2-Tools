using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.VLC
{
    [YAXSerializeAs("VLC")]

    public class VLC_File
    {
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Camera")]
        public List<VLC_Entry> ZoomInCamera { get; set; } = new List<VLC_Entry>();

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Camera")]
        public List<VLC_Entry> UnkCamera { get; set; } = new List<VLC_Entry>();

        #region LoadSave
        public static VLC_File Parse(string path, bool writeXml)
        {
            VLC_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(VLC_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static VLC_File Parse(byte[] bytes)
        {
            VLC_File vlcFile = new VLC_File();
            int numEntries = BitConverter.ToInt32(bytes, 0);
            int offsetLeftCam = 16;
            int offsetRightCam = offsetLeftCam + (numEntries * 16);
            int entrySize = (int)((bytes.Length) / numEntries);

            if (bytes.Length != 16 + (entrySize * numEntries))
                throw new InvalidDataException($"Error on reading vlc file: Invalid file size!");

            for(int i = 0; i < numEntries; i++)
            {
                vlcFile.ZoomInCamera.Add(new VLC_Entry()
                {
                    CharaId = (int)(BitConverter.ToSingle(bytes, offsetLeftCam + 12)),
                    OffsetXYZ = new float[]
                    {
                        BitConverter.ToSingle(bytes, offsetLeftCam + 0),
                        BitConverter.ToSingle(bytes, offsetLeftCam + 4),
                        BitConverter.ToSingle(bytes, offsetLeftCam + 8)
                    }
                });

                vlcFile.UnkCamera.Add(new VLC_Entry()
                {
                    CharaId = (int)(BitConverter.ToSingle(bytes, offsetRightCam + 12)),
                    OffsetXYZ = new float[]
                    {
                        BitConverter.ToSingle(bytes, offsetRightCam + 0),
                        BitConverter.ToSingle(bytes, offsetRightCam + 4),
                        BitConverter.ToSingle(bytes, offsetRightCam + 8)
                    }
                });
                offsetLeftCam += 16;
                offsetRightCam += 16;
            }

            return vlcFile;
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .vlc file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(VLC_File), YAXSerializationOptions.DontSerializeNullObjects);
            var vlcFile = (VLC_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, vlcFile.Write());
        }

        /// <summary>
        /// Save the VLC_File to the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public byte[] Write()
        {
            if (ZoomInCamera == null) ZoomInCamera = new List<VLC_Entry>();
            if (UnkCamera == null) UnkCamera = new List<VLC_Entry>();

            List<byte> bytes = new List<byte>();

            //Header
            bytes.AddRange(BitConverter.GetBytes(ZoomInCamera.Count));
            bytes.AddRange(BitConverter.GetBytes(0));
            bytes.AddRange(BitConverter.GetBytes(0));
            bytes.AddRange(BitConverter.GetBytes(0));

            if (ZoomInCamera.Count != UnkCamera.Count)
            {
                throw new InvalidDataException($"Error on building vlc: CameraLeft and CameraLeft count mismatch!\nCameraLeft Count = " + ZoomInCamera.Count + "\nCameraRight Count = " + UnkCamera.Count);
            }

            //Entries
            for (int i = 0; i < ZoomInCamera.Count; i++) // Using a for loop like this instead of foreach for the validation, cause we need the index
            {
                bytes.AddRange(BitConverter.GetBytes(ZoomInCamera[i].OffsetXYZ[0]));
                bytes.AddRange(BitConverter.GetBytes(ZoomInCamera[i].OffsetXYZ[1]));
                bytes.AddRange(BitConverter.GetBytes(ZoomInCamera[i].OffsetXYZ[2]));
                bytes.AddRange(BitConverter.GetBytes((float)ZoomInCamera[i].CharaId));

                if (ZoomInCamera[i].CharaId != UnkCamera[i].CharaId)
                    throw new Exception("Chara ID mismatch at CameraLeft ID = " + ZoomInCamera[i].CharaId + "!\nCameraRight ID = " + UnkCamera[i].CharaId);
            }

            for (int i = 0; i < UnkCamera.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(UnkCamera[i].OffsetXYZ[0]));
                bytes.AddRange(BitConverter.GetBytes(UnkCamera[i].OffsetXYZ[1]));
                bytes.AddRange(BitConverter.GetBytes(UnkCamera[i].OffsetXYZ[2]));
                bytes.AddRange(BitConverter.GetBytes((float)UnkCamera[i].CharaId));

                if (ZoomInCamera[i].CharaId != UnkCamera[i].CharaId)
                    throw new Exception("Chara ID mismatch at CameraLeft ID = " + ZoomInCamera[i].CharaId + "!\nCameraRight ID = " + UnkCamera[i].CharaId);
            }

            //validation
            int validationAmount = 16 + (32 * ZoomInCamera.Count);
            if (bytes.Count != 16 + (32 * ZoomInCamera.Count))
                throw new InvalidDataException($"Error on building vlc: Invalid file size!");

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }
        #endregion
    }

    [YAXSerializeAs("Camera")]
    public class VLC_Entry : IInstallable
    {
        #region NonSerialized

        //interface
        [YAXDontSerialize]
        public int SortID { get { return CharaId; } }
        [YAXDontSerialize]
        public string Index
        {
            get
            {
                return $"{CharaId}";
            }
            set
            {
                string[] split = value.Split('_');

                if (split.Length == 2)
                {
                    CharaId = int.Parse(split[0]);
                }
            }
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("CharaId")]
        public int CharaId { get; set; } // Is actually a float

        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        [YAXSerializeAs("OffsetXYZ")]
        public float[] OffsetXYZ { get; set; }
    }
}
