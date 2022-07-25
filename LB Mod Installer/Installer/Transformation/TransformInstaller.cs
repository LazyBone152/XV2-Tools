using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.Eternity;
using LB_Mod_Installer.Binding;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CUS;

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
            //Create BCM
            //Create EEPK, EAN, CAM.EAN, SE ACB, VOX ACB

            //Assign ID (generate dummy cms if needed)
            //Create PUP entries (and talisman)
            //Create skill idb entry
            //Create MSG entries
            //Create CUS entry
        }
        #endregion


        private CMS_Entry AssignCmsEntry()
        {
            //Look for CMS entry above ID 150, starting with "X" and a with null structure (no paths)
            //If one is found to have a free Awoken skill slot, return it
            CMS_File cmsFile = (CMS_File)install.GetParsedFile<CMS_File>(BindingManager.CMS_PATH);
            CUS_File cusFile = (CUS_File)install.GetParsedFile<CUS_File>(BindingManager.CUS_PATH);


            //Create a dummy CMS entry (see eternity code, I want it to be identical)
            //Return it
            //This dummy entry wont be uninstalled with the mod. It can safely be left behind, and future reinstalls or X2M mods will just use it.
        }
    }
}
