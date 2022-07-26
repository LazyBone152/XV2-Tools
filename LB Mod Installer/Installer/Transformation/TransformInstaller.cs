using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.Eternity;
using LB_Mod_Installer.Binding;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CUS;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource;

namespace LB_Mod_Installer.Installer.Transformation
{
    public class TransformInstaller
    {
        private Install install;

        private List<TransformationDefine> TransformationDefines = new List<TransformationDefine>();
        private List<TransformPartSet> PartSets = new List<TransformPartSet>();

        public TransformInstaller(Install install)
        {
            this.install = install;
        }

        #region Public
        public void LoadTransformations(TransformationDefines defines)
        {
            if(defines?.Transformations != null)
            {
                foreach (var define in defines.Transformations)
                {
                    var existing = TransformationDefines.FirstOrDefault(x => x.Key == define.Key);

                    if(existing != null)
                    {
                        TransformationDefines[TransformationDefines.IndexOf(existing)] = define;
                    }
                    else
                    {
                        TransformationDefines.Add(define);
                    }
                }
            }
        }
    
        public void LoadPartSets(TransformPartSets partSets)
        {
            if(partSets?.PartSets != null)
            {
                foreach(var partSet in partSets.PartSets)
                {
                    var existing = PartSets.FirstOrDefault(x => x.Key == partSet.Key && x.Race == partSet.Race);

                    if (existing != null)
                    {
                        PartSets[PartSets.IndexOf(existing)] = partSet;
                    }
                    else
                    {
                        PartSets.Add(partSet);
                    }
                }
            }
        }
        
        public void InstallSkill(TransformSkill skill)
        {
            //Create BAC
            BAC_File bacFile = CreateBacFile(skill);
            //Create BCM
            //Create EEPK
            //EANs and ACBs will be statically linked, and installed as normal files (for now... and likely forever). 

            //Assign ID (generate dummy cms if needed)
            //Change all BAC Skill IDs into the proper Skill ID
            //Create PUP entries (and talisman)
            //Create skill idb entry
            //Create MSG entries
            //Create CUS entry
        }
        #endregion

        #region Install
        private BAC_File CreateBacFile(TransformSkill skill)
        {
            BAC_File bacFile = BAC_File.DefaultBacFile();

            foreach(var stage in skill.Stages)
            {
                var defineEntry = TransformationDefines.FirstOrDefault(x => x.Key?.Equals(stage.Key) == true);

                if (defineEntry == null)
                    throw new Exception($"TransformInstaller.CreateBacFile: Cannot find the BacFile with Key: {stage.Key}");

                BAC_File bacDefines = install.zipManager.DeserializeXmlFromArchive_Ext<BAC_File>(GeneralInfo.GetPathInZipDataDir(defineEntry.BacPath));
                BAC_Entry bacEntry = bacDefines.GetEntry(defineEntry.BacEntry);

                if(bacEntry == null)
                    throw new Exception($"TransformInstaller.CreateBacFile: BacEntry with ID: {defineEntry.BacEntry} was not found for Key: {stage.Key}, but the bac file was loaded.\n\nCould be a misconfigured bac file, or a wrong ID/BacPath?");

                bacFile.BacEntries.Add(bacEntry.Copy());
            }

            //TODO: Add other logic...

            return bacFile;
        }

        private CMS_Entry AssignCmsEntry()
        {
            CMS_File cmsFile = (CMS_File)install.GetParsedFile<CMS_File>(BindingManager.CMS_PATH);
            CUS_File cusFile = (CUS_File)install.GetParsedFile<CUS_File>(BindingManager.CUS_PATH);

            //Find dummy CMS entry
            CMS_Entry dummyCmsEntry = null;

            foreach(var cmsEntry in cmsFile.CMS_Entries)
            {
                if (cmsEntry.IsDummyEntry())
                {
                    if(!cusFile.IsSkillIdRangeUsed(cmsEntry, CUS_File.SkillType.Awoken))
                    {
                        dummyCmsEntry = cmsEntry;
                        break;
                    }
                }
            }

            //If no suitable dummy was found, create a new one
            if(dummyCmsEntry == null)
            {
                dummyCmsEntry = cmsFile.CreateDummyEntry();
            }

            return dummyCmsEntry;
        }

        #endregion
    }
}
