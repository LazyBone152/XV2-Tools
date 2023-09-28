using System;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using LB_Common.Numbers;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.EMM
{
    /*
    Structure of values in EMM:
    
    UV Values:
    {Name}{U,V}{Index}
    {Name}{Index}{U,V}

    Vectors:
    {Name}{Index}{X,Y,Z,W}

    Colors:
    {Name}{Index}{R,G,B,A}
    
    When the {U,V} section is missing, then the value applies to both U and V.
    When the {Index} section is missing, then the value applies to all indices.
    The {Index} section is never missing from Vectors and Colors, just UV values. 
    
    The direct name of the shader parameter can also be used in some cases. Such as g_MaterialOffset0_VS... but I'm unsure how this works yet since that is a Vector4, with no axis defined...

    */
    [Serializable]
    public class DecompiledMaterial : INotifyPropertyChanged
    {
        #region NotifyPropChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region CachedValues
        /// <summary>
        /// Used for creating default <see cref="DecompiledMaterial"/> objects. Cached here for performance reasons.
        /// </summary>
        private static readonly EmmMaterial DefaultMaterial = new EmmMaterial();
        private static readonly FieldInfo[] Fields;
        private static readonly ParameterGroup[] Groups;

        static DecompiledMaterial()
        {
            Fields = typeof(DecompiledMaterial).GetFields();
            Groups = new ParameterGroup[Fields.Length];

            for (int i = 0; i < Fields.Length; i++)
            {
                ParameterGroupAttribute attr = (ParameterGroupAttribute)Fields[i].GetCustomAttribute(typeof(ParameterGroupAttribute));

                if (attr != null)
                {
                    Groups[i] = attr.Group;
                }
                else
                {
                    Groups[i] = ParameterGroup.Unsorted;
                }
            }
        }
        #endregion

        //Glare
        [ParameterGroup(ParameterGroup.Color)]
        public int Glare;
        [ParameterGroup(ParameterGroup.Color)]
        public CustomColor GlareCol;

        //MatOffsets
        [ParameterGroup(ParameterGroup.MatScaleOffset)]
        public CustomVector4 MatOffset0;
        [ParameterGroup(ParameterGroup.MatScaleOffset)]
        public CustomVector4 MatOffset1;

        //MatScales
        [ParameterGroup(ParameterGroup.MatScaleOffset)]
        public CustomVector4 MatScale0;
        [ParameterGroup(ParameterGroup.MatScaleOffset)]
        public CustomVector4 MatScale1;

        //MatCols
        [ParameterGroup(ParameterGroup.Color)]
        public CustomColor MatCol0;
        [ParameterGroup(ParameterGroup.Color)]
        public CustomColor MatCol1;
        [ParameterGroup(ParameterGroup.Color)]
        public CustomColor MatCol2;
        [ParameterGroup(ParameterGroup.Color)]
        public CustomColor MatCol3;

        //Texture Scroll
        [ParameterGroup(ParameterGroup.Texture)]
        public CustomMatUV TexScrl0;
        [ParameterGroup(ParameterGroup.Texture)]
        public CustomMatUV TexScrl1;

        //Texture Sampler parameters
        //In these values the index lines up with a sampler index. So in theory, the limit is 4, but in vanilla EMM files only 3 is used.
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        [ParameterGroup(ParameterGroup.Texture)]
        public CustomMatRepUV TexRep0;
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        [ParameterGroup(ParameterGroup.Texture)]
        public CustomMatRepUV TexRep1;
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        [ParameterGroup(ParameterGroup.Texture)]
        public CustomMatRepUV TexRep2;
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        public CustomMatRepUV TexRep3;
        [ParameterGroup(ParameterGroup.Texture)]
        public int TextureFilter0;
        [ParameterGroup(ParameterGroup.Texture)]
        public int TextureFilter1;
        [ParameterGroup(ParameterGroup.Texture)]
        public int TextureFilter2;
        public int TextureFilter3;
        [ParameterGroup(ParameterGroup.Texture)]
        public float MipMapLod0; //"Float2" type in EMM (so are 1,2,3).
        [ParameterGroup(ParameterGroup.Texture)]
        public float MipMapLod1;
        [ParameterGroup(ParameterGroup.Texture)]
        public float MipMapLod2;
        public float MipMapLod3;

        //Other Texture Parameters:
        [ParameterGroup(ParameterGroup.Texture)]
        public float gToonTextureWidth;
        [ParameterGroup(ParameterGroup.Texture)]
        public float gToonTextureHeight;
        [ParameterGroup(ParameterGroup.Texture)]
        public CustomMatUV ToonSamplerAddress; //U/V
        [ParameterGroup(ParameterGroup.Texture)]
        public CustomMatUV MarkSamplerAddress; //U/V
        [ParameterGroup(ParameterGroup.Texture)]
        public CustomMatUV MaskSamplerAddress; //U/V

        //MatDiff/Amb Lighting
        [ParameterGroup(ParameterGroup.Lighting)]
        public CustomVector4 gCamPos;
        [ParameterGroup(ParameterGroup.Lighting)]
        public CustomColor MatDif;
        [ParameterGroup(ParameterGroup.Lighting)]
        public CustomColor MatAmb;
        [ParameterGroup(ParameterGroup.Lighting)]
        public CustomColor MatSpc;
        [ParameterGroup(ParameterGroup.Lighting)]
        public float MatDifScale;
        [ParameterGroup(ParameterGroup.Lighting)]
        public float MatAmbScale;
        [ParameterGroup(ParameterGroup.Lighting)]
        public float SpcCoeff;
        [ParameterGroup(ParameterGroup.Lighting)]
        public float SpcPower;

        //Alpha
        [ParameterGroup(ParameterGroup.Alpha)]
        public int AlphaBlend;
        [ParameterGroup(ParameterGroup.Alpha)]
        public int AlphaBlendType;
        [ParameterGroup(ParameterGroup.Alpha)]
        public int AlphaTest;
        [ParameterGroup(ParameterGroup.Alpha)]
        public bool AlphaTestThreshold;
        [ParameterGroup(ParameterGroup.Alpha)]
        public bool AlphaRef;

        //Mask
        [ParameterGroup(ParameterGroup.Alpha)]
        public int AlphaSortMask;
        [ParameterGroup(ParameterGroup.Alpha)]
        public int ZTestMask;
        [ParameterGroup(ParameterGroup.Alpha)]
        public int ZWriteMask;

        //Unknown lighting values. These aren't used by many EMM files and are mostly in stages.
        public CustomColor gLightDir; //Unsure if this is actually the light direction at all... why would that be a color?
        public CustomColor gLightDif;
        public CustomColor gLightSpc;
        public CustomColor gLightAmb;
        public CustomColor DirLight0Dir;
        public CustomColor DirLight0Col;
        public CustomColor AmbLight0Col;

        //Flags
        [ParameterGroup(ParameterGroup.Misc)]
        public bool VsFlag0;
        [ParameterGroup(ParameterGroup.Misc)]
        public bool VsFlag1;
        [ParameterGroup(ParameterGroup.Misc)]
        public bool VsFlag2;
        [ParameterGroup(ParameterGroup.Misc)]
        public bool VsFlag3;
        [ParameterGroup(ParameterGroup.Misc)]
        public int CustomFlag; //bit flags. 0x40 max value.

        //Cull
        [ParameterGroup(ParameterGroup.Misc)]
        public int BackFace;
        [ParameterGroup(ParameterGroup.Misc)]
        public int TwoSidedRender;

        //LowRez
        [ParameterGroup(ParameterGroup.Misc)]
        public int LowRez; //bool. Renders at a reduced resolution (1/2)
        [ParameterGroup(ParameterGroup.Misc)]
        public int LowRezSmoke; //1/4 render resolution

        //Incidence
        [ParameterGroup(ParameterGroup.Misc)]
        public float IncidencePower;
        [ParameterGroup(ParameterGroup.Misc)]
        public float IncidenceAlphaBias;

        //Billboard
        public int Billboard;
        public int BillboardType;

        //Reflect
        [ParameterGroup(ParameterGroup.Misc)]
        public float ReflectCoeff;
        [ParameterGroup(ParameterGroup.Misc)]
        public float ReflectFresnelBias;
        [ParameterGroup(ParameterGroup.Misc)]
        public float ReflectFresnelCoeff;

        //Other
        [ParameterGroup(ParameterGroup.Misc)]
        public int AnimationChannel; //byte[4], with 4th byte never being used in any material
        [ParameterGroup(ParameterGroup.Misc)]
        public int NoEdge;
        public int Shimmer;
        public float gTime;

        //Not sure what these are but they are only used a few times, so nothing important
        public CustomColor Ambient;
        public CustomColor Diffuse;
        public CustomColor Specular;
        public float SpecularPower;

        //Fade
        public float FadeInit;
        public float FadeSpeed;

        //Gradient
        public float GradientInit;
        public float GradientSpeed;
        public CustomColor gGradientCol;

        //Rim
        public float RimCoeff;
        public float RimPower;

        //Unknown texture scroll. Only used a few times. Could be an alias for TexScrl?
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        public CustomMatUV gScroll0;
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        public CustomMatUV gScroll1;

        //Initially set values for this material. When saving back, these values will always be saved regardless of if they are "default".
        private List<string> InitialParameters = new List<string>();

        //Notify
        private int _parametersChanged = 0;
        public int ParametersChanged
        {
            get => _parametersChanged;
            set
            {
                if(_parametersChanged != value)
                {
                    _parametersChanged = value;
                    NotifyPropertyChanged(nameof(ParametersChanged));
                }
            }
        }

        #region Compile
        public List<Parameter> Compile()
        {
            List<Parameter> parameters = new List<Parameter>();

            foreach (var field in Fields)
            {
                VectorFormatAttribute formatAttr = (VectorFormatAttribute)field.GetCustomAttribute(typeof(VectorFormatAttribute), false);

                if (field.FieldType == typeof(int))
                {
                    if (HasParameterInt(field))
                    {
                        parameters.Add(new Parameter(field.Name, Parameter.ParameterType.Int, field.GetValue(this).ToString()));
                    }
                }
                else if (field.FieldType == typeof(bool))
                {
                    if (HasParameterBool(field))
                    {
                        parameters.Add(new Parameter(field.Name, Parameter.ParameterType.Bool, field.GetValue(this).ToString()));
                    }
                }
                else if (field.FieldType == typeof(float))
                {
                    if (HasParameterFloat(field))
                    {
                        if (field.Name.Contains("MipMapLod"))
                            parameters.Add(new Parameter(field.Name, Parameter.ParameterType.Float2, field.GetValue(this).ToString()));
                        else
                            parameters.Add(new Parameter(field.Name, Parameter.ParameterType.Float, field.GetValue(this).ToString()));
                    }
                }
                else if (field.FieldType == typeof(CustomVector4))
                {
                    if (HasParameterVector(field, VectorType.Vector4))
                    {
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.Vector4, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 0), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.Vector4, 0).ToString()));
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.Vector4, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 1), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.Vector4, 1).ToString()));
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.Vector4, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 2), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.Vector4, 2).ToString()));
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.Vector4, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 3), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.Vector4, 3).ToString()));
                    }
                }
                else if (field.FieldType == typeof(CustomColor))
                {
                    if (HasParameterVector(field, VectorType.Color))
                    {
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.Color, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 0), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.Color, 0).ToString()));
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.Color, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 1), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.Color, 1).ToString()));
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.Color, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 2), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.Color, 2).ToString()));
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.Color, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 3), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.Color, 3).ToString()));
                    }
                }
                else if (field.FieldType == typeof(CustomMatUV))
                {
                    if (HasParameterVector(field, VectorType.UV))
                    {
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.UV, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 0), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.UV, 0).ToString()));
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.UV, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 1), Parameter.ParameterType.Float, GetVectorValue(field, VectorType.UV, 1).ToString()));
                    }
                }
                else if (field.FieldType == typeof(CustomMatRepUV))
                {
                    if (HasParameterVector(field, VectorType.UV))
                    {
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.UV, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 0), Parameter.ParameterType.Bool, GetVectorValue(field, VectorType.UV, 0).ToString()));
                        parameters.Add(new Parameter(GetParamterName(field.Name, VectorType.UV, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3), 1), Parameter.ParameterType.Bool, GetVectorValue(field, VectorType.UV, 1).ToString()));
                    }
                }

            }

            return parameters;
        }

        private bool HasParameterInt(FieldInfo fieldInfo)
        {
            if (InitialParameters.Contains(fieldInfo.Name)) return true;

            int defaultValue = DefaultMaterialValues.GetDefautInt(fieldInfo.Name);
            int currentValue = (int)fieldInfo.GetValue(this);
            return defaultValue != currentValue;
        }

        private bool HasParameterFloat(FieldInfo fieldInfo)
        {
            if (InitialParameters.Contains(fieldInfo.Name)) return true;

            float defaultValue = DefaultMaterialValues.GetDefautFloat(fieldInfo.Name);
            float currentValue = (float)fieldInfo.GetValue(this);
            return defaultValue != currentValue;
        }

        private bool HasParameterBool(FieldInfo fieldInfo)
        {
            if (InitialParameters.Contains(fieldInfo.Name)) return true;

            bool defaultValue = DefaultMaterialValues.GetDefautBool(fieldInfo.Name);
            bool currentValue = (bool)fieldInfo.GetValue(this);
            return defaultValue != currentValue;
        }

        private bool HasParameterVector(FieldInfo fieldInfo, VectorType type, int valueIdx)
        {
            float[] defaultValue = DefaultMaterialValues.GetDefautVector(fieldInfo.Name);

            object currentValue = fieldInfo.GetValue(this);
            float value = 0;

            if (type == VectorType.Vector4)
            {
                value = ((CustomVector4)currentValue).GetValue(valueIdx);
            }
            else if (type == VectorType.Color)
            {
                if(valueIdx == 3)
                {
                    bool test = true;
                }
                value = ((CustomColor)currentValue).GetValue(valueIdx);
            }
            else if (type == VectorType.UV && currentValue is CustomMatUV)
            {
                value = ((CustomMatUV)currentValue).GetValue(valueIdx);
            }
            else if (type == VectorType.UV && currentValue is CustomMatRepUV)
            {
                value = ((CustomMatRepUV)currentValue).GetValue(valueIdx) ? 1f : 0f;
            }

            if (defaultValue == null)
            {
                return value != 0f;
            }

            return defaultValue[valueIdx] != value;
        }

        private bool HasParameterVector(FieldInfo fieldInfo, VectorType type)
        {
            if (InitialParameters.Contains(fieldInfo.Name)) return true;

            return HasParameterVector(fieldInfo, type, 0) || HasParameterVector(fieldInfo, type, 1) || HasParameterVector(fieldInfo, type, 2) || HasParameterVector(fieldInfo, type, 3);
        }

        private float GetVectorValue(FieldInfo fieldInfo, VectorType type, int valueIdx)
        {
            object currentValue = fieldInfo.GetValue(this);

            if (type == VectorType.Vector4)
            {
                return ((CustomVector4)currentValue).GetValue(valueIdx);
            }
            else if (type == VectorType.Color)
            {
                return ((CustomColor)currentValue).GetValue(valueIdx);
            }
            else if (type == VectorType.UV && currentValue is CustomMatUV)
            {
                return ((CustomMatUV)currentValue).GetValue(valueIdx);
            }
            else if (type == VectorType.UV && currentValue is CustomMatRepUV)
            {
                return ((CustomMatRepUV)currentValue).GetValue(valueIdx) ? 1f : 0f;
            }

            throw new InvalidOperationException("DecompiledMaterial.GetVectorValue: invalid state.");
        }

        #endregion

        #region Decompile
        public static DecompiledMaterial Default()
        {
            return Decompile(DefaultMaterial);
        }

        public static DecompiledMaterial Decompile(EmmMaterial material)
        {
            DecompiledMaterial decMat = new DecompiledMaterial();

            foreach (var field in Fields)
            {
                VectorFormatAttribute formatAttr = (VectorFormatAttribute)field.GetCustomAttribute(typeof(VectorFormatAttribute), false);

                if (field.FieldType == typeof(int))
                {
                    Parameter param = material.GetParameter(field.Name);

                    if (param != null)
                    {
                        field.SetValue(decMat, param.IntValue);
                        decMat.InitialParameters.Add(field.Name);
                    }
                    else
                    {
                        field.SetValue(decMat, DefaultMaterialValues.GetDefautInt(field.Name));
                    }
                }
                else if (field.FieldType == typeof(float))
                {
                    Parameter param = material.GetParameter(field.Name);

                    if (param != null)
                    {
                        field.SetValue(decMat, param.FloatValue);
                        decMat.InitialParameters.Add(field.Name);
                    }
                    else
                    {
                        field.SetValue(decMat, DefaultMaterialValues.GetDefautFloat(field.Name));
                    }
                }
                else if (field.FieldType == typeof(bool))
                {
                    Parameter param = material.GetParameter(field.Name);

                    if (param != null)
                    {
                        field.SetValue(decMat, param.IntValue > 0);
                        decMat.InitialParameters.Add(field.Name);
                    }
                    else
                    {
                        field.SetValue(decMat, DefaultMaterialValues.GetDefautBool(field.Name));
                    }
                }
                else if (field.FieldType == typeof(CustomMatRepUV))
                {
                    decMat.AddVectorInitialParams(field.Name, material);

                    float[] vector = GetVectorValues(field.Name, material, VectorType.UV, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3));
                    field.SetValue(decMat, new CustomMatRepUV(vector[0] >= 1f, vector[1] >= 1f));
                }
                else if (field.FieldType == typeof(CustomMatUV))
                {
                    decMat.AddVectorInitialParams(field.Name, material);

                    float[] vector = GetVectorValues(field.Name, material, VectorType.UV, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3));
                    field.SetValue(decMat, new CustomMatUV(vector[0], vector[1]));
                }
                else if (field.FieldType == typeof(CustomColor))
                {
                    decMat.AddVectorInitialParams(field.Name, material);

                    float[] vector = GetVectorValues(field.Name, material, VectorType.Color, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3));
                    field.SetValue(decMat, new CustomColor(vector[0], vector[1], vector[2], vector[3]));
                }
                else if (field.FieldType == typeof(CustomVector4))
                {
                    decMat.AddVectorInitialParams(field.Name, material);

                    float[] vector = GetVectorValues(field.Name, material, VectorType.Vector4, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3));
                    field.SetValue(decMat, new CustomVector4(vector[0], vector[1], vector[2], vector[3]));
                }
            }

            //Special case of g_MaterialOffset0_VS (the shader parameter) being referenced directly in the EMM:
            Parameter parameter = material.GetParameter("g_MaterialOffset0_VS");

            if (parameter != null)
            {
                decMat.MatOffset0 = new CustomVector4(parameter.FloatValue);
            }

            //Register PropertyChanged events for all vectors. This is needed so that XenoKit can automnatically update shaders upon any user edits.
            decMat.RegisterEvents();

            return decMat;
        }

        private void RegisterEvents()
        {
            GlareCol.PropertyChanged += VectorValuesChangedEvent;
            MatOffset0.PropertyChanged += VectorValuesChangedEvent;
            MatOffset1.PropertyChanged += VectorValuesChangedEvent;
            MatScale0.PropertyChanged += VectorValuesChangedEvent;
            MatScale1.PropertyChanged += VectorValuesChangedEvent;
            MatCol0.PropertyChanged += VectorValuesChangedEvent;
            MatCol1.PropertyChanged += VectorValuesChangedEvent;
            MatCol2.PropertyChanged += VectorValuesChangedEvent;
            MatCol3.PropertyChanged += VectorValuesChangedEvent;
            TexScrl0.PropertyChanged += VectorValuesChangedEvent;
            TexScrl1.PropertyChanged += VectorValuesChangedEvent;
            TexRep0.PropertyChanged += VectorValuesChangedEvent;
            TexRep1.PropertyChanged += VectorValuesChangedEvent;
            TexRep2.PropertyChanged += VectorValuesChangedEvent;
            TexRep3.PropertyChanged += VectorValuesChangedEvent;
            ToonSamplerAddress.PropertyChanged += VectorValuesChangedEvent;
            MarkSamplerAddress.PropertyChanged += VectorValuesChangedEvent;
            MaskSamplerAddress.PropertyChanged += VectorValuesChangedEvent;
            gCamPos.PropertyChanged += VectorValuesChangedEvent;
            MatDif.PropertyChanged += VectorValuesChangedEvent;
            MatAmb.PropertyChanged += VectorValuesChangedEvent;
            MatSpc.PropertyChanged += VectorValuesChangedEvent;
            gLightDir.PropertyChanged += VectorValuesChangedEvent;
            gLightDif.PropertyChanged += VectorValuesChangedEvent;
            gLightSpc.PropertyChanged += VectorValuesChangedEvent;
            gLightAmb.PropertyChanged += VectorValuesChangedEvent;
            DirLight0Dir.PropertyChanged += VectorValuesChangedEvent;
            DirLight0Col.PropertyChanged += VectorValuesChangedEvent;
            AmbLight0Col.PropertyChanged += VectorValuesChangedEvent;
            Ambient.PropertyChanged += VectorValuesChangedEvent;
            Diffuse.PropertyChanged += VectorValuesChangedEvent;
            Specular.PropertyChanged += VectorValuesChangedEvent;
            gGradientCol.PropertyChanged += VectorValuesChangedEvent;
            gScroll0.PropertyChanged += VectorValuesChangedEvent;
            gScroll1.PropertyChanged += VectorValuesChangedEvent;
        }

        private static float[] GetVectorValues(string fieldName, EmmMaterial material, VectorType type, ParameterNameFormat pos2, ParameterNameFormat pos3)
        {

            //Check for a parameter without the "index". This should initialize all members of the array to this value, so we need to look no further. (NOTE: array means stuff like MatCol0, MatCol1, MatCol2...)
            Parameter param = material.GetParameter(GetParameterNameWithoutIndex(fieldName));

            if (param != null)
            {
                return new float[4] { param.FloatValue, param.FloatValue, param.FloatValue, param.FloatValue };
            }

            //Check for a parameter without the "value". This sets all the vector values.
            param = material.GetParameter(fieldName);

            if (param != null)
            {
                return new float[4] { param.FloatValue, param.FloatValue, param.FloatValue, param.FloatValue };
            }

            //Check for individual values
            float x = 0;
            float y = 0;
            float z = 0;
            float w = 0;
            bool hasValue = false;

            param = material.GetParameter(GetParamterName(fieldName, type, pos2, pos3, 0));

            if (param != null)
            {
                hasValue = true;
                x = param.FloatValue;
            }

            param = material.GetParameter(GetParamterName(fieldName, type, pos2, pos3, 1));

            if (param != null)
            {
                hasValue = true;
                y = param.FloatValue;
            }

            if (type == VectorType.Color || type == VectorType.Vector4)
            {
                param = material.GetParameter(GetParamterName(fieldName, type, pos2, pos3, 2));

                if (param != null)
                {
                    hasValue = true;
                    z = param.FloatValue;
                }

                param = material.GetParameter(GetParamterName(fieldName, type, pos2, pos3, 3));

                if (param != null)
                {
                    hasValue = true;
                    w = param.FloatValue;
                }
            }

            var defaultVector = DefaultMaterialValues.GetDefautVector(fieldName);

            if (defaultVector == null)
            {
                defaultVector = new float[4];
            }

            //if (x != defaultVector[0] || y != defaultVector[1] || z != defaultVector[2] || w != defaultVector[3])
            if (hasValue)
            {
                //Values were found, so return here.
                return new float[4] { x, y, z, w };
            }
            else
            {
                return defaultVector.Copy();
            }

        }

        #endregion

        #region Helper
        private static string GetParamterName(string fieldName, VectorType type, ParameterNameFormat pos2, ParameterNameFormat pos3, int valueIdx)
        {
            if ((pos2 == pos3) || pos2 == ParameterNameFormat.Name || pos3 == ParameterNameFormat.Name)
            {
                throw new ArgumentException("DecompiledMaterial.GetParameterName: Invalid pos2 and pos3 arguments.");
            }

            //Seperate the index if needed
            string idx = null;

            if (pos2 == ParameterNameFormat.Value)
            {
                idx = fieldName[fieldName.Length - 1].ToString();
                fieldName = GetParameterNameWithoutIndex(fieldName);
            }

            //Get the "value". This is the X,Y,Z,W or R,G,B,A...
            string value = null;

            switch (type)
            {
                case VectorType.Vector4:
                    if (valueIdx == 0) value = "X";
                    if (valueIdx == 1) value = "Y";
                    if (valueIdx == 2) value = "Z";
                    if (valueIdx == 3) value = "W";
                    break;
                case VectorType.Color:
                    if (valueIdx == 0) value = "R";
                    if (valueIdx == 1) value = "G";
                    if (valueIdx == 2) value = "B";
                    if (valueIdx == 3) value = "A";
                    break;
                case VectorType.UV:
                    if (valueIdx == 0) value = "U";
                    if (valueIdx == 1) value = "V";
                    break;
                default:
                    throw new Exception("DecompiledMaterial.GetParameterName: invalid VectorType.");
            }

            return (pos2 == ParameterNameFormat.Index) ? $"{fieldName}{value}" : $"{fieldName}{value}{idx}";
        }

        private static string GetParameterNameWithoutIndex(string fieldName)
        {
            return fieldName.Remove(fieldName.Length - 1, 1);
        }

        private static ParameterNameFormat GetNameFormat(VectorFormatAttribute attr, int pos)
        {
            switch (pos)
            {
                case 1:
                    return attr != null ? attr.Position1 : ParameterNameFormat.Name;
                case 2:
                    return attr != null ? attr.Position2 : ParameterNameFormat.Index;
                case 3:
                    return attr != null ? attr.Position3 : ParameterNameFormat.Value;
                default:
                    throw new ArgumentOutOfRangeException("DecompiledMaterial.GetNameFormat: pos must be between 1 >= and 3 <=");
            }
        }

        private int GetIndexOfValueType(string valueType)
        {
            switch (valueType.ToLower())
            {
                case "r":
                case "x":
                case "u":
                    return 0;
                case "g":
                case "y":
                case "v":
                    return 1;
                case "b":
                case "z":
                    return 2;
                case "a":
                case "w":
                    return 3;
                default:
                    return 0;
            }
        }

        public CustomVector4 GetVector(string name)
        {
            return (CustomVector4)GetType().GetField(name).GetValue(this);
        }

        public CustomColor GetColor(string name)
        {
            return (CustomColor)GetType().GetField(name).GetValue(this);
        }

        public float GetMipMapLod(int index)
        {
            switch (index)
            {
                case 0:
                    return MipMapLod0;
                case 1:
                    return MipMapLod1;
                case 2:
                    return MipMapLod2;
                case 3:
                    return MipMapLod3;
            }

            return 0f;
        }

        public CustomColor GetMatCol(int index)
        {
            switch (index)
            {
                case 0:
                    return MatCol0;
                case 1:
                    return MatCol1;
                case 2:
                    return MatCol2;
                case 3:
                    return MatCol3;
            }

            return null;
        }

        public CustomMatUV GetTexScrl(int index)
        {
            switch (index)
            {
                case 0:
                    return TexScrl0;
                case 1:
                    return TexScrl1;
            }

            return null;
        }
        public bool Compare(DecompiledMaterial material)
        {
            foreach (var field in Fields)
            {
                //Skip non-parameters
                if (field.Name == nameof(InitialParameters) || field.Name == nameof(_parametersChanged)) continue;

                object oldValue = field.GetValue(this);
                object newValue = field.GetValue(material);

                //Handle any null values
                if (oldValue == null && newValue == null) continue;
                if ((oldValue == null && newValue != null) || (oldValue != null && newValue == null)) return false;

                //Compare the values
                if (!oldValue.Equals(newValue)) 
                    return false;
            }

            return true;
        }

        public bool HasParameter(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name.Length < 5) return false; //The shortest parameter name is 5

            string nameNoIdx = GetParameterNameWithoutIndex(name);
            int valIdx = GetIndexOfValueType(name.Substring(name.Length - 1, 1));

            foreach (var field in Fields)
            {
                if (field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (field.FieldType == typeof(int))
                        return HasParameterInt(field);

                    if (field.FieldType == typeof(float))
                        return HasParameterFloat(field);

                    if (field.FieldType == typeof(bool))
                        return HasParameterBool(field);

                    //Whole vectors
                    if (field.FieldType == typeof(CustomVector4))
                        return HasParameterVector(field, VectorType.Vector4);

                    if (field.FieldType == typeof(CustomColor))
                        return HasParameterVector(field, VectorType.Color);

                    if (field.FieldType == typeof(CustomMatUV) || field.FieldType == typeof(CustomMatRepUV))
                        return HasParameterVector(field, VectorType.UV);

                }
                else if (field.Name.Equals(nameNoIdx, StringComparison.OrdinalIgnoreCase))
                {
                    //1 element of a vector
                    if (field.FieldType == typeof(CustomVector4))
                        return HasParameterVector(field, VectorType.Vector4, valIdx);

                    if (field.FieldType == typeof(CustomColor))
                        return HasParameterVector(field, VectorType.Color, valIdx);

                    if (field.FieldType == typeof(CustomMatUV) || field.FieldType == typeof(CustomMatRepUV))
                        return HasParameterVector(field, VectorType.UV, valIdx);
                }
            }

            return false;
        }

        public bool IsGroupUsed(ParameterGroup group)
        {
            for (int i = 0; i < Fields.Length; i++)
            {
                if (Groups[i] == group)
                {
                    if (HasParameter(Fields[i].Name)) return true;
                }
            }

            return false;
        }

        private void AddVectorInitialParams(string fieldName, EmmMaterial mat)
        {
            if (mat.ParameterExists(fieldName))
                InitialParameters.Add(fieldName);
        }

        public static readonly string[] ColorParameters = new string[]
        {
            "GlareCol",
            "MatSpc",
            "MatCol0",
            "MatCol1",
            "MatCol2",
            "MatCol3",
            "MatAmb",
            "MatDif",
        };
        #endregion

        #region Editing
        public List<IUndoRedo> PasteValues(EmmMaterial material)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var field in Fields)
            {
                //Get both values
                object oldValue = field.GetValue(this);
                object newValue = field.GetValue(material.DecompiledParameters);

                //Update value
                field.SetValue(this, newValue);

                //Add the undoable step
                undos.Add(new UndoableField(field.Name, this, oldValue, newValue));
            }

            return undos;
        }

        #endregion

        private void VectorValuesChangedEvent(object sender, PropertyChangedEventArgs e)
        {
            _parametersChanged = 1;
            NotifyPropertyChanged(nameof(ParametersChanged));
        }

        public void ResetParametersChanged()
        {
            _parametersChanged = 0;
        }

    }

    public enum AlphaBlendType : int
    {
        NotSet = -1,
        Normal = 0,
        Additive = 1,
        Subtractive = 2
    }

    internal enum VectorType
    {
        Vector4,
        Color,
        UV
    }

    internal enum ParameterNameFormat
    {
        Name,
        Index,
        Value
    }

    public enum ParameterGroup
    {
        Unsorted,
        Misc,
        MatScaleOffset,
        Color,
        Alpha,
        Texture,
        Lighting
    }

    #region Attributes
    /// <summary>
    /// Specify the format of this parameter name. The default format is: Name -> Index -> Value. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class VectorFormatAttribute : Attribute
    {
        public ParameterNameFormat Position1;
        public ParameterNameFormat Position2;
        public ParameterNameFormat Position3;

        public VectorFormatAttribute(ParameterNameFormat pos1, ParameterNameFormat pos2, ParameterNameFormat pos3)
        {
            Position1 = pos1;
            Position2 = pos2;
            Position3 = pos3;
        }

    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class ParameterGroupAttribute : Attribute
    {
        public ParameterGroup Group;

        public ParameterGroupAttribute(ParameterGroup group)
        {
            Group = group;
        }

    }
    #endregion
}
