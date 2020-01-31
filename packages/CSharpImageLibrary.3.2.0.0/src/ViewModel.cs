using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UsefulThings.WPF;

namespace CSharpImageLibrary
{
    /// <summary>
    /// View model for the main Converter form
    /// </summary>
    public class ViewModel : ViewModelBase
    {
        public ImageEngineImage img { get; set; }
        Stopwatch stopwatch = new Stopwatch();
        DispatcherTimer savePreviewUpdateTimer = new DispatcherTimer();

        public bool IsDXT1AlphaVisible
        {
            get
            {
                return SaveFormat == ImageEngineFormat.DDS_DXT1;
            }
        }

        bool flattenBlend = true;
        public bool FlattenBlend
        {
            get
            {
                return flattenBlend;
            }
            set
            {
                SetProperty(ref flattenBlend, value);
                stripAlpha = !value;
                OnPropertyChanged(nameof(StripAlpha));
                DDSGeneral.DXT1AlphaThreshold = blendValue;
                GenerateSavePreview();
            }
        }

        bool stripAlpha = false;
        public bool StripAlpha
        {
            get
            {
                return stripAlpha;
            }
            set
            {
                SetProperty(ref stripAlpha, value);
                flattenBlend = !value;
                OnPropertyChanged(nameof(FlattenBlend));
                GenerateSavePreview();
                DDSGeneral.DXT1AlphaThreshold = 0f;  // KFreon: Strips the alpha out 
            }
        }

        float blendValue = DDSGeneral.DXT1AlphaThreshold;
        public float DXT1AlphaThreshold
        {
            get
            {
                DDSGeneral.DXT1AlphaThreshold = blendValue;
                return DDSGeneral.DXT1AlphaThreshold*100f;
            }
            set
            {
                DDSGeneral.DXT1AlphaThreshold = value/100f;
                OnPropertyChanged(nameof(DXT1AlphaThreshold));
                blendValue = value/100f;
                savePreviewUpdateTimer.Start();
            }
        }


        bool showAlphaPreviews = false;
        public bool ShowAlphaPreviews
        {
            get
            {
                return showAlphaPreviews;
            }
            set
            {
                SetProperty(ref showAlphaPreviews, value);
                UpdatePreviews();
                OnPropertyChanged(nameof(SavePreview));
            }
        }

        long saveElapsed = -1;
        public long SaveElapsedTime
        {
            get
            {
                return saveElapsed;
            }
            set
            {
                SetProperty(ref saveElapsed, value);
            }
        }

        #region Original Image Properties
        public MTRangedObservableCollection<BitmapSource> Previews { get; set; }
        List<BitmapSource> AlphaPreviews { get; set; }
        List<BitmapSource> NonAlphaPreviews { get; set; }

        public int NumMipMaps
        {
            get
            {
                if (img != null)
                    return img.NumMipMaps;

                return -1;
            }
        }

        public string Format
        {
            get
            {
                return img?.Format.SurfaceFormat.ToString();
            }
        }

        public string ImagePath
        {
            get
            {
                return img?.FilePath;
            }
        }

        public BitmapSource Preview
        {
            get
            {
                if (Previews?.Count == 0 || MipIndex >= Previews?.Count + 1)
                    return null;

                return Previews?[MipIndex - 1];
            }
        }


        public double? MipWidth
        {
            get
            {
                return Preview?.PixelWidth;
            }
        }

        public double? MipHeight
        {
            get
            {
                return Preview?.PixelHeight;
            }
        }

        int mipindex = 1;
        public int MipIndex
        {
            get
            {
                return mipindex;
            }
            set
            {
                if (value < 0 || value >= NumMipMaps + 1)
                    return;

                SetProperty(ref mipindex, value);
                OnPropertyChanged(nameof(Preview));
                OnPropertyChanged(nameof(MipWidth));
                OnPropertyChanged(nameof(MipHeight));
            }
        }
        #endregion Original Image Properties


        #region Save Properties
        MipHandling generateMips = MipHandling.Default;
        public MipHandling GenerateMipMaps
        {
            get
            {
                return generateMips;
            }
            set
            {
                SaveSuccess = null;
                SetProperty(ref generateMips, value);
            }
        }

        string savePath = null;
        public string SavePath
        {
            get
            {
                return savePath;
            }
            set
            {
                SaveSuccess = null;
                SetProperty(ref savePath, value);
                OnPropertyChanged(nameof(IsSaveReady));
            }
        }

        ImageEngineFormat saveFormat = ImageEngineFormat.Unknown;
        public ImageEngineFormat SaveFormat
        {
            get
            {
                return saveFormat;
            }
            set
            {
                SaveSuccess = null;
                SetProperty(ref saveFormat, value);
                OnPropertyChanged(nameof(IsSaveReady));
                OnPropertyChanged(nameof(IsDXT1AlphaVisible));
            }
        }

        BitmapSource[] savePreviews = new BitmapSource[2];
        public BitmapSource SavePreview
        {
            get
            {
                return ShowAlphaPreviews ? savePreviews[0] : savePreviews[1];
            }
        }


        public bool IsSaveReady
        {
            get
            {
                return !String.IsNullOrEmpty(SavePath) && SaveFormat != ImageEngineFormat.Unknown;
            }
        }

        public string SavingFailedErrorMessage
        {
            get; private set;
        }

        bool? saveSuccess = null;
        public bool? SaveSuccess
        {
            get
            {
                return saveSuccess;
            }
            set
            {
                SetProperty(ref saveSuccess, value);
            }
        }
        #endregion Save Properties


        public ViewModel()
        {
            Previews = new MTRangedObservableCollection<BitmapSource>();

            // KFreon: Timer starts when alpha slider is updated, waits for a second of inaction before making new previews (inaction because it's restarted everytime the slider changes, and when it makes a preview, it stops itself)
            savePreviewUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            savePreviewUpdateTimer.Tick += (s, b) =>
            {
                // KFreon: Delay regeneration if previous previews are still being generated
                if (!stopwatch.IsRunning)
                {
                    GenerateSavePreview();
                    savePreviewUpdateTimer.Stop();
                }
            };
        }

        internal string GetAutoSavePath(ImageEngineFormat newformat)
        {
            string newpath = null;
            bool acceptablePath = false;
            int count = 1;


            string formatString = ImageFormats.GetExtensionOfFormat(newformat);

            string basepath = Path.GetDirectoryName(ImagePath) + "\\" + Path.GetFileNameWithoutExtension(ImagePath) + "." +
                (newformat == ImageEngineFormat.Unknown ? Path.GetExtension(ImagePath) : formatString);

            newpath = basepath;

            // KFreon: Check that path is not already taken
            while (!acceptablePath)
            {
                if (File.Exists(newpath))
                    newpath = Path.Combine(Path.GetDirectoryName(basepath),  Path.GetFileNameWithoutExtension(basepath) + "_" + count++ + Path.GetExtension(basepath));
                else
                    acceptablePath = true;
            }
            
            return newpath;
        }

        internal async void GenerateSavePreview()
        {
            if (img == null || SaveFormat == ImageEngineFormat.Unknown)
                return;


            // KFreon: TGA saving not supported
            if (img.Format.SurfaceFormat == ImageEngineFormat.TGA)
                SaveFormat = ImageEngineFormat.PNG;

            stopwatch.Start();
            savePreviews = await Task.Run(() =>
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    img.Save(stream, SaveFormat, MipHandling.KeepTopOnly, 1024, mergeAlpha: (SaveFormat == ImageEngineFormat.DDS_DXT1 ? FlattenBlend : false));  // KFreon: Smaller size for quicker loading
                    watch.Stop();
                    Debug.WriteLine($"Preview Save took {watch.ElapsedMilliseconds}ms");
                    using (ImageEngineImage previewimage = new ImageEngineImage(stream))
                    {
                        BitmapSource[] tempImgs = new BitmapSource[2];
                        tempImgs[0] = previewimage.GeneratePreview(0, true);
                        tempImgs[1] = previewimage.GeneratePreview(0, false);
                        return tempImgs;
                    }
                }
            });
            stopwatch.Stop();
            Debug.WriteLine($"Preview generation took {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Reset();
            OnPropertyChanged(nameof(SavePreview));
        }

        public async Task LoadImage(string path)
        {
            bool testing = false;  // Set to true to load mips single threaded and only the full image instead of a smaller one first.

            Task<List<object>> fullLoadingTask = null;
            if (!testing)
            {
                // Load full size image
                ////////////////////////////////////////////////////////////////////////////////////////
                fullLoadingTask = Task.Run(() =>
                {
                    ImageEngineImage fullimage = new ImageEngineImage(path);

                    List<BitmapSource> alphas = new List<BitmapSource>();
                    List<BitmapSource> nonalphas = new List<BitmapSource>();

                    for (int i = 0; i < fullimage.NumMipMaps; i++)
                    {
                        alphas.Add(fullimage.GeneratePreview(i, true));
                        nonalphas.Add(fullimage.GeneratePreview(i, false));
                    }

                    List<object> bits = new List<object>();
                    bits.Add(fullimage);
                    bits.Add(alphas);
                    bits.Add(nonalphas);
                    return bits;
                });
                ////////////////////////////////////////////////////////////////////////////////////////
            }




            SaveSuccess = null;
            Previews.Clear();
            savePreviews = new BitmapSource[2];
            SavePath = null;
            SaveFormat = ImageEngineFormat.Unknown;

            stopwatch.Start();



            ////////////////////////////////////////////////////////////////////////////////////////
            if (testing)
                img = await Task.Run(() => new ImageEngineImage(path));
            else
                img = await Task.Run(() => new ImageEngineImage(path, 256, false));
            ////////////////////////////////////////////////////////////////////////////////////////



            Console.WriteLine("");
            Console.WriteLine($"Format: {img.Format}");
            stopwatch.Stop();
            Console.WriteLine($"Image Loading: {stopwatch.ElapsedMilliseconds}");
            stopwatch.Restart();

            Previews.Add(img.GeneratePreview(0, ShowAlphaPreviews));
            MipIndex = 1;  // 1 based

            stopwatch.Stop();
            Debug.WriteLine($"Image Preview: {stopwatch.ElapsedMilliseconds}");
            stopwatch.Reset();

            OnPropertyChanged(nameof(ImagePath));
            OnPropertyChanged(nameof(Format));
            OnPropertyChanged(nameof(NumMipMaps));
            OnPropertyChanged(nameof(Preview));
            OnPropertyChanged(nameof(MipWidth));
            OnPropertyChanged(nameof(MipHeight));

            // KFreon: Get full image details
            ////////////////////////////////////////////////////////////////////////////////////////
            if (!testing)
            {
                List<object> FullImageObjects = await fullLoadingTask;
                double? oldMipWidth = MipWidth;
                img = (ImageEngineImage)FullImageObjects[0];

                AlphaPreviews = (List<BitmapSource>)FullImageObjects[1];
                NonAlphaPreviews = (List<BitmapSource>)FullImageObjects[2];

                UpdatePreviews();

                // KFreon: Set selected mip index
                /*for (int i = 0; i < Previews.Count; i++)
                {
                    if (Previews[i].Width == oldMipWidth)
                    {
                        MipIndex = i + 1;  // 1 based
                        break;
                    }
                }*/
                MipIndex = 1;
            }
            
            ////////////////////////////////////////////////////////////////////////////////////////


            OnPropertyChanged(nameof(NumMipMaps));
            OnPropertyChanged(nameof(Preview));
            OnPropertyChanged(nameof(MipIndex));
            OnPropertyChanged(nameof(MipWidth));
            OnPropertyChanged(nameof(MipHeight));
            OnPropertyChanged(nameof(img));
        }

        private void UpdatePreviews()
        {
            if (AlphaPreviews == null || NonAlphaPreviews == null)
                return; 

            Previews.Clear();
            Previews.AddRange(ShowAlphaPreviews ? AlphaPreviews : NonAlphaPreviews);
            OnPropertyChanged(nameof(Preview));
        }

        internal bool Save()
        {
            if (img != null && !String.IsNullOrEmpty(SavePath) && SaveFormat != ImageEngineFormat.Unknown)
            {
                try
                {
                    stopwatch.Start();
                    img.Save(SavePath, SaveFormat, generateMips, mergeAlpha: (SaveFormat == ImageEngineFormat.DDS_DXT1 ? FlattenBlend : false));
                    stopwatch.Stop();
                    Debug.WriteLine($"Saved format: {SaveFormat} in {stopwatch.ElapsedMilliseconds} milliseconds.");

                    SaveElapsedTime = stopwatch.ElapsedMilliseconds;

                    stopwatch.Reset();
                    SaveSuccess = true;
                    return true;
                }
                catch(Exception e)
                {
                    SavingFailedErrorMessage = e.ToString();
                    SaveSuccess = false;
                    return false;
                }
            }

            return false;
        }
    }
}
