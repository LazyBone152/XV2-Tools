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
        private static readonly DecompiledMaterial DefaultDecompiledMaterial = DecompiledMaterial.Default();
        private DecompiledMaterial _material;
        private DecompiledMaterial CurrentMaterial
        {
            get => _material != null ? _material : DefaultDecompiledMaterial;
        }

        //Glare
        public bool Glare
        {
            get => CurrentMaterial.Glare == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.Glare));
        }
        public CustomColor GlareCol => CurrentMaterial.GlareCol;

        //MatOffsets
        public CustomVector4 MatOffset0 => CurrentMaterial.MatOffset0;
        public CustomVector4 MatOffset1 => CurrentMaterial.MatOffset1;

        //MatScales
        public CustomVector4 MatScale0 => CurrentMaterial.MatScale0;
        public CustomVector4 MatScale1 => CurrentMaterial.MatScale1;

        //MatCols
        public CustomColor MatCol0 => CurrentMaterial.MatCol0;
        public CustomColor MatCol1 => CurrentMaterial.MatCol1;
        public CustomColor MatCol2 => CurrentMaterial.MatCol2;
        public CustomColor MatCol3 => CurrentMaterial.MatCol3;

        //Texture
        public CustomMatUV TexScrl0 => CurrentMaterial.TexScrl0;
        public CustomMatUV TexScrl1 => CurrentMaterial.TexScrl1;
        public CustomMatUV gScroll0 => CurrentMaterial.gScroll0;
        public CustomMatUV gScroll1 => CurrentMaterial.gScroll1;

        public CustomMatRepUV TexRep0 => CurrentMaterial.TexRep0;
        public CustomMatRepUV TexRep1 => CurrentMaterial.TexRep1;
        public CustomMatRepUV TexRep2 => CurrentMaterial.TexRep2;
        public CustomMatRepUV TexRep3 => CurrentMaterial.TexRep3;

        public int TextureFilter0
        {
            get => CurrentMaterial.TextureFilter0;
            set => SetIntValue(value, nameof(CurrentMaterial.TextureFilter0));
        }
        public int TextureFilter1
        {
            get => CurrentMaterial.TextureFilter1;
            set => SetIntValue(value, nameof(CurrentMaterial.TextureFilter1));
        }
        public int TextureFilter2
        {
            get => CurrentMaterial.TextureFilter2;
            set => SetIntValue(value, nameof(CurrentMaterial.TextureFilter2));
        }
        public int TextureFilter3
        {
            get => CurrentMaterial.TextureFilter3;
            set => SetIntValue(value, nameof(CurrentMaterial.TextureFilter3));
        }

        public float MipMapLod0
        {
            get => CurrentMaterial.MipMapLod0;
            set => SetFloatValue(value, nameof(CurrentMaterial.MipMapLod0));
        }
        public float MipMapLod1
        {
            get => CurrentMaterial.MipMapLod1;
            set => SetFloatValue(value, nameof(CurrentMaterial.MipMapLod1));
        }
        public float MipMapLod2
        {
            get => CurrentMaterial.MipMapLod2;
            set => SetFloatValue(value, nameof(CurrentMaterial.MipMapLod2));
        }
        public float MipMapLod3
        {
            get => CurrentMaterial.MipMapLod3;
            set => SetFloatValue(value, nameof(CurrentMaterial.MipMapLod3));
        }

        //Other texture shit
        public float gToonTextureWidth
        {
            get => CurrentMaterial.gToonTextureWidth;
            set => SetFloatValue(value, nameof(CurrentMaterial.gToonTextureWidth));
        }
        public float gToonTextureHeight
        {
            get => CurrentMaterial.gToonTextureHeight;
            set => SetFloatValue(value, nameof(CurrentMaterial.gToonTextureHeight));
        }
        public CustomMatUV ToonSamplerAddress => CurrentMaterial.ToonSamplerAddress;
        public CustomMatUV MarkSamplerAddress => CurrentMaterial.MarkSamplerAddress;
        public CustomMatUV MaskSamplerAddress => CurrentMaterial.MaskSamplerAddress;

        //MatDiff/Amb
        public CustomColor MatDif => CurrentMaterial.MatDif;
        public CustomColor MatAmb => CurrentMaterial.MatAmb;
        public CustomColor MatSpc => CurrentMaterial.MatSpc;
        public float MatDifScale
        {
            get => CurrentMaterial.MatDifScale;
            set => SetFloatValue(value, nameof(CurrentMaterial.MatDifScale));
        }
        public float MatAmbScale
        {
            get => CurrentMaterial.MatAmbScale;
            set => SetFloatValue(value, nameof(CurrentMaterial.MatAmbScale));
        }
        public float SpcCoeff
        {
            get => CurrentMaterial.SpcCoeff;
            set => SetFloatValue(value, nameof(CurrentMaterial.SpcCoeff));
        }
        public float SpcPower
        {
            get => CurrentMaterial.SpcPower;
            set => SetFloatValue(value, nameof(CurrentMaterial.SpcPower));
        }

        //Alpha
        public bool AlphaBlend
        {
            get => CurrentMaterial.AlphaBlend == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.AlphaBlend), 2);
        }
        public AlphaBlendType AlphaBlendType
        {
            get => (AlphaBlendType)CurrentMaterial.AlphaBlendType;
            set => SetIntValue((int)value, nameof(CurrentMaterial.AlphaBlendType), 2);
        }
        public int AlphaTest
        {
            get => CurrentMaterial.AlphaTest;
            set => SetIntValue(value, nameof(CurrentMaterial.AlphaTest));
        }
        public bool AlphaTestThreshold
        {
            get => CurrentMaterial.AlphaTestThreshold;
            set => SetBoolValue(value, nameof(CurrentMaterial.AlphaTestThreshold));
        }
        public bool AlphaRef
        {
            get => CurrentMaterial.AlphaRef;
            set => SetBoolValue(value, nameof(CurrentMaterial.AlphaRef));
        }

        //Mask
        public bool AlphaSortMask
        {
            get => CurrentMaterial.AlphaSortMask == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.AlphaSortMask), 2);
        }
        public bool ZTestMask
        {
            get => CurrentMaterial.ZTestMask == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.ZTestMask), 2);
        }
        public bool ZWriteMask
        {
            get => CurrentMaterial.ZWriteMask == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.ZWriteMask));
        }


        //Flags
        public bool VsFlag0
        {
            get => CurrentMaterial.VsFlag0;
            set => SetBoolValue(value, nameof(CurrentMaterial.VsFlag0));
        }
        public bool VsFlag1
        {
            get => CurrentMaterial.VsFlag1;
            set => SetBoolValue(value, nameof(CurrentMaterial.VsFlag1));
        }
        public bool VsFlag2
        {
            get => CurrentMaterial.VsFlag2;
            set => SetBoolValue(value, nameof(CurrentMaterial.VsFlag2));
        }
        public bool VsFlag3
        {
            get => CurrentMaterial.VsFlag3;
            set => SetBoolValue(value, nameof(CurrentMaterial.VsFlag3));
        }
        public int CustomFlag
        {
            get => CurrentMaterial.CustomFlag;
            set => SetIntValue(value, nameof(CurrentMaterial.CustomFlag));
        }

        //Culling
        public bool BackFace
        {
            get => CurrentMaterial.BackFace == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.BackFace), 2);
        }
        public bool TwoSidedRender
        {
            get => CurrentMaterial.TwoSidedRender == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.TwoSidedRender), 2);
        }

        //LowRez
        public bool LowRez
        {
            get => CurrentMaterial.LowRez == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.LowRez));
        }
        public bool LowRezSmoke
        {
            get => CurrentMaterial.LowRezSmoke == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.LowRezSmoke));
        }
        public int RenderRes
        {
            get
            {
                if (LowRez) return 1;
                if (LowRezSmoke) return 2;
                return 0;
            }
            set
            {
                int lowRez = CurrentMaterial.LowRez;
                int lowRezSmoke = CurrentMaterial.LowRezSmoke;

                if (value == 0)
                {
                    CurrentMaterial.LowRez = 0;
                    CurrentMaterial.LowRezSmoke = 0;
                }
                else if (value == 1)
                {
                    CurrentMaterial.LowRez = 1;
                    CurrentMaterial.LowRezSmoke = 0;
                }
                if (value == 2)
                {
                    CurrentMaterial.LowRez = 0;
                    CurrentMaterial.LowRezSmoke = 1;
                }

                if(lowRez != CurrentMaterial.LowRez || lowRezSmoke != CurrentMaterial.LowRezSmoke)
                {
                    UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                    {
                        new UndoableField(nameof(CurrentMaterial.LowRez), CurrentMaterial, lowRez, CurrentMaterial.LowRez),
                        new UndoableField(nameof(CurrentMaterial.LowRezSmoke), CurrentMaterial, lowRezSmoke, CurrentMaterial.LowRezSmoke),
                        new UndoablePropertyGeneric(nameof(CurrentMaterial.ParametersChanged), CurrentMaterial, 1, 1)
                    }, $"Render Res");
                }
            }
        }

        //Incidence
        public float IncidencePower
        {
            get => CurrentMaterial.IncidencePower;
            set => SetFloatValue(value, nameof(CurrentMaterial.IncidencePower));
        }
        public float IncidenceAlphaBias
        {
            get => CurrentMaterial.IncidenceAlphaBias;
            set => SetFloatValue(value, nameof(CurrentMaterial.IncidenceAlphaBias));
        }

        //Billboard
        public bool Billboard
        {
            get => CurrentMaterial.Billboard == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.Billboard));
        }
        public int BillboardType
        {
            get => CurrentMaterial.BillboardType;
            set => SetIntValue(value, nameof(CurrentMaterial.BillboardType));
        }

        //Reflect
        public float ReflectCoeff
        {
            get => CurrentMaterial.ReflectCoeff;
            set => SetFloatValue(value, nameof(CurrentMaterial.ReflectCoeff));
        }
        public float ReflectFresnelBias
        {
            get => CurrentMaterial.ReflectFresnelBias;
            set => SetFloatValue(value, nameof(CurrentMaterial.ReflectFresnelBias));
        }
        public float ReflectFresnelCoeff
        {
            get => CurrentMaterial.ReflectFresnelCoeff;
            set => SetFloatValue(value, nameof(CurrentMaterial.ReflectFresnelCoeff));
        }

        //Other
        public int AnimationChannel
        {
            get => CurrentMaterial.AnimationChannel;
            set => SetIntValue(value, nameof(CurrentMaterial.AnimationChannel));
        }
        public bool NoEdge
        {
            get => CurrentMaterial.NoEdge == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.NoEdge));
        }
        public bool Shimmer
        {
            get => CurrentMaterial.Shimmer == 1;
            set => SetIntBoolValue(value, nameof(CurrentMaterial.Shimmer));
        }
        public float gTime
        {
            get => CurrentMaterial.gTime;
            set => SetFloatValue(value, nameof(CurrentMaterial.gTime));
        }

        //Light
        public CustomColor gLightDir => CurrentMaterial.gLightDir;
        public CustomColor gLightDif => CurrentMaterial.gLightDif;
        public CustomColor gLightSpc => CurrentMaterial.gLightSpc;
        public CustomColor gLightAmb => CurrentMaterial.gLightAmb;
        public CustomVector4 gCamPos => CurrentMaterial.gCamPos;
        public CustomColor DirLight0Dir => CurrentMaterial.DirLight0Dir;
        public CustomColor DirLight0Col => CurrentMaterial.DirLight0Col;
        public CustomColor AmbLight0Col => CurrentMaterial.AmbLight0Col;

        //Other
        public CustomColor Ambient => CurrentMaterial.Ambient;
        public CustomColor Diffuse => CurrentMaterial.Diffuse;
        public CustomColor Specular => CurrentMaterial.Specular;
        public float SpecularPower
        {
            get => CurrentMaterial.SpecularPower;
            set => SetFloatValue(value, nameof(CurrentMaterial.SpecularPower));
        }

        //Fade/Gradient
        public float FadeInit
        {
            get => CurrentMaterial.FadeInit;
            set => SetFloatValue(value, nameof(CurrentMaterial.FadeInit));
        }
        public float FadeSpeed
        {
            get => CurrentMaterial.FadeSpeed;
            set => SetFloatValue(value, nameof(CurrentMaterial.FadeSpeed));
        }
        public float GradientInit
        {
            get => CurrentMaterial.GradientInit;
            set => SetFloatValue(value, nameof(CurrentMaterial.GradientInit));
        }
        public float GradientSpeed
        {
            get => CurrentMaterial.GradientSpeed;
            set => SetFloatValue(value, nameof(CurrentMaterial.GradientSpeed));
        }
        public CustomColor gGradientCol => CurrentMaterial.gGradientCol;

        //Rim
        public float RimCoeff
        {
            get => CurrentMaterial.RimCoeff;
            set => SetFloatValue(value, nameof(CurrentMaterial.RimCoeff));
        }
        public float RimPower
        {
            get => CurrentMaterial.RimPower;
            set => SetFloatValue(value, nameof(CurrentMaterial.RimPower));
        }



        public MaterialViewModel()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void SetMaterial(DecompiledMaterial material)
        {
            _material = material;
        }

        private void Instance_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
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
        private void SetIntBoolValue(bool newValue, string fieldName, int changeType = 1)
        {
            int value = newValue ? 1 : 0;
            SetIntValue(value, fieldName, changeType);
            RaisePropertyChanged(fieldName);
        }

        private void SetIntValue(int newValue, string fieldName, int changeType = 1)
        {
            int original = (int)CurrentMaterial.GetType().GetField(fieldName).GetValue(CurrentMaterial);

            if(original != newValue)
            {
                CurrentMaterial.GetType().GetField(fieldName).SetValue(CurrentMaterial, newValue);
                CurrentMaterial.ParametersChanged = changeType;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableField(fieldName, CurrentMaterial, original, newValue),
                    new UndoablePropertyGeneric(nameof(CurrentMaterial.ParametersChanged), CurrentMaterial, changeType, changeType)
                }, $"{fieldName}");
            }
        }

        private void SetBoolValue(bool newValue, string fieldName, int changeType = 1)
        {
            bool original = (bool)CurrentMaterial.GetType().GetField(fieldName).GetValue(CurrentMaterial);

            if (original != newValue)
            {
                CurrentMaterial.GetType().GetField(fieldName).SetValue(CurrentMaterial, newValue);
                CurrentMaterial.ParametersChanged = changeType;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableField(fieldName, CurrentMaterial, original, newValue),
                    new UndoablePropertyGeneric(nameof(CurrentMaterial.ParametersChanged), CurrentMaterial, changeType, changeType)
                }, $"{fieldName}");
            }
        }

        private void SetFloatValue(float newValue, string fieldName, int changeType = 1)
        {
            float original = (float)CurrentMaterial.GetType().GetField(fieldName).GetValue(CurrentMaterial);

            if (original != newValue)
            {
                CurrentMaterial.GetType().GetField(fieldName).SetValue(CurrentMaterial, newValue);
                CurrentMaterial.ParametersChanged = changeType;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableField(fieldName, CurrentMaterial, original, newValue),
                    new UndoablePropertyGeneric(nameof(CurrentMaterial.ParametersChanged), CurrentMaterial, changeType, changeType)
                }, $"{fieldName}");
            }
        }
        
        #endregion
    }
}
