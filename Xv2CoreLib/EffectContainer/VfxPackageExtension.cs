using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xv2CoreLib.Resource;
using YAXLib;

namespace Xv2CoreLib.EffectContainer
{
    public enum VfxPackageVersion
    {
        Initial = 0, 
        Expanded = 1,
        Unsupported
    }

    [Serializable]
    public class VfxPackageExtension
    {
        private const string PATH = "VfxPackageExtension.xml";

        [YAXAttributeForClass]
        public VfxPackageVersion Version { get; set; } = VfxPackageVersion.Expanded;

        public List<VfxPackageExtendedEffect> Effects { get; set; } = new List<VfxPackageExtendedEffect>();
        public List<VfxPackageCategory> Categories { get; set; } = new List<VfxPackageCategory>();

        #region LoadSave
        public static VfxPackageExtension Load(ZipReader zipReader, EffectContainerFile eepk)
        {
            VfxPackageExtension file = zipReader.DeserializeXmlFromArchive<VfxPackageExtension>(PATH);

            if(file == null)
            {
                file = new VfxPackageExtension();
            }

            if (file.Version >= VfxPackageVersion.Unsupported)
                throw new ArgumentException("This VfxPackage requires a newer version of the tools (Installer / EEPK Organiser).");

            if (file.Effects == null)
                file.Effects = new List<VfxPackageExtendedEffect>();

            if (file.Categories == null)
                file.Categories = new List<VfxPackageCategory>();

            //Link Effects
            foreach(var effect in eepk.Effects)
            {
                effect.ExtendedEffectData = file.Effects.FirstOrDefault(x => x.EffectID == effect.SortID);
            }

            file.Effects.Clear();

            return file;
        }

        public void Save(ZipWriter zipWriter, EffectContainerFile eepk)
        {
            //Get all ExtendedEffects
            Effects.Clear();

            foreach(var effect in eepk.Effects)
            {
                if(effect.ExtendedEffectData?.HasData() == true)
                {
                    var extendedEffect = effect.ExtendedEffectData.Copy();
                    extendedEffect.EffectID = effect.SortID;
                    Effects.Add(extendedEffect);
                }
            }

            //Save file
            YAXSerializer serializer = new YAXSerializer(typeof(VfxPackageExtension));
            XDocument xml = serializer.SerializeToXDocument(this);

            using (MemoryStream ms = new MemoryStream())
            {
                xml.Save(ms);
                zipWriter.AddFile(PATH, ms.ToArray());
            }
        }
        #endregion
    }

    [Serializable]
    public class VfxPackageExtendedEffect
    {
        [YAXAttributeForClass]
        public int EffectID { get; set; }

        //Copy the EffectParts from the specified Effect into this Effect (priority being: VfxPackage -> EEPK)
        [YAXAttributeFor("CopyFrom")]
        [YAXSerializeAs("value")]
        public int CopyFrom { get; set; } = -1;

        //Enables AutoID functionality
        [YAXAttributeFor("AutoIdEnabled")]
        [YAXSerializeAs("value")]
        public bool AutoIdEnabled { get; set; } = false;
        [YAXAttributeFor("Alias")]
        [YAXSerializeAs("value")]
        public string Alias { get; set; }

        public bool HasData()
        {
            if (CopyFrom != -1 || AutoIdEnabled || !string.IsNullOrWhiteSpace(Alias)) return true;

            return false;
        }
    }

    [Serializable]
    public class VfxPackageCategory
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeFor("EffectIDs")]
        [YAXSerializeAs("values")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<int> EffectIDs { get; set; }
    }
}
