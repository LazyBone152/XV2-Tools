using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.Eternity;

namespace LB_Mod_Installer.Installer.Transformation
{
    public class TransformInstaller
    {
        private List<TransformationDefine> TransformationDefines = new List<TransformationDefine>();
        private List<TransformPartSet> PartSets = new List<TransformPartSet>();

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

        }
        #endregion

    }
}
