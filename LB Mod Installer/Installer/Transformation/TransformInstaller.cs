using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.Eternity;
using LB_Mod_Installer.Binding;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CUS;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource;
using Xv2CoreLib.BCM;
using Xv2CoreLib.PUP;
using Xv2CoreLib.IDB;
using Xv2CoreLib.BCS;
using YAXLib;
using Xv2CoreLib.Resource.Image;
using System.IO;
using Xv2CoreLib.EffectContainer;

namespace LB_Mod_Installer.Installer.Transformation
{
    public class TransformInstaller
    {

        private Install install;

        private List<TransformDefine> TransformationDefines = new List<TransformDefine>();
        private List<TransformPartSet> PartSets = new List<TransformPartSet>();
        private List<TransformPowerUp> PupEntries = new List<TransformPowerUp>();
        private List<TransformCusAura> CusAuras = new List<TransformCusAura>();

        public TransformInstaller(Install install)
        {
            this.install = install;
        }

        #region Public
        public void LoadTransformations(TransformDefines defines)
        {
            if(defines?.Transformations != null)
            {
                foreach (var define in defines.Transformations)
                {
                    BAC_File bacDefines = install.zipManager.DeserializeXmlFromArchive_Ext<BAC_File>(GeneralInfo.GetPathInZipDataDir(define.BacPath));
                    define.BacEntryInstance = bacDefines.GetEntry(define.BacEntry);

                    if (define.BacEntryInstance == null)
                        throw new Exception($"TransformInstaller.LoadTransformations: BacEntry with ID: {define.BacEntry} was not found for Key: {define.Key}, but the bac file was loaded.\n\nCould be a misconfigured bac file, or a wrong ID/BacPath?");

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
                    var existing = PartSets.FirstOrDefault(x => x.Key == partSet.Key && x.Race == partSet.Race && x.Gender == partSet.Gender);

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
        
        public void LoadPupEntries(TransformPowerUps powerUps)
        {
            if (powerUps?.PowerUps != null)
            {
                foreach (var powerUp in powerUps.PowerUps)
                {
                    var existing = PupEntries.FirstOrDefault(x => x.Key == powerUp.Key);

                    if (existing != null)
                    {
                        PupEntries[PupEntries.IndexOf(existing)] = powerUp;
                    }
                    else
                    {
                        PupEntries.Add(powerUp);
                    }
                }
            }
        }

        public void LoadCusAuras(TransformCusAuras cusAuras)
        {
            if (cusAuras?.CusAuras != null)
            {
                foreach (var cusAura in cusAuras.CusAuras)
                {
                    var existing = CusAuras.FirstOrDefault(x => x.Key == cusAura.Key);

                    if (existing != null)
                    {
                        CusAuras[CusAuras.IndexOf(existing)] = cusAura;
                    }
                    else
                    {
                        CusAuras.Add(cusAura);
                    }
                }
            }
        }

        public void InstallSkill(TransformSkill skill)
        {
            if (skill.SkillCode?.Length != 3 && skill.SkillCode?.Length != 4)
                throw new ArgumentException(string.Format("TransformInstaller.InstallSkill: SkillCode \"{0}\" is an invalid size. All SkillCodes must be either 3 or 4 characters.", skill.SkillCode));

            UpdateSkillRequirements(skill);

            BAC_File bacFile = CreateBacFile(skill);

            //EEPK file is stored in "data" as a vfxpackage
            EffectContainerFile eepkFile = null;

            if (!string.IsNullOrWhiteSpace(skill.VfxPath))
            {
                if (!install.zipManager.Exists(GeneralInfo.GetPathInZipDataDir(skill.VfxPath)))
                {
                    throw new FileNotFoundException(string.Format("InstallSkill: Cant find the VFX file at path \"{0}\", declared on skill \"{1}\".", skill.VfxPath, skill.SkillCode));
                }

                using (Stream stream = install.zipManager.GetZipEntry(GeneralInfo.GetPathInZipDataDir(skill.VfxPath)).Open())
                {
                    eepkFile = EffectContainerFile.LoadVfx2(stream, skill.VfxPath);
                }
            }

            //EAN, ACB - all statically linked. Just put the path in cus.

            //Assign ID (generate dummy cms if needed)
            CUS_File cusFile = (CUS_File)install.GetParsedFile<CUS_File>(BindingManager.CUS_PATH);
            CMS_Entry dummyCms = AssignCmsEntry();
            int skillID2 = cusFile.AssignNewSkillId(dummyCms, CUS_File.SkillType.Awoken);
            int skillID1 = skillID2 + 25000;

            if (skillID1 == -1)
                throw new ArgumentOutOfRangeException($"TransformInstaller.InstallSkill: the assigned skill ID is invalid (shouldn't happen... so something went wrong somewhere else)");

            bacFile.ChangeNeutralSkillId((ushort)skillID2);

            //Create PUP entries
            int pupID = InstallPupEntries(skill);

            //Create skill idb entry
            InstallIdbEntry(skill, skillID2);

            //Create CusAura entries
            if (skill.CusAura == -1)
                skill.CusAura = InstallCusAuras(skill);

            //Create PartSets
            if (skill.PartSet == -1)
                skill.PartSet = InstallPartSets(skill);

            //Create StageSelectors
            int[] transformStageOverlayIds = new int[skill.TransformStates.Count];
            int[] untransformStageOverlayIds = new int[skill.TransformStates.Count];

            if (install.installerXml.FlagIsSet(NO_AWOKEN_OVERLAY_FLAG))
            {
                //No stage selector UI = Use default entry
                for(int i = 0; i < transformStageOverlayIds.Length; i++)
                {
                    transformStageOverlayIds[i] = TransformDefine.BAC_HOLD_DOWN_LOOP_IDX;
                    untransformStageOverlayIds[i] = TransformDefine.BAC_HOLD_DOWN_LOOP_IDX;
                }
            }
            else
            {
                transformStageOverlayIds = CreateStageSelectorEffects(skill, bacFile, eepkFile, skillID2, false);
                untransformStageOverlayIds = CreateStageSelectorEffects(skill, bacFile, eepkFile, skillID2, true);
            }

            //Add Overlay Effect disables for every bac entry (immediately cancel overlay upon bac entry change within the skill)
            for(int i = 0; i < 50; i++)
            {
                if(eepkFile.Effects.Any(x => x.IndexNum == 20000 + i))
                {
                    foreach(var bacEntry in bacFile.BacEntries)
                    {
                        if (!bacEntry.IsBacEntryEmpty())
                        {
                            BAC_Type8 disableEffect = new BAC_Type8();
                            disableEffect.StartTime = 0;
                            disableEffect.Duration = 1;
                            disableEffect.EepkType = BAC_Type8.EepkTypeEnum.AwokenSkill;
                            disableEffect.EffectID = 20000 + i;
                            disableEffect.SkillID = (ushort)skillID2;
                            disableEffect.UseSkillId = BAC_Type8.UseSkillIdEnum.True;
                            disableEffect.EffectFlags = BAC_Type8.EffectFlagsEnum.Off | BAC_Type8.EffectFlagsEnum.Loop | BAC_Type8.EffectFlagsEnum.UserOnly;

                            if (bacEntry.Type8 == null)
                                bacEntry.Type8 = new List<BAC_Type8>();

                            bacEntry.Type8.Insert(0, disableEffect);

                        }
                    }
                }
                else
                {
                    break;
                }
            }

            //Create BCM
            BCM_File bcmFile = CreateBcmFile(skill, transformStageOverlayIds, untransformStageOverlayIds);

            //Create CUS entry
            Skill cusEntry = new Skill();
            cusEntry.ShortName = skill.SkillCode;
            cusEntry.ID1 = (ushort)skillID1;
            cusEntry.ID2 = (ushort)skillID2;
            cusEntry.I_12 = skill.RaceLock;
            cusEntry.I_13 = 0x76; //BAC, BCM, EAN, Awoken Skill
            cusEntry.FilesLoadedFlags1 = Skill.FilesLoadedFlags.Eepk | Skill.FilesLoadedFlags.CamEan | Skill.FilesLoadedFlags.CharaVOX | Skill.FilesLoadedFlags.CharaSE;
            cusEntry.I_16 = (short)skill.PartSet;
            cusEntry.I_18 = 65280;
            cusEntry.EanPath = skill.EanPath;
            cusEntry.CamEanPath = skill.CamEanPath;
            cusEntry.SePath = skill.SeAcbPath;
            cusEntry.VoxPath = skill.VoxAcbPath;
            cusEntry.I_50 = 271;
            cusEntry.I_52 = 3;
            cusEntry.I_54 = 2;
            cusEntry.PUP = (ushort)pupID;
            cusEntry.CusAura = (short)skill.CusAura;
            cusEntry.CharaSwapId = (ushort)skill.CharaSwapId;
            cusEntry.I_62 = (short)skill.GetSkillSetChangeId();
            cusEntry.NumTransformations = (ushort)(skill.NumStages > 3 ? skill.NumStages : 4); //Always set this to atleast 4 so that the stage names breaks and only shows the first stage (otherwise, we get "Unknown Skill" unless more msg entries are added)

            cusFile.AwokenSkills.Add(cusEntry);
            GeneralInfo.Tracker.AddID(BindingManager.CUS_PATH, Sections.CUS_AwokenSkills, cusEntry.Index);

            //Save files (add to file cache)
            string folderName = $"{skillID2.ToString("D3")}_{dummyCms.ShortName}_{skill.SkillCode}";
            string skillDir = $"skill/MET/{folderName}";
            string bacPath = $"skill/MET/{folderName}/{folderName}.bac";
            string bcmPath = $"skill/MET/{folderName}/{folderName}_PLAYER.bcm";
            string eepkPath = $"skill/MET/{folderName}/{folderName}.eepk";

            install.fileManager.AddParsedFile(bacPath, bacFile);
            install.fileManager.AddParsedFile(bcmPath, bcmFile);
            install.fileManager.AddParsedFile(eepkPath, eepkFile);

            //Just track the whole directory instead of individual files. Much less clutter.
            GeneralInfo.Tracker.AddJungleFile(skillDir);
        }
        #endregion

        #region Install
        private BAC_File CreateBacFile(TransformSkill skill)
        {
            BAC_File bacFile = BAC_File.DefaultBacFile();
            
            foreach(var stage in skill.Stages)
            {
                var defineEntry = TransformationDefines.FirstOrDefault(x => x.Key?.Equals(stage.Key, StringComparison.OrdinalIgnoreCase) == true);

                if (defineEntry == null)
                    throw new Exception($"TransformInstaller.CreateBacFile: Cannot find the BacFile with Key: {stage.Key}");

                if (bacFile.GetEntry(stage.StageIndex) != null)
                    throw new Exception($"TransformInstaller.CreateBacFile: Duplicate StageIndex encountered.");

                var entry = defineEntry.BacEntryInstance.Copy();
                entry.SortID = stage.StageIndex;

                bacFile.BacEntries.Add(entry);

                //Add awoken deactivation commands before the stage is activated. This is needed to workaround some undesired behaviour, such as PartSet changes, PUP souls, CusAuras and moveset changes persisting
                //In some cases we will want the changes to persist, so this must be optional
                if (stage.DeactivateFirst)
                {
                    if(!entry.Type15.Any(x => x.FunctionType == 14))
                    {
                        var transformActivator = entry.Type15.FirstOrDefault(x => x.FunctionType == 13);

                        if (transformActivator != null)
                        {
                            BAC_Type15 transformDeactivator = new BAC_Type15();
                            transformDeactivator.StartTime = transformActivator.StartTime;
                            transformDeactivator.Duration = 1;
                            transformDeactivator.FunctionType = 14;

                            //Insert it before the activator entry, so that it is executed first.
                            entry.Type15.Insert(0, transformDeactivator);
                        }
                    }
                }
            }

            //Add transform / revert mechanic entries
            var holdDownEntryDefine = TransformationDefines.FirstOrDefault(x => x.Key?.Equals(TransformDefine.BAC_HOLD_DOWN_LOOP_KEY) == true);
            var untransformDefine = TransformationDefines.FirstOrDefault(x => x.Key?.Equals(TransformDefine.BAC_UNTRANSFORM_KEY) == true);
            var revertDefine = TransformationDefines.FirstOrDefault(x => x.Key?.Equals(TransformDefine.BAC_REVERT_LOOP_KEY) == true);
            var notAllowedSeDefine = TransformationDefines.FirstOrDefault(x => x.Key?.Equals(TransformDefine.BAC_NOT_ALLOWED_SE_CALLBACK_KEY) == true);
            var pageChangeSeDefine = TransformationDefines.FirstOrDefault(x => x.Key?.Equals(TransformDefine.BAC_PAGE_SE_CALLBACK_KEY) == true);

            if (holdDownEntryDefine == null)
                throw new Exception(string.Format("TransformInstaller.CreateBacFile: Cannot find the define entry for \"{0}\"", TransformDefine.BAC_HOLD_DOWN_LOOP_KEY));

            if (untransformDefine == null)
                throw new Exception(string.Format("TransformInstaller.CreateBacFile: Cannot find the define entry for \"{0}\"", TransformDefine.BAC_UNTRANSFORM_KEY));

            if (revertDefine == null)
                throw new Exception(string.Format("TransformInstaller.CreateBacFile: Cannot find the define entry for \"{0}\"", TransformDefine.BAC_REVERT_LOOP_KEY));

            if (notAllowedSeDefine == null)
                throw new Exception(string.Format("TransformInstaller.CreateBacFile: Cannot find the define entry for \"{0}\"", TransformDefine.BAC_NOT_ALLOWED_SE_CALLBACK_KEY));

            if (pageChangeSeDefine == null)
                throw new Exception(string.Format("TransformInstaller.CreateBacFile: Cannot find the define entry for \"{0}\"", TransformDefine.BAC_PAGE_SE_CALLBACK_KEY));

            var holdDownEntry = holdDownEntryDefine.BacEntryInstance.Copy();
            var untransformEntry = untransformDefine.BacEntryInstance.Copy();
            var revertEntry = revertDefine.BacEntryInstance.Copy();
            var notAllowedEntry = notAllowedSeDefine.BacEntryInstance.Copy();
            var pageChangeSeEntry = pageChangeSeDefine.BacEntryInstance.Copy();

            holdDownEntry.SortID = TransformDefine.BAC_HOLD_DOWN_LOOP_IDX;
            untransformEntry.SortID = TransformDefine.BAC_UNTRANSFORM_IDX;
            revertEntry.SortID = TransformDefine.BAC_REVERT_IDX;
            notAllowedEntry.SortID = TransformDefine.BAC_NOT_ALLOWED_SE_CALLBACK_IDX;
            pageChangeSeEntry.SortID = TransformDefine.BAC_PAGE_SE_CALLBACK_IDX;

            bacFile.BacEntries.Add(holdDownEntry);
            bacFile.BacEntries.Add(untransformEntry);
            bacFile.BacEntries.Add(revertEntry);
            bacFile.BacEntries.Add(notAllowedEntry);
            bacFile.BacEntries.Add(pageChangeSeEntry);

            return bacFile;
        }

        private BCM_File CreateBcmFile(TransformSkill skill, int[] transformHoldDownIds, int[] untransformHoldDownIds)
        {
            return null;
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
                cmsFile.CMS_Entries.Add(dummyCmsEntry);
            }

            return dummyCmsEntry;
        }
        
        private int InstallPupEntries(TransformSkill skill)
        {
            PUP_File pupFile = (PUP_File)install.GetParsedFile<PUP_File>(Xv2CoreLib.Xenoverse2.PUP_PATH);
            int pupID = Install.bindingManager.GetAutoId(null, pupFile.PupEntries, 350, ushort.MaxValue, skill.NumStages);

            //Dummy entry for stage 0 
            if(skill.HasMoveSkillSetChange())
                pupFile.PupEntries.Add(new PUP_Entry(pupID));

            for (int i = 0; i < skill.Stages.Count; i++)
            {
                var pup = GetPupDefine(skill.Stages[i].Key);

                var pupEntry = pup.Copy();
                pupEntry.ID = pupID + skill.GetTransStage(i);

                pupFile.PupEntries.Add(pupEntry);
                GeneralInfo.Tracker.AddID(Xv2CoreLib.Xenoverse2.PUP_PATH, Sections.PUP_Entry, pupEntry.Index);
            }

            return pupID;
        }

        private int InstallCusAuras(TransformSkill skill)
        {
            PrebakedFile prebaked = (PrebakedFile)install.GetParsedFile<PrebakedFile>(PrebakedFile.PATH);
            int auraId = prebaked.GetFreeCusAuraID(skill.NumStages);

            //Dummy entry for stage 0 
            if(skill.HasMoveSkillSetChange())
                prebaked.CusAuras.Add(new CusAuraData(auraId));

            for (int i = 0; i < skill.Stages.Count; i++)
            {
                var cusAura = CusAuras.FirstOrDefault(x => x.Key == skill.Stages[i].Key);

                if (cusAura == null)
                    throw new Exception($"Could not find a CusAuraData entry for Key: {skill.Stages[i].Key}.");

                var entry = cusAura.CusAuraData.Copy();
                entry.Integer_2 = (uint)i;
                entry.Integer_3 = (uint)(i < 3 ? i + 1 : 0);
                entry.CusAuraID = (ushort)(auraId + skill.GetTransStage(skill.Stages[i].StageIndex));

                prebaked.CusAuras.Add(entry);
                GeneralInfo.Tracker.AddID(PrebakedFile.PATH, Sections.PrebakedCusAura, entry.CusAuraID.ToString());
            }

            return auraId;
        }

        private int InstallPartSets(TransformSkill skill)
        {
            int id = Install.bindingManager.GetFreePartSet(1000, ushort.MaxValue, skill.NumStages);
            int movesetChangeIdx = skill.IndexOfMoveSkillSetChange();

            List<BCS_File> bcsFiles = new List<BCS_File>();
            List<PartSet> dummyPartSets = new List<PartSet>();

            for(int i = 0; i < skill.Stages.Count; i++)
            {
                foreach (var partSet in PartSets.Where(x => x.Key == skill.Stages[i].Key))
                {
                    string path = BCS_File.GetBcsFilePath(partSet.Race, partSet.Gender);
                    BCS_File bcsFile = (BCS_File)install.GetParsedFile<BCS_File>(path);

                    PartSet newPartSet = partSet.PartSet.Copy();
                    newPartSet.ID = id + skill.GetTransStage(skill.Stages[i].StageIndex);
                    bcsFile.PartSets.Add(newPartSet);

                    GeneralInfo.Tracker.AddID(path, Sections.BCS_PartSets, newPartSet.ID.ToString());

                    //Save bcs file for adding dummy stage 0 entry later
                    if (movesetChangeIdx == i)
                    {
                        if (!bcsFiles.Contains(bcsFile))
                        {
                            bcsFiles.Add(bcsFile);
                            dummyPartSets.Add(newPartSet);
                        }
                    }
                }
            }

            //Add entry at 0 if the skill has a moveset or skillset change, using the PartSet of the stage where the change should occur
            if (skill.HasMoveSkillSetChange())
            {
                for(int i = 0; i < bcsFiles.Count; i++)
                {
                    PartSet newPartSet = dummyPartSets[i].Copy();
                    newPartSet.ID = id;

                    bcsFiles[i].PartSets.Add(newPartSet);

                    GeneralInfo.Tracker.AddID(BCS_File.GetBcsFilePath(bcsFiles[i].Race, bcsFiles[i].Gender), Sections.BCS_PartSets, id.ToString());
                }
            }

            return id;
        }

        private void InstallIdbEntry(TransformSkill skill, int skillID2)
        {
            //Create IDB entry
            IDB_File idbFile = (IDB_File)install.GetParsedFile<IDB_File>(Xv2CoreLib.Xenoverse2.SKILL_IDB_PATH);
            IDB_Entry idbEntry = IDB_Entry.GetDefaultSkillEntry(skillID2, 5, skill.GetMaxKiRequired(), skill.BuyPrice);

            idbFile.Entries.Add(idbEntry);
            GeneralInfo.Tracker.AddID(Xv2CoreLib.Xenoverse2.SKILL_IDB_PATH, Sections.IDB_Entries, idbEntry.Index);

            //Create MSG entries
            string[] names = install.installerXml.GetLocalisedArray(skill.Name);
            string[] descs = !string.IsNullOrWhiteSpace(skill.Info) ? install.installerXml.GetLocalisedArray(skill.Info) : null;

            install.msgComponentInstall.WriteSkillMsgEntries(names, skillID2, CUS_File.SkillType.Awoken, MsgComponentInstall.SkillMode.Name);
            install.msgComponentInstall.WriteSkillMsgEntries(names, skillID2, CUS_File.SkillType.Awoken, MsgComponentInstall.SkillMode.BtlHud);

            if (descs != null)
            {
                install.msgComponentInstall.WriteSkillMsgEntries(descs, skillID2, CUS_File.SkillType.Awoken, MsgComponentInstall.SkillMode.Info);
            }
            else
            {
                idbEntry.DescMsgID = 0;
            }
        }

        private byte[] CreateStageSelectorTexture(TransformSkill skill, List<TransformOption> options, bool isUntransform)
        {
            const string ds4Texture = "data/awoken_overlay/ds4.dds";
            const string xboxTexture = "data/awoken_overlay/xbox.dds";
            const string kbmTexture = "data/awoken_overlay/kbm.dds";
            const string noControllerTexture = "data/awoken_overlay/none.dds";

            byte[] bytes;

            if (install.installerXml.FlagIsSet(DS4_FLAG))
            {
                bytes = install.zipManager.GetFileFromArchive(ds4Texture);
            }
            else if (install.installerXml.FlagIsSet(XBOX_CONTROLLER_FLAG))
            {
                bytes = install.zipManager.GetFileFromArchive(xboxTexture);
            }
            else if (install.installerXml.FlagIsSet(KEYBOARD_MOUSE_FLAG))
            {
                bytes = install.zipManager.GetFileFromArchive(kbmTexture);
            }
            else
            {
                bytes = install.zipManager.GetFileFromArchive(noControllerTexture);
            }

            string[] stageNames = new string[4];

            if (isUntransform)
            {
                stageNames[0] = install.installerXml.GetLocalisedString("REVERT_TO_BASE");

                for (int i = 0; i < 3; i++)
                {
                    if (i < options?.Count)
                    {
                        stageNames[i + 1] = GetStageName(skill, options[i].StageIndex);
                    }
                    else
                    {
                        stageNames[i + 1] = "---";
                    }
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i < options?.Count)
                    {
                        stageNames[i] = GetStageName(skill, options[i].StageIndex);
                    }
                    else
                    {
                        stageNames[i] = "---";
                    }
                }
            }
            

            return TextureHelper.WriteStageNames(bytes, stageNames);

         }
       
        private int[] CreateStageSelectorEffects(TransformSkill skill, BAC_File bacFile, EffectContainerFile eepkFile, int skillID2, bool isUntransform)
        {
            int[] bacIds = new int[skill.TransformStates.Count];

            for (int i = 0; i < skill.TransformStates.Count; i++)
            {
                byte[] texture = isUntransform ? CreateStageSelectorTexture(skill, skill.TransformStates[i].RevertOptions, isUntransform) : CreateStageSelectorTexture(skill, skill.TransformStates[i].TransformOptions, isUntransform);

                int effectId = eepkFile.CreateStageSelectorEntry(texture);

                BAC_Entry newHoldDownEntry = bacFile.GetEntry(TransformDefine.BAC_HOLD_DOWN_LOOP_IDX).Copy();
                bacIds[i] = bacFile.AddEntry(newHoldDownEntry);

                if (newHoldDownEntry.Type8 == null)
                    newHoldDownEntry.Type8 = new List<BAC_Type8>();

                //Add Effect Start and Disables every frame. This ensures that the effect wont linger for long if the bac entry is canceled.
                for (int a = 0; a < 120; a += 1)
                {
                    BAC_Type8 disableEffect = new BAC_Type8();
                    disableEffect.StartTime = (ushort)a;
                    disableEffect.Duration = 1;
                    disableEffect.EepkType = BAC_Type8.EepkTypeEnum.AwokenSkill;
                    disableEffect.EffectID = effectId;
                    disableEffect.SkillID = (ushort)skillID2;
                    disableEffect.UseSkillId = BAC_Type8.UseSkillIdEnum.True;
                    disableEffect.EffectFlags = BAC_Type8.EffectFlagsEnum.Off | BAC_Type8.EffectFlagsEnum.Loop | BAC_Type8.EffectFlagsEnum.UserOnly;

                    //Start
                    BAC_Type8 effect = new BAC_Type8();
                    effect.StartTime = (ushort)a;
                    effect.Duration = 1;
                    effect.EepkType = BAC_Type8.EepkTypeEnum.AwokenSkill;
                    effect.EffectID = effectId;
                    effect.SkillID = (ushort)skillID2;
                    effect.UseSkillId = BAC_Type8.UseSkillIdEnum.True;
                    effect.EffectFlags = BAC_Type8.EffectFlagsEnum.Loop | BAC_Type8.EffectFlagsEnum.UserOnly;

                    newHoldDownEntry.Type8.Add(disableEffect.Copy());
                    newHoldDownEntry.Type8.Add(effect);
                }

            }

            return bacIds;
        }

        private void UpdateSkillRequirements(TransformSkill skill)
        {
            foreach(var state in skill.TransformStates)
            {
                if(state.TransformOptions != null)
                {
                    foreach (var option in state.TransformOptions)
                    {
                        var define = GetStageDefine(skill, option.StageIndex);

                        if (option.KiCost == 0)
                            option.KiCost = define.KiCost;

                        if (option.KiRequired == 0)
                            option.KiRequired = define.KiRequired;

                        if (option.HealthRequired == 0f)
                            option.HealthRequired = define.HealthRequired;
                    }
                }
            }
        }
        #endregion

        #region GetDefine
        private PUP_Entry GetPupDefine(string key)
        {
            var entry = PupEntries.FirstOrDefault(x => x.Key == key);

            if (!string.IsNullOrWhiteSpace(entry.AliasFor))
            {
                entry = PupEntries.FirstOrDefault(x => x.Key == entry.AliasFor);
            }

            if(entry == null)
                throw new Exception($"Could not find a PUP entry for Key: {key}.\n\nNote: All transformations must have an assigned PUP entry!");

            return entry.PupEntry;
        }

        #endregion

        private ButtonInput GetButtonInputForSlot(int slot)
        {
            switch (slot)
            {
                case 0:
                    return ButtonInput.blast;
                case 1:
                    return ButtonInput.heavy;
                case 2:
                    return ButtonInput.light;
                case 3:
                    return ButtonInput.jump;
            }

            throw new ArgumentException($"TransformInstaller.GetButtonInputForSlot: Slot number out of range ({slot}), must be between 0 and 3.");
        }
        
        private TransformDefine GetStageDefine(TransformSkill skill, int stageIndex)
        {
            TransformStage stage = skill.Stages.FirstOrDefault(x => x.StageIndex == stageIndex);

            if(stage != null)
            {
                return TransformationDefines.FirstOrDefault(x => x.Key == stage.Key);
            }

            throw new Exception($"TransformInstaller.GetStageDefine: Could not find the define entry.");
        }

        private string GetStageName(TransformSkill skill, int stageIndex)
        {
            var stage = skill.Stages.FirstOrDefault(x => x.StageIndex == stageIndex);

            if(stage != null)
            {
                return install.installerXml.GetLocalisedString(stage.Key);
            }

            return string.Empty;
        }


        #region FLAGS
        private const string DS4_FLAG = "DS4_FLAG";
        private const string XBOX_CONTROLLER_FLAG = "XBOX_FLAG";
        private const string KEYBOARD_MOUSE_FLAG = "KEYBOARD_MOUSE_FLAG";
        private const string NO_AWOKEN_OVERLAY_FLAG = "NO_AWOKEN_OVERLAY_FLAG";
        #endregion
    }
}
