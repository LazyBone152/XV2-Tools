using CSharpImageLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Color;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.Image;
using Xv2CoreLib.Resource.UndoRedo;
using System.Windows;

namespace Xv2CoreLib.EMB_CLASS
{
    [Serializable]
    public class EMB_TextureFile : EMB_BaseFile<EMB_TextureEntry>
    {
        public event EventHandler TexturesChanged;

        public void TriggerTexturesChanged()
        {
            TexturesChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Load
        public EMB_TextureFile() { }

        public EMB_TextureFile(EMB_BaseFile<EMB_TextureEntry> embFile)
        {
            IsEMZ = embFile.IsEMZ;
            Version = embFile.Version;
            I_10 = embFile.I_10;
            UseFileNames = embFile.UseFileNames;
            Entry = embFile.Entry;
        }

        public static EMB_TextureFile Load(string path)
        {
            return new EMB_TextureFile(LoadInternal(File.ReadAllBytes(path)));
        }

        [FileLoad]
        public static EMB_TextureFile Load(byte[] bytes)
        {
            return new EMB_TextureFile(LoadInternal(bytes));
        }
        #endregion

        #region Texture
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
                foreach (var entry in Entry)
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

        public void AccumulateUsedColors(Xv2ColorAccumulator accumulator)
        {
            foreach (var entry in Entry)
            {
                entry.AccumulateUsedColor(accumulator);
            }
        }

        public List<EMB_TextureEntry> GetAllEmbEntriesByBitmap(List<WriteableBitmap> bitmaps)
        {
            List<EMB_TextureEntry> entries = new List<EMB_TextureEntry>();

            foreach (var bitmap in bitmaps)
            {
                entries.Add(Entry.FirstOrDefault(x => x.Texture == bitmap));
            }

            return entries;
        }

        public async Task ChangeHue(double hue, double saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (Entry == null) return;

            //Manipulation of WriteableBitmaps must be done on the main UI thread
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                foreach (var entry in Entry)
                {
                    await entry.ChangeHue(hue, saturation, lightness, undos, hueSet, variance);
                    entry.SaveDds(true, undos);
                }
            });
        }
        #endregion

        /// <summary>
        /// Try to add a unique <see cref="EMB_TextureEntry"/> to the <see cref="EMB_TextureFile"/>. If the texture already exists, then that existing instance will be returned.
        /// </summary>
        public EMB_TextureEntry AddTexture(EMB_TextureEntry embEntry, List<IUndoRedo> undos = null, bool useTextureLimit = true)
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
            if (Entry.Count >= MAX_EFFECT_TEXTURES && useTextureLimit)
            {
                throw new Exception(string.Format("EMB_TextureFile.AddTexture: Texture limit has been reached. Cannot add any more."));
            }

            if (undos != null)
                undos.Add(new UndoableListAdd<EMB_TextureEntry>(Entry, embEntry));

            Entry.Add(embEntry);

            return embEntry;
        }
        
        public static EMB_TextureFile GetDefault()
        {
            return new EMB_TextureFile()
            {
                Version = 1,
                I_10 = 1,
                UseFileNames = true
            };
        }
    }


    [Serializable]
    public class EMB_TextureEntry : EmbEntry, INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly PropertyChangedEventArgs IDChangedEventArgs = new(nameof(ID));
        private static readonly PropertyChangedEventArgs NameChangedEventArgs = new(nameof(Name));
        private static readonly PropertyChangedEventArgs DataChangedEventArgs = new(nameof(Data));
        private static readonly PropertyChangedEventArgs TextureChangedEventArgs = new(nameof(Texture));
        #endregion

        public const int DDS_SIGNATURE = 542327876;

        public override int ID
        {
            get => base.ID;
            set
            {
                if (base.ID != value)
                {
                    base.ID = value;
                    PropertyChanged?.Invoke(this, IDChangedEventArgs);
                }
            }
        }
        public override string Name
        {
            get => base.Name;
            set
            {
                if (base.Name != value)
                {
                    base.Name = value;
                    PropertyChanged?.Invoke(this, NameChangedEventArgs);
                }
            }
        }
        public override byte[] Data
        {
            get
            {
                return base.Data;
            }
            set
            {
                if (value != base.Data)
                {
                    base.Data = value;
                    loadDdsFail = false;

                    //Reload DdsImage IF it has been loaded already (loadDds == true) AND it is not currently being saved (loadDdsLock == false)
                    if (loadDds && !_loadDdsLock)
                    {
                        //Reload the dds with the new image data
                        LoadDds();
                        wasEdited = false;
                    }

                    PropertyChanged?.Invoke(this, DataChangedEventArgs);
                }
            }
        }

        #region Texture
        //Texture loading & handling
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
        [NonSerialized]
        private WriteableBitmap _texture = null;
        public WriteableBitmap Texture
        {
            get
            {
                if (_texture == null && !loadDdsFail && !ddsIsLoading)
                {
                    LoadDds();
                }
                return _texture;
            }
            set
            {
                if (value != _texture)
                {
                    _texture = value;
                    PropertyChanged?.Invoke(this, TextureChangedEventArgs);
                }
            }
        }


        //Texture Details (readonly)
        public int Height
        {
            get
            {
                //It is possible for the texture to not be DDS (and loads perfectly fine ingame), so we must check.
                if (IsNull() || Texture == null) return 0;
                return (BitConverter.ToInt32(Data, 0) == DDS_SIGNATURE) ? BitConverter.ToInt32(Data, 16) : (int)Texture.Height;
            }
        }
        public int Width
        {
            get
            {
                if (IsNull() || Texture == null) return 0;
                return (BitConverter.ToInt32(Data, 0) == DDS_SIGNATURE) ? BitConverter.ToInt32(Data, 12) : (int)Texture.Width;
            }
        }
        public string FilesizeString
        {
            get
            {
                if (Data != null)
                {
                    if (Data.Length < 1000)
                    {
                        //Is less than a kilobyte
                        return String.Format("{0} bytes", Data.Length);
                    }
                    else if (Data.Length < 1000000)
                    {
                        //Is atleast a kilobyte and less than a megabyte
                        return String.Format("{0} KB", Utils.BytesToKilobytes(Data.Length));
                    }
                    else
                    {
                        //Is a megabyte or more
                        return String.Format("{0} MB", Utils.BytesToMegabytes(Data.Length));
                    }
                }
                else
                {
                    return "Unknown";
                }
            }
        }
        public string ImageFormatString
        {
            get
            {
                switch (ImageFormat)
                {
                    case ImageEngineFormat.DDS_DXT1:
                        return "DDS BC1";
                    case ImageEngineFormat.DDS_DXT3:
                        return "DDS BC2";
                    case ImageEngineFormat.DDS_DXT5:
                        return "DDS BC3";
                    case ImageEngineFormat.DDS_ATI1:
                        return "DDS BC4";
                    case ImageEngineFormat.DDS_ATI2_3Dc:
                        return "DDS BC5";
                    default:
                        return ImageFormat.ToString();
                }
            }
        }
        public string TextureToolTip
        {
            get
            {
                if (Texture != null)
                {
                    return $"Type: {ImageFormatString}\n" +
                           $"Dimensions: {Height}x{Width}\n" +
                           $"Size: {FilesizeString}";
                }
                return null;
            }
        }
        #endregion


        #region Texture

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
                ImageEngineFormat format;
                Texture = TextureHelper.GetWpfBitmap(Data, out format);

                ImageFormat = format;
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
        public void SaveDds(bool onlySaveIfEdited = true, List<IUndoRedo> undos = null)
        {
            if (Texture == null || (!wasEdited && onlySaveIfEdited))
                return;

            try
            {
                _loadDdsLock = true;
                byte[] data = TextureHelper.SaveToBytes(Texture, ImageFormat);

                if (undos != null)
                    undos.Add(new UndoablePropertyGeneric(nameof(Data), this, Data, data));

                Data = data;
            }
            finally
            {
                wasReloaded = true;
                _loadDdsLock = false;
            }

        }

        public async Task ChangeHue(double hue, double _saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (Texture == null)
                return;

            WriteableBitmapEditOperation editOperation = new WriteableBitmapEditOperation(Texture);

            if (hueSet)
            {
                if (variance != 0)
                    hue += Random.Range(-variance, variance);

                await editOperation.AsyncApplyHueSet((int)hue);
            }
            else
            {
                float brightness = (float)lightness / 5f;
                float saturation = (float)_saturation;
                await editOperation.AsyncApplyHueAdjust((int)hue, saturation, brightness);
            }

            wasEdited = true;
            Texture = editOperation.OutputBitmap;

            if (undos != null)
                undos.Add(new UndoableProperty<EmbEntry>(nameof(Texture), this, editOperation.SourceBitmap, editOperation.OutputBitmap));
        }

        public void AccumulateUsedColor(Xv2ColorAccumulator accumulator)
        {
            if (Texture == null) throw new InvalidOperationException("AccumulateUsedColor: DdsImage was null.");
            accumulator.AddBitmap(Texture);
        }

        public Xv2Color CalculateAverageColor()
        {
            if (Texture == null) throw new InvalidOperationException("GetUsedColor: DdsImage was null.");

            Xv2ColorAccumulator accumulator = new Xv2ColorAccumulator();
            accumulator.AddBitmap(Texture);
            return accumulator.AverageColor;
        }

        public BitmapSource GetBitmap()
        {
            return Texture;
        }

        #endregion

        #region Texture Mering (Supertexture)
        public static List<WriteableBitmap> GetBitmaps(IList<EMB_TextureEntry> entries)
        {
            List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();

            foreach (var entry in entries)
                bitmaps.Add(entry.Texture);

            return bitmaps;
        }

        public static double SelectTextureSize(double maxDimension, int textureCount)
        {
            double size = Math.Sqrt(textureCount) * maxDimension;
            double textureSize = 64;

            while (textureSize < size)
            {
                if (textureSize >= 2048)
                    return -1; //2k max texture size

                textureSize *= 2;
            }

            return textureSize;
        }

        public static double HighestDimension(List<WriteableBitmap> bitmaps)
        {
            double dimension = 0;

            foreach (var bitmap in bitmaps)
            {
                if (bitmap.Width > dimension) dimension = bitmap.Width;
                if (bitmap.Height > dimension) dimension = bitmap.Height;
            }

            return dimension;
        }

        #endregion

        public override EmbEntry Clone()
        {
            return new EMB_TextureEntry()
            {
                Name = Name,
                ID = ID,
                Data = Data,
                _texture = _texture
            };
        }
    }
}
