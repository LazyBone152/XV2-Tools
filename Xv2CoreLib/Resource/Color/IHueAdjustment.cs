using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.Color
{
    public interface IHueAdjustment
    {
        void AccumulateUsedColors(Xv2ColorAccumulator accumulator);

        Task ApplyHueAdjustment(HueAdjustmentInput input);
    }

    public class HueAdjustmentInput
    {
        public double Hue { get; private set; }
        public double Saturation { get; private set; }
        public double Lightness { get; private set; }
        public bool HueSet { get; private set; }
        public bool TextureVariance { get; private set; }
        public int Variance { get; private set; }
        public int GlareAdd { get; private set; }
        public bool IncludeGlareCol { get; private set; }
        public List<IUndoRedo> Undos { get; private set; }

        public HueAdjustmentInput(double hue, double saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, bool textureVariance = true, int variance = 0, int glareAdd = 0, bool includeGlareCol = true)
        {
            Hue = hue;
            Saturation = saturation;
            Lightness = lightness;
            Undos = undos ?? new List<IUndoRedo>();
            HueSet = hueSet;
            Variance = variance;
            GlareAdd = glareAdd;
            IncludeGlareCol = includeGlareCol;
            TextureVariance = textureVariance;
        }
    }
}
