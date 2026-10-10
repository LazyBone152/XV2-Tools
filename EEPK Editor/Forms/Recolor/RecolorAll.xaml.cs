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
    public partial class RecolorAll : AutoObservableWindow
    {
        private enum Mode
        {
            Default,
            AssetContainer,
        }

        private readonly IEnumerable<IHueAdjustment> RecolorItems = null;
        private readonly AssetContainerTool AssetContainer = null;
        private readonly AssetType AssetType = AssetType.EMO;
        private readonly bool IsGlobal = false;
        private readonly bool IsMaterial = false;
        private List<EmmMaterial> UsedMaterials = null;


        private readonly Mode CurrentMode = Mode.AssetContainer;
        private readonly bool IsHueSet = false;
        private readonly string RecolorItemName;

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
            IsMaterial || IsGlobal || ((CurrentMode == Mode.AssetContainer && AssetType == AssetType.EMO) || AssetType == AssetType.TBIND || AssetType == AssetType.PBIND)
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
        public RecolorAll(IHueAdjustment recolorItem, bool isHueSet, string recolorItemName)
        {
            RecolorItemName = recolorItemName;
            IsHueSet = isHueSet;
            CurrentMode = Mode.Default;
            RecolorItems = new IHueAdjustment[] { recolorItem };
            IsGlobal = recolorItem is EffectContainerFile;

            InitializeComponent();
            DataContext = this;
        }

        public RecolorAll(IEnumerable<IHueAdjustment> recolorItems, bool isHueSet, string recolorItemName)
        {
            RecolorItemName = recolorItemName;
            CurrentMode = Mode.Default;
            RecolorItems = recolorItems;
            IsHueSet = isHueSet;

            foreach(var item in recolorItems)
            {
                IsMaterial = item is EmmMaterial;
                break;
            }

            InitializeComponent();
            DataContext = this;
        }

        public RecolorAll(AssetContainerTool assetContainer, ParticleNode recolorItem, bool isHueSet)
        {
            IsHueSet = isHueSet;
            CurrentMode = Mode.AssetContainer;
            AssetType = assetContainer.ContainerAssetType;
            AssetContainer = assetContainer;
            RecolorItems = new List<ParticleNode>(1) { recolorItem };
            UsedMaterials = GetUsedMaterials();
            RecolorItemName = GetContainerItemName();

            InitializeComponent();
            DataContext = this;
        }

        public RecolorAll(AssetContainerTool assetContainer, IEnumerable<IHueAdjustment> recolorItems, bool isHueSet)
        {
            IsHueSet = isHueSet;
            CurrentMode = Mode.AssetContainer;
            AssetType = assetContainer.ContainerAssetType;
            AssetContainer = assetContainer;
            RecolorItems = recolorItems;
            UsedMaterials = GetUsedMaterials();
            RecolorItemName = GetContainerItemName();

            InitializeComponent();
            DataContext = this;
        }

        private string GetContainerItemName()
        {
            foreach(var item in RecolorItems)
            {
                if(item is Asset)
                {
                    return $"{AssetType} Asset";
                }
                break;
            }

            switch (AssetType)
            {
                case AssetType.PBIND:
                    return "Particle Node";
                case AssetType.TBIND:
                    return "Trace Node";
                case AssetType.CBIND:
                    return "Color Fade Node";
            }

            return "";
        }
        #endregion

        #region Setup
        public bool Initialize()
        {
            if (IsHueSet)
            {
                helpTextBlock.Text = "Sets the hue value to the desired amount, keeping the saturation and lightness values the same. This will result in everything being different shades of the same color.";
                saturationGrid.Visibility = Visibility.Collapsed;
                lightnessGrid.Visibility = Visibility.Collapsed;
                varianceGrid.Visibility = Visibility.Visible;
                Title = "Hue Set";
            }
            else
            {
                helpTextBlock.Text = "Adjusts the hue, saturation and lightness (HSL) values, shifting them by the desired amount while preserving any color variation.";
                saturationGrid.Visibility = Visibility.Visible;
                lightnessGrid.Visibility = Visibility.Visible;
                varianceGrid.Visibility = Visibility.Collapsed;
                Title = "Hue Adjustment";
            }

            int count = RecolorItems.Count();
            Title += count > 1 ? $" ({count} {RecolorItemName}s)" : $" ({RecolorItemName})";


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

            if (RecolorItems is IList<Asset> assets)
            {
                if (!AssetContainer.AreMaterialsExclusive(assets))
                {
                    if (MessagePrompt.Show(NotExclusiveMessage, "Materials Not Exclusive", MessagePromptButtons.AffirmativeNegative, MessagePromptIcon.Question, "Make Copies", "Ignore") == MessagePromptResult.Affirmative)
                    {
                        AssetContainer.MakeMaterialsExclusive(assets, initUndos);
                    }
                }
            }
            else if(RecolorItems is IList<ParticleNode> particleNodes)
            {
                if (!AssetContainer.AreMaterialsExclusive(particleNodes[0]))
                {
                    if (MessagePrompt.Show(NotExclusiveMessage, "Materials Not Exclusive", MessagePromptButtons.AffirmativeNegative, MessagePromptIcon.Question, "Make Copies", "Ignore") == MessagePromptResult.Affirmative)
                    {
                        AssetContainer.MakeMaterialsExclusive(particleNodes[0], initUndos);
                    }
                }
            }
            else if(RecolorItems is IList<ETR_Node> etrNodes)
            {
                if (!AssetContainer.AreMaterialsExclusive(etrNodes))
                {
                    if (MessagePrompt.Show(NotExclusiveMessage, "Materials Not Exclusive", MessagePromptButtons.AffirmativeNegative, MessagePromptIcon.Question, "Make Copies", "Ignore") == MessagePromptResult.Affirmative)
                    {
                        AssetContainer.MakeMaterialsExclusive(etrNodes, initUndos);
                    }
                }
            }

            UsedMaterials = GetUsedMaterials();
        }

        private long CalculatePreviewBrushColor()
        {
            ColorAccumulator ??= new Xv2ColorAccumulator();
            ColorAccumulator.Clear();

            foreach(var recolorItem in RecolorItems)
            {
                recolorItem.AccumulateUsedColors(ColorAccumulator);
            }

            if (IncludeMaterials && UsedMaterials != null)
            {
                foreach(var material in UsedMaterials)
                {
                    material.AccumulateUsedColors(ColorAccumulator);
                }
            }

            PreviewColor = ColorAccumulator.ToCustomColor();
            return ColorAccumulator.NumColors;
        }

        private void UndoRecolorOperation(bool undoInit = false)
        {
            if (undoInit && initUndos.Count > 0)
            {
                for (int i = initUndos.Count - 1; i >= 0; i--)
                {
                    initUndos[i].Undo();
                }

                initUndos.Clear();
            }

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
            return RecolorItems;
        }
        
        private List<EmmMaterial> GetUsedMaterials()
        {
            List<EmmMaterial> materials = new List<EmmMaterial>(RecolorItems.Count());

            foreach(var recolorItem in RecolorItems)
            {
                if (recolorItem is ParticleNode particleNode)
                {
                    materials.AddRange(particleNode.GetUsedMaterials());
                }
                else if (recolorItem is ETR_Node etrNode && etrNode.MaterialRef != null)
                {
                    materials.Add(etrNode.MaterialRef);
                }
                else if (recolorItem is Asset asset)
                {
                    if (asset.AssetType == AssetType.PBIND)
                        materials.AddRange(asset.Files[0].EmpFile.GetUsedMaterials());
                    else if (asset.AssetType == AssetType.TBIND)
                        materials.AddRange(asset.Files[0].EtrFile.GetUsedMaterials());
                }
            }

            return materials.Distinct().ToList();
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

            hueChange = IsHueSet ? lastRecolorHue : lastRecolorHue - initialHue;
            saturationChange = lastRecolorSaturation - initialSaturation;
            lightnessChange = lastRecolorLightness - initialLightness;

            HueAdjustmentInput input = new HueAdjustmentInput(hueChange, saturationChange, lightnessChange, recolorUndos, IsHueSet, TextureVariance, Variance, 0, ShiftGlareColor);

            foreach(var recolorItem in RecolorItems)
            {
                await recolorItem.ApplyHueAdjustment(input);
            }

            if (IncludeMaterials && UsedMaterials != null)
            {
                foreach(var material in UsedMaterials)
                {
                    await material.ApplyHueAdjustment(input);
                }
            }

            //UndoManager.Instance.ForceEventCall(UndoGroup.ColorControl, null, GetContext());

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

            List<IUndoRedo> undos = new List<IUndoRedo>(initUndos.Count + recolorUndos.Count);
            undos.AddRange(initUndos);
            undos.AddRange(recolorUndos);
            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Hue Adjustment"), UndoGroup.ColorControl, null, GetContext());
            recolorUndos.Clear(); //Important to clear this - if undos exist when the window is closed, they will be undone
            initUndos.Clear();
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (recolorUndos.Count > 0 || initUndos.Count > 0)
            {
                UndoRecolorOperation(true);
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