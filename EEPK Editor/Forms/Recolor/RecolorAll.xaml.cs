using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LB_Common.Forms;
using MahApps.Metro.Controls;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ETR;
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

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private enum Mode
        {
            Asset,
            Material,
            Global,
            ParticleNode,
            TraceNode,
            ColorFadeNode
        }

        private readonly AssetType assetType = AssetType.EMO;
        private readonly Asset asset = null;
        private readonly EmmMaterial material = null;
        private readonly EffectContainerFile effectContainerFile = null;
        private readonly ParticleNode particleNode = null;
        private readonly ETR_Node etrNode = null;
        private readonly ECF_Node ecfNode = null;

        private readonly Mode currentMode = Mode.Asset;
        private readonly bool isHueSet = false;

        //Values
        private double initialHue = 0;
        private double hueChange = 0;
        private double initialSaturation = 0;
        private double saturationChange = 0;
        private double initialLightness = 0;
        private double lightnessChange = 0;
        private int _variance = 0;
        private bool _textureVariance = false;
        private RgbColor _rgbColor = new RgbColor(255, 255, 255);
        private HslColor _hslColor = null;


        //For use with Global mode. Without multiplying this changing saturation globally is difficult as it doesn't change much.
        private double SaturationChangeMulti
        {
            get
            {
                return saturationChange * 2.5;
            }
        }

        public bool ShiftGlareColor { get; set; } = true;

        public Visibility ShiftGlareColorVisibility =>
            currentMode == Mode.Material || currentMode == Mode.Global ||
            (currentMode == Mode.Asset &&
                (assetType == AssetType.EMO || assetType == AssetType.PBIND))
                ? Visibility.Visible
                : Visibility.Collapsed;

        public RgbColor RgbColor
        {
            get
            {
                return _rgbColor;
            }
            set
            {
                if (value != _rgbColor)
                {
                    _rgbColor = value;
                    NotifyPropertyChanged(nameof(RgbColor));
                    NotifyPropertyChanged(nameof(PreviewBrush));
                }
            }
        }
        public HslColor HslColor
        {
            get
            {
                return _hslColor;
            }
            set
            {
                if (value != _hslColor)
                {
                    _hslColor = value;
                    NotifyPropertyChanged(nameof(HslColor));
                }
            }
        }
        public int Variance
        {
            get
            {
                return _variance;
            }
            set
            {
                if (value != _variance)
                {
                    _variance = value;
                    NotifyPropertyChanged(nameof(Variance));
                }
            }
        }
        public bool TextureVariance
        {
            get
            {
                return _textureVariance;
            }
            set
            {
                if (value != _textureVariance)
                {
                    _textureVariance = value;
                    NotifyPropertyChanged(nameof(TextureVariance));
                }
            }
        }

        public Brush PreviewBrush => new SolidColorBrush(Color.FromArgb(255, RgbColor.R_int, RgbColor.G_int, RgbColor.B_int));

        #region Tooltips
        public string HueRevertTooltip => string.Format("Revert to original value of {0}", initialHue);
        public string SaturationRevertTooltip => string.Format("Revert to original value of {0}", initialSaturation);
        public string LightnessRevertTooltip => string.Format("Revert to original value of {0}", initialLightness);
        public string RgbPreviewTooltip => string.Format("R: {0} ({3}), G: {1} ({4}), B: {2} ({5})", RgbColor.R, RgbColor.G, RgbColor.B, RgbColor.R_int, RgbColor.G_int, RgbColor.B_int);
        #endregion

        /// <summary>
        /// Hue shift a asset.
        /// </summary>
        public RecolorAll(AssetType _assetType, Asset _asset, bool isHueSet, Window parent)
        {
            this.isHueSet = isHueSet;
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
        public RecolorAll(EmmMaterial _material, bool isHueSet, Window parent, bool shiftGlareColor)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.Material;
            material = _material;
            ShiftGlareColor = shiftGlareColor;

            InitializeComponent();
            Owner = parent;
            DataContext = this;
        }

        /// <summary>
        /// Hue shift all assets, materials and textures in a EffectContainerFile.
        /// </summary>
        public RecolorAll(EffectContainerFile _effectContainerFile, bool isHueSet, Window parent)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.Global;
            effectContainerFile = _effectContainerFile;
            InitializeComponent();
            Owner = parent;
            DataContext = this;
        }

        /// <summary>
        /// Hue shift a ParticleEffect.
        /// </summary>
        public RecolorAll(ParticleNode node, bool isHueSet, Window parent)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.ParticleNode;
            particleNode = node;

            InitializeComponent();
            Owner = parent;
            DataContext = this;
        }

        public RecolorAll(ETR_Node node, bool isHueSet, Window parent)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.TraceNode;
            etrNode = node;

            InitializeComponent();
            Owner = parent;
            DataContext = this;
        }

        public RecolorAll(ECF_Node node, bool isHueSet, Window parent)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.ColorFadeNode;
            ecfNode = node;

            InitializeComponent();
            Owner = parent;
            DataContext = this;
        }


        public bool Initialize()
        {
            if (isHueSet)
            {
                helpTextBlock.Text = "Sets the hue value to the desired amount, keeping the saturation and lightness values the same. This will result in everything being different shades of the same color.";
                saturationGrid.Visibility = Visibility.Collapsed;
                lightnessGrid.Visibility = Visibility.Collapsed;
                varianceGrid.Visibility = Visibility.Visible;
                Height = 270;
                Title = "Hue Set";
            }
            else
            {
                helpTextBlock.Text = "Adjusts the hue, saturation and lightness (HSL) values, shifting them by the desired amount. This recolors the effect while preserving any color variation.";
                saturationGrid.Visibility = Visibility.Visible;
                lightnessGrid.Visibility = Visibility.Visible;
                varianceGrid.Visibility = Visibility.Collapsed;
                Height = 320;
                Title = "Hue Adjustment";
            }

            if (((currentMode == Mode.Asset && assetType == AssetType.EMO) || currentMode == Mode.Global) && !SettingsManager.Instance.LoadTextures)
            {
                MessagePrompt.Show("This option is not available while textures are turned off. Enable Load Textures in the settings to use this option.", "Not Available", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return false;
            }

            List<RgbColor> colors = null;

            if(currentMode == Mode.Asset)
            {
                colors = asset.GetUsedColors();
                if (assetType == AssetType.PBIND)
                {
                    foreach (var material in asset.Files[0].EmpFile.GetUsedMaterials())
                    {
                        colors.AddRange(material.GetUsedColors());
                    }
                }
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
            else if (currentMode == Mode.TraceNode)
            {
                colors = etrNode.GetUsedColors();
            }
            else if (currentMode == Mode.ColorFadeNode)
            {
                colors = ecfNode.GetUsedColors();
            }

            if (colors.Count == 0)
            {
                MessagePrompt.Show("No color information was found on this asset so it cannot be modified.\n\nThe most likely cause of this is that all color sources for this asset were either all white (1,1,1) or all black (0,0,0).", "No color information", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return false;
            }

            RgbColor = ColorEx.GetAverageColor(colors);
            HslColor = RgbColor.ToHsl();

            //hslColor.Lightness = 0.5f; //Gives a "pure" color. Not light or dark. Good for previewing.
            //hslColor.Saturation = 1f; //Completely saturated. Good for previewing.
            initialHue = HslColor.Hue;
            initialSaturation = HslColor.Saturation;
            initialLightness = HslColor.Lightness;

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
            RgbColor = HslColor.ToRgb();
            NotifyPropertyChanged("HueRevertTooltip");
            NotifyPropertyChanged("SaturationRevertTooltip");
            NotifyPropertyChanged("LightnessRevertTooltip");
            NotifyPropertyChanged("RgbPreviewTooltip");
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            object context = null;

            hueChange = isHueSet ? HslColor.Hue : HslColor.Hue - initialHue;
            saturationChange = HslColor.Saturation - initialSaturation;
            lightnessChange = HslColor.Lightness - initialLightness;

            if (currentMode == Mode.Asset)
            {
                ChangeHueForAsset(asset, hueChange, saturationChange, lightnessChange, undos);
                context = asset;
            }
            else if(currentMode == Mode.Material)
            {
                material.ChangeHsl(hueChange, saturationChange, lightnessChange, undos, shiftGlareColor: ShiftGlareColor);
            }
            else if(currentMode == Mode.Global)
            {
                ChangeHueForEverything(hueChange, SaturationChangeMulti, lightnessChange, undos);
                context = effectContainerFile;
            }
            else if (currentMode == Mode.ParticleNode)
            {
                particleNode.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
                context = particleNode;
            }
            else if (currentMode == Mode.TraceNode)
            {
                etrNode.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
                context = etrNode;
            }
            else if (currentMode == Mode.ColorFadeNode)
            {
                ecfNode.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
                context = ecfNode;
            }

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Hue Adjustment"), UndoGroup.ColorControl, null, context);
            UndoManager.Instance.ForceEventCall(UndoGroup.ColorControl, null, context);

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        

        private void ChangeHueForAsset(Asset _asset, double hueChange, double saturationChange, double lightnessChange, List<IUndoRedo> undos, bool shiftGlareColor = true)
        {
            switch (_asset.assetType)
            {
                case AssetType.PBIND:
                    _asset.Files[0].EmpFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance, shiftGlareColor && ShiftGlareColor);
                    break;
                case AssetType.TBIND:
                    _asset.Files[0].EtrFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance);
                    break;
                case AssetType.CBIND:
                    _asset.Files[0].EcfFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance);
                    break;
                case AssetType.LIGHT:
                    _asset.Files[0].EmaFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance);
                    break;
                case AssetType.EMO:
                    foreach (EffectFile file in _asset.Files)
                    {
                        switch (file.Extension)
                        {
                            case ".emb":
                                file.EmbFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, TextureVariance ? Variance : 0); //No lightness change
                                break;
                            case ".emm":
                                file.EmmFile.ChangeHsl(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance, ShiftGlareColor);
                                break;
                            case ".mat.ema":
                                EMM_File emmFile = _asset.Files.FirstOrDefault(x => x.fileType == EffectFile.FileType.EMM)?.EmmFile;
                                file.EmaFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance, emmFile);
                                break;
                        }
                    }
                    break;
            }

        }

        private List<RgbColor> GetUsedColorsByEverything()
        {
            List<RgbColor> colors = new List<RgbColor>(1024);

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
            ChangeHueForContainer(effectContainerFile.Pbind, hueChange, saturationChange, lightnessChange, undos, false);
            ChangeHueForContainer(effectContainerFile.Tbind, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForContainer(effectContainerFile.Cbind, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForContainer(effectContainerFile.Emo, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForContainer(effectContainerFile.LightEma, hueChange, saturationChange, lightnessChange, undos);
            effectContainerFile.Pbind.File3_Ref.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance);
            effectContainerFile.Tbind.File3_Ref.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance);
            effectContainerFile.Pbind.File2_Ref.ChangeHsl(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance, ShiftGlareColor);
            effectContainerFile.Tbind.File2_Ref.ChangeHsl(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance, ShiftGlareColor);
        }

        private void ChangeHueForContainer(AssetContainerTool container, double hueChange, double saturationChange, double lightnessChange, List<IUndoRedo> undos, bool shiftGlareColor = true)
        {
            foreach(var _asset in container.Assets)
            {
                ChangeHueForAsset(_asset, hueChange, saturationChange, lightnessChange, undos, shiftGlareColor);
            }
        }

        private void Button_UndoHueChange_Click(object sender, RoutedEventArgs e)
        {
            HslColor.Hue = initialHue;
            ValueChanged();
        }

        private void Button_UndoSaturationChange_Click(object sender, RoutedEventArgs e)
        {
            HslColor.Saturation = initialSaturation;
            ValueChanged();
        }

        private void Button_UndoLightnessChange_Click(object sender, RoutedEventArgs e)
        {
            HslColor.Lightness = initialLightness;
            ValueChanged();
        }

    }
}