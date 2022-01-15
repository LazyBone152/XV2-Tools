using System;
using System.Reflection;
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
    public class DecompiledMaterial
    {
        #region CachedValues
        /// <summary>
        /// Used for creating default <see cref="DecompiledMaterial"/> objects. Cached here for performance reasons.
        /// </summary>
        private static readonly EmmMaterial DefaultMaterial = new EmmMaterial();
        private static readonly FieldInfo[] Fields;
        private static readonly ParameterGroupAttribute.ParameterGroup[] Groups;

        static DecompiledMaterial()
        {
            Fields = typeof(DecompiledMaterial).GetFields();
            Groups = new ParameterGroupAttribute.ParameterGroup[Fields.Length];

            for (int i = 0; i < Fields.Length; i++)
            {
                ParameterGroupAttribute attr = (ParameterGroupAttribute)Fields[i].GetCustomAttribute(typeof(ParameterGroupAttribute));

                if (attr != null)
                {
                    Groups[i] = attr.Group;
                }
                else
                {
                    Groups[i] = ParameterGroupAttribute.ParameterGroup.Unsorted;
                }
            }
        }
        #endregion

        //Glare
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Color)]
        public int Glare;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Color)]
        public CustomColor GlareCol;

        //MatOffsets
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.MatScaleOffset)]
        public CustomVector4 MatOffset0;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.MatScaleOffset)]
        public CustomVector4 MatOffset1;

        //MatScales
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.MatScaleOffset)]
        public CustomVector4 MatScale0;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.MatScaleOffset)]
        public CustomVector4 MatScale1;

        //MatCols
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Color)]
        public CustomColor MatCol0;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Color)]
        public CustomColor MatCol1;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Color)]
        public CustomColor MatCol2;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Color)]
        public CustomColor MatCol3;

        //Texture Scroll
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public CustomMatUV TexScrl0;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public CustomMatUV TexScrl1;

        //Texture Sampler parameters
        //In these values the index lines up with a sampler index. So in theory, the limit is 4, but in vanilla EMM files only 3 is used.
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public CustomMatRepUV TexRep0;
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public CustomMatRepUV TexRep1;
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public CustomMatRepUV TexRep2;
        [VectorFormat(ParameterNameFormat.Name, ParameterNameFormat.Value, ParameterNameFormat.Index)]
        public CustomMatRepUV TexRep3;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public int TextureFilter0;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public int TextureFilter1;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public int TextureFilter2;
        public int TextureFilter3;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public float MipMapLod0; //"Float2" type in EMM (so are 1,2,3).
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public float MipMapLod1;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public float MipMapLod2;
        public float MipMapLod3;

        //Other Texture Parameters:
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public float gToonTextureWidth;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public float gToonTextureHeight;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public CustomMatUV ToonSamplerAddress; //U/V
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public CustomMatUV MarkSamplerAddress; //U/V
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Texture)]
        public CustomMatUV MaskSamplerAddress; //U/V

        //MatDiff/Amb Lighting
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Lighting)]
        public CustomVector4 gCamPos;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Lighting)]
        public CustomColor MatDif;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Lighting)]
        public CustomColor MatAmb;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Lighting)]
        public CustomColor MatSpc;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Lighting)]
        public float MatDifScale;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Lighting)]
        public float MatAmbScale;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Lighting)]
        public float SpcCoeff;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Lighting)]
        public float SpcPower;

        //Alpha
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Alpha)]
        public int AlphaBlend;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Alpha)]
        public int AlphaBlendType;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Alpha)]
        public int AlphaTest;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Alpha)]
        public bool AlphaTestThreshold;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Alpha)]
        public bool AlphaRef;

        //Mask
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Alpha)]
        public int AlphaSortMask;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Alpha)]
        public int ZTestMask;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.Alpha)]
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
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public bool VsFlag0;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public bool VsFlag1;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public bool VsFlag2;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public bool VsFlag3;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public int CustomFlag; //bit flags. 0x40 max value.

        //Cull
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public int BackFace;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public int TwoSidedRender;

        //LowRez
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public int LowRez;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public int LowRezSmoke;

        //Incidence
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public float IncidencePower;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public float IncidenceAlphaBias;

        //Billboard
        public int Billboard;
        public int BillboardType;

        //Reflect
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public float ReflectCoeff;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public float ReflectFresnelBias;
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public float ReflectFresnelCoeff;

        //Other
        [ParameterGroup(ParameterGroupAttribute.ParameterGroup.General)]
        public int AnimationChannel;
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


        #region Compile
        public List<Parameter> Compile()
        {
            List<Parameter> parameters = new List<Parameter>();

            foreach(var field in Fields)
            {
                VectorFormatAttribute formatAttr = (VectorFormatAttribute)field.GetCustomAttribute(typeof(VectorFormatAttribute), false);

                if (field.FieldType == typeof(int))
                {
                    if(HasParameterInt(field))
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
                        if(field.Name.Contains("MipMapLod"))
                            parameters.Add(new Parameter(field.Name, Parameter.ParameterType.Float2, field.GetValue(this).ToString()));
                        else
                            parameters.Add(new Parameter(field.Name, Parameter.ParameterType.Float, field.GetValue(this).ToString()));
                    }
                }
                else if(field.FieldType == typeof(CustomVector4))
                {
                    if(HasParameterVector(field, VectorType.Vector4))
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
            int defaultValue = DefaultMaterialValues.GetDefautInt(fieldInfo.Name);
            int currentValue = (int)fieldInfo.GetValue(this);
            return defaultValue != currentValue;
        }

        private bool HasParameterFloat(FieldInfo fieldInfo)
        {
            float defaultValue = DefaultMaterialValues.GetDefautFloat(fieldInfo.Name);
            float currentValue = (float)fieldInfo.GetValue(this);
            return defaultValue != currentValue;
        }

        private bool HasParameterBool(FieldInfo fieldInfo)
        {
            bool defaultValue = DefaultMaterialValues.GetDefautBool(fieldInfo.Name);
            bool currentValue = (bool)fieldInfo.GetValue(this);
            return defaultValue != currentValue;
        }

        private bool HasParameterVector(FieldInfo fieldInfo, VectorType type, int valueIdx)
        {
            float[] defaultValue = DefaultMaterialValues.GetDefautVector(fieldInfo.Name);

            object currentValue = fieldInfo.GetValue(this);
            float value = 0;

            if(type == VectorType.Vector4)
            {
                value = ((CustomVector4)currentValue).GetValue(valueIdx);
            }
            else if (type == VectorType.Color)
            {
                value = ((CustomColor)currentValue).GetValue(valueIdx);
            }
            else if (type == VectorType.UV && currentValue is CustomMatUV)
            {
                value = ((CustomMatUV)currentValue).GetValue(valueIdx);
            }
            else if (type == VectorType.UV && currentValue is CustomMatRepUV)
            {
                value = ((CustomMatRepUV)currentValue).GetValue(valueIdx) ? 1f: 0f;
            }

            if (defaultValue == null)
            {
                return value != 0f;
            }

            return defaultValue[valueIdx] != value;
        }

        private bool HasParameterVector(FieldInfo fieldInfo, VectorType type)
        {
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

            foreach(var field in Fields)
            {
                VectorFormatAttribute formatAttr = (VectorFormatAttribute)field.GetCustomAttribute(typeof(VectorFormatAttribute), false);

                if (field.FieldType == typeof(int))
                {
                    Parameter param = material.GetParameter(field.Name);

                    if (param != null)
                        field.SetValue(decMat, param.IntValue);
                    else
                        field.SetValue(decMat, DefaultMaterialValues.GetDefautInt(field.Name));
                }
                else if (field.FieldType == typeof(float))
                {
                    Parameter param = material.GetParameter(field.Name);

                    if (param != null)
                        field.SetValue(decMat, param.FloatValue);
                    else
                        field.SetValue(decMat, DefaultMaterialValues.GetDefautFloat(field.Name));
                }
                else if (field.FieldType == typeof(bool))
                {
                    Parameter param = material.GetParameter(field.Name);

                    if (param != null)
                        field.SetValue(decMat, param.IntValue > 0);
                    else
                        field.SetValue(decMat, DefaultMaterialValues.GetDefautBool(field.Name));
                }
                else if (field.FieldType == typeof(CustomMatRepUV))
                {
                    float[] vector = GetVectorValues(field.Name, material, VectorType.UV, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3));
                    field.SetValue(decMat, new CustomMatRepUV(vector[0] >= 1f, vector[1] >= 1f));
                }
                else if(field.FieldType == typeof(CustomMatUV))
                {
                    float[] vector = GetVectorValues(field.Name, material, VectorType.UV, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3));
                    field.SetValue(decMat, new CustomMatUV(vector[0], vector[1]));
                }
                else if (field.FieldType == typeof(CustomColor))
                {
                    float[] vector = GetVectorValues(field.Name, material, VectorType.Color, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3));
                    field.SetValue(decMat, new CustomColor(vector[0], vector[1], vector[2], vector[3]));
                }
                else if (field.FieldType == typeof(CustomVector4))
                {
                    float[] vector = GetVectorValues(field.Name, material, VectorType.Vector4, GetNameFormat(formatAttr, 2), GetNameFormat(formatAttr, 3));
                    field.SetValue(decMat, new CustomVector4(vector[0], vector[1], vector[2], vector[3]));
                }
            }

            //Special case of g_MaterialOffset0_VS (the shader parameter) being referenced directly in the EMM:
            Parameter parameter = material.GetParameter("g_MaterialOffset0_VS");

            if(parameter != null)
            {
                decMat.MatOffset0 = new CustomVector4(parameter.FloatValue);
            }

            return decMat;
        }

        private static float[] GetVectorValues(string fieldName, EmmMaterial material, VectorType type, ParameterNameFormat pos2, ParameterNameFormat pos3)
        {
            //Check for a parameter without the "index". This should initialize all members of the array to this value, so we need to look no further. (NOTE: array means stuff like MatCol0, MatCol1, MatCol2...)
            Parameter param = material.GetParameter(GetParameterNameWithoutIndex(fieldName));

            if(param != null)
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

            param = material.GetParameter(GetParamterName(fieldName, type, pos2, pos3, 0));

            if (param != null)
                x = param.FloatValue;

            param = material.GetParameter(GetParamterName(fieldName, type, pos2, pos3, 1));

            if (param != null)
                y = param.FloatValue;

            if(type == VectorType.Color || type == VectorType.Vector4)
            {
                param = material.GetParameter(GetParamterName(fieldName, type, pos2, pos3, 2));

                if (param != null)
                    z = param.FloatValue;

                param = material.GetParameter(GetParamterName(fieldName, type, pos2, pos3, 3));

                if (param != null)
                    w = param.FloatValue;
            }

            if(x != 0 || y != 0 || z != 0 || w != 0)
            {
                //Values were found, so return here.
                return new float[4] { x, y, z, w };
            }
            else
            {
                //No values were found. Set default values here.
                var vector = DefaultMaterialValues.GetDefautVector(fieldName);

                if (vector == null)
                    vector = new float[4];

                return vector;
            }

        }

        #endregion

        #region Helper
        private static string GetParamterName(string fieldName, VectorType type, ParameterNameFormat pos2, ParameterNameFormat pos3, int valueIdx)
        {
            if((pos2 == pos3) || pos2 == ParameterNameFormat.Name || pos3 == ParameterNameFormat.Name)
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
                    if(valueIdx == 0) value = "R";
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

        public bool Compare(DecompiledMaterial material)
        {
            foreach(var field in Fields)
            {
                object oldValue = field.GetValue(this);
                object newValue = field.GetValue(material);

                //Handle any null values
                if (oldValue == null && newValue == null) continue;
                if ((oldValue == null && newValue != null) || (oldValue != null && newValue == null)) return false;

                //Compare the values
                if (!oldValue.Equals(newValue)) return false;
            }

            return true;
        }

        public bool HasParameter(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name.Length < 5) return false; //The shortest parameter name is 5

            string nameNoIdx = GetParameterNameWithoutIndex(name);
            int valIdx = GetIndexOfValueType(name.Substring(name.Length - 1, 1));

            foreach(var field in Fields)
            {
                if(field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
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
                else if(field.Name.Equals(nameNoIdx, StringComparison.OrdinalIgnoreCase))
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

            foreach(var field in Fields)
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
        public enum ParameterGroup 
        { 
            Unsorted,
            General,
            MatScaleOffset,
            Color,
            Alpha,
            Texture,
            Lighting
        }

        public ParameterGroup Group;

        public ParameterGroupAttribute(ParameterGroup group)
        {
            Group = group;
        }

    }
    #endregion
}
