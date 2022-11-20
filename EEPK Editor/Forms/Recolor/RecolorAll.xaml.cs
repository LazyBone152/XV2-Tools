using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for RecolorAll.xaml
    /// </summary>
    public partial class RecolorAll : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private enum Mode
        {
            Asset,
            Material,
            Global,
            ParticleNode
        }

        private AssetType assetType = AssetType.EMO;
        private Asset asset = null;
        private EmmMaterial material = null;
        private EffectContainerFile effectContainerFile = null;
        private ParticleNode particleNode = null;

        private Mode currentMode = Mode.Asset;

        //Values
        private double initialHue = 0;
        private double hueChange = 0;
        private double initialSaturation = 0;
        private double saturationChange = 0;
        private double initialLightness = 0;
        private double lightnessChange = 0;

        //For use with Global mode. Without multiplying this changing saturation globally is difficult as it doesn't change much.
        private double _saturationChangeMulti
        {
            get
            {
                return saturationChange * 2.5;
            }
        }

        private RgbColor _rgbColor = new RgbColor(255,255,255);
        public RgbColor rgbColor
        {
            get
            {
                return this._rgbColor;
            }
            set
            {
                if (value != this._rgbColor)
                {
                    this._rgbColor = value;
                    NotifyPropertyChanged("rgbColor");
                    NotifyPropertyChanged("preview");
                }
            }
        }
        private HslColor _hslColor = null;
        public HslColor hslColor
        {
            get
            {
                return this._hslColor;
            }
            set
            {
                if (value != this._hslColor)
                {
                    this._hslColor = value;
                    NotifyPropertyChanged("hslColor");
                }
            }
        }

        public Brush preview
        {
            get
            {
                return new SolidColorBrush(Color.FromArgb(255, rgbColor.R_int, rgbColor.G_int, rgbColor.B_int));
            }
        }

        #region Tooltips
        public string HueRevertTooltip => string.Format("Revert to original value of {0}", initialHue);
        public string SaturationRevertTooltip => string.Format("Revert to original value of {0}", initialSaturation);
        public string LightnessRevertTooltip => string.Format("Revert to original value of {0}", initialLightness);
        public string RgbPreviewTooltip => string.Format("R: {0} ({3}), G: {1} ({4}), B: {2} ({5})", rgbColor.R, rgbColor.G, rgbColor.B, rgbColor.R_int, rgbColor.G_int, rgbColor.B_int);
        #endregion

        /// <summary>
        /// Hue shift a asset.
        /// </summary>
        public RecolorAll(AssetType _assetType, Asset _asset, Window parent)
        {
            currentMode = Mode.Asset;
            assetType = _assetType;
            asset = _asset;

            InitializeComponent();
            Owner = parent;
            DataContext = this;
        }

        /// <summary>
        /// Hue shift a material.
        /// </summary>
        /// <param name="_material"></param>
        public RecolorAll(EmmMaterial _material, Window parent)
        {
            currentMode = Mode.Material;
            material = _material;

            InitializeComponent();
            Owner = parent;
            DataContext = this;
        }

        /// <summary>
        /// Hue shift all assets, materials and textures in a EffectContainerFile.
        /// </summary>
        public RecolorAll(EffectContainerFile _effectContainerFile, Window parent)
        {
            currentMode = Mode.Global;
            effectContainerFile = _effectContainerFile;
            InitializeComponent();
            Owner = parent;
            DataContext = this;
        }

        /// <summary>
        /// Hue shift a ParticleEffect.
        /// </summary>
        public RecolorAll(ParticleNode node, Window parent)
        {
            currentMode = Mode.ParticleNode;
            particleNode = node;

            InitializeComponent();
            Owner = parent;
            DataContext = this;
        }

        public bool Initialize()
        {
            if (((currentMode == Mode.Asset && assetType == AssetType.EMO) || currentMode == Mode.Global) && !SettingsManager.Instance.LoadTextures)
            {
                MessageBox.Show("This option is not available while textures are turned off. Enable Load Textures in the settings to use this option.", "Not Available", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            List<RgbColor> colors = null;

            if(currentMode == Mode.Asset)
            {
                colors = asset.GetUsedColors();
            }
            else if (currentMode == Mode.Material)
            {
                colors = material.GetUsedColors();
            }
            else if (currentMode == Mode.Global)
            {
                colors = GetUsedColorsByEverything();
            }
            else if (currentMode == Mode.ParticleNode)
            {
                colors = particleNode.GetUsedColors();
            }
            

            if(colors.Count == 0)
            {
                MessageBox.Show("No color information was found on this asset so it cannot be hue shifted.\n\nThe most likely cause of this is that all color sources for this asset were either all white (1,1,1) or all black (0,0,0).", "No color information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            rgbColor = ColorEx.GetAverageColor(colors);
            hslColor = rgbColor.ToHsl();

            //hslColor.Lightness = 0.5f; //Gives a "pure" color. Not light or dark. Good for previewing.
            //hslColor.Saturation = 1f; //Completely saturated. Good for previewing.
            initialHue = hslColor.Hue;
            initialSaturation = hslColor.Saturation;
            initialLightness = hslColor.Lightness;

            ValueChanged();

            return true;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ValueChanged();
        }

        private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            ValueChanged();
        }

        private void ValueChanged()
        {
            rgbColor = hslColor.ToRgb();
            NotifyPropertyChanged("HueRevertTooltip");
            NotifyPropertyChanged("SaturationRevertTooltip");
            NotifyPropertyChanged("LightnessRevertTooltip");
            NotifyPropertyChanged("RgbPreviewTooltip");
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            hueChange = hslColor.Hue - initialHue;
            saturationChange = hslColor.Saturation - initialSaturation;
            lightnessChange = hslColor.Lightness - initialLightness;

            if (currentMode == Mode.Asset)
            {
                ChangeHueForAsset(asset, hueChange, saturationChange, lightnessChange, undos);
            }
            else if(currentMode == Mode.Material)
            {
                material.ChangeHsl(hueChange, saturationChange, lightnessChange, undos);
            }
            else if(currentMode == Mode.Global)
            {
                ChangeHueForEverything(hueChange, _saturationChangeMulti, lightnessChange, undos);
            }
            else if (currentMode == Mode.ParticleNode)
            {
                particleNode.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
            }

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Hue Adjustment"));
            UndoManager.Instance.ForceEventCall();

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        

        private void ChangeHueForAsset(Asset _asset, double hueChange, double saturationChange, double lightnessChange, List<IUndoRedo> undos)
        {
            switch (_asset.assetType)
            {
                case AssetType.PBIND:
                    _asset.Files[0].EmpFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
                    break;
                case AssetType.TBIND:
                    _asset.Files[0].EtrFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
                    break;
                case AssetType.CBIND:
                    _asset.Files[0].EcfFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
                    break;
                case AssetType.LIGHT:
                    _asset.Files[0].EmaFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
                    break;
                case AssetType.EMO:
                    foreach (EffectFile file in _asset.Files)
                    {
                        switch (file.Extension)
                        {
                            case ".emb":
                                file.EmbFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos); //No lightness change
                                break;
                            case ".emm":
                                file.EmmFile.ChangeHsl(hueChange, saturationChange, lightnessChange, undos);
                                break;
                            case ".mat.ema":
                                EMM_File emmFile = _asset.Files.FirstOrDefault(x => x.fileType == EffectFile.FileType.EMM)?.EmmFile;
                                file.EmaFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, emmFile:emmFile);
                                break;
                        }
                    }
                    break;
            }

        }

        private List<RgbColor> GetUsedColorsByEverything()
        {
            List<RgbColor> colors = new List<RgbColor>();

            colors.AddRange(GetUsedColersByContainer(effectContainerFile.Pbind));
            colors.AddRange(GetUsedColersByContainer(effectContainerFile.Tbind));
            colors.AddRange(GetUsedColersByContainer(effectContainerFile.Cbind));
            colors.AddRange(GetUsedColersByContainer(effectContainerFile.LightEma));
            colors.AddRange(GetUsedColersByContainer(effectContainerFile.Emo));
            colors.AddRange(effectContainerFile.Pbind.File3_Ref.GetUsedColors());
            colors.AddRange(effectContainerFile.Tbind.File3_Ref.GetUsedColors());
            colors.AddRange(effectContainerFile.Pbind.File2_Ref.GetUsedColors());
            colors.AddRange(effectContainerFile.Tbind.File2_Ref.GetUsedColors());

            return colors;
        }

        private List<RgbColor> GetUsedColersByContainer(AssetContainerTool container)
        {
            List<RgbColor> colors = new List<RgbColor>();

            foreach(var asset in container.Assets)
            {
                colors.AddRange(asset.GetUsedColors());
            }

            return colors;
        }

        private void ChangeHueForEverything(double hueChange, double saturationChange, double lightnessChange, List<IUndoRedo> undos)
        {
            ChangeHueForContainer(effectContainerFile.Pbind, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForContainer(effectContainerFile.Tbind, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForContainer(effectContainerFile.Cbind, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForContainer(effectContainerFile.Emo, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForContainer(effectContainerFile.LightEma, hueChange, saturationChange, lightnessChange, undos);
            effectContainerFile.Pbind.File3_Ref.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
            effectContainerFile.Tbind.File3_Ref.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
            effectContainerFile.Pbind.File2_Ref.ChangeHsl(hueChange, saturationChange, lightnessChange, undos);
            effectContainerFile.Tbind.File2_Ref.ChangeHsl(hueChange, saturationChange, lightnessChange, undos);
        }

        private void ChangeHueForContainer(AssetContainerTool container, double hueChange, double saturationChange, double lightnessChange, List<IUndoRedo> undos)
        {
            foreach(var _asset in container.Assets)
            {
                ChangeHueForAsset(_asset, hueChange, saturationChange, lightnessChange, undos);
            }
        }

        private void Button_UndoHueChange_Click(object sender, RoutedEventArgs e)
        {
            hslColor.Hue = initialHue;
            ValueChanged();
        }

        private void Button_UndoSaturationChange_Click(object sender, RoutedEventArgs e)
        {
            hslColor.Saturation = initialSaturation;
            ValueChanged();
        }

        private void Button_UndoLightnessChange_Click(object sender, RoutedEventArgs e)
        {
            hslColor.Lightness = initialLightness;
            ValueChanged();
        }

    }
}
