using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.AUR
{
    [YAXSerializeAs("AUR")]
    public class AUR_File : ISorting
    {
        public const uint AUR_SIGNATURE = 0x52554123;

        public List<AUR_Type> AuraTypes { get; set; } = new List<AUR_Type>();
        [BindingSubList]
        public List<AUR_Aura> Auras { get; set; } = new List<AUR_Aura>();
        [BindingSubList]
        public List<AUR_Character> CharacterAuras { get; set; } = new List<AUR_Character>();

        public void SortEntries()
        {
            if (Auras != null)
                Auras.Sort((x, y) => x.SortID - y.SortID);
            //if (CharacterAuras != null)
                //CharacterAuras.Sort((x, y) => x.SortID - y.SortID);
        }

        public static AUR_File Serialize(string path, bool writeXml)
        {
            byte[] rawBytes = File.ReadAllBytes(path);

            AUR_File file = Load(rawBytes);

            //Write Xml
            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(AUR_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static AUR_File Load(byte[] bytes)
        {
            AUR_File aurFile = new AUR_File();

            //Header
            int auraCount = BitConverter.ToInt32(bytes, 8);
            int auraOffset = BitConverter.ToInt32(bytes, 12);
            int auraTypeCount = BitConverter.ToInt32(bytes, 16);
            int auraTypeOffset = BitConverter.ToInt32(bytes, 20);
            int charaCount = BitConverter.ToInt32(bytes, 24);
            int charaOffset = BitConverter.ToInt32(bytes, 28);

            //AuraTypes
            for(int i = 0; i < auraTypeCount; i++)
            {
                aurFile.AuraTypes.Add(new AUR_Type()
                {
                    Type = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, auraTypeOffset + (4 * i)), false)
                });
            }

            //Auras
            for(int i = 0; i < auraCount; i++)
            {
                AUR_Aura aura = new AUR_Aura();

                aura.Index = BitConverter.ToInt32(bytes, auraOffset + 0).ToString();
                aura.I_04 = BitConverter.ToInt32(bytes, auraOffset + 4);

                int effectCount = BitConverter.ToInt32(bytes, auraOffset + 8);
                int effectOffset = BitConverter.ToInt32(bytes, auraOffset + 12);

                for(int a = 0; a < effectCount; a++)
                {
                    aura.AuraEffects.Add(new AUR_Effect()
                    {
                        Type = aurFile.GetAuraType(BitConverter.ToInt32(bytes, effectOffset)),
                        I_04 = BitConverter.ToInt32(bytes, effectOffset + 4)
                    });
                    effectOffset += 8;
                }

                auraOffset += 16;

                if(effectCount > 0)
                    aurFile.Auras.Add(aura);
            }

            //Characters
            for(int i = 0; i < charaCount; i++)
            {
                aurFile.CharacterAuras.Add(new AUR_Character()
                {
                    CharaID = BitConverter.ToInt32(bytes, charaOffset + (16 * i) + 0).ToString(),
                    I_04 = BitConverter.ToInt32(bytes, charaOffset + (16 * i) + 4),
                    I_08 = BitConverter.ToInt32(bytes, charaOffset + (16 * i) + 8).ToString(),
                    I_12 = Convert.ToBoolean(BitConverter.ToInt32(bytes, charaOffset + (16 * i) + 12)),
                });
            }

            return aurFile;
        }

        public byte[] SaveToBytes()
        {
            PadWithNullEntries();
            SortEntries();
            List<byte> bytes = new List<byte>();

            //Counts
            int auraTypeCount = (AuraTypes != null) ? AuraTypes.Count : 0;
            int auraCount = (Auras != null) ? Auras.Count : 0;
            int characterCount = (CharacterAuras != null) ? CharacterAuras.Count : 0;

            //Header (32 bytes)
            bytes.AddRange(BitConverter.GetBytes(AUR_SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes((ushort)65534));
            bytes.AddRange(BitConverter.GetBytes((ushort)32));
            bytes.AddRange(BitConverter.GetBytes(auraCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(auraTypeCount));
            bytes.AddRange(new byte[4]);
            bytes.AddRange(BitConverter.GetBytes(characterCount));
            bytes.AddRange(new byte[4]);

            //Auras
            int auraOffset = bytes.Count;
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(auraOffset), 12);

            for(int i = 0; i < auraCount; i++)
            {
                int effectCount = (Auras[i].AuraEffects != null) ? Auras[i].AuraEffects.Count : 0;

                bytes.AddRange(BitConverter.GetBytes(int.Parse(Auras[i].Index)));
                bytes.AddRange(BitConverter.GetBytes(Auras[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(effectCount));
                bytes.AddRange(new byte[4]); //offset to replace later
            }

            //Write aura effects
            for(int i = 0; i < auraCount; i++)
            {
                //Fill in offset
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), auraOffset + (i * 16) + 12);

                int effectCount = (Auras[i].AuraEffects != null) ? Auras[i].AuraEffects.Count : 0;

                for (int a = 0; a < effectCount; a++)
                {
                    bytes.AddRange(BitConverter.GetBytes(GetAuraType(Auras[i].AuraEffects[a].Type)));
                    bytes.AddRange(BitConverter.GetBytes(Auras[i].AuraEffects[a].I_04));
                }
            }

            //Write AuraTypes
            int auraTypeOffset = bytes.Count;
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(auraTypeOffset), 20);

            //Offsets
            bytes.AddRange(new byte[4 * auraTypeCount]);

            //Write strings, fill offsets
            int strSize = 0;
            for(int i = 0; i < auraTypeCount; i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), auraTypeOffset + (4 * i));
                bytes.AddRange(Encoding.ASCII.GetBytes(AuraTypes[i].Type));
                bytes.Add(0);
                strSize += AuraTypes[i].Type.Length + 1;
            }

            //Pad to 32 byte alignment
            bytes.AddRange(new byte[Utils.CalculatePadding(strSize, 16)]); //Pad strings to be 16 byte alignment
            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 16)]); //Now pad the file to be 16 byte alignment

            //Characters
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), 28);

            for(int i = 0; i < characterCount; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(CharacterAuras[i].CharaID)));
                bytes.AddRange(BitConverter.GetBytes(CharacterAuras[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(CharacterAuras[i].I_08)));
                bytes.AddRange(BitConverter.GetBytes(Convert.ToInt32(CharacterAuras[i].I_12)));
            }

            return bytes.ToArray();
        }

        public static void Deserialize(string xmlPath)
        {
            string path = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(AUR_File), YAXSerializationOptions.DontSerializeNullObjects);
            Write((AUR_File)serializer.DeserializeFromFile(xmlPath), path);
        }

        public static void Write(AUR_File file, string path)
        {
            byte[] bytes = file.SaveToBytes();

            //Saving
            File.WriteAllBytes(path, bytes.ToArray());
        }


        //Helper
        private string GetAuraType(int id)
        {
            return (AuraTypes.Count - 1 >= id) ? AuraTypes[id].Type : null;
        }

        private int GetAuraType(string typeName)
        {
            return (AuraTypes.Any(e => e.Type == typeName)) ? AuraTypes.FindIndex(e => e.Type == typeName) : -1;
        }

        public void PadWithNullEntries()
        {
            //Keeps all IDs consecutive by adding null entries (need for compatibility with eternity tools)

            int maxId = Auras.Max(x => x.SortID);

            for (int i = 0; i < maxId; i++)
            {
                if (!Auras.Any(x => x.SortID == i))
                    Auras.Add(new AUR_Aura { Index = i.ToString(), AuraEffects = new List<AUR_Effect>() });
            }
        }
    }

    [YAXSerializeAs("AuraType")]
    public class AUR_Type
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("name")]
        public string Type { get; set; }
    }

    public class AUR_Aura : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("ID")]
        [BindingAutoId]
        public string Index { get; set; } //Int32, offset 0
        [YAXAttributeForClass]
        [YAXSerializeAs("I_04")]
        public int I_04 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "AuraEffect")]
        public List<AUR_Effect> AuraEffects { get; set; } = new List<AUR_Effect>();

    }

    [YAXSerializeAs("AuraEffect")]
    public class AUR_Effect 
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public string Type { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Effect_ID")]
        public int I_04 { get; set; }

    }

    [YAXSerializeAs("CharacterAura")]
    public class AUR_Character : IInstallable
    {
        #region IInstallable
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(CharaID); } }
        [YAXDontSerialize]
        public string Index 
        {
            get { return $"{CharaID}_{I_04}"; }
            set
            {
                string[] values = value.Split('_');
                if(values.Length == 2)
                {
                    CharaID = values[0];
                    I_04 = int.Parse(values[1]);
                }
            }
        }
        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("Chara_ID")]
        public string CharaID { get; set; } //Int32, offset 0
        [YAXAttributeForClass]
        [YAXSerializeAs("Costume")]
        public int I_04 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Aura_ID")]
        public string I_08 { get; set; } //int32
        [YAXAttributeForClass]
        [YAXSerializeAs("Glare")]
        public bool I_12 { get; set; } //int32
    }
}
