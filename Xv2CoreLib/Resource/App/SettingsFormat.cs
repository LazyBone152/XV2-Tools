using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Xv2CoreLib.Resource.App
{
    public class SettingsFormat
    {
        public List<SettingsFormatEntry> Entries = new List<SettingsFormatEntry>();

        public SettingsFormat() { }

        #region LoadSave

        public string Write()
        {
            StringBuilder strBuilder = new StringBuilder();

            foreach(var entry in Entries)
            {
                strBuilder.AppendLine($"{entry.PropertyName}, {entry.TypeString}, {ValueToString(entry.ValueType, entry.Value)}");
            }

            return strBuilder.ToString();
        }

        public static SettingsFormat Load(string path)
        {
            SettingsFormat format = new SettingsFormat();

            int lineNum = 0;
            using(StringReader reader = new StringReader(File.ReadAllText(path)))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] split = line.Split(',');

                    if (split.Length != 3 && split.Length != 0) throw new InvalidDataException($"SettingsFormat.Load: invalid number of arguments on line {lineNum}");
                    
                    if(split.Length == 0)
                    {
                        lineNum++;
                        continue;
                    }

                    SettingsFormatEntry.SettingsValueType type;
                    if(!Enum.TryParse(split[1].Trim(), out type))
                    {
                        type = SettingsFormatEntry.SettingsValueType.NotDefined;
                    }

                    if(type == SettingsFormatEntry.SettingsValueType.NotDefined)
                    {
                        SettingsFormatEntry entry = new SettingsFormatEntry(split[0].Trim(), split[1].Trim(), split[2].Trim());
                        format.Entries.Add(entry);
                    }
                    else
                    {
                        SettingsFormatEntry entry = new SettingsFormatEntry(split[0].Trim(), type, ParseValue(type, split[2].Trim()));
                        format.Entries.Add(entry);
                    }
                    

                    lineNum++;
                }
            }

            return format;
        }

        private static string ValueToString(SettingsFormatEntry.SettingsValueType valueType, object value)
        {
            switch (valueType)
            {
                case SettingsFormatEntry.SettingsValueType.Bool:
                    return Convert.ToString((bool)value);
                case SettingsFormatEntry.SettingsValueType.Int:
                    return Convert.ToString((int)value);
                case SettingsFormatEntry.SettingsValueType.Float:
                    return Convert.ToString((float)value);
                case SettingsFormatEntry.SettingsValueType.String:
                    return (string)value;
                case SettingsFormatEntry.SettingsValueType.UInt:
                    return Convert.ToString((uint)value);
                case SettingsFormatEntry.SettingsValueType.UShort:
                    return Convert.ToString((ushort)value);
                case SettingsFormatEntry.SettingsValueType.Short:
                    return Convert.ToString((short)value);
                case SettingsFormatEntry.SettingsValueType.Double:
                    return Convert.ToString((double)value);
                case SettingsFormatEntry.SettingsValueType.Byte:
                    return Convert.ToString((byte)value);
                case SettingsFormatEntry.SettingsValueType.SByte:
                    return Convert.ToString((sbyte)value);
                case SettingsFormatEntry.SettingsValueType.Long:
                    return Convert.ToString((long)value);
                case SettingsFormatEntry.SettingsValueType.ULong:
                    return Convert.ToString((ulong)value);
                case SettingsFormatEntry.SettingsValueType.AppAccent:
                    return value.ToString();
                case SettingsFormatEntry.SettingsValueType.NotDefined:
                    return value.ToString();
                default:
                    throw new InvalidOperationException("SettingsFormat.ValueToString: Unsupported value type.");
            }
        }

        private static object ParseValue(SettingsFormatEntry.SettingsValueType valueType, string value)
        {
            switch (valueType) 
            {
                case SettingsFormatEntry.SettingsValueType.Bool:
                    return Convert.ToBoolean(value);
                case SettingsFormatEntry.SettingsValueType.Int:
                    return Convert.ToInt32(value);
                case SettingsFormatEntry.SettingsValueType.Float:
                    return Convert.ToSingle(value);
                case SettingsFormatEntry.SettingsValueType.String:
                    return value;
                case SettingsFormatEntry.SettingsValueType.UInt:
                    return Convert.ToUInt32(value);
                case SettingsFormatEntry.SettingsValueType.UShort:
                    return Convert.ToUInt16(value);
                case SettingsFormatEntry.SettingsValueType.Short:
                    return Convert.ToInt16(value);
                case SettingsFormatEntry.SettingsValueType.Byte:
                    return Convert.ToByte(value);
                case SettingsFormatEntry.SettingsValueType.SByte:
                    return Convert.ToSByte(value);
                case SettingsFormatEntry.SettingsValueType.Double:
                    return Convert.ToDouble(value);
                case SettingsFormatEntry.SettingsValueType.Long:
                    return Convert.ToInt64(value);
                case SettingsFormatEntry.SettingsValueType.ULong:
                    return Convert.ToUInt64(value);
                case SettingsFormatEntry.SettingsValueType.AppAccent:
                    return Enum.Parse(typeof(AppAccent), value);
                case SettingsFormatEntry.SettingsValueType.NotDefined:
                    return value;
                default:
                    throw new InvalidOperationException("SettingsFormat.ParseValue: Unsupported value type.");
            }

        }

        #endregion

        public void DeserializeProps(Settings settings)
        {
            //Deserialize all props that have Set and Get methods

            foreach (var prop in settings.GetType().GetProperties())
            {
                if (prop.GetSetMethod() != null && prop.GetGetMethod() != null)
                {
                    var dontSerializeAttr = (DontSerialize[])prop.GetCustomAttributes(typeof(DontSerialize), false);

                    if (dontSerializeAttr.Length == 0)
                    {
                        var entry = GetEntry(prop.Name);

                        if (entry != null)
                        {
                            //Validate value type
                            if (GetValueType(prop) != entry.ValueType) throw new InvalidDataException($"SettingsFormat.DeserializeProps: valueType for prop {prop.Name} does not match.");

                            //Set value
                            prop.SetValue(settings, entry.Value);
                        }
                    }
                }
            }
        }

        public void SerializeProps(Settings settings)
        {
            //Serialize all props that have Set and Get methods

            foreach(var prop in settings.GetType().GetProperties())
            {
                if(prop.GetSetMethod() != null && prop.GetGetMethod() != null)
                {
                    var dontSerializeAttr = (DontSerialize[])prop.GetCustomAttributes(typeof(DontSerialize), false);

                    if(dontSerializeAttr.Length == 0)
                        AddEntry(prop.Name, GetValueType(prop), prop.GetValue(settings));
                }
            }
        }

        private void AddEntry(string propName, SettingsFormatEntry.SettingsValueType valueType, object value)
        {
            var existing = GetEntry(propName);

            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                Entries.Add(new SettingsFormatEntry(propName, valueType, value));
            }
        }

        private SettingsFormatEntry GetEntry(string propName)
        {
            return Entries.FirstOrDefault(x => x.PropertyName == propName);
        }

        private SettingsFormatEntry.SettingsValueType GetValueType(PropertyInfo prop)
        {
            if (prop.PropertyType == typeof(string))
            {
                return SettingsFormatEntry.SettingsValueType.String;
            }
            else if (prop.PropertyType == typeof(int))
            {
                return SettingsFormatEntry.SettingsValueType.Int;
            }
            else if (prop.PropertyType == typeof(uint))
            {
                return SettingsFormatEntry.SettingsValueType.UInt;
            }
            else if (prop.PropertyType == typeof(bool))
            {
                return SettingsFormatEntry.SettingsValueType.Bool;
            }
            else if (prop.PropertyType == typeof(float))
            {
                return SettingsFormatEntry.SettingsValueType.Float;
            }
            else if (prop.PropertyType == typeof(double))
            {
                return SettingsFormatEntry.SettingsValueType.Double;
            }
            else if (prop.PropertyType == typeof(ushort))
            {
                return SettingsFormatEntry.SettingsValueType.UShort;
            }
            else if (prop.PropertyType == typeof(short))
            {
                return SettingsFormatEntry.SettingsValueType.Short;
            }
            else if (prop.PropertyType == typeof(byte))
            {
                return SettingsFormatEntry.SettingsValueType.Byte;
            }
            else if (prop.PropertyType == typeof(sbyte))
            {
                return SettingsFormatEntry.SettingsValueType.SByte;
            }
            else if (prop.PropertyType == typeof(long))
            {
                return SettingsFormatEntry.SettingsValueType.Long;
            }
            else if (prop.PropertyType == typeof(ulong))
            {
                return SettingsFormatEntry.SettingsValueType.ULong;
            }
            else if (prop.PropertyType == typeof(AppAccent))
            {
                return SettingsFormatEntry.SettingsValueType.AppAccent;
            }

            return SettingsFormatEntry.SettingsValueType.NotDefined;
        }

    }

    public class SettingsFormatEntry
    {
        public enum SettingsValueType
        {
            Byte,
            SByte,
            Int,
            UInt,
            UShort,
            Short,
            Long,
            ULong,
            Float,
            Double,
            Bool,
            String,
            AppAccent,
            NotDefined
        }

        public string PropertyName;
        public SettingsValueType ValueType;
        public object Value;
        private string _nonDefinedType;
        public string TypeString
        {
            get
            {
                if (ValueType == SettingsValueType.NotDefined) return _nonDefinedType;
                return ValueType.ToString();
            }
        }

        public SettingsFormatEntry(string propName, SettingsValueType valueType, object value)
        {
            PropertyName = propName;
            ValueType = valueType;
            Value = value;
            _nonDefinedType = null;
        }

        public SettingsFormatEntry(string propName, string nonDefinedValueType, string value)
        {
            PropertyName = propName;
            ValueType = SettingsValueType.NotDefined;
            Value = value;
            _nonDefinedType = nonDefinedValueType;
        }




    }

    public class DontSerialize : Attribute
    {

    }
}
