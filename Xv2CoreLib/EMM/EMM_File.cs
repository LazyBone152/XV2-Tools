using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Xv2CoreLib.HslColor;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace Xv2CoreLib.EMM
{
    [Serializable]
    [YAXSerializeAs("EMM")]
    public class EMM_File : INotifyPropertyChanged
    {

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        [YAXDontSerialize]
        public static Dictionary<int, string> EmmValueTypes { get; set; } = new Dictionary<int, string>()
        {
            { 0, "Float" },
            { 65537, "Int" },
            { 65536, "Float2" },
            { 1, "Bool" }
        };

        [YAXAttributeForClass]
        [YAXSerializeAs("Version")]
        public UInt32 I_08 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Material")]
        public AsyncObservableCollection<EmmMaterial> Materials { get; set; } = new AsyncObservableCollection<EmmMaterial>();
        [YAXSerializeAs("UnknownData")]
        [YAXDontSerializeIfNull]
        public UnknownData Unknown_Data { get; set; }

        //ViewModel
        private EmmMaterial _selectedMaterial = null;
        [YAXDontSerialize]
        public EmmMaterial SelectedMaterial
        {
            get
            {
                return this._selectedMaterial;
            }
            set
            {
                if (value != this._selectedMaterial)
                {
                    this._selectedMaterial = value;
                    NotifyPropertyChanged("SelectedMaterial");
                }
            }
        }

        //Search
        private string _materialSearchFilter = null;
        [YAXDontSerialize]
        public string MaterialSearchFilter
        {
            get
            {
                return this._materialSearchFilter;
            }

            set
            {
                if (value != this._materialSearchFilter)
                {
                    this._materialSearchFilter = value;
                    NotifyPropertyChanged("MaterialSearchFilter");
                }
            }
        }

        [NonSerialized]
        private ListCollectionView _viewMaterials = null;
        [YAXDontSerialize]
        public ListCollectionView ViewMaterials
        {
            get
            {
                if (_viewMaterials != null)
                {
                    return _viewMaterials;
                }
                _viewMaterials = new ListCollectionView(Materials.Binding);
                _viewMaterials.Filter = new Predicate<object>(MaterialFilterCheck);
                return _viewMaterials;
            }
            set
            {
                if (value != _viewMaterials)
                {
                    _viewMaterials = value;
                    NotifyPropertyChanged("ViewMaterials");
                }
            }
        }

        public bool MaterialFilterCheck(object matObject)
        {
            if (String.IsNullOrWhiteSpace(MaterialSearchFilter)) return true;
            var _material = matObject as EmmMaterial;

            if (_material != null)
            {
                if (_material.Name.ToLower().Contains(MaterialSearchFilter.ToLower())) return true;
            }

            return false;
        }

        public void UpdateMaterialFilter()
        {
            if (_viewMaterials == null)
                _viewMaterials = new ListCollectionView(Materials.Binding);

            _viewMaterials.Filter = new Predicate<object>(MaterialFilterCheck);
            NotifyPropertyChanged("ViewMaterials");
        }



        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static EMM_File LoadEmm(byte[] bytes)
        {
            return new Parser(bytes).emmFile;
        }

        /// <summary>
        /// Loads the specified emm file. It can be in either binary or xml format. 
        /// 
        /// If a file can not be found at the specified location, then a empty one will be returned.
        /// </summary>
        public static EMM_File LoadEmm(string path, bool returnEmptyIfNotValid = true)
        {
            if (Path.GetExtension(path) == ".emm")
            {
                return new Xv2CoreLib.EMM.Parser(path, false).GetEmmFile();
            }
            else if (Path.GetExtension(path) == ".xml" && Path.GetExtension(Path.GetFileNameWithoutExtension(path)) == ".emm")
            {
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.EMM.EMM_File), YAXSerializationOptions.DontSerializeNullObjects);
                return (Xv2CoreLib.EMM.EMM_File)serializer.DeserializeFromFile(path);
            }
            else
            {
                if (returnEmptyIfNotValid)
                {
                    return new EMM_File()
                    {
                        I_08 = 0,
                        Materials = AsyncObservableCollection<EmmMaterial>.Create()
                    };
                }
                else
                {
                    throw new FileNotFoundException("An .emm could not be found at the specified location.");
                }

            }
        }

        public void SaveXmlEmmFile(string saveLocation)
        {
            if (!Directory.Exists(Path.GetDirectoryName(saveLocation)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveLocation));
            }
            YAXSerializer serializer = new YAXSerializer(typeof(EMM_File));
            serializer.SerializeToFile(this, saveLocation);
        }

        public void SaveBinaryEmmFile(string saveLocation)
        {
            if (!Directory.Exists(Path.GetDirectoryName(saveLocation)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveLocation));
            }
            new Deserializer(saveLocation, this);
        }

        public static EMM_File DefaultEmmFile()
        {
            return new EMM_File()
            {
                I_08 = 16,
                Materials = AsyncObservableCollection<EmmMaterial>.Create(),
                Unknown_Data = new UnknownData()
            };
        }

        public EmmMaterial GetMaterial(string name)
        {
            foreach(var e in Materials)
            {
                if(e.Name == name)
                {
                    return e;
                }
            }

            return null;
        }

        public EmmMaterial GetEntry(int index)
        {
            if (index >= Materials.Count || index < 0)
            {
                throw new InvalidDataException(String.Format("EMM_File.GetEntry (int index): index out of range.\nindex = {0}", index));
            }

            return Materials[index];
        }

        public EmmMaterial Compare(EmmMaterial emmEntry2)
        {
            foreach (var entry in Materials)
            {
                if (entry.Compare(emmEntry2))
                {
                    return entry;
                }
            }

            return null;
        }

        public string GetUnusedName(string name)
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name);
            string newName = name;
            int num = 1;

            while (NameUsed(newName))
            {
                newName = String.Format("{0}_{1}{2}", nameWithoutExtension, num, extension);
                num++;
            }

            return newName;
        }

        public bool NameUsed(string name)
        {
            foreach (var entry in Materials)
            {
                if (entry.Name == name) return true;

            }

            return false;
        }

        public void Validate(bool typing = false)
        {
            foreach(var mat in Materials)
            {
                mat.Validate(typing);
            }
        }

        public void ValidateNames()
        {
            List<string> names = new List<string>();

            for(int i = 0; i < Materials.Count; i++)
            {
                if (names.Contains(Materials[i].Name))
                {
                    //Name was used previously
                    Materials[i].Name = GetUnusedName(Materials[i].Name);
                }
                else
                {
                    //Name is unused
                    names.Add(Materials[i].Name);
                }
            }
        }

        public List<RgbColor> GetUsedColors()
        {
            List<RgbColor> colors = new List<RgbColor>();
            if (Materials == null) return colors;

            foreach(var mat in Materials)
            {
                colors.AddRange(mat.GetUsedColors());
            }

            return colors;
        }

        public void ChangeHsl(double hue, double saturation, double lightness, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (Materials == null) return;
            if (undos == null) undos = new List<IUndoRedo>();

            foreach (var mat in Materials)
            {
                mat.ChangeHsl(hue, saturation, lightness, undos, hueSet, variance);
            }
        }
    }

    [Serializable]
    [YAXSerializeAs("Material")]
    public class EmmMaterial : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //values
        private string _name = null;
        private string _shader = null;

        //properties
        [YAXAttributeForClass]
        public int Index { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if(value.Length > 32)
                {
                    _name = value.Remove(32, value.Length - 32);
                    NotifyPropertyChanged(nameof(Name));
                }
                else if (value != this._name)
                {
                    this._name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Shader")]
        public string ShaderProgram
        {
            get
            {
                return this._shader;
            }
            set
            {
                if (value.Length > 32)
                {
                    _shader = value.Remove(32, value.Length - 32);
                    NotifyPropertyChanged(nameof(ShaderProgram));
                    NotifyPropertyChanged(nameof(UndoableShader));
                }
                else if (value != this._shader)
                {
                    this._shader = value;
                    NotifyPropertyChanged(nameof(ShaderProgram));
                    NotifyPropertyChanged(nameof(UndoableShader));
                }
            }
        }
        [YAXAttributeFor("I_66")]
        [YAXSerializeAs("value")]
        public ushort I_66 { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Parameter")]
        public AsyncObservableCollection<Parameter> Parameters { get; set; }

        #region View
        [YAXDontSerialize]
        public AsyncObservableCollection<Parameter> SelectedParameters { get; } = AsyncObservableCollection<Parameter>.Create();
        private Parameter _selectedParameter = null;
        [YAXDontSerialize]
        public Parameter SelectedParameter
        {
            get
            {
                return this._selectedParameter;
            }
            set
            {
                if (value != this._selectedParameter)
                {
                    this._selectedParameter = value;
                    NotifyPropertyChanged(nameof(SelectedParameter));
                }
            }
        }

        [YAXDontSerialize]
        public string UndoableShader
        {
            get
            {
                return ShaderProgram;
            }
            set
            {
                if(ShaderProgram != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<EmmMaterial>(nameof(ShaderProgram), this, ShaderProgram, value, "Shader"));
                    ShaderProgram = value;
                }
            }
        }
        #endregion


        /// <summary>
        /// Adds a prefix to the start of the name, and trims the string if required to stay within the 32 char limit. Returns the modified name but does not change it.
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public string AddPrefixToName(string prefix)
        {
            StringBuilder newName = new StringBuilder((string)Name.Clone());

            while(newName.Length > 25)
            {
                newName.Remove(0, 1);
            }

            return String.Format("{0}{1}", prefix, newName.ToString());
        }

        public bool Compare(EmmMaterial material2)
        {
            //Name can be different. We only care about the shader type and parameters.

            if (ShaderProgram != material2.ShaderProgram)
                return false;

            if (I_66 != material2.I_66)
                return false;

            if (material2.Parameters.Count != Parameters.Count)
                return false;

            for(int i = 0; i < Parameters.Count; i++)
            {
                if (!Parameters[i].Compare(material2.Parameters[i]))
                    return false;
            }

            return true;
        }

        public void Validate(bool typing = false)
        {
            foreach(var param in Parameters)
            {
                param.Validate(typing);
            }
        }

        public static EmmMaterial NewMaterial()
        {
            var param = AsyncObservableCollection<Parameter>.Create();
            param.Add(Parameter.NewParameter());

            return new EmmMaterial()
            {
                Name = "NewMaterial",
                ShaderProgram = "ParticleDecal",
                Parameters = param
            };
        }

        public EmmMaterial Clone()
        {
            EmmMaterial newMaterial = new EmmMaterial();
            newMaterial.Name = Name;
            newMaterial.ShaderProgram = ShaderProgram;
            newMaterial.I_66 = I_66;
            newMaterial.Parameters = AsyncObservableCollection<Parameter>.Create();

            foreach(var param in Parameters)
            {
                newMaterial.Parameters.Add(param.Clone());
            }

            return newMaterial;
        }

        /// <summary>
        /// Ensure that all RGB components always exist for any color parameter.
        /// </summary>
        public void SyncColorParameters()
        {
            foreach(var param in Parameter.ColorParameters)
            {
                var r = GetParameter(string.Format("{0}R", param));
                var g = GetParameter(string.Format("{0}G", param));
                var b = GetParameter(string.Format("{0}B", param));

                if(r != null || g != null || b != null)
                {
                    if(r == null)
                    {
                        Parameters.Add(new Parameter()
                        {
                            Name = string.Format("{0}R", param),
                            value = "0.0",
                            Type = "Float"
                        });
                    }
                    if (g == null)
                    {
                        Parameters.Add(new Parameter()
                        {
                            Name = string.Format("{0}G", param),
                            value = "0.0",
                            Type = "Float"
                        });
                    }
                    if (b == null)
                    {
                        Parameters.Add(new Parameter()
                        {
                            Name = string.Format("{0}B", param),
                            value = "0.0",
                            Type = "Float"
                        });
                    }


                }

            }
        }

        public void ChangeHsl(double hue, double saturation = 0.0, double lightness = 0.0, List<IUndoRedo> undos = null, bool hueSet = false, int variance = 0)
        {
            if (Parameters == null) return;

            foreach (var param in Parameter.ColorParameters)
            {
                var r = GetParameter(string.Format("{0}R", param));
                var g = GetParameter(string.Format("{0}G", param));
                var b = GetParameter(string.Format("{0}B", param));

                if (r != null && g != null && b != null)
                {
                    //Create rgbColor, divide by 10
                    RgbColor rgbColor = new RgbColor(r.Float, g.Float, b.Float);
                    var hslColor = rgbColor.ToHsl();
                    RgbColor newColor;

                    if (hueSet)
                    {
                        hslColor.SetHue(hue, variance);
                    }
                    else
                    {
                        hslColor.ChangeHue(hue);
                        hslColor.ChangeLightness(lightness);
                        hslColor.ChangeSaturation(saturation);
                    }

                    newColor = hslColor.ToRgb();

                    undos.Add(new UndoableProperty<Parameter>(nameof(Parameter.value), r, r.value, newColor.R.ToString()));
                    undos.Add(new UndoableProperty<Parameter>(nameof(Parameter.value), g, g.value, newColor.G.ToString()));
                    undos.Add(new UndoableProperty<Parameter>(nameof(Parameter.value), b, b.value, newColor.B.ToString()));

                    //Add new color to parameters, multiply by 10
                    r.value = (newColor.R).ToString();
                    g.value = (newColor.G).ToString();
                    b.value = (newColor.B).ToString();
                }
            }
        }

        #region Get
        public float[] GetVector4(string name)
        {
            var param = Parameters.FirstOrDefault(x => x.Name == name);
            if (param != null)
            {
                return new float[] { param.Float, param.Float, param.Float, param.Float };
            }

            float val0 = float.Parse(GetValue($"{name}X")); 
            float val1 = float.Parse(GetValue($"{name}Y"));
            float val2 = float.Parse(GetValue($"{name}Z"));
            float val3 = float.Parse(GetValue($"{name}W"));
            return new float[] { val0, val1, val2, val3 };
        }

        public float[] GetColor(string name)
        {
            var param = Parameters.FirstOrDefault(x => x.Name == name);
            if (param != null)
            {
                return new float[] { param.Float, param.Float, param.Float, param.Float };
            }

            float val0 = float.Parse(GetValue($"{name}R"));
            float val1 = float.Parse(GetValue($"{name}G"));
            float val2 = float.Parse(GetValue($"{name}B"));
            float val3 = float.Parse(GetValue($"{name}A"));
            return new float[] { val0, val1, val2, val3 };
        }

        public float[] GetUV(string name)
        {
            float val0 = float.Parse(GetValue($"{name}U"));
            float val1 = float.Parse(GetValue($"{name}V"));
            return new float[] { val0, val1 };
        }

        public string GetValue(string parameter, bool returnDefault = false)
        {
            foreach (var e in Parameters)
            {
                if (e.Name == parameter)
                {
                    return e.value;
                }
            }

            return returnDefault ? "0" : null;
        }

        public Parameter GetParameter(string parameterName)
        {
            if (Parameters == null) Parameters = AsyncObservableCollection<Parameter>.Create();

            foreach (var parameter in Parameters)
            {
                if (parameter.Name == parameterName) return parameter;
            }

            return null;
        }

        public List<RgbColor> GetUsedColors()
        {
            SyncColorParameters();
            List<RgbColor> colors = new List<RgbColor>();
            if (Parameters == null) return colors;

            foreach (var param in Parameter.ColorParameters)
            {
                var r = GetParameter(string.Format("{0}R", param));
                var g = GetParameter(string.Format("{0}G", param));
                var b = GetParameter(string.Format("{0}B", param));

                if (r != null && g != null && b != null)
                {
                    var color = new RgbColor(r.Float, g.Float, b.Float);

                    if (!color.IsWhiteOrBlack)
                    {
                        colors.Add(color);
                    }
                }
            }

            return colors;
        }

        #endregion
    }

    [Serializable]
    [YAXSerializeAs("Parameter")]
    public class Parameter : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        
        [YAXDontSerialize]
        /// <summary>
        /// Color parameters for effects. Append R, G, B, A.
        /// </summary>
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

        //values
        private string _str_00 = null;

        //Properties
        [YAXAttributeForClass]
        [YAXSerializeAs("Name")]
        public string Name
        {
            get
            {
                return this._str_00;
            }
            set
            {
                if (value.Length > 32)
                {
                    _str_00 = value.Remove(32, value.Length - 32);
                    NotifyPropertyChanged(nameof(Name));
                    NotifyPropertyChanged(nameof(UndoableName));
                }
                else if (value != this._str_00)
                {
                    this._str_00 = value;
                    NotifyPropertyChanged(nameof(Name));
                    NotifyPropertyChanged(nameof(UndoableName));
                }
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public string Type { get; set; } //int16, but write it twice (so it takes up 4 bytes)
        private string _value = null;
        [YAXAttributeForClass]
        [YAXSerializeAs("value")]
        public string value  //can be Int32 or Float, depending on I_32
        {
            get
            {
                return this._value;
            }
            set
            {
                if (value != this._value)
                {
                    this._value = value;
                    NotifyPropertyChanged("value");
                    NotifyPropertyChanged(nameof(UndoableValue));
                }
            }
        }

        //Value Properties
        [YAXDontSerialize]
        public float Float
        {
            get
            {
                float _float;
                if(float.TryParse(value, out _float))
                {
                    return _float;
                }
                return 0f;
            }
        }
        [YAXDontSerialize]
        public int IntValue
        {
            get
            {
                int _intValue;
                if (int.TryParse(value, out _intValue))
                {
                    return _intValue;
                }
                return 0;
            }
        }

        #region View
        [YAXDontSerialize]
        public string UndoableName
        {
            get
            {
                return Name;
            }
            set
            {
                if (Name != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<Parameter>(nameof(Name), this, Name, value, "Name"));
                    Name = value;
                }
            }
        }
        [YAXDontSerialize]
        public string UndoableType
        {
            get
            {
                return Type;
            }
            set
            {
                if (Type != value)
                {
                    UndoManager.Instance.AddUndo(new CompositeUndo(new List<IUndoRedo>() { new UndoableProperty<Parameter>(nameof(Type), this, Type, value), new UndoActionDelegate(this, nameof(RefreshProperties), true) }, "Type"));
                    Type = value;
                }
            }
        }
        [YAXDontSerialize]
        public string UndoableValue
        {
            get
            {
                return value;
            }
            set
            {
                if (_value != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<Parameter>("value", this, _value, value, "Value"));
                    _value = value;
                }
            }
        }
        
        public void RefreshProperties()
        {
            NotifyPropertyChanged(nameof(Name));
            NotifyPropertyChanged(nameof(Type));
            NotifyPropertyChanged(nameof(value));
            NotifyPropertyChanged(nameof(UndoableName));
            NotifyPropertyChanged(nameof(UndoableType));
            NotifyPropertyChanged(nameof(UndoableValue));
        }
        #endregion

        public bool Compare(Parameter parameter2)
        {
            if (Name != parameter2.Name)
                return false;
            
            if (Type != parameter2.Type)
                return false;

            if (value != parameter2.value)
                return false;

            return true;
        }

        public void Validate(bool typing = false)
        {
            //Only validate Ints when typing == true.

            switch (Type)
            {
                case "Float":
                case "Float2":
                    if(!typing)
                    {
                        float ret;
                        bool success = float.TryParse(value, out ret);
                        if (!success)
                        {
                            ret = 0;
                        }
                        value = ret.ToString();
                    }
                    break;
                case "Bool":
                    if(value != "true" && value != "false" && !typing)
                    {
                        if (value == "1" || value.ToLower() == "true")
                        {
                            value = "true";
                        }
                        else
                        {
                            value = "false";
                        }
                    }
                    break;
                default:
                case "Int":
                    {
                        int ret;
                        bool success = int.TryParse(value, out ret);
                        if (!success)
                        {
                            ret = 0;
                        }
                        value = ret.ToString();
                    }
                    break;
            }
        }
        
        public static Parameter NewParameter()
        {
            return new Parameter()
            {
                Name = "ParameterName",
                Type = "Int",
                value = "0"
            };
        }

        public Parameter Clone()
        {
            return new Parameter()
            {
                Type = Type,
                Name = Name,
                value = value
            };
        }
    }

    [Serializable]
    public class UnknownData
    {
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
    }

}
