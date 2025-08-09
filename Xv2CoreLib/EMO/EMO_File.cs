using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Xv2CoreLib.EMA;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.EMO
{
    [YAXSerializeAs("EMO")]
    [Serializable]
    public class EMO_File : IModelFile
    {
        #region Notify
        [field: NonSerialized]
        public event ModelModifiedEventHandler ModelModified;

        public void TriggerModelModifiedEvent(EditTypeEnum editType, object context, object parent)
        {
            ModelModified?.Invoke(this, new ModelModifiedEventArgs(editType, context, parent));
        }
        #endregion
        
        public const int EMO_SIGNATURE = 0x4F4D4523;

        [YAXAttributeFor("MaterialsCount")]
        [YAXSerializeAs("value")]
        public ushort MaterialsCount { get; set; }
        [YAXAttributeFor("Version")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public int Version { get; set; } = 0x92c0; //DBXV2 default
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        [YAXHexValue]
        public ulong I_24 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Part")]
        public AsyncObservableCollection<EMO_Part> Parts { get; set; } = new AsyncObservableCollection<EMO_Part>();
        public Skeleton Skeleton { get; set; }

        #region XmlLoadSave
        public static void CreateXml(string path)
        {
            var file = Load(path);

            YAXSerializer serializer = new YAXSerializer(typeof(EMO_File));
            serializer.SerializeToFile(file, path + ".xml");
        }

        public static void ConvertFromXml(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));

            YAXSerializer serializer = new YAXSerializer(typeof(EMO_File), YAXSerializationOptions.DontSerializeNullObjects);
            var file = (EMO_File)serializer.DeserializeFromFile(xmlPath);

            file.SaveFile(saveLocation);
        }
        #endregion

        #region LoadSave
        public static EMO_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static EMO_File Load(byte[] bytes)
        {
            if (BitConverter.ToInt32(bytes, 0) != EMO_SIGNATURE)
                throw new InvalidDataException("EMO_File.Read: \"#EMO\" signature not found!");

            EMO_File emoFile = new EMO_File();

            //Header:
            emoFile.Version = BitConverter.ToInt32(bytes, 8);
            emoFile.I_24 = BitConverter.ToUInt64(bytes, 24);

            int partsHeaderOffset = BitConverter.ToInt32(bytes, 12);
            int skeletonOffset = BitConverter.ToInt32(bytes, 16);
            //int verticesOffset = BitConverter.ToInt32(bytes, 20); //Not needed for loading. Vertices will be loaded with the EMG.

            //Parts:
            emoFile.MaterialsCount = BitConverter.ToUInt16(bytes, partsHeaderOffset + 2);
            ushort partCount = BitConverter.ToUInt16(bytes, partsHeaderOffset + 0);
            int namesOffset = BitConverter.ToInt32(bytes, partsHeaderOffset + 4) + partsHeaderOffset;
            int partsOffset = partsHeaderOffset + 8;

            for (int i = 0; i < partCount; i++)
            {
                int partOffset = BitConverter.ToInt32(bytes, partsOffset + (4 * i)) + partsHeaderOffset;
                int nameOffset = BitConverter.ToInt32(bytes, namesOffset + (4 * i)) + partsHeaderOffset;

                emoFile.Parts.Add(EMO_Part.Read(bytes, partOffset, nameOffset));
            }

            //Skeleton:
            emoFile.Skeleton = Skeleton.Parse(bytes, skeletonOffset);
            emoFile.ResolveBoneLinks();

            return emoFile;
        }

        public void SaveFile(string path)
        {
            byte[] bytes = SaveToBytes();
            File.WriteAllBytes(path, bytes);
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();

            int partsCount = Parts != null ? Parts.Count : 0;

            ResolvePartIndices();

            //Header:
            bytes.AddRange(BitConverter.GetBytes(EMO_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)32));
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(32)); //Parts offset (12). WIll always be 32 as it comes directly after the header.
            bytes.AddRange(BitConverter.GetBytes(0)); //Skeleton offset (16)
            bytes.AddRange(BitConverter.GetBytes(0)); //Vertices offset (20)
            bytes.AddRange(BitConverter.GetBytes(I_24));

            //PartsHeader:
            int partsHeaderStart = bytes.Count;
            bytes.AddRange(BitConverter.GetBytes((ushort)partsCount));
            bytes.AddRange(BitConverter.GetBytes(MaterialsCount));
            bytes.AddRange(BitConverter.GetBytes(0)); //name pointers offset (36)

            int pointerList = bytes.Count;
            bytes.AddRange(new byte[partsCount * 4]);

            //Add padding to keep alignment
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

            for (int i = 0; i < partsCount; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - partsHeaderStart), pointerList + (4 * i));
                bytes.AddRange(Parts[i].Write(bytes.Count));

                //Pad to 16 byte alignment
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);
            }

            //Write part names
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - partsHeaderStart), partsHeaderStart + 4);
            pointerList = bytes.Count;
            bytes.AddRange(new byte[partsCount * 4]);

            //Add padding to keep alignment
            //bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

            for (int i = 0; i < partsCount; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - partsHeaderStart), pointerList + (4 * i));
                bytes.AddRange(Encoding.UTF8.GetBytes(Parts[i].LinkedBone != null ? Parts[i].LinkedBone.Name : Parts[i].Name));
                bytes.Add(0);
            }


            //Skeleton:
            if (Skeleton != null)
            {
                bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 64)]);

                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 16);

                bytes.AddRange(Skeleton.Write());
            }

            //Vertices:
            if (partsCount > 0)
            {

                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 20);

                foreach (var part in Parts)
                {
                    foreach (var emg in part.EmgFiles)
                    {
                        foreach (var mesh in emg.EmgMeshes)
                        {
                            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count - mesh.StartOffset), mesh.VertexOffset);
                            bytes.AddRange(EMD_Vertex.GetBytes(mesh.Vertices, mesh.VertexFlags));

                            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);
                        }
                    }
                }
            }

            return bytes.ToArray();
        }

        public byte[] SaveToBytes()
        {
            return Write();
        }
        
        private void ResolveBoneLinks()
        {
            if(Skeleton != null)
            {
                for (int i = 0; i < Skeleton.Bones.Count; i++)
                {
                    if (Skeleton.Bones[i].EmoPartIndex != ushort.MaxValue && Skeleton.Bones[i].EmoPartIndex < Parts.Count)
                    {
                        Parts[Skeleton.Bones[i].EmoPartIndex].LinkedBone = Skeleton.Bones[i];
                    }
                }
            }
        }

        private void ResolvePartIndices()
        {
            //Update EMO PartIndex in the skeleton with the current part indices, as long as they have a LinkedBone
            //Also update the bone link on all Emg files to match the parent EMO_Part bone link

            if(Skeleton != null)
            {
                for (int i = 0; i < Parts.Count; i++)
                {
                    if (Parts[i].LinkedBone != null)
                    {
                        int boneIdx = Skeleton.Bones.IndexOf(Parts[i].LinkedBone);

                        if (boneIdx != -1)
                        {
                            Skeleton.Bones[boneIdx].EmoPartIndex = (ushort)i;
                        }

                        for(int a = 0; a < Parts[i].EmgFiles.Count; a++)
                        {
                            Parts[i].EmgFiles[a].LinkedBoneIdx = (ushort)boneIdx;
                        }
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Convert a group of EMD files into a single EMO file. 
        /// </summary>
        /// <param name="emdFiles">All EMD files that will be merged together into an EMO</param>
        /// <param name="embFiles">Must be in sync with emdFiles. Can have null entries.</param>
        /// <param name="emmFiles">Must be in sync with emdFiles. Can have null entries.</param>
        /// <param name="eskFile">The skeleton that the EMD files are based on. This will be converted into an EMO skeleton.</param>
        /// <param name="mergedEmb">The final merged EMB file that contains all the EMO textures.</param>
        /// <param name="mergedEmm">The final merged EMM file that contains all the EMO materials.</param>
        public static EMO_File ConvertToEmo(EMD_File[] emdFiles, EMB_File[] embFiles, EMB_File[] dytFiles, EMM_File[] emmFiles, ESK.ESK_File eskFile, out EMB_File mergedEmb, out EMM_File mergedEmm)
        {
            if (embFiles.Length != emdFiles.Length)
                throw new ArgumentException($"EMO_File.ConvertToEmo: There must be an EMB file for each EMD file.");

            if (emmFiles.Length != emdFiles.Length)
                throw new ArgumentException($"EMO_File.ConvertToEmo: There must be an EMM file for each EMD file.");

            if (dytFiles.Length != emdFiles.Length && dytFiles.Length != 0)
                throw new ArgumentException($"EMO_File.ConvertToEmo: There must be a DYT file for each EMD file, or no DYT files.");

            if (eskFile == null)
                throw new ArgumentException($"EMO_File.ConvertToEmo: ESK_Skeleton cannot be null.");


            EMO_File emoFile = new EMO_File();
            EMB_File embFile = EMB_File.DefaultEmbFile(false);
            EMM_File emmFile = EMM_File.DefaultEmmFile();
            emoFile.Skeleton = Skeleton.Convert(eskFile.Skeleton);

            for(int i = 0; i < emdFiles.Length; i++)
            {
                //Merge textures
                int embIndex = embFile.Entry.Count;

                //Add dyt texture to main EMB
                if (dytFiles.Length > 0)
                {
                    embFile.AddEntry(dytFiles[i].Entry[0].Data);
                }

                //Main textures
                embFile.MergeEmbFile(embFiles[i]);


                if (embFile.Entry.Count > EMB_File.MAX_EFFECT_TEXTURES)
                    throw new Exception($"EMO_File.ConvertToEmo: Texture overflow (more than 128). To try to fix, use less models/embs.");

                //Merge materials
                Dictionary<string, string> matNames = new Dictionary<string, string>();

                foreach(var mat in emmFiles[i].Materials)
                {
                    string name = emmFile.GetUnusedName(mat.Name);
                    EmmMaterial newMat = mat.Copy();
                    newMat.Name = name;
                    emmFile.Materials.Add(newMat);

                    matNames.Add(mat.Name, name);

                    if(dytFiles.Length > 0)
                    {
                        //Regular character shaders do not work as effects (game crashes). So they need to be swapped with vfx-specific shaders.
                        //Luckily the game has some VFX shaders that are for character EMOs which load a dyt as a regular texture (on sampler/texture 0, rather than 4 as regular characters do)

                        if(newMat.ShaderProgram.Contains("TOON_UNIF"))
                        {
                            newMat.ShaderProgram = "TOON_UNIFfx_VFX_DFDna_FCM";
                        }
                    }
                }

                emoFile.AddModel(emdFiles[i], eskFile.Skeleton, embIndex, matNames, i, dytFiles.Length > 0);

            }

            emoFile.MaterialsCount = (ushort)emmFile.Materials.Count;
            mergedEmb = embFile;
            mergedEmm = emmFile;
            return emoFile;
        }

        public void AddModel(EMD_File emdFile, ESK.ESK_Skeleton skeleton, int embIndex, Dictionary<string, string> matNames, int emdIdx, bool hasDytSamler)
        {
            //Create EMO Part
            EMO_Part part = new EMO_Part();
            part.Name = string.IsNullOrWhiteSpace(emdFile.Name) ? $"mesh_{emdIdx}" : Path.GetFileNameWithoutExtension(emdFile.Name);

            Parts.Add(part);

            //Create EMG. The whole EMD will be added onto this.
            EMG_File emg = EMG_File.Convert(emdFile, skeleton, embIndex, matNames, emdIdx, hasDytSamler);

            part.EmgFiles.Add(emg);

        }
    
        public int CalculateMaterialCount()
        {
            int count = 0;

            foreach(var part in Parts)
            {
                foreach(var emg in part.EmgFiles)
                {
                    foreach(var mesh in emg.EmgMeshes)
                    {
                        count += mesh.SubmeshGroups.Count;
                    }
                }
            }

            return count;
        }
    
        public bool ModelExists(EMO_Part emoPart, EMG_File emg)
        {
            for(int i = 0; i < Parts.Count; i++)
            {
                if (Parts[i] == emoPart)
                {
                    for(int a = 0; a < Parts[i].EmgFiles.Count; a++)
                    {
                        if (Parts[i].EmgFiles[a] == emg)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return false;
        }

        #region Helpers
        public EMO_Part GetParentPart(EMG_File emg)
        {
            foreach (var model in Parts)
            {
                if (model.EmgFiles.Contains(emg)) return model;
            }

            return null;
        }

        public EMG_File GetParentEmg(EMG_Mesh mesh)
        {
            foreach (var model in Parts)
            {
                foreach (var emg in model.EmgFiles)
                {
                    if(emg.EmgMeshes.Contains(mesh)) return emg;
                }
            }

            return null;
        }

        public EMG_Mesh GetParentMesh(EMG_SubmeshGroup submeshGroup)
        {
            foreach (var model in Parts)
            {
                foreach (var emg in model.EmgFiles)
                {
                    foreach (var mesh in emg.EmgMeshes)
                    {
                        if(mesh.SubmeshGroups.Contains(submeshGroup)) return mesh;
                    }
                }
            }

            return null;
        }

        public EMG_SubmeshGroup GetParentSubmeshGroup(EMD_TextureSamplerDef textureSampler)
        {
            foreach (var model in Parts)
            {
                foreach(var emg in model.EmgFiles)
                {
                    foreach(var mesh in emg.EmgMeshes)
                    {
                        foreach(var submeshGroup in mesh.SubmeshGroups)
                        {
                            if(submeshGroup.TextureSamplerDefs.Contains(textureSampler)) return submeshGroup;
                        }
                    }
                }
            }

            return null;
        }
        #endregion
    }

    [YAXSerializeAs("Part")]
    [Serializable]
    public class EMO_Part : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        [YAXAttributeForClass]
        public string Name { get; set; }

        private Bone _linkedBone = null;
        [YAXDontSerialize]
        public Bone LinkedBone
        {
            get => _linkedBone;
            set
            {
                if(value != _linkedBone)
                {
                    _linkedBone = value;
                    if(_linkedBone != null)
                    {
                        Name = _linkedBone.Name;
                        NotifyPropertyChanged(nameof(Name));
                    }
                    NotifyPropertyChanged(nameof(LinkedBone));
                }
            }
        }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EMG")]
        public AsyncObservableCollection<EMG_File> EmgFiles { get; set; } = new AsyncObservableCollection<EMG_File>();

        public static EMO_Part Read(byte[] bytes, int offset, int nameOffset)
        {
            EMO_Part part = new EMO_Part();
            part.Name = StringEx.GetString(bytes, nameOffset, false, StringEx.EncodingType.UTF8);
            int count = BitConverter.ToInt32(bytes, offset);

            for(int i = 0; i < count; i++)
            {
                int emgOffset = BitConverter.ToInt32(bytes, offset + 4 + (4 * i)) + offset;
                part.EmgFiles.Add(EMG_File.Read(bytes, emgOffset));
            }

            return part;
        }
    
        public List<byte> Write(int absOffset)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(EmgFiles.Count));

            //Emg file offsets
            bytes.AddRange(new byte[4 * EmgFiles.Count]);

            //Padding
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]);

            for(int i = 0; i < EmgFiles.Count; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 4 + (4 * i));
                bytes.AddRange(EmgFiles[i].Write(false, absOffset + bytes.Count));
            }

            return bytes;
        }
    }
}
