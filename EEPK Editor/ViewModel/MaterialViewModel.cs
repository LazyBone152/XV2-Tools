using System;
using System.Reflection;
using Xv2CoreLib.EMM;
using Xv2CoreLib.Resource.UndoRedo;
using LB_Common.Numbers;
using GalaSoft.MvvmLight;

namespace EEPK_Organiser.ViewModel
{
    public class MaterialViewModel : ObservableObject, IDisposable
    {
        private readonly DecompiledMaterial material;

        //Glare
        public int Glare
        {
            get => material.Glare;
            set => SetIntValue(value, nameof(material.Glare));
        }
        public CustomColor GlareCol => material.GlareCol;

        //MatOffsets
        public CustomVector4 MatOffset0 => material.MatOffset0;
        public CustomVector4 MatOffset1 => material.MatOffset1;

        //MatScales
        public CustomVector4 MatScale0 => material.MatScale0;
        public CustomVector4 MatScale1 => material.MatScale1;

        //MatCols
        public CustomColor MatCol0 => material.MatCol0;
        public CustomColor MatCol1 => material.MatCol1;
        public CustomColor MatCol2 => material.MatCol2;
        public CustomColor MatCol3 => material.MatCol3;

        //Texture
        public CustomMatUV TexScrl0 => material.TexScrl0;
        public CustomMatUV TexScrl1 => material.TexScrl1;
        public CustomMatUV gScroll0 => material.gScroll0;
        public CustomMatUV gScroll1 => material.gScroll1;

        public CustomMatRepUV TexRep0 => material.TexRep0;
        public CustomMatRepUV TexRep1 => material.TexRep1;
        public CustomMatRepUV TexRep2 => material.TexRep2;
        public CustomMatRepUV TexRep3 => material.TexRep3;

        public int TextureFilter0
        {
            get => material.TextureFilter0;
            set => SetIntValue(value, nameof(material.TextureFilter0));
        }
        public int TextureFilter1
        {
            get => material.TextureFilter1;
            set => SetIntValue(value, nameof(material.TextureFilter1));
        }
        public int TextureFilter2
        {
            get => material.TextureFilter2;
            set => SetIntValue(value, nameof(material.TextureFilter2));
        }
        public int TextureFilter3
        {
            get => material.TextureFilter3;
            set => SetIntValue(value, nameof(material.TextureFilter3));
        }

        public float MipMapLod0
        {
            get => material.MipMapLod0;
            set => SetFloatValue(value, nameof(material.MipMapLod0));
        }
        public float MipMapLod1
        {
            get => material.MipMapLod1;
            set => SetFloatValue(value, nameof(material.MipMapLod1));
        }
        public float MipMapLod2
        {
            get => material.MipMapLod2;
            set => SetFloatValue(value, nameof(material.MipMapLod2));
        }
        public float MipMapLod3
        {
            get => material.MipMapLod3;
            set => SetFloatValue(value, nameof(material.MipMapLod3));
        }

        //Other texture shit
        public float gToonTextureWidth
        {
            get => material.gToonTextureWidth;
            set => SetFloatValue(value, nameof(material.gToonTextureWidth));
        }
        public float gToonTextureHeight
        {
            get => material.gToonTextureHeight;
            set => SetFloatValue(value, nameof(material.gToonTextureHeight));
        }
        public CustomMatUV ToonSamplerAddress => material.ToonSamplerAddress;
        public CustomMatUV MarkSamplerAddress => material.MarkSamplerAddress;
        public CustomMatUV MaskSamplerAddress => material.MaskSamplerAddress;

        //MatDiff/Amb
        public CustomColor MatDif => material.MatDif;
        public CustomColor MatAmb => material.MatAmb;
        public CustomColor MatSpc => material.MatSpc;
        public float MatDifScale
        {
            get => material.MatDifScale;
            set => SetFloatValue(value, nameof(material.MatDifScale));
        }
        public float MatAmbScale
        {
            get => material.MatAmbScale;
            set => SetFloatValue(value, nameof(material.MatAmbScale));
        }
        public float SpcCoeff
        {
            get => material.SpcCoeff;
            set => SetFloatValue(value, nameof(material.SpcCoeff));
        }
        public float SpcPower
        {
            get => material.SpcPower;
            set => SetFloatValue(value, nameof(material.SpcPower));
        }

        //Alpha
        public int AlphaBlend
        {
            get => material.AlphaBlend;
            set => SetIntValue(value, nameof(material.AlphaBlend));
        }
        public int AlphaBlendType
        {
            get => material.AlphaBlendType;
            set => SetIntValue(value, nameof(material.AlphaBlendType));
        }
        public int AlphaTest
        {
            get => material.AlphaTest;
            set => SetIntValue(value, nameof(material.AlphaTest));
        }
        public bool AlphaTestThreshold
        {
            get => material.AlphaTestThreshold;
            set => SetBoolValue(value, nameof(material.AlphaTestThreshold));
        }
        public bool AlphaRef
        {
            get => material.AlphaRef;
            set => SetBoolValue(value, nameof(material.AlphaRef));
        }

        //Mask
        public int AlphaSortMask
        {
            get => material.AlphaSortMask;
            set => SetIntValue(value, nameof(material.AlphaSortMask));
        }
        public int ZTestMask
        {
            get => material.ZTestMask;
            set => SetIntValue(value, nameof(material.ZTestMask));
        }
        public int ZWriteMask
        {
            get => material.ZWriteMask;
            set => SetIntValue(value, nameof(material.ZWriteMask));
        }


        //Flags
        public bool VsFlag0
        {
            get => material.VsFlag0;
            set => SetBoolValue(value, nameof(material.VsFlag0));
        }
        public bool VsFlag1
        {
            get => material.VsFlag1;
            set => SetBoolValue(value, nameof(material.VsFlag1));
        }
        public bool VsFlag2
        {
            get => material.VsFlag2;
            set => SetBoolValue(value, nameof(material.VsFlag2));
        }
        public bool VsFlag3
        {
            get => material.VsFlag3;
            set => SetBoolValue(value, nameof(material.VsFlag3));
        }
        public int CustomFlag
        {
            get => material.CustomFlag;
            set => SetIntValue(value, nameof(material.CustomFlag));
        }

        //Culling
        public int BackFace
        {
            get => material.BackFace;
            set => SetIntValue(value, nameof(material.BackFace));
        }
        public int TwoSidedRender
        {
            get => material.TwoSidedRender;
            set => SetIntValue(value, nameof(material.TwoSidedRender));
        }

        //LowRez
        public int LowRez
        {
            get => material.LowRez;
            set => SetIntValue(value, nameof(material.LowRez));
        }
        public int LowRezSmoke
        {
            get => material.LowRezSmoke;
            set => SetIntValue(value, nameof(material.LowRezSmoke));
        }

        //Incidence
        public float IncidencePower
        {
            get => material.IncidencePower;
            set => SetFloatValue(value, nameof(material.IncidencePower));
        }
        public float IncidenceAlphaBias
        {
            get => material.IncidenceAlphaBias;
            set => SetFloatValue(value, nameof(material.IncidenceAlphaBias));
        }

        //Billboard
        public int Billboard
        {
            get => material.Billboard;
            set => SetIntValue(value, nameof(material.Billboard));
        }
        public int BillboardType
        {
            get => material.BillboardType;
            set => SetIntValue(value, nameof(material.BillboardType));
        }

        //Reflect
        public float ReflectCoeff
        {
            get => material.ReflectCoeff;
            set => SetFloatValue(value, nameof(material.ReflectCoeff));
        }
        public float ReflectFresnelBias
        {
            get => material.ReflectFresnelBias;
            set => SetFloatValue(value, nameof(material.ReflectFresnelBias));
        }
        public float ReflectFresnelCoeff
        {
            get => material.ReflectFresnelCoeff;
            set => SetFloatValue(value, nameof(material.ReflectFresnelCoeff));
        }

        //Other
        public int AnimationChannel
        {
            get => material.AnimationChannel;
            set => SetIntValue(value, nameof(material.AnimationChannel));
        }
        public int NoEdge
        {
            get => material.NoEdge;
            set => SetIntValue(value, nameof(material.NoEdge));
        }
        public int Shimmer
        {
            get => material.Shimmer;
            set => SetIntValue(value, nameof(material.Shimmer));
        }
        public float gTime
        {
            get => material.gTime;
            set => SetFloatValue(value, nameof(material.gTime));
        }

        //Light
        public CustomColor gLightDir => material.gLightDir;
        public CustomColor gLightDif => material.gLightDif;
        public CustomColor gLightSpc => material.gLightSpc;
        public CustomColor gLightAmb => material.gLightAmb;
        public CustomVector4 gCamPos => material.gCamPos;
        public CustomColor DirLight0Dir => material.DirLight0Dir;
        public CustomColor DirLight0Col => material.DirLight0Col;
        public CustomColor AmbLight0Col => material.AmbLight0Col;

        //Other
        public CustomColor Ambient => material.Ambient;
        public CustomColor Diffuse => material.Diffuse;
        public CustomColor Specular => material.Specular;
        public float SpecularPower
        {
            get => material.SpecularPower;
            set => SetFloatValue(value, nameof(material.SpecularPower));
        }

        //Fade/Gradient
        public float FadeInit
        {
            get => material.FadeInit;
            set => SetFloatValue(value, nameof(material.FadeInit));
        }
        public float FadeSpeed
        {
            get => material.FadeSpeed;
            set => SetFloatValue(value, nameof(material.FadeSpeed));
        }
        public float GradientInit
        {
            get => material.GradientInit;
            set => SetFloatValue(value, nameof(material.GradientInit));
        }
        public float GradientSpeed
        {
            get => material.GradientSpeed;
            set => SetFloatValue(value, nameof(material.GradientSpeed));
        }
        public CustomColor gGradientCol => material.gGradientCol;

        //Rim
        public float RimCoeff
        {
            get => material.RimCoeff;
            set => SetFloatValue(value, nameof(material.RimCoeff));
        }
        public float RimPower
        {
            get => material.RimPower;
            set => SetFloatValue(value, nameof(material.RimPower));
        }



        public MaterialViewModel(DecompiledMaterial material)
        {
            this.material = material;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        public void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            PropertyInfo[] props = GetType().GetProperties();

            foreach(var prop in props)
            {
                RaisePropertyChanged(prop.Name);
            }
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        #region UndoableSetMethods
        private void SetIntValue(int newValue, string fieldName)
        {
            int original = (int)material.GetType().GetField(fieldName).GetValue(material);

            if(original != newValue)
            {
                material.GetType().GetField(fieldName).SetValue(material, newValue);

                UndoManager.Instance.AddUndo(new UndoableField(fieldName, material, original, newValue, $"{fieldName}"));
            }
        }

        private void SetBoolValue(bool newValue, string fieldName)
        {
            bool original = (bool)material.GetType().GetField(fieldName).GetValue(material);

            if (original != newValue)
            {
                material.GetType().GetField(fieldName).SetValue(material, newValue);

                UndoManager.Instance.AddUndo(new UndoableField(fieldName, material, original, newValue, $"{fieldName}"));
            }
        }

        private void SetFloatValue(float newValue, string fieldName)
        {
            float original = (float)material.GetType().GetField(fieldName).GetValue(material);

            if (original != newValue)
            {
                material.GetType().GetField(fieldName).SetValue(material, newValue);

                UndoManager.Instance.AddUndo(new UndoableField(fieldName, material, original, newValue, $"{fieldName}"));
            }
        }
        
        #endregion
    }
}
