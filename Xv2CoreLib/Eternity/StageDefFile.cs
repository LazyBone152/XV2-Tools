using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using YAXLib;
using System.Text;

namespace Xv2CoreLib.Eternity
{
    [YAXSerializeAs("Xv2StageDef")]
    public class StageDefFile
    {
        public const string PATH = "xv2_stage_def.xml";
        private const int SUPPORTED_VERSION = 0;

        private static StageDefFile _defaultFile = null;
        /// <summary>
        /// Gets the default instance of "xv2_stage_def.xml".
        /// </summary>
        public static StageDefFile DefaultFile
        {
            get
            {
                if(_defaultFile == null)
                {
                    _defaultFile = Parse(Properties.Resources.xv2_stage_def);
                }
                return _defaultFile;
            }
        }

        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string Version { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Stage")]
        public List<StageDef> Stages { get; set; }

        #region LoadSave
        //Load:
        public static StageDefFile Load(string path)
        {
            string text = File.ReadAllText(path);
            return Parse(text);
        }

        public static StageDefFile Load(byte[] bytes)
        {
            return Parse(Encoding.ASCII.GetString(bytes));
        }

        private static StageDefFile Parse(string xmlText)
        {
            //Read xml
            XDocument xml;

            using (StringReader reader = new StringReader(xmlText))
                xml = XDocument.Load(reader);

            //Version check
            XAttribute versionAttr = xml.Element("Xv2StageDef").Attribute("Version");

            if (versionAttr != null)
            {
                if (!int.TryParse(versionAttr.Value, out int version))
                    throw new InvalidDataException("StageDefFile.Load: Version is not in the correct format.");

                if (version > SUPPORTED_VERSION)
                    throw new InvalidDataException("StageDefFile.Load: This xv2_stage_def.xml version is not supported by the installer. An update is required.");
            }

            //Deserialize XML
            YAXSerializer serializer = new YAXSerializer(typeof(StageDefFile), YAXSerializationOptions.DontSerializeNullObjects);
            return (StageDefFile)serializer.Deserialize(xml.Root);
        }

        //Save:
        public void SaveToDisk(string path)
        {
            var xml = Write();
            xml.Save(path);
        }

        public byte[] SaveToBytes()
        {
            byte[] bytes;
            var xml = Write();

            using (MemoryStream ms = new MemoryStream())
            {
                xml.Save(ms);
                bytes = ms.ToArray();
            }

            return bytes;
        }

        private XDocument Write()
        {
            Stages?.Sort((x, y) => (int)x.Index - (int)y.Index);

            //Serialize XML
            YAXSerializer serializer = new YAXSerializer(typeof(StageDefFile));
            return serializer.SerializeToXDocument(this);
        }

        #endregion

        #region Install
        public List<string> InstallStages(List<StageDef> stages)
        {
            List<string> ids = new List<string>();

            foreach (var stage in stages)
            {
                int index = Stages.IndexOf(Stages.FirstOrDefault(x => x.Index == stage.Index));

                if (index == -1)
                {
                    Stages.Add(stage);
                }
                else
                {
                    Stages[index] = stage;
                }

                ids.Add(stage.Index.ToString());
            }

            return ids;
        }

        public void UninstallStages(List<string> ids)
        {
            foreach (string stringId in ids)
            {
                int id;

                if (int.TryParse(stringId, out id))
                {
                    Stages.RemoveAll(x => x.Index == id);

                    //Restore original entries
                    StageDef originalEntry = DefaultFile.Stages.FirstOrDefault(x => x.Index == id);

                    if(originalEntry != null)
                    {
                        Stages.Add(originalEntry.Copy());
                    }
                }

            }
        }
        
        #endregion
    }

    [YAXSerializeAs("Stage")]
    [Serializable]
    public class StageDef
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("idx")]
        public uint Index { get; set; }
        [YAXDontSerialize]
        public ushort SSID { get; set; } //Really, a byte
        [YAXAttributeForClass]
        [YAXSerializeAs("ssid")]
        [YAXDontSerializeIfNull]
        public string SSID_XmlBinding
        {
            get => SSID != ushort.MaxValue ? SSID.ToString() : null;
            set => SSID = value != null ? (ushort)Utils.TryParseInt(value) : ushort.MaxValue;
        }

        [YAXAttributeFor("BASE_DIR")]
        [YAXSerializeAs("value")]
        public string BASE_DIR { get; set; }
        [YAXAttributeFor("CODE")]
        [YAXSerializeAs("value")]
        public string CODE { get; set; }
        [YAXAttributeFor("DIR")]
        [YAXSerializeAs("value")]
        public string DIR { get; set; }
        [YAXAttributeFor("STR4")]
        [YAXSerializeAs("value")]
        public string STR4 { get; set; }
        [YAXAttributeFor("EVE")]
        [YAXSerializeAs("value")]
        public string EVE { get; set; }
        [YAXAttributeFor("UNK5")]
        [YAXSerializeAs("value")]
        public ulong UNK5 { get; set; }
        [YAXAttributeFor("F6")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        public float KI_BLAST_SIZE_LIMIT { get; set; } 
        [YAXAttributeFor("SE")]
        [YAXSerializeAs("value")]
        public string SE { get; set; }
        [YAXAttributeFor("BGM_CUE_ID")]
        [YAXSerializeAs("value")]
        public uint BGM_CUE_ID { get; set; }

        [YAXAttributeFor("NAME_EN")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_EN { get; set; }
        [YAXAttributeFor("NAME_FR")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_FR { get; set; }
        [YAXAttributeFor("NAME_IT")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_IT { get; set; }
        [YAXAttributeFor("NAME_DE")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_DE { get; set; }
        [YAXAttributeFor("NAME_ES")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_ES { get; set; }
        [YAXAttributeFor("NAME_CA")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_CA { get; set; }
        [YAXAttributeFor("NAME_PT")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_PT { get; set; }
        [YAXAttributeFor("NAME_KR")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_KR { get; set; }
        [YAXAttributeFor("NAME_TW")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_TW { get; set; }
        [YAXAttributeFor("NAME_ZH")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_ZH { get; set; }
        [YAXAttributeFor("NAME_PL")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_PL { get; set; }
        [YAXAttributeFor("NAME_RU")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string NAME_RU { get; set; }

        [YAXAttributeFor("OVERRIDE_FAR_CLIP")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 0.0f)]
        public float OVERRIDE_FAR_CLIP { get; set; }

        [YAXAttributeFor("LIMIT")]
        [YAXSerializeAs("value")]
        [YAXFormat("0.0###########")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore, DefaultValue = 500.0f)]
        public float LIMIT { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Gate")]
        [YAXDontSerializeIfNull]
        public List<GateDef> Gates { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "GateGBB")]
        [YAXDontSerializeIfNull]
        public List<GateDef> GatesGGB { get; set; }


        //Optional, for Installer purposes:
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("AddToStageSelect")]
        public string AddToStageSelect_String { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        [YAXSerializeAs("AddToStageSelectLocal")]
        public string AddToStageSelectLocal_String { get; set; }

        [YAXDontSerialize]
        public bool AddToStageSelect => !string.IsNullOrWhiteSpace(AddToStageSelect_String) ? AddToStageSelect_String.Equals("true", StringComparison.OrdinalIgnoreCase) : false;
        [YAXDontSerialize]
        public bool AddToStageSelectLocal => !string.IsNullOrWhiteSpace(AddToStageSelectLocal_String) ? AddToStageSelectLocal_String.Equals("true", StringComparison.OrdinalIgnoreCase) : false;

        public StageDef()
        {
            BASE_DIR = "data/stage/";
            UNK5 = 0;
            SSID = 0xFF;
            KI_BLAST_SIZE_LIMIT = 200.0f;
            BGM_CUE_ID = 0;
            OVERRIDE_FAR_CLIP = 0.0f;
            LIMIT = 500.0f;
        }
    }

    [YAXSerializeAs("Gate")]
    [Serializable]
    public class GateDef
    {
        [YAXAttributeFor("NAME")]
        [YAXSerializeAs("value")]
        public string NAME { get; set; }

        [CustomSerialize(MaxHex = true)]
        public uint TARGET_STAGE_IDX { get; set; }
        [CustomSerialize(IsHex = true)]
        public uint U_0C { get; set; }
        [CustomSerialize(IsHex = true)]
        public uint U_10 { get; set; }

        [CustomSerialize]
        [YAXDontSerializeIfNull]
        public string U_14 { get; set; }

        [CustomSerialize]
        [YAXDontSerializeIfNull]
        public string U_18 { get; set; }

        public GateDef()
        {
            TARGET_STAGE_IDX = uint.MaxValue;
            U_10 = 1;
        }
    }
}
