using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UsefulThings;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMZ;
using YAXLib;

namespace Xv2CoreLib.SDS
{
    [YAXSerializeAs("SDS")]
    public class SDS_File
    {
        internal const int SIGNATURE = 1396986659;

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public bool IsEMZ { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SDSShaderProgram")]
        public List<SDSShaderProgram> ShaderPrograms { get; set; } = new List<SDSShaderProgram>();

        #region Load
        public static SDS_File LoadFromXml(string xmlPath)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(SDS_File), YAXSerializationOptions.DontSerializeNullObjects);
            return (SDS_File)serializer.DeserializeFromFile(xmlPath);
        }
        
        public static SDS_File Parse(string path, bool writeXml)
        {
            SDS_File sdsFile = Load(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(SDS_File));
                serializer.SerializeToFile(sdsFile, path + ".xml");
            }

            return sdsFile;
        }

        public static SDS_File Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static SDS_File Load(byte[] bytes)
        {
            SDS_File sdsFile = new SDS_File();

            if(BitConverter.ToInt32(bytes, 0) == EMZ_File.SIGNATURE)
            {
                //Unpack the SDS if it is within an EMZ file
                EMZ_File emz = EMZ_File.Load(bytes);
                bytes = emz.Data;
                sdsFile.IsEMZ = true;
            }

            if (BitConverter.ToInt32(bytes, 0) != SIGNATURE)
                throw new InvalidDataException("#SDS signature not found.");

            int shaderCount = BitConverter.ToInt32(bytes, 12);
            int shaderOffset = BitConverter.ToInt32(bytes, 16);

            for (int i = 0; i < shaderCount; i++)
            {
                string shaderStr = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, shaderOffset), true, StringEx.EncodingType.UTF8);
                int endIndex = shaderStr.GetMinIndexOfSubstrings(0, '\t', ' ');
                string name = shaderStr.Substring(0, endIndex);

                int startIndex = endIndex + 1;
                endIndex = shaderStr.GetMinIndexOfSubstrings(startIndex, '\t', ' ');
                string vertexShader = shaderStr.Substring(startIndex, endIndex - startIndex);

                startIndex = endIndex + 1;
                endIndex = shaderStr.GetMinIndexOfSubstrings(startIndex, '\t', ' ');
                string pixelShader = endIndex != -1 ? shaderStr.Substring(startIndex, endIndex - startIndex) : shaderStr.Substring(startIndex);

                SDSShaderProgram shaderProgram = new SDSShaderProgram();
                shaderProgram.Name = name;
                shaderProgram.VertexShader = vertexShader;
                shaderProgram.PixelShader = pixelShader;

                //Parse parameters
                if (endIndex != -1)
                {
                    shaderStr = shaderStr.Substring(endIndex + 2);
                    string[] parameters = shaderStr.Split(StringSplitOptions.RemoveEmptyEntries, "\t/");

                    if (parameters != null)
                    {
                        foreach (string parameter in parameters)
                        {
                            string[] _splitParam = parameter.Split(' ');
                            ParameterType type = (ParameterType)Enum.Parse(typeof(ParameterType), _splitParam[0].Replace("/", ""));

                            for (int a = 1; a < _splitParam.Length; a++)
                            {
                                SDSParameter sdsParam = new SDSParameter();
                                sdsParam.Type = type;
                                sdsParam.Name = _splitParam[a];
                                shaderProgram.Parameters.Add(sdsParam);
                            }
                        }
                    }
                }

                sdsFile.ShaderPrograms.Add(shaderProgram);

                shaderOffset += 4;
            }

            return sdsFile;
        }

        #endregion

        #region Save
        public void SaveXml(string path)
        {
            YAXSerializer serializer = new YAXSerializer(typeof(SDS_File));
            serializer.SerializeToFile(this, path);
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, Write());
        }

        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(SDS_File), YAXSerializationOptions.DontSerializeNullObjects);
            SDS_File sdsFile = (SDS_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, sdsFile.Write());
        }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)20));
            bytes.AddRange(BitConverter.GetBytes(0));
            bytes.AddRange(BitConverter.GetBytes(ShaderPrograms.Count));
            bytes.AddRange(BitConverter.GetBytes(20));

            //Pointers for shaders
            bytes.AddRange(new byte[4 * ShaderPrograms.Count]);

            for (int i = 0; i < ShaderPrograms.Count; i++)
            {
                //Add pointer to header
                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 20 + (4 * i));

                bytes.AddRange(Encoding.UTF8.GetBytes(ShaderPrograms[i].Name));
                bytes.Add(0x20);

                bytes.AddRange(Encoding.UTF8.GetBytes(ShaderPrograms[i].VertexShader));
                bytes.Add(0x9);

                bytes.AddRange(Encoding.UTF8.GetBytes(ShaderPrograms[i].PixelShader));

                if (ShaderPrograms[i].Parameters?.Count > 0)
                {
                    bytes.Add(0x9);

                    if (ShaderPrograms[i].Parameters.Where(x => x.Type == ParameterType.MtxP).Count() > 0)
                    {
                        bytes.Add(0x2f);
                        bytes.AddRange(Encoding.UTF8.GetBytes("MtxP"));

                        foreach (SDSParameter param in ShaderPrograms[i].Parameters.Where(x => x.Type == ParameterType.MtxP))
                        {
                            bytes.AddRange(Encoding.UTF8.GetBytes($" {param.Name}"));
                        }

                        bytes.Add(0x9);
                    }

                    if (ShaderPrograms[i].Parameters.Where(x => x.Type == ParameterType.GblP).Count() > 0)
                    {
                        bytes.Add(0x2f);
                        bytes.AddRange(Encoding.UTF8.GetBytes("GblP"));

                        foreach (SDSParameter param in ShaderPrograms[i].Parameters.Where(x => x.Type == ParameterType.GblP))
                        {
                            bytes.AddRange(Encoding.UTF8.GetBytes($" {param.Name}"));
                        }

                        bytes.Add(0x9);
                    }

                    if (ShaderPrograms[i].Parameters.Where(x => x.Type == ParameterType.VsP).Count() > 0)
                    {
                        bytes.Add(0x2f);
                        bytes.AddRange(Encoding.UTF8.GetBytes("VsP"));

                        foreach (SDSParameter param in ShaderPrograms[i].Parameters.Where(x => x.Type == ParameterType.VsP))
                        {
                            bytes.AddRange(Encoding.UTF8.GetBytes($" {param.Name}"));
                        }

                        bytes.Add(0x9);
                    }

                    if (ShaderPrograms[i].Parameters.Where(x => x.Type == ParameterType.PsP).Count() > 0)
                    {
                        bytes.Add(0x2f);
                        bytes.AddRange(Encoding.UTF8.GetBytes("PsP"));

                        foreach (SDSParameter param in ShaderPrograms[i].Parameters.Where(x => x.Type == ParameterType.PsP))
                        {
                            bytes.AddRange(Encoding.UTF8.GetBytes($" {param.Name}"));
                        }

                        bytes.Add(0x9);
                    }

                    //If there are parameters, then replace the last written character as the null terminator
                    bytes[bytes.Count - 1] = 0;
                }
                else
                {
                    bytes.Add(0); //Null byte
                }
            }

            byte[] fileBytes;

            if (IsEMZ)
            {
                EMZ_File emz = new EMZ_File(bytes.ToArray());
                fileBytes = emz.Write();
            }
            else
            {
                fileBytes = bytes.ToArray();
            }

            return fileBytes;
        }

        #endregion

        #region Install
        public List<string> InstallEntries(List<SDSShaderProgram> shaders)
        {
            List<string> ids = new List<string>();

            foreach(var shader in shaders)
            {
                SDSShaderProgram existing = ShaderPrograms.FirstOrDefault(x => x.Name == shader.Name);

                if (existing != null)
                {
                    ShaderPrograms[ShaderPrograms.IndexOf(existing)] = shader;
                }
                else
                {
                    ShaderPrograms.Add(shader);
                }

                ids.Add(shader.Name);
            }

            return ids;
        }

        public void UninstallEntries(List<string> ids, SDS_File cpkSdsFile)
        {
            foreach (string id in ids)
            {
                SDSShaderProgram cpkEntry = cpkSdsFile.ShaderPrograms.FirstOrDefault(x => x.Name == id);
                SDSShaderProgram entry = ShaderPrograms.FirstOrDefault(x => x.Name == id);

                if(cpkEntry != null)
                {
                    ShaderPrograms[ShaderPrograms.IndexOf(entry)] = cpkEntry;
                }
                else
                {
                    ShaderPrograms.Remove(entry);
                }
            }
        }
        #endregion

    }

    public class SDSShaderProgram
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("name")]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("vertexShader_name")]
        public string VertexShader { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("pixelShader_name")]
        public string PixelShader { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SDSParameter")]
        public List<SDSParameter> Parameters { get; set; } = new List<SDSParameter>();

    }

    public class SDSParameter
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("name")]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("type")]
        public ParameterType Type { get; set; }
    }

    public enum ParameterType
    {
        MtxP,
        GblP,
        VsP,
        PsP
    }

    public enum ShaderParameter
    {
        Unknown,
        SkinningEnable, //Enables vs_mtxplt_cb and sets g_bSkinning_VS to true
        W, //g_mW_VS
        VP, //g_mVP_VS
        WVP, //g_mWVP_VS
        WV, //g_mWV_VS
        WLPB_SM, //g_mWLPB_SM_VS
        WLPB_PM, //g_mWLPB_PM_VS
        WLP_PM, //g_mWLP_PM_VS
        WLP_SM, //g_mWLP_SM_VS
        WIT, //g_mWIT_VS
        WVP_Prev, //Enables g_mWVP_Prev_VS, g_mMatrixPalette_VS and g_mMatrixPalettePrev_VS (seems to be mutally exclusive to SkinningEnable, used for velocity shaders?)
        MatCol0_PS,
        MatCol1_PS,
        MatCol2_PS,
        MatCol3_PS,
        MatCol0_VS,
        MatCol1_VS,
        MatCol2_VS,
        MatCol3_VS,
        MatScale0_PS,
        MatScale1_PS,
        MatScale1_VS,
        MatScale0_VS,
        MatOffset0_PS,
        MatOffset1_PS,
        MatOffset0_VS,
        MatOffset1_VS,
        MatDif0_VS, //g_vLightDif0_VS (MatDif from EMM, 1+ are not though)
        MatDif1_VS, //g_vLightDif1_VS
        MatDif2_VS, //g_vLightDif2_VS
        MatDif3_VS, //g_vLightDif3_VS
        MatDif0_PS, //g_vLightDif0_PS (MatDif from EMM)
        MatDif1_PS, //g_vLightDif1_PS
        MatDif2_PS, //g_vLightDif2_PS
        MatDif3_PS, //g_vLightDif3_PS
        MatSpc0_PS, //g_vLightSpc0_PS
        MatSpc1_PS, //g_vLightSpc1_PS
        MatSpc2_PS,
        MatSpc3_PS,
        MatSpc0_VS, //g_vLightSpc0_VS
        MatSpc1_VS,
        MatSpc2_VS,
        MatSpc3_VS, //g_vLightSpc3_VS
        MatAmbUni_VS, //g_vAmbUni_VS
        MatAmbUni_PS, //g_vAmbUni_PS
        MatAmb_VS, //g_vHemiA_VS, g_vHemiB_VS, g_vHemiC_VS
        MatAmb_PS, //g_vHemiA_PS, g_vHemiB_PS, g_vHemiC_PS
        LightVec0_VS,
        LightVec1_VS,
        LightVec2_VS,
        LightVec3_VS,
        LightVec0_PS,
        LightVec1_PS,
        LightVec2_PS,
        LightVec3_PS,
        TexScrl0_VS, //g_TexScroll0_VS
        TexScrl1_VS, //g_TexScroll1_VS
        TexScrl0_PS, //g_TexScroll0_PS
        TexScrl1_PS, //g_TexScroll1_PS
        UserFlag0_VS, //g_vUserFlag0_VS
        UserFlag1_VS, //g_vUserFlag1_VS
        UserFlag2_VS, //g_vUserFlag2_VS
        UserFlag3_VS, //g_vUserFlag3_VS
        EyePos_VS, //g_vEyePos_VS
        Fade_VS, //g_AlphaFade_VS
        FadeColor, //Apply ECF values (g_vFadeMulti_PS, g_vFadeRim_PS and g_vFadeAdd_PS)
        Reflection_VS, //g_Reflection_VS (Possibly sets fog values as well?)
        Reflection_PS, //g_Reflection_PS
        Spc_VS, //g_vSpecular_VS
        Spc_PS, //g_vSpecular_PS
        RimLight_PS, //g_vRimColor_PS and Fade values (add, mult, rim). Can be used with FadeColor, unclear how
        Brush_PS, //g_Brush_PS
        GlareCol_VS, //g_GlareCoeff_VS
        Elapsed_VS, //g_ElapsedTime_VS (g_SystemTime_VS aswell? or this could be something thats always set)
        Elapsed_PS, //g_ElapsedTime_PS
        VsFlag, //g_bVersatile0_VS (likely 1,2,3 as well)
        Incd_VS, //g_Incidence_VS
        SSS_PS, //g_vSubSurface_PS
        SSS_VS, //g_vSubSurface_VS
        Rim_VS, //g_vRim_VS
        Rim_PS, //g_vRim_PS
        Grad_VS, //g_Gradient_VS
        AmbOclColor //Enables g_vAmbOcl_VS and sets g_bAmbOcl_VS to true
    }
}
