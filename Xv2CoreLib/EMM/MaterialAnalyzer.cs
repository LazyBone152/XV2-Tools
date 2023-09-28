using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.EMM.Analyzer
{
    public class MaterialAnalyzer
    {
        #region Singleton
        private static Lazy<MaterialAnalyzer> instance = new Lazy<MaterialAnalyzer>(() => new MaterialAnalyzer());
        public static MaterialAnalyzer Instance => instance.Value;

        private MaterialAnalyzer()
        {
            LoadXml();
        }
        #endregion

        public ShaderHelper ShaderHelper { get; set; }

        #region LoadSave

        private void LoadXml()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(ShaderHelper), YAXSerializationOptions.DontSerializeNullObjects);
            ShaderHelper = (ShaderHelper)serializer.Deserialize(Properties.Resources.ShaderHelper);
        }

        public void AnalyzeMaterials()
        {
            const string path = @"D:\VS_Test\EMM\ALL EMM - XV1 included";

            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            ShaderHelper shaders = new ShaderHelper();

            List<string> parameters = new List<string>();

            foreach (var file in files)
            {
                Console.WriteLine(file);

                byte[] bytes = File.ReadAllBytes(file);

                if (BitConverter.ToInt32(bytes, 0) != Parser.EMM_SIGNATURE)
                    continue;

                EMM_File emmFile = EMM_File.LoadEmm(bytes);

                foreach (EmmMaterial material in emmFile.Materials)
                {
                    shaders.AnalyzeShader(material, parameters);
                }
            }

            shaders.AllParameters = parameters.ToArray();

            YAXSerializer serializer = new YAXSerializer(typeof(ShaderHelper));
            serializer.SerializeToFile(shaders, "ShaderHelper.xml");
        }

        #endregion

        #region Parameters
        internal string[] UV_Parameters = new string[]
        {
            "ToonSamplerAddress",
            "MarkSamplerAddress",
            "MaskSamplerAddress",
            "TexRep0",
            "TexRep1",
            "TexRep2",
            "TexRep3",
            "TexScrl0",
            "TexScrl1",
            "gScroll0",
            "gScroll1"
        };

        internal string[] RGBA_Parameters = new string[]
        {
            "MatCol0",
            "MatCol1",
            "MatCol2",
            "MatCol3",
            "GlareCol",
            "MatDif",
            "MatAmb",
            "MatSpc",
            "gLightDir",
            "gLightDif",
            "gLightSpc",
            "gLightAmb",
            "DirLight0Dir",
            "DirLight0Col",
            "AmbLight0Col",
            "Ambient",
            "Diffuse",
            "Specular",
            "gGradientCol"
        };

        internal string[] XYZW_Parameters = new string[]
        {
            "gCamPos",
            "MatScale0",
            "MatScale1",
            "MatOffset0",
            "MatOffset1"
        };

        internal string GetUVParameter(string parameter)
        {
            foreach (var uv in UV_Parameters)
            {
                if (parameter == $"{uv}U" || parameter == $"{uv}V" || parameter == uv)
                    return uv;
            }

            return null;
        }

        internal string GetRGBAParameter(string parameter)
        {
            foreach (var uv in RGBA_Parameters)
            {
                if (parameter == $"{uv}R" || parameter == $"{uv}G" || parameter == $"{uv}B" || parameter == $"{uv}A" || uv == parameter)
                    return uv;
            }

            return null;
        }

        internal string GetXYZWParameter(string parameter)
        {
            foreach (var uv in XYZW_Parameters)
            {
                if (parameter == $"{uv}X" || parameter == $"{uv}Y" || parameter == $"{uv}Z" || parameter == $"{uv}W" || uv == parameter)
                    return uv;
            }

            return null;
        }
        #endregion
    }

    public class ShaderHelper
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Shader")]
        public List<AnalyzedShader> Shaders { get; set; } = new List<AnalyzedShader>();

        public string[] AllParameters { get; set; }

        public void Init()
        {
            if (Shaders == null) Shaders = new List<AnalyzedShader>();

            foreach (var shader in Shaders)
            {
                if (shader.Parameters == null)
                    shader.Parameters = new List<AnalyzedParameter>();
            }
        }

        public void AnalyzeShader(EmmMaterial material, List<string> parameters)
        {
            AnalyzedShader shader = Shaders.FirstOrDefault(x => x.ShaderProgram == material.ShaderProgram);

            if (shader == null)
            {
                shader = new AnalyzedShader();
                shader.ShaderProgram = material.ShaderProgram;
                Shaders.Add(shader);
            }

            shader.ParseParameters(material, parameters);
        }
    }

    public class AnalyzedShader
    {
        [YAXAttributeForClass]
        public string ShaderProgram { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Parameter")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public List<AnalyzedParameter> Parameters { get; set; } = new List<AnalyzedParameter>();

        public void ParseParameters(EmmMaterial material, List<string> parameters)
        {
            if (material.ShaderProgram == ShaderProgram)
            {
                foreach (Parameter param in material.Parameters)
                {
                    string vector = MaterialAnalyzer.Instance.GetUVParameter(param.Name);

                    if (vector == null)
                        vector = MaterialAnalyzer.Instance.GetRGBAParameter(param.Name);

                    if (vector == null)
                        vector = MaterialAnalyzer.Instance.GetXYZWParameter(param.Name);

                    if (vector != null)
                    {
                        AddParameter(vector);

                        if (!parameters.Contains(vector))
                            parameters.Add(vector);
                    }
                    else
                    {
                        AddParameter(param.Name);

                        if (!parameters.Contains(param.Name))
                            parameters.Add(param.Name);
                    }

                }
            }
        }

        private void AddParameter(string parameter)
        {
            if (Parameters.FirstOrDefault(x => x.Name == parameter) == null)
            {
                Parameters.Add(new AnalyzedParameter(parameter));
            }
        }
    }

    public class AnalyzedParameter
    {
        [YAXAttributeForClass]
        public string Name { get; set; }

        public AnalyzedParameter(string parameter)
        {
            Name = parameter;
        }
        
        public AnalyzedParameter() { }
    }
}
