using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Numbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Xv2CoreLib.Color;
using Xv2CoreLib.ECF;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.ETR;
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

        private readonly AssetType AssetType = AssetType.EMO;
        private readonly IList<Asset> Assets = null;
        private readonly IList<EmmMaterial> Materials = null;
        private readonly EffectContainerFile effectContainerFile = null;
        private readonly AssetContainerTool AssetContainer = null;
        private readonly ParticleNode particleNode = null;
        private readonly IList<ETR_Node> etrNodes = null;
        private readonly IList<ECF_Node> ecfNodes = null;


        private readonly Mode CurrentMode = Mode.Asset;
        private readonly bool isHueSet = false;

        private readonly List<IUndoRedo> initUndos = new List<IUndoRedo>();
        private readonly List<IUndoRedo> recolorUndos = new List<IUndoRedo>();

        //Values
        private double initialHue = 0;
        private double hueChange = 0;
        private double initialSaturation = 0;
        private double saturationChange = 0;
        private double initialLightness = 0;
        private double lightnessChange = 0;
        private int _variance = 0;
        private bool _textureVariance = false;
        private bool _includeMaterials = false;
        private bool _materialsChecked = false;

        private bool _isRecoloring = false;
        private double lastRecolorHue = double.MinValue;
        private double lastRecolorSaturation = double.MinValue;
        private double lastRecolorLightness = double.MinValue;
        private int lastRecolorVariance = int.MinValue;
        private bool lastRecolorTextureVariance = false;

        //For use with Global mode. Without multiplying this changing saturation globally is difficult as it doesn't change much.
        private double SaturationChangeMulti
        {
            get
            {
                return saturationChange * 2.5;
            }
        }

        public bool ShiftGlareColor { get; set; } = true;
        public bool IncludeMaterials
        {
            get => _includeMaterials;
            set
            {
                if(_includeMaterials != value)
                {
                    _includeMaterials = value;
                    CalculatePreviewBrushColor();
                    SetInitialHslValues();
                    ValueChanged();
                    InitializeMaterials();
                    NotifyPropertyChanged(nameof(IncludeMaterials));
                }
            }
        }

        public Visibility IncludeGlareColorVisibility =>
            CurrentMode == Mode.Material || CurrentMode == Mode.Global || ((CurrentMode == Mode.Asset && AssetType == AssetType.EMO) || AssetType == AssetType.TBIND || AssetType == AssetType.PBIND)
                ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IncludeMaterialsVisibility =>
            AssetType == AssetType.PBIND || AssetType == AssetType.TBIND ? Visibility.Visible : Visibility.Collapsed;


        private Xv2ColorAccumulator ColorAccumulator = null;
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

        public Brush PreviewBrush => PreviewColor != null ? new SolidColorBrush(PreviewColor.ToWpfColor(true)) : null;

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
        public RecolorAll(AssetContainerTool assetContainer, AssetType _assetType, IList<Asset> assets, bool isHueSet)
        {
            this.isHueSet = isHueSet;
            CurrentMode = Mode.Asset;
            AssetType = _assetType;
            Assets = assets;
            AssetContainer = assetContainer;

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
            CurrentMode = Mode.Material;
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
            CurrentMode = Mode.Global;
            effectContainerFile = _effectContainerFile;
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Hue shift a ParticleEffect.
        /// </summary>
        public RecolorAll(AssetContainerTool assetContainer, ParticleNode node, bool isHueSet)
        {
            this.isHueSet = isHueSet;
            CurrentMode = Mode.ParticleNode;
            AssetType = AssetType.PBIND;
            particleNode = node;
            AssetContainer = assetContainer;

            InitializeComponent();
            DataContext = this;
        }

        public RecolorAll(AssetContainerTool assetContainer, IList<ETR_Node> nodes, bool isHueSet)
        {
            this.isHueSet = isHueSet;
            CurrentMode = Mode.TraceNode;
            AssetType = AssetType.TBIND;
            etrNodes = nodes;
            AssetContainer = assetContainer;

            InitializeComponent();
            DataContext = this;
        }

        public RecolorAll(IList<ECF_Node> nodes, bool isHueSet)
        {
            this.isHueSet = isHueSet;
            CurrentMode = Mode.ColorFadeNode;
            AssetType = AssetType.CBIND;
            ecfNodes = nodes;

            InitializeComponent();
            DataContext = this;
        }

        #endregion

        #region Setup
        public bool Initialize()
        {
            if (isHueSet)
            {
                helpTextBlock.Text = "Sets the hue value to the desired amount, keeping the saturation and lightness values the same. This will result in everything being different shades of the same color.";
                saturationGrid.Visibility = Visibility.Collapsed;
                lightnessGrid.Visibility = Visibility.Collapsed;
                varianceGrid.Visibility = Visibility.Visible;
                Title = "Hue Set";
            }
            else
            {
                helpTextBlock.Text = "Adjusts the hue, saturation and lightness (HSL) values, shifting them by the desired amount. This recolors the effect while preserving any color variation.";
                saturationGrid.Visibility = Visibility.Visible;
                lightnessGrid.Visibility = Visibility.Visible;
                varianceGrid.Visibility = Visibility.Collapsed;
                Title = "Hue Adjustment";
            }

            if (((CurrentMode == Mode.Asset && AssetType == AssetType.EMO) || CurrentMode == Mode.Global) && !SettingsManager.Instance.LoadTextures)
            {
                MessagePrompt.Show("This option is not available while textures are turned off. Enable Load Textures in the settings to use this option.", "Not Available", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return false;
            }

            //Set window name based on type and number being recolored 
            switch (CurrentMode)
            {
                case Mode.Global:
                    Title += $" (Global)";
                    break;
                case Mode.Asset:
                    Title += Assets.Count > 1 ? $" ({Assets.Count} {AssetType} Assets)" : $" (1 {AssetType} Asset)";
                    break;
                case Mode.Material:
                    Title += Materials.Count > 1 ? $" ({Materials.Count} Materials)" : " (1 Material)";
                    break;
                case Mode.ParticleNode:
                    Title += $" (1 Particle Node)";
                    break;
                case Mode.TraceNode:
                    Title += etrNodes.Count > 1 ? $" ({etrNodes.Count} Trace Nodes)" : " (1 Trace Node)";
                    break;
                case Mode.ColorFadeNode:
                    Title += ecfNodes.Count > 1 ? $" ({ecfNodes.Count} Color Fade Nodes)" : " (1 Color Fade Node)";
                    break;
            }

            if (CalculatePreviewBrushColor() > 0)
            {
                SetInitialHslValues();
                ValueChanged();
                XenoKitInit();

                return true;
            }
            else
            {
                MessagePrompt.Show("No color information was found on this asset so it cannot be modified.\n\nThe most likely cause of this is that all color sources for this asset were either all white (1,1,1) or all black (0,0,0).", "No color information", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return false;
            }
        }

        private void SetInitialHslValues()
        {
            PreviewColor.ExtractHsl(out double hue, out double saturation, out double lightness);

            initialHue = hue;
            initialSaturation = saturation;
            initialLightness = lightness;
            Hue = initialHue;
            Saturation = initialSaturation;
            Lightness = initialLightness;
            lastRecolorHue = double.MinValue;
            lastRecolorSaturation = double.MinValue;
            lastRecolorLightness = double.MinValue;

            NotifyPropertyChanged(nameof(HueRevertTooltip));
            NotifyPropertyChanged(nameof(SaturationRevertTooltip));
            NotifyPropertyChanged(nameof(LightnessRevertTooltip));
        }

        private void InitializeMaterials()
        {
            if (_materialsChecked) return;
            _materialsChecked = true;

            const string NotExclusiveMessage = "Some materials are shared with other assets. If you continue, the tool can copy them so they’re exclusive to the recolored assets, and not affected by the recoloring process.\n\n" +
                "Would you like to make copies of the shared materials?";

            if ((AssetType == AssetType.PBIND || AssetType == AssetType.TBIND) && CurrentMode == Mode.Asset)
            {
                if (!AssetContainer.AreMaterialsExclusive(Assets))
                {
                    if (MessagePrompt.Show(NotExclusiveMessage, "Materials Not Exclusive", MessagePromptButtons.AffirmativeNegative, MessagePromptIcon.Question, "Make Copies", "Ignore") == MessagePromptResult.Affirmative)
                    {
                        AssetContainer.MakeMaterialsExclusive(Assets, initUndos);
                    }
                }
            }
            else if(CurrentMode == Mode.ParticleNode)
            {
                if (!AssetContainer.AreMaterialsExclusive(particleNode))
                {
                    if (MessagePrompt.Show(NotExclusiveMessage, "Materials Not Exclusive", MessagePromptButtons.AffirmativeNegative, MessagePromptIcon.Question, "Make Copies", "Ignore") == MessagePromptResult.Affirmative)
                    {
                        AssetContainer.MakeMaterialsExclusive(particleNode, initUndos);
                    }
                }
            }
            else if(CurrentMode == Mode.TraceNode)
            {
                if (!AssetContainer.AreMaterialsExclusive(etrNodes))
                {
                    if (MessagePrompt.Show(NotExclusiveMessage, "Materials Not Exclusive", MessagePromptButtons.AffirmativeNegative, MessagePromptIcon.Question, "Make Copies", "Ignore") == MessagePromptResult.Affirmative)
                    {
                        AssetContainer.MakeMaterialsExclusive(etrNodes, initUndos);
                    }
                }
            }

        }

        private long CalculatePreviewBrushColor()
        {
            ColorAccumulator ??= new Xv2ColorAccumulator();
            ColorAccumulator.Clear();

            switch (CurrentMode)
            {
                case Mode.Global:
                    GetUsedColorsByEverything(ColorAccumulator);
                    break;
                case Mode.Asset:
                    GetUsedColorsForAssets(Assets, ColorAccumulator);
                    break;
                case Mode.Material:
                    foreach (var material in Materials)
                    {
                        material.AccumulateUsedColors(ColorAccumulator);
                    }
                    break;
                case Mode.ParticleNode:
                    particleNode.AccumulateUsedColors(ColorAccumulator);

                    if (IncludeMaterials)
                    {
                        foreach (var material in particleNode.GetUsedMaterials())
                        {
                            material.AccumulateUsedColors(ColorAccumulator);
                        }
                    }
                    break;
                case Mode.TraceNode:
                    foreach (var node in etrNodes)
                    {
                        node.AccumulateUsedColors(ColorAccumulator);
                    }
                    break;
                case Mode.ColorFadeNode:
                    foreach (var node in ecfNodes)
                    {
                        node.AccumulateUsedColors(ColorAccumulator);
                    }
                    break;
            }

            PreviewColor = ColorAccumulator.ToCustomColor();
            return ColorAccumulator.NumColors;
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

                if (IncludeMaterials && AssetType == AssetType.PBIND)
                {
                    foreach (var material in _asset.Files[0].EmpFile.GetUsedMaterials())
                    {
                        material.AccumulateUsedColors(accumulator);
                    }
                }
            }
        }

        private void UndoRecolorOperation()
        {
            if (recolorUndos.Count == 0) return; //Nothing to undo

            for(int i = recolorUndos.Count - 1; i >= 0; i--)
            {
                recolorUndos[i].Undo();
            }

            recolorUndos.Clear();
            UndoManager.Instance.ForceEventCall(UndoGroup.ColorControl, null, GetContext());
        }

        private object GetContext()
        {
            //The UndoContext / Group system will be simplified soon...

            switch (CurrentMode)
            {
                case Mode.Asset:
                    return Assets;
                case Mode.Material:
                    return Materials;
                case Mode.Global:
                    return effectContainerFile;
                case Mode.ParticleNode:
                    return particleNode;
                case Mode.TraceNode:
                    return etrNodes;
                case Mode.ColorFadeNode:
                    return ecfNodes;
                default:
                    return null;
            }
        }
        #endregion

        #region Recolor
        private async Task PerformRecolorOperation()
        {
            if (Hue == lastRecolorHue && Saturation == lastRecolorSaturation && Lightness == lastRecolorLightness && Variance == lastRecolorVariance && TextureVariance == lastRecolorTextureVariance)
                return;

            if (recolorUndos.Count > 0)
                UndoRecolorOperation();

            lastRecolorHue = Hue;
            lastRecolorSaturation = Saturation;
            lastRecolorLightness = Lightness;
            lastRecolorVariance = _variance;
            lastRecolorTextureVariance = _textureVariance;

            hueChange = isHueSet ? lastRecolorHue : lastRecolorHue - initialHue;
            saturationChange = lastRecolorSaturation - initialSaturation;
            lightnessChange = lastRecolorLightness - initialLightness;

            List<IUndoRedo> undos = new List<IUndoRedo>(CurrentMode == Mode.Global ? 2048 : 64);

            if (CurrentMode == Mode.Asset)
            {
                await ChangeHueForAssets(Assets, undos);
            }
            else if (CurrentMode == Mode.Material)
            {
                ChangeHueForMaterials(Materials, undos);
            }
            else if (CurrentMode == Mode.Global)
            {
                await ChangeHueForEverything(undos);
            }
            else if (CurrentMode == Mode.ParticleNode)
            {
                particleNode.ChangeHue(hueChange, saturationChange, lightnessChange, undos);

                if (IncludeMaterials)
                {
                    ChangeHueForMaterials(particleNode.GetUsedMaterials(), undos);
                }
            }
            else if (CurrentMode == Mode.TraceNode)
            {
                foreach (var node in etrNodes)
                {
                    node.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance);

                    if (IncludeMaterials && node.MaterialRef != null)
                    {
                        node.MaterialRef.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance, ShiftGlareColor);
                    }

                }
            }
            else if (CurrentMode == Mode.ColorFadeNode)
            {
                foreach (var node in ecfNodes)
                {
                    node.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance);
                }
            }

            recolorUndos.AddRange(undos);
            //UndoManager.Instance.ForceEventCall(UndoGroup.ColorControl, null, GetContext());

        }
        
        private async Task ChangeHueForAsset(Asset _asset, List<IUndoRedo> undos)
        {
            double saturationChange = CurrentMode == Mode.Global ? SaturationChangeMulti : this.saturationChange;

            switch (_asset.AssetType)
            {
                case AssetType.PBIND:
                    _asset.Files[0].EmpFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance);

                    if (IncludeMaterials)
                    {
                        ChangeHueForMaterials(_asset.Files[0].EmpFile.GetUsedMaterials(), undos);
                    }
                    break;
                case AssetType.TBIND:
                    _asset.Files[0].EtrFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance);
                    break;
                case AssetType.CBIND:
                    _asset.Files[0].EcfFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance);
                    break;
                case AssetType.LIGHT:
                    _asset.Files[0].EmaFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance);
                    break;
                case AssetType.EMO:
                    foreach (EffectFile file in _asset.Files)
                    {
                        switch (file.Extension)
                        {
                            case ".emb":
                                await file.EmbFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorTextureVariance ? lastRecolorVariance : 0); //No lightness change
                                break;
                            case ".emm":
                                file.EmmFile.ChangeHsl(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance, ShiftGlareColor);
                                break;
                            case ".mat.ema":
                                EMM_File emmFile = _asset.Files.FirstOrDefault(x => x.fileType == EffectFile.FileType.EMM)?.EmmFile;
                                file.EmaFile.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance, emmFile);
                                break;
                        }
                    }
                    break;
            }

        }

        private async Task ChangeHueForEverything(List<IUndoRedo> undos)
        {
            await ChangeHueForAssets(effectContainerFile.Pbind.Assets, undos);
            await ChangeHueForAssets(effectContainerFile.Tbind.Assets, undos);
            await ChangeHueForAssets(effectContainerFile.Cbind.Assets, undos);
            await ChangeHueForAssets(effectContainerFile.Emo.Assets, undos);
            await ChangeHueForAssets(effectContainerFile.LightEma.Assets, undos);
            await effectContainerFile.Pbind.File3_Ref.ChangeHue(hueChange, SaturationChangeMulti, lightnessChange, undos, isHueSet, lastRecolorTextureVariance ? lastRecolorVariance : 0);
            await effectContainerFile.Tbind.File3_Ref.ChangeHue(hueChange, SaturationChangeMulti, lightnessChange, undos, isHueSet, lastRecolorTextureVariance ? lastRecolorVariance : 0);
            effectContainerFile.Pbind.File2_Ref.ChangeHsl(hueChange, SaturationChangeMulti, lightnessChange, undos, isHueSet, lastRecolorVariance, ShiftGlareColor);
            effectContainerFile.Tbind.File2_Ref.ChangeHsl(hueChange, SaturationChangeMulti, lightnessChange, undos, isHueSet, lastRecolorVariance, ShiftGlareColor);
        }

        private async Task ChangeHueForAssets(IList<Asset> assets, List<IUndoRedo> undos)
        {
            foreach (Asset _asset in assets)
            {
                await ChangeHueForAsset(_asset, undos);
            }
        }

        private void ChangeHueForMaterials(IList<EmmMaterial> materials, List<IUndoRedo> undos)
        {
            foreach (var material in materials)
            {
                material.ChangeHue(hueChange, saturationChange, lightnessChange, undos, isHueSet, lastRecolorVariance, ShiftGlareColor);
            }
        }

        private bool IsRecolorValid()
        {
            if (recolorUndos.Count == 0) return false;
            return HaveRecolorValuesChanged();
        }

        private bool HaveRecolorValuesChanged()
        {
            return !MathHelpers.FloatEquals(Hue, lastRecolorHue) || !MathHelpers.FloatEquals(Saturation, lastRecolorSaturation) || !MathHelpers.FloatEquals(Lightness, lastRecolorLightness)
                || Variance != lastRecolorVariance || TextureVariance != lastRecolorTextureVariance;
        }
        #endregion

        #region Control Events
        private async void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (recolorUndos.Count == 0 || HaveRecolorValuesChanged())
            {
                await PerformRecolorOperation(); 
                UndoManager.Instance.ForceEventCall(UndoGroup.ColorControl, null, GetContext());
            }

            initUndos.AddRange(recolorUndos);
            UndoManager.Instance.AddUndo(new CompositeUndo(initUndos, "Hue Adjustment"), UndoGroup.ColorControl, null, GetContext());
            recolorUndos.Clear(); //Important to clear this - if undos exist when the window is closed, they will be undone
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (recolorUndos.Count > 0)
            {
                UndoRecolorOperation();
            }
        }

        private void ValueChanged()
        {
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

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ValueChanged();

        private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e) => ValueChanged();

        #endregion

        #if !XenoKit
        //To be implemented in XenoKit
        private void XenoKitInit() { }
        #endif

    }
}