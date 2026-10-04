using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Numbers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ETR;
using Xv2CoreLib.Color;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for RecolorAll.xaml
    /// </summary>
    public partial class RecolorAll : AutoObservableWindow
    {
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
        private readonly IList<Asset> Assets = null;
        private readonly IList<EmmMaterial> Materials = null;
        private readonly EffectContainerFile effectContainerFile = null;
        private readonly ParticleNode particleNode = null;
        private readonly IList<ETR_Node> etrNodes = null;
        private readonly IList<ECF_Node> ecfNodes = null;


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


        public CustomColor PreviewColor { get; set; }
        public double Hue { get; set; }
        public double Saturation { get; set; }
        public double Lightness { get; set; }

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

        public Brush PreviewBrush => PreviewColor != null ? new SolidColorBrush(Color.FromArgb(255, (byte)(PreviewColor.R * 255f), (byte)(PreviewColor.G * 255f), (byte)(PreviewColor.B * 255f))) : null;

        #region Tooltips
        public string HueRevertTooltip => $"Revert to original value of {initialHue:##0.0#}";
        public string SaturationRevertTooltip => $"Revert to original value of {initialSaturation:0.0}";
        public string LightnessRevertTooltip => $"Revert to original value of {initialLightness:0.0}";
        public string RgbPreviewTooltip => PreviewColor != null ? string.Format("R: {0} ({3}), G: {1} ({4}), B: {2} ({5})", PreviewColor.R, PreviewColor.G, PreviewColor.B, (byte)(PreviewColor.R * 255f), (byte)(PreviewColor.G * 255f), (byte)(PreviewColor.B * 255f)) : null;
        #endregion

        #region Constructor
        /// <summary>
        /// Hue shift a asset.
        /// </summary>
        public RecolorAll(AssetType _assetType, IList<Asset> assets, bool isHueSet)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.Asset;
            assetType = _assetType;
            Assets = assets;

            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Hue shift a material.
        /// </summary>
        /// <param name="_material"></param>
        public RecolorAll(IList<EmmMaterial> _materials, bool isHueSet, bool shiftGlareColor)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.Material;
            Materials = _materials;
            ShiftGlareColor = shiftGlareColor;

            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Hue shift all assets, materials and textures in a EffectContainerFile.
        /// </summary>
        public RecolorAll(EffectContainerFile _effectContainerFile, bool isHueSet)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.Global;
            effectContainerFile = _effectContainerFile;
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Hue shift a ParticleEffect.
        /// </summary>
        public RecolorAll(ParticleNode node, bool isHueSet)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.ParticleNode;
            particleNode = node;

            InitializeComponent();
            DataContext = this;
        }

        public RecolorAll(IList<ETR_Node> nodes, bool isHueSet)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.TraceNode;
            etrNodes = nodes;

            InitializeComponent();
            DataContext = this;
        }

        public RecolorAll(IList<ECF_Node> nodes, bool isHueSet)
        {
            this.isHueSet = isHueSet;
            currentMode = Mode.ColorFadeNode;
            ecfNodes = nodes;

            InitializeComponent();
            DataContext = this;
        }

        #endregion

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

            //List<RgbColor> colors = new List<RgbColor>();
            Xv2ColorAccumulator accumulator = new Xv2ColorAccumulator();

            if(currentMode == Mode.Asset)
            {
                Title += Assets.Count > 1 ? $" ({Assets.Count} {assetType} Assets)" : $" (1 {assetType} Asset)";
                GetUsedColorsForAssets(Assets, accumulator);
            }
            else if (currentMode == Mode.Material)
            {
                Title += Materials.Count > 1 ? $" ({Materials.Count} Materials)" : " (1 Material)";
                foreach (var material in Materials)
                {
                    material.AccumulateUsedColors(accumulator);
                }
            }
            else if (currentMode == Mode.Global)
            {
                Title += $" (Global)";
                GetUsedColorsByEverything(accumulator);
            }
            else if (currentMode == Mode.ParticleNode)
            {
                Title += $" (1 Particle Node)";
                particleNode.AccumulateUsedColors(accumulator);
            }
            else if (currentMode == Mode.TraceNode)
            {
                Title += etrNodes.Count > 1 ? $" ({etrNodes.Count} Trace Nodes)" : " (1 Trace Node)";
                foreach (var node in etrNodes)
                {
                    node.AccumulateUsedColors(accumulator);
                }
            }
            else if (currentMode == Mode.ColorFadeNode)
            {
                Title += ecfNodes.Count > 1 ? $" ({ecfNodes.Count} Color Fade Nodes)" : " (1 Color Fade Node)";
                foreach (var node in ecfNodes)
                {
                    node.AccumulateUsedColors(accumulator);
                }
            }

            if (accumulator.NumColors == 0)
            {
                MessagePrompt.Show("No color information was found on this asset so it cannot be modified.\n\nThe most likely cause of this is that all color sources for this asset were either all white (1,1,1) or all black (0,0,0).", "No color information", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return false;
            }

            PreviewColor = accumulator.ToCustomColor();
            PreviewColor.ExtractHsl(out double hue, out double saturation, out double lightness);

            initialHue = hue;
            initialSaturation = saturation;
            initialLightness = lightness;
            Hue = initialHue;
            Saturation = initialSaturation;
            Lightness = initialLightness;

            ValueChanged();

            return true;
        }


        private void ChangeHueForAsset(Asset _asset, double hueChange, double saturationChange, double lightnessChange, List<IUndoRedo> undos, bool shiftGlareColor = true)
        {
            switch (_asset.AssetType)
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

        private void ChangeHueForEverything(double hueChange, double saturationChange, double lightnessChange, List<IUndoRedo> undos)
        {
            ChangeHueForAssets(effectContainerFile.Pbind.Assets, hueChange, saturationChange, lightnessChange, undos, false);
            ChangeHueForAssets(effectContainerFile.Tbind.Assets, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForAssets(effectContainerFile.Cbind.Assets, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForAssets(effectContainerFile.Emo.Assets, hueChange, saturationChange, lightnessChange, undos);
            ChangeHueForAssets(effectContainerFile.LightEma.Assets, hueChange, saturationChange, lightnessChange, undos);
            effectContainerFile.Pbind.File3_Ref.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance);
            effectContainerFile.Tbind.File3_Ref.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance);
            effectContainerFile.Pbind.File2_Ref.ChangeHsl(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance, ShiftGlareColor);
            effectContainerFile.Tbind.File2_Ref.ChangeHsl(hueChange, saturationChange, lightnessChange, undos, isHueSet, Variance, ShiftGlareColor);
        }

        private void ChangeHueForAssets(IList<Asset> assets, double hueChange, double saturationChange, double lightnessChange, List<IUndoRedo> undos, bool shiftGlareColor = true)
        {
            foreach (var _asset in assets)
            {
                ChangeHueForAsset(_asset, hueChange, saturationChange, lightnessChange, undos, shiftGlareColor);
            }
        }

        private void GetUsedColorsByEverything(Xv2ColorAccumulator accumulator)
        {
            GetUsedColorsForAssets(effectContainerFile.Pbind.Assets, accumulator);
            GetUsedColorsForAssets(effectContainerFile.Tbind.Assets, accumulator);
            GetUsedColorsForAssets(effectContainerFile.Cbind.Assets, accumulator);
            GetUsedColorsForAssets(effectContainerFile.LightEma.Assets, accumulator);
            GetUsedColorsForAssets(effectContainerFile.Emo.Assets, accumulator);
            effectContainerFile.Pbind.File3_Ref.AccumulateUsedColors(accumulator);
            effectContainerFile.Tbind.File3_Ref.AccumulateUsedColors(accumulator);
            effectContainerFile.Pbind.File2_Ref.AccumulateUsedColors(accumulator);
            effectContainerFile.Tbind.File2_Ref.AccumulateUsedColors(accumulator);
        }

        private void GetUsedColorsForAssets(IList<Asset> _assets, Xv2ColorAccumulator accumulator)
        {
            foreach (var _asset in _assets)
            {
                _asset.AccumulateUsedColors(accumulator);

                if (assetType == AssetType.PBIND)
                {
                    foreach (var material in _asset.Files[0].EmpFile.GetUsedMaterials())
                    {
                        //material.AccumulateUsedColors(accumulator);
                    }
                }
            }
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
            //PreviewColor.SetHls(initialHue, initialLightness, initialSaturation);
            PreviewColor?.SetHsl(Hue, Saturation, Lightness);
            NotifyPropertyChanged(nameof(HueRevertTooltip));
            NotifyPropertyChanged(nameof(SaturationRevertTooltip));
            NotifyPropertyChanged(nameof(LightnessRevertTooltip));
            NotifyPropertyChanged(nameof(RgbPreviewTooltip));
            NotifyPropertyChanged(nameof(Hue));
            NotifyPropertyChanged(nameof(Saturation));
            NotifyPropertyChanged(nameof(Lightness));
            NotifyPropertyChanged(nameof(PreviewBrush));
            NotifyPropertyChanged(nameof(PreviewColor));
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            object context = null;

            hueChange = isHueSet ? Hue : Hue - initialHue;
            saturationChange = Saturation - initialSaturation;
            lightnessChange = Lightness - initialLightness;

            if (currentMode == Mode.Asset)
            {
                ChangeHueForAssets(Assets, hueChange, saturationChange, lightnessChange, undos);
                context = Assets;
            }
            else if (currentMode == Mode.Material)
            {
                foreach(var material in Materials)
                {
                    material.ChangeHsl(hueChange, saturationChange, lightnessChange, undos, shiftGlareColor: ShiftGlareColor);
                }
            }
            else if (currentMode == Mode.Global)
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
                foreach(var node in etrNodes)
                {
                    node.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
                }
                context = etrNodes;
            }
            else if (currentMode == Mode.ColorFadeNode)
            {
                foreach(var node in ecfNodes)
                {
                    node.ChangeHue(hueChange, saturationChange, lightnessChange, undos);
                }
                context = ecfNodes;
            }

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Hue Adjustment"), UndoGroup.ColorControl, null, context);
            UndoManager.Instance.ForceEventCall(UndoGroup.ColorControl, null, context);

            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_UndoHueChange_Click(object sender, RoutedEventArgs e)
        {
            Hue = initialHue;
            ValueChanged();
        }

        private void Button_UndoSaturationChange_Click(object sender, RoutedEventArgs e)
        {
            Saturation = initialSaturation;
            ValueChanged();
        }

        private void Button_UndoLightnessChange_Click(object sender, RoutedEventArgs e)
        {
            Lightness = initialLightness;
            ValueChanged();
        }

    }
}