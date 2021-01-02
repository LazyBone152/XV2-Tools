using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using YAXLib;
using System.Windows.Media.Imaging;
using CSharpImageLibrary;
using Xv2CoreLib.EffectContainer;
using System.Windows.Data;
using Xv2CoreLib.HslColor;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMB_CLASS
{
    public enum InstallMode
    {
        MatchIndex,
        MatchName
    }

    [YAXComment("InstallMode values (used by LB Mod Installer):" +
        "\nMatchIndex: install entry into this index" +
        "\nMatchName: if entry with same name exists, overwrite it, else add as new")]
    [Serializable]
    public class EMB_File : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public const int MAX_EFFECT_TEXTURES = 128;

        [YAXAttributeForClass]
        [YAXSerializeAs("I_08")]
        public UInt16 I_08 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_10")]
        public UInt16 I_10 { get; set; }
        [YAXAttributeForClass]
        public bool UseFileNames { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("InstallMode")]
        public InstallMode installMode { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "EmbEntry")]
        public ObservableCollection<EmbEntry> Entry { get; set; } = new ObservableCollection<EmbEntry>();

        //Filters
        [YAXDontSerialize]
        private string _textureSearchFilter = null;
        [YAXDontSerialize]
        public string TextureSearchFilter
        {
            get
            {
                return this._textureSearchFilter;
            }
            set
            {
                if (value != this._textureSearchFilter)
                {
                    this._textureSearchFilter = value;
                    NotifyPropertyChanged("TextureSearchFilter");
                }
            }
        }

        [NonSerialized]
        [YAXDontSerialize]
        private ListCollectionView _viewTextures = null;
        [YAXDontSerialize]
        public ListCollectionView ViewTextures
        {
            get
            {
                if (_viewTextures != null)
                {
                    return _viewTextures;
                }
                _viewTextures = new ListCollectionView(Entry);
                _viewTextures.Filter = new Predicate<object>(TextureFilterCheck);
                return _viewTextures;
            }
            set
            {
                if (value != _viewTextures)
                {
                    _viewTextures = value;
                    NotifyPropertyChanged("ViewTextures");
                }
            }
        }

        public bool TextureFilterCheck(object texture)
        {
            if (String.IsNullOrWhiteSpace(TextureSearchFilter)) return true;
            var _texture = texture as EmbEntry;

            if (_texture != null)
            {
                if (_texture.Name.ToLower().Contains(TextureSearchFilter.ToLower())) return true;
            }

            return false;
        }

        public void UpdateTextureFilter()
        {
            if (_viewTextures == null)
                _viewTextures = new ListCollectionView(Entry);

            _viewTextures.Filter = new Predicate<object>(TextureFilterCheck);
            NotifyPropertyChanged("ViewTextures");
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static EMB_File LoadEmb(byte[] bytes)
        {
            return new Parser(bytes).embFile;
        }

        /// <summary>
        /// Loads the specified emb file. It can be in either binary or xml format. 
        /// 
        /// If a file can not be found at the specified location, then a empty one will be returned.
        /// </summary>
        public static EMB_File LoadEmb(string path, bool returnEmptyIfNotValid = true)
        {
            if (Path.GetExtension(path) == ".emb")
            {
                return new Xv2CoreLib.EMB_CLASS.Parser(path, false).GetEmbFile();
            }
            else if (Path.GetExtension(path) == ".xml" && Path.GetExtension(Path.GetFileNameWithoutExtension(path)) == ".emb")
            {
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.EMB_CLASS.EMB_File), YAXSerializationOptions.DontSerializeNullObjects);
                return (Xv2CoreLib.EMB_CLASS.EMB_File)serializer.DeserializeFromFile(path);
            }
            else
            {
                if (returnEmptyIfNotValid)
                {
                    return new Xv2CoreLib.EMB_CLASS.EMB_File()
                    {
                        I_08 = 37568,
                        I_10 = 0,
                        UseFileNames = true,
                        Entry = new ObservableCollection<EmbEntry>()
                    };
                }
                else
                {
                    throw new FileNotFoundException("An .emb could not be found at the specified location.");
                }
                
            }
        }

        public void SaveXmlEmbFile(string saveLocation)
        {
            if(Entry != null)
            {
                for (int i = 0; i < Entry.Count(); i++)
                {
                    Entry[i].Index = i.ToString();
                }
            }
            

            if (!Directory.Exists(Path.GetDirectoryName(saveLocation)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveLocation));
            }
            YAXSerializer serializer = new YAXSerializer(typeof(EMB_File));
            serializer.SerializeToFile(this, saveLocation);
        }

        public void SaveBinaryEmbFile(string saveLocation)
        {
            if (!Directory.Exists(Path.GetDirectoryName(saveLocation)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveLocation));
            }
            
            new Deserializer(saveLocation, this);
        }

        public static EMB_File DefaultEmbFile(bool textureEmb)
        {
            if (textureEmb == true)
            {
                return new EMB_File()
                {
                    I_08 = 1,
                    I_10 = 1,
                    UseFileNames = true,
                    Entry = new ObservableCollection<EmbEntry>()
                };
            }
            else
            {
                return new EMB_File()
                {
                    I_08 = 37568,
                    I_10 = 0,
                    UseFileNames = true,
                    Entry = new ObservableCollection<EmbEntry>()
                };
            }
            
        }

        public bool DoesFileExist(string file)
        {
            for(int i = 0; i < Entry.Count(); i++)
            {
                if(Entry[i].Name == file)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsFileType(string extension)
        {
            foreach(var e in Entry)
            {
                if(Path.GetExtension(e.Name) == extension)
                {
                    return true;
                }
            }

            return false;
        }
        
        public int AddEntry(string name, byte[] bytes, bool overWrite, int expectedSize = -1)
        {
            if(expectedSize != -1 && Entry.Count != expectedSize)
            {
                throw new Exception(String.Format("The EEPK container and EMB are out of sync. Cannot add the entry."));
            }

            //Check if entry exists
            for(int i = 0; i < Entry.Count; i++)
            {
                if(Entry[i].Name == name)
                {
                    if (overWrite)
                    {
                        Entry[i].Data = bytes.ToList();
                    }
                    return i;
                }

            }

            //Add it
            int newIdx = Entry.Count;
            Entry.Add(new EmbEntry()
            {
                Index = newIdx.ToString(),
                Name = name,
                Data = bytes.ToList()
            });

            return newIdx;
        }
        
        public EmbEntry GetEntry(int index)
        {
            if(index >= Entry.Count || index < 0)
            {
                return null;
            }

            return Entry[index];
        }

        public EmbEntry GetEntry(string name)
        {
            foreach(var entry in Entry)
            {
                if (entry.Name == name) return entry;
            }

            return null;
        }


        public EmbEntry Compare(EmbEntry embEntry2)
        {
            foreach(var entry in Entry)
            {
                if (entry == embEntry2) return entry;

                if (entry.Compare(embEntry2))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Add embEntry if new. If a similar one already exists then that will be returned.
        /// </summary>
        /// <returns></returns>
        public EmbEntry Add(EmbEntry embEntry, List<IUndoRedo> undos = null)
        {
            foreach (var entry in Entry)
            {
                if (entry == embEntry) return entry;

                if (entry.Compare(embEntry))
                {
                    return entry;
                }
            }

            //Check entry size
            if(Entry.Count >= MAX_EFFECT_TEXTURES)
            {
                throw new Exception(String.Format("EMB_File.Add: Texture limit has been reached. Cannot add any more."));
            }

            if (undos != null)
                undos.Add(new UndoableListAdd<EmbEntry>(Entry, embEntry));

            Entry.Add(embEntry);

            return embEntry;
        }


        public string GetUnusedName(string name)
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name);
            string newName = name;
            int num = 1;

            while (NameUsed(newName))
            {
                newName = String.Format("{0}_{1}{2}", nameWithoutExtension, num, extension);
                num++;
            }

            return newName;
        }

        public bool NameUsed(string name)
        {
            foreach (var entry in Entry)
            {
                if (entry.Name == name) return true;
                
            }

            return false;
        }

        /// <summary>
        /// Attemps to load all EMB entries as a DDS image file and saves them to a ImageSource object.
        /// </summary>
        public void LoadDdsImages(bool reload = true)
        {
            for (int i = 0; i < Entry.Count; i++)
            {
                if (!Entry[i].loadDds || reload)
                {
                    Entry[i].LoadDds();
                }
            }
        }

        /// <summary>
        /// Saves all loaded DdsImages into Data.
        /// </summary>
        public void SaveDdsImages()
        {
            string name = null;
            try
            {
                foreach(var entry in Entry)
                {
                    name = entry.Name;
                    if (entry.loadDds)
                    {
                        entry.SaveDds();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("SaveDdsImages: Failed on entry with name = {0}.", name), ex);
            }
        }

        public void ValidateNames()
        {
            List<string> names = new List<string>();

            for (int i = 0; i < Entry.Count; i++)
            {
                if (names.Contains(Entry[i].Name))
                {
                    //Name was used previously
                    Entry[i].Name = GetUnusedName(Entry[i].Name);
                }
                else
                {
                    //Name is unused
                    names.Add(Entry[i].Name);
                }
            }
        }

        public int AddEntry(EmbEntry embEntry, string _idx, InstallMode _installMode)
        {
            int idx = int.Parse(_idx);

            if(_installMode == InstallMode.MatchIndex)
            {

                if (idx <= (Entry.Count - 1))
                {
                    Entry[idx] = embEntry;
                    return idx;
                }
                else
                {
                    //Add empty entries until idx is reached
                    while ((Entry.Count - 1) < (idx - 1))
                    {
                        Entry.Add(new EmbEntry() { Name = "dummy_" + (Entry.Count - 1).ToString(), Data = new List<byte>() });
                    }

                    Entry.Add(embEntry);
                    return Entry.Count - 1;
                }
            }
            else if (_installMode == InstallMode.MatchName)
            {
                for(int i = 0; i < Entry.Count; i++)
                {
                    if(Entry[i].Name == embEntry.Name)
                    {
                        Entry[i] = embEntry;
                        return i;
                    }
                }

                Entry.Add(embEntry);
                return Entry.Count - 1;
            }

            return -1;
        }

        public void RemoveEntry(string _idx, EmbEntry original = null)
        {
            int idx = int.Parse(_idx);

            if(idx == Entry.Count - 1)
            {
                //Last entry, so just remove it
                Entry.RemoveAt(idx);

                if (original != null)
                    Entry.Add(original);
            }
            else if (idx < Entry.Count - 1 && idx >= 0)
            {
                //Replace entry with an empty entry
                if(original != null)
                {
                    Entry[idx] = original;
                }
                else
                {
                    Entry[idx] = EmbEntry.Empty(idx);
                }
            }
        }

        public void TrimNullEntries()
        {
            for (int i = Entry.Count - 1; i >= 0; i--)
            {
                if (!Entry[i].Name.Contains("dummy_"))
                    break;

                if(Entry[i].Data == null)
                {
                    Entry.RemoveAt(i);
                }
                else if(Entry[i].Data.Count == 0)
                {
                    Entry.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
        }

        public List<RgbColor> GetUsedColors()
        {
            if (Entry == null) Entry = new ObservableCollection<EmbEntry>();
            List<RgbColor> colors = new List<RgbColor>();

            foreach(var entry in Entry)
            {
                colors.Add(entry.GetDdsColor());
            }

            return colors;
        }
    }

    [Serializable]
    public class EmbEntry : INotifyPropertyChanged, IInstallable
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        private string _name = null;
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        [BindingString]
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("UseLocalCopy")]
        public bool LocalCopy { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public string Index { get; set; }
        private List<byte> _dataValue = null;
        [YAXAttributeFor("Data")]
        [YAXSerializeAs("bytes")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<byte> Data
        {
            get
            {
                return this._dataValue;
            }
            set
            {
                if (value != this._dataValue)
                {
                    this._dataValue = value;
                    loadDdsFail = false;

                    //Reload DdsImage IF it has been loaded already (loadDds == true) AND it is not currently being saved (loadDdsLock == false)
                    if (loadDds && !_loadDdsLock)
                    {
                        //Reload the dds with the new image data
                        LoadDds();
                        wasEdited = false; 
                    }
                    
                    NotifyPropertyChanged("Data");
                    NotifyPropertyChanged("Height");
                    NotifyPropertyChanged("Width");
                    NotifyPropertyChanged("FilesizeString");
                }
            }
        }

        //DDS loading & handling
        public ImageEngineFormat ImageFormat = ImageEngineFormat.DDS_DXT5;
        [NonSerialized]
        public bool wasEdited = false; //If false, we won't save the DDS file.
        [NonSerialized]
        public bool wasReloaded = false; //Was SaveDds() ever called on this object?
        [NonSerialized]
        private bool _loadDdsLock = false;
        [NonSerialized]
        public bool loadDds = false;
        [NonSerialized]
        private bool ddsIsLoading = false;
        [NonSerialized]
        public bool loadDdsFail = false;
        [NonSerialized] //WriteableBitmap cannot be serialized, and so the related bools should be ignored as well. (DdsImage doesn't need to be copied, anyway... it will just be reloaded from Data)
        private WriteableBitmap _ddsImage = null;
        [YAXDontSerialize]
        public WriteableBitmap DdsImage
        {
            get
            {
                if(_ddsImage == null && !loadDdsFail && !ddsIsLoading)
                {
                    LoadDds();
                }
                return this._ddsImage;
            }
            set
            {
                if (value != this._ddsImage)
                {
                    this._ddsImage = value;
                    NotifyPropertyChanged("DdsImage");
                }
            }
        }
        

        //DDS Details
        [YAXDontSerialize]
        public int Height
        {
            get
            {
                if (DdsImage == null) return 0;
                return (int)DdsImage.Height;
            }
        }
        [YAXDontSerialize]
        public int Width
        {
            get
            {
                if (DdsImage == null) return 0;
                return (int)DdsImage.Width;
            }
        }
        [YAXDontSerialize]
        public string FilesizeString
        {
            get
            {
                if(Data != null)
                {
                    if(Data.Count < 1000)
                    {
                        //Is less than a kilobyte
                        return String.Format("{0} bytes", Data.Count);
                    }
                    else if(Data.Count < 1000000)
                    {
                        //Is atleast a kilobyte and less than a megabyte
                        return String.Format("{0} KB", Utils.BytesToKilobytes(Data.Count));
                    }
                    else
                    {
                        //Is a megabyte or more
                        return String.Format("{0} MB", Utils.BytesToMegabytes(Data.Count));
                    }
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        public bool Compare(EmbEntry embEntry2, bool ignoreName = false)
        {
            //Name and bytes must be the same to return true
            if(embEntry2.Name == Name || ignoreName)
            {
                return Data.SequenceEqual(embEntry2.Data);
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Loads DdsImage from Data.
        /// </summary>
        public void LoadDds()
        {
            if (!EepkToolInterlop.LoadTextures)
                return;

            try
            {
                ddsIsLoading = true;
                byte[] data = Data.ToArray();
                int numMipMaps = 0;

                //PROBLEM: Most files load fine with DDSReader, but it CANT load files that are saved with CSharpImageLibrary.
                //CSharpImageLibrary may be able to load them on Win7/Win8 machines, but I cant test that...

                using (var ImageSource = new ImageEngineImage(data))
                {
                    DdsImage = new WriteableBitmap(ImageSource.GetWPFBitmap());
                    ImageFormat = ImageSource.Format.SurfaceFormat;
                    numMipMaps = ImageSource.NumMipMaps;
                }

                //If CSharpImageLibrary fails to load the image, then try DDSReader
                //IMPORTANT: DDSReader CANNOT load files saved by CSharpImageLibrary!
                //if (DdsImage == null || numMipMaps == 0)
                //{

                //    DDSReader.Utils.PixelFormat format;
                //    DdsImage = new WriteableBitmap(DDSReader.DDS.LoadImage(data, out format, true));
                //    ImageFormat = ImageEngineFormat.DDS_DXT5; //Default to DXT5

                    //If DdsImage is still null, then the image has failed to load.
                //    if (DdsImage == null)
                //    {
                //        throw new InvalidDataException(string.Format("Unable to parse \"{0}\".", Name));
                //    }
                //}



            }
            catch
            {
                loadDdsFail = true;
            }
            finally
            {
                loadDds = true;
                ddsIsLoading = false;
            }
        }

        /// <summary>
        /// Saves DdsImage into Data.
        /// </summary>
        public void SaveDds(bool onlySaveIfEdited = true)
        {
            if (DdsImage == null || (!wasEdited && onlySaveIfEdited))
                return;

            try
            {
                _loadDdsLock = true;
                ImageEngineImage newImage = null;

                using (MemoryStream png = new MemoryStream())
                {
                    DdsImage.Save(png);
                    newImage = new ImageEngineImage(png);
                }
                Data = newImage.Save(ImageFormat, MipHandling.Default).ToList();
            }
            finally
            {
                wasReloaded = true;
                _loadDdsLock = false;
            }
        }
        

        public EmbEntry Clone()
        {
            EmbEntry newEntry = new EmbEntry();
            newEntry.Name = Name;
            newEntry.Data = Data;
            newEntry.DdsImage = DdsImage;
            newEntry.LocalCopy = LocalCopy;
            return newEntry;
        }
        
        public static EmbEntry Empty(int idx = 0)
        {
            return new EmbEntry()
            {
                Name = "dummy_" + idx.ToString(),
                Data = new List<byte>(),
                Index = idx.ToString()
            };
        }

        public RgbColor GetDdsColor()
        {
            if (DdsImage == null) throw new InvalidOperationException("GetDdsColor: DdsImage was null.");
            List<RgbColor> colors = new List<RgbColor>();

            //Lazy code. Checking every single pixel would be WAY too slow, so we just skim through them instead.
            for(int i = 0; i < DdsImage.Width; i+=15)
            {
                if (i > DdsImage.Width) break;

                for (int a = 0; a < DdsImage.Height; a+=15)
                {
                    if (a > DdsImage.Height) break;

                    var pixel = DdsImage.GetPixel(i, a);
                    RgbColor rgbColor = new RgbColor(pixel.R, pixel.G, pixel.B);

                    if (!rgbColor.IsWhiteOrBlack)
                    {
                        colors.Add(rgbColor);
                    }
                }
            }

            if(colors.Count == 0)
            {
                return new RgbColor(255, 255, 255);
            }

            return ColorEx.GetAverageColor(colors);
        }
        
    }
    
}
