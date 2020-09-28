using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BCS;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BEV;
using Xv2CoreLib.BPE;
using Xv2CoreLib.BSA;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CNC;
using Xv2CoreLib.CNS;
using Xv2CoreLib.CSO;
using Xv2CoreLib.CUS;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ERS;
using Xv2CoreLib.IDB;
using Xv2CoreLib.MSG;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource;
using System.IO;
using System.Windows;
using System.Threading;
using System.Globalization;
using Xv2CoreLib.PSC;
using Xv2CoreLib.PUP;
using Xv2CoreLib.AUR;
using Xv2CoreLib.TSD;
using Xv2CoreLib.TNL;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.QXD;
using Xv2CoreLib.PAL;
using Xv2CoreLib.TTB;
using Xv2CoreLib.TTC;
using Xv2CoreLib.SEV;
using Xv2CoreLib.HCI;
using Xv2CoreLib.CML;

namespace LB_Mod_Installer.Installer
{
    public class Uninstall
    {
        private MainWindow parent;
        private Xv2FileIO FileIO;
        public FileCacheManager fileManager { get; private set; }
        private Mod currentMod = GeneralInfo.Tracker.GetCurrentMod();


        public Uninstall(MainWindow _parent, Xv2FileIO _fileIO, FileCacheManager _fileManager)
        {
            parent = _parent;
            FileIO = _fileIO;
            fileManager = _fileManager;

            if (currentMod.Files == null) currentMod.Files = new List<_File>();
        }

        public async Task Start()
        {
#if !DEBUG
            try
#endif
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                SetProgressBarSteps();
                UninstallMod();
            }
#if !DEBUG
            catch (Exception ex)
            {
                //Error handling
                //Restore files
                //Close app
                //Handle install errors here
                SaveErrorLog(ex.ToString());
                MessageBox.Show(string.Format("{0}\n\n{1}", ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : ""), "Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                MessageBox.Show("No changes were made to the installation. The installer will now close.", "Uninstall Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                parent.ShutdownApp();
            }
#endif
        }

        private void UninstallMod()
        {
            //Parsed files
            foreach(var file in currentMod.Files)
            {
                UpdateProgessBarText(string.Format("_Uninstalling \"{0}\"...", file.filePath));
                ResolveFileType(file.filePath, file);
            }

            //MsgComponents
            foreach(var file in currentMod.MsgComponents)
            {
                UpdateProgessBarText(string.Format("_Uninstalling \"{0}\"...", file.filePath));
                Uninstall_MsgComponent(file.filePath, file);
            }

            //Clear trackers
            currentMod.Files.Clear();
            currentMod.MsgComponents.Clear();
        }

        private void SaveErrorLog(string ex)
        {
            try
            {
                File.WriteAllText("install_error.txt", ex);
            }
            catch
            {

            }
        }

        public async Task SaveFiles()
        {
            //Call externally. We might want to uninstall + install in one go, so the uninstall class shouldn't save by itself.
            UpdateProgessBarText("_Saving files...", false);

#if !DEBUG
            try
#endif
            {
                fileManager.SaveParsedFiles();
                fileManager.SaveStreamFiles();
            }
#if !DEBUG
            catch (Exception ex)
            {
                //Handle install errors here
                MessageBox.Show(string.Format("{0}\n\n{1}", ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : ""), "Uninstall Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                MessageBox.Show("Installation changes will now be undone.", "Uninstall Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                try
                {
                    UpdateProgessBarText("_Restoring files...", false);
                    fileManager.RestoreBackups();
                }
                catch
                {
                    MessageBox.Show("Warning: Installation changes could not be undone. The installer will now close.", "Uninstall Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    parent.ShutdownApp();
                }

                MessageBox.Show("Installation changes were undone. The installer will now close.", "Changes Undone", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                parent.ShutdownApp();
            }
#endif
        }
        
        private void ResolveFileType(string path, _File file)
        {
            //Special cases
            if (path == Music.MusicInstaller.DIRECT_INSTALL_TYPE || path == Music.MusicInstaller.OPTION_INSTALL_TYPE)
            {
                Uninstall_BGM_ACB(path, file);
                return;
            }

            //If file doesn't exist in game data dir then it doesn't need to be uninstalled.
            if (!FileIO.FileExistsInGameDataDir(path)) return;
            
            //Standard cases
            switch (Path.GetExtension(path))
            {
                case ".idb":
                    Uninstall_IDB(path, file);
                    break;
                case ".cus":
                    Uninstall_CUS(path, file);
                    break;
                case ".bcs":
                    Uninstall_BCS(path, file);
                    break;
                case ".ers":
                    Uninstall_ERS(path, file);
                    break;
                case ".cms":
                    Uninstall_CMS(path, file);
                    break;
                case ".bac":
                    Uninstall_BAC(path, file);
                    break;
                case ".bdm":
                    Uninstall_BDM(path, file);
                    break;
                case ".bev":
                    Uninstall_BEV(path, file);
                    break;
                case ".bpe":
                    Uninstall_BPE(path, file);
                    break;
                case ".bsa":
                    Uninstall_BSA(path, file);
                    break;
                case ".cnc":
                    Uninstall_CNC(path, file);
                    break;
                case ".cns":
                    Uninstall_CNS(path, file);
                    break;
                case ".cso":
                    Uninstall_CSO(path, file);
                    break;
                case ".ean":
                    Uninstall_EAN(path, file);
                    break;
                case ".msg":
                    Uninstall_MSG(path, file);
                    break;
                case ".eepk":
                    Uninstall_EEPK(path, file);
                    break;
                case ".psc":
                    Uninstall_PSC(path, file);
                    break;
                case ".aur":
                    Uninstall_AUR(path, file);
                    break;
                case ".pup":
                    Uninstall_PUP(path, file);
                    break;
                case ".tsd":
                    Uninstall_TSD(path, file);
                    break;
                case ".tnl":
                    Uninstall_TNL(path, file);
                    break;
                case ".emb":
                    Uninstall_EMB(path, file);
                    break;
                case ".qxd":
                    Uninstall_QXD(path, file);
                    break;
                case ".pal":
                    Uninstall_PAL(path, file);
                    break;
                case ".ttb":
                    Uninstall_TTB(path, file);
                    break;
                case ".ttc":
                    Uninstall_TTC(path, file);
                    break;
                case ".sev":
                    Uninstall_SEV(path, file);
                    break;
                case ".hci":
                    Uninstall_HCI(path, file);
                    break;
                case ".cml":
                    Uninstall_CML(path, file);
                    break;
                default:
                    throw new Exception(string.Format("The filetype of \"{0}\" is unsupported. Uninstall failed.\n\nThis mod was likely installed by a newer version of the installer.", path));
            }
        }

        public async Task Uninstall_JungleFiles()
        {
            try
            {
                UpdateProgessBarText("_Uninstalling binary files...", false);

                if (currentMod.JungleFiles == null) return;

                foreach (var file in currentMod.JungleFiles)
                {
                    if (File.Exists(GeneralInfo.GetPathInGameDir(file.filePath)))
                        File.Delete(GeneralInfo.GetPathInGameDir(file.filePath));
                }

                //Clear tracker
                currentMod.JungleFiles.Clear();

            }
            catch (Exception ex)
            {
                SaveErrorLog(ex.ToString());
                MessageBox.Show(string.Format("{0}\n\n{1}", ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : ""), "Install Error", MessageBoxButton.OK, MessageBoxImage.Error);

                MessageBox.Show("Some installation changes cannot be reverted. The installer will now close.", "Uninstall Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                parent.ShutdownApp();
            }
        }

        private void Uninstall_IDB(string path, _File file)
        {
            try
            {
                IDB_File binaryFile = (IDB_File)GetParsedFile<IDB_File>(path, false);
                IDB_File cpkBinFile = (IDB_File)GetParsedFile<IDB_File>(path, true);

                Section section = file.GetSection(Sections.IDB_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.IDB, path);
                throw new Exception(error, ex);
            }
        }
        
        private void Uninstall_CUS(string path, _File file)
        {
            try
            {
                CUS_File binaryFile = (CUS_File)GetParsedFile<CUS_File>(path, false);
                CUS_File cpkBinFile = (CUS_File)GetParsedFile<CUS_File>(path, true);

                Section skillsetSection = file.GetSection(Sections.CUS_Skillsets);
                Section superSection = file.GetSection(Sections.CUS_SuperSkills);
                Section ultimateSection = file.GetSection(Sections.CUS_UltimateSkills);
                Section evasiveSection = file.GetSection(Sections.CUS_EvasiveSkills);
                Section blastSection = file.GetSection(Sections.CUS_BlastSkills);
                Section awokenSection = file.GetSection(Sections.CUS_AwokenSkills);

                if (skillsetSection != null)
                    UninstallEntries(binaryFile.Skillsets, (cpkBinFile != null) ? cpkBinFile.Skillsets : null, skillsetSection.IDs);
                if (superSection != null)
                    UninstallEntries(binaryFile.SuperSkills, (cpkBinFile != null) ? cpkBinFile.SuperSkills : null, superSection.IDs);
                if (ultimateSection != null)
                    UninstallEntries(binaryFile.UltimateSkills, (cpkBinFile != null) ? cpkBinFile.UltimateSkills : null, ultimateSection.IDs);
                if (evasiveSection != null)
                    UninstallEntries(binaryFile.EvasiveSkills, (cpkBinFile != null) ? cpkBinFile.EvasiveSkills : null, evasiveSection.IDs);
                if (blastSection != null)
                    UninstallEntries(binaryFile.BlastSkills, (cpkBinFile != null) ? cpkBinFile.BlastSkills : null, blastSection.IDs);
                if (awokenSection != null)
                    UninstallEntries(binaryFile.AwokenSkills, (cpkBinFile != null) ? cpkBinFile.AwokenSkills : null, awokenSection.IDs);

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.CUS, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_BCS(string path, _File file)
        {
            try
            {
                BCS_File binaryFile = (BCS_File)GetParsedFile<BCS_File>(path, false);
                BCS_File cpkBinFile = (BCS_File)GetParsedFile<BCS_File>(path, true, false);

                Section partSetSection = file.GetSection(Sections.BCS_PartSets);
                Section bodiesSection = file.GetSection(Sections.BCS_Bodies);

                if (partSetSection != null)
                    UninstallEntries(binaryFile.PartSets, (cpkBinFile != null) ? cpkBinFile.PartSets : null, partSetSection.IDs);
                
                if(binaryFile.Part_Colors != null)
                {
                    foreach (var section in binaryFile.Part_Colors)
                    {
                        Section partColorSection = file.GetSection(Sections.GetBcsPartColor(section.Index));

                        if (partColorSection != null)
                        {
                            var cpkSection = (cpkBinFile != null) ? cpkBinFile.GetPartColors(section.Index, section.Str_00) : null;
                            UninstallEntries(section._Colors, (cpkSection != null) ? cpkSection._Colors : null, partColorSection.IDs);
                        }
                    }
                }
                
                if (bodiesSection != null)
                    UninstallEntries(binaryFile.Bodies, (cpkBinFile != null) ? cpkBinFile.Bodies : null, bodiesSection.IDs);

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.BCS, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_ERS(string path, _File file)
        {
            try
            {
                ERS_File binaryFile = (ERS_File)GetParsedFile<ERS_File>(path, false);
                ERS_File cpkBinFile = (ERS_File)GetParsedFile<ERS_File>(path, true);

                UninstallSubEntries<ERS_MainTableEntry, ERS_MainTable>(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, file, false);
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at ERS uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_CMS(string path, _File file)
        {
            try
            {
                CMS_File binaryFile = (CMS_File)GetParsedFile<CMS_File>(path, false);
                CMS_File cpkBinFile = (CMS_File)GetParsedFile<CMS_File>(path, true);

                Section section = file.GetSection(Sections.CMS_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.CMS_Entries, (cpkBinFile != null) ? cpkBinFile.CMS_Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.CMS, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_BAC(string path, _File file)
        {
            try
            {
                BAC_File binaryFile = (BAC_File)GetParsedFile<BAC_File>(path, false);
                BAC_File cpkBinFile = (BAC_File)GetParsedFile<BAC_File>(path, true, false);

                Section section = file.GetSection(Sections.BAC_Entries);

                if (section != null)
                {
                    for (int i = binaryFile.BacEntries.Count - 1; i >= 0; i--)
                    {
                        if (section.IDs.Contains(binaryFile.BacEntries[i].Index))
                        {
                            BAC_Entry newEntry = GetOriginalEntry((cpkBinFile != null) ? cpkBinFile.BacEntries : null, binaryFile.BacEntries[i].Index);
                            
                            if (newEntry == null)
                            {
                                newEntry = BAC_Entry.Empty();
                                newEntry.Index = binaryFile.BacEntries[i].Index;
                            }

                            binaryFile.BacEntries.RemoveAt(i);
                            binaryFile.BacEntries.Add(newEntry);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.BAC, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_BDM(string path, _File file)
        {
            try
            {
                BDM_File binaryFile = (BDM_File)GetParsedFile<BDM_File>(path, false);
                BDM_File cpkBinFile = (BDM_File)GetParsedFile<BDM_File>(path, true, false);

                binaryFile.ConvertToXv2();

                if(cpkBinFile != null)
                    cpkBinFile.ConvertToXv2();

                Section section = file.GetSection(Sections.BDM_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.BDM_Entries, (cpkBinFile != null) ? cpkBinFile.BDM_Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.BDM, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_BEV(string path, _File file)
        {
            try
            {
                BEV_File binaryFile = (BEV_File)GetParsedFile<BEV_File>(path, false);
                BEV_File cpkBinFile = (BEV_File)GetParsedFile<BEV_File>(path, true);

                Section section = file.GetSection(Sections.BEV_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.BEV, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_BPE(string path, _File file)
        {
            try
            {
                BPE_File binaryFile = (BPE_File)GetParsedFile<BPE_File>(path, false);
                BPE_File cpkBinFile = (BPE_File)GetParsedFile<BPE_File>(path, true);

                Section section = file.GetSection(Sections.BPE_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.BPE, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_BSA(string path, _File file)
        {
            try
            {
                BSA_File binaryFile = (BSA_File)GetParsedFile<BSA_File>(path, false);
                BSA_File cpkBinFile = (BSA_File)GetParsedFile<BSA_File>(path, true, false);

                Section section = file.GetSection(Sections.BSA_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.BSA_Entries, (cpkBinFile != null) ? cpkBinFile.BSA_Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.BSA, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_CNC(string path, _File file)
        {
            try
            {
                CNC_File binaryFile = (CNC_File)GetParsedFile<CNC_File>(path, false);
                CNC_File cpkBinFile = (CNC_File)GetParsedFile<CNC_File>(path, true);

                Section section = file.GetSection(Sections.CNC_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.CncEntries, (cpkBinFile != null) ? cpkBinFile.CncEntries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.CNC, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_CNS(string path, _File file)
        {
            try
            {
                CNS_File binaryFile = (CNS_File)GetParsedFile<CNS_File>(path, false);
                CNS_File cpkBinFile = (CNS_File)GetParsedFile<CNS_File>(path, true);

                Section section = file.GetSection(Sections.CNS_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.CnsEntries, (cpkBinFile != null) ? cpkBinFile.CnsEntries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.CNS, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_CSO(string path, _File file)
        {
            try
            {
                CSO_File binaryFile = (CSO_File)GetParsedFile<CSO_File>(path, false);
                CSO_File cpkBinFile = (CSO_File)GetParsedFile<CSO_File>(path, true);

                Section section = file.GetSection(Sections.CSO_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.CsoEntries, (cpkBinFile != null) ? cpkBinFile.CsoEntries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.CSO, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_EAN(string path, _File file)
        {
            try
            {
                EAN_File binaryFile = (EAN_File)GetParsedFile<EAN_File>(path, false);
                EAN_File cpkBinFile = (EAN_File)GetParsedFile<EAN_File>(path, true);

                Section section = file.GetSection(Sections.EAN_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Animations, (cpkBinFile != null) ? cpkBinFile.Animations : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.EAN, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_MSG(string path, _File file)
        {
            try
            {
                MSG_File binaryFile = (MSG_File)GetParsedFile<MSG_File>(path, false);
                MSG_File cpkBinFile = (MSG_File)GetParsedFile<MSG_File>(path, true);

                Section section = file.GetSection(Sections.MSG_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.MSG_Entries, (cpkBinFile != null) ? cpkBinFile.MSG_Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.MSG, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_EEPK(string path, _File file)
        {
#if !DEBUG
            try
#endif
            {
                EepkToolInterlop.TextureImportMatchNames = true;
                EepkToolInterlop.AssetReuseMatchName = true;

                EffectContainerFile binaryFile = (EffectContainerFile)GetParsedFile<EffectContainerFile>(path, false);
                EffectContainerFile cpkBinFile = (EffectContainerFile)GetParsedFile<EffectContainerFile>(path, true);

                Section section = file.GetSection(Sections.EEPK_Effect);

                if (section != null)
                {
                    binaryFile.UninstallEffects(section.IDs, cpkBinFile);
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.EEPK, path);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Uninstall_PSC(string path, _File file)
        {
            try
            {
                PSC_File binaryFile = (PSC_File)GetParsedFile<PSC_File>(path, false);
                PSC_File cpkBinFile = (PSC_File)GetParsedFile<PSC_File>(path, true);

                for (int i = binaryFile.Configurations.Count - 1; i >= 0; i--)
                {
                    PSC_Configuration cpkConfig = (cpkBinFile != null) ? cpkBinFile.GetConfiguration(binaryFile.Configurations[i].Index) : null;

                    for (int a = binaryFile.Configurations[i].PscEntries.Count - 1; a >= 0; a--)
                    {
                        PSC_Entry cpkPscEntry = (cpkConfig != null) ? cpkConfig.GetPscEntry(binaryFile.Configurations[i].PscEntries[a].Index) : null;

                        //Check if mod has SPec Entries installed for this PSC Entry
                        Section section = file.GetSection(Sections.GetPscEntry(binaryFile.Configurations[i].PscEntries[a].Index));
                        if (section != null)
                        {
                            //It does, so uninstall them.
                            UninstallEntries(binaryFile.Configurations[i].PscEntries[a].PscSpecEntries, (cpkPscEntry != null) ? cpkPscEntry.PscSpecEntries : null, section.IDs);
                        }

                        //If PSC Entry now has 0 Spec entries, then remove it
                        if(binaryFile.Configurations[i].PscEntries[a].PscSpecEntries.Count == 0)
                        {
                            binaryFile.Configurations[i].PscEntries.RemoveAt(a);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.PSC, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_PUP(string path, _File file)
        {
            try
            {
                PUP_File binaryFile = (PUP_File)GetParsedFile<PUP_File>(path, false);
                PUP_File cpkBinFile = (PUP_File)GetParsedFile<PUP_File>(path, true);

                Section section = file.GetSection(Sections.PUP_Entry);

                if (section != null)
                {
                    UninstallEntries(binaryFile.PupEntries, (cpkBinFile != null) ? cpkBinFile.PupEntries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.PUP, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_AUR(string path, _File file)
        {
            try
            {
                AUR_File binaryFile = (AUR_File)GetParsedFile<AUR_File>(path, false);
                AUR_File cpkBinFile = (AUR_File)GetParsedFile<AUR_File>(path, true);

                Section aurSection = file.GetSection(Sections.AUR_Aura);
                Section charaSection = file.GetSection(Sections.AUR_Chara);

                if (aurSection != null)
                {
                    UninstallEntries(binaryFile.Auras, (cpkBinFile != null) ? cpkBinFile.Auras : null, aurSection.IDs);
                }
                if (charaSection != null)
                {
                    UninstallEntries(binaryFile.CharacterAuras, (cpkBinFile != null) ? cpkBinFile.CharacterAuras : null, charaSection.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.AUR, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_TSD(string path, _File file)
        {
            try
            {
                TSD_File binaryFile = (TSD_File)GetParsedFile<TSD_File>(path, false);
                TSD_File cpkBinFile = (TSD_File)GetParsedFile<TSD_File>(path, true);

                Section triggerSection = file.GetSection(Sections.TSD_Trigger);
                Section constantSection = file.GetSection(Sections.TSD_Constant);
                Section zoneSection = file.GetSection(Sections.TSD_Zone);
                Section globalSection = file.GetSection(Sections.TSD_Global);
                Section eventSection = file.GetSection(Sections.TSD_Event);

                if (triggerSection != null)
                    UninstallEntries(binaryFile.Triggers, (cpkBinFile != null) ? cpkBinFile.Triggers : null, triggerSection.IDs);
                if (constantSection != null)
                    UninstallEntries(binaryFile.Constants, (cpkBinFile != null) ? cpkBinFile.Constants : null, constantSection.IDs);
                if (zoneSection != null)
                    UninstallEntries(binaryFile.Zones, (cpkBinFile != null) ? cpkBinFile.Zones : null, zoneSection.IDs);
                if (globalSection != null)
                    UninstallEntries(binaryFile.Globals, (cpkBinFile != null) ? cpkBinFile.Globals : null, globalSection.IDs);
                if (eventSection != null)
                    UninstallEntries(binaryFile.Events, (cpkBinFile != null) ? cpkBinFile.Events : null, eventSection.IDs);

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.TSD, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_TNL(string path, _File file)
        {
            try
            {
                TNL_File binaryFile = (TNL_File)GetParsedFile<TNL_File>(path, false);
                TNL_File cpkBinFile = (TNL_File)GetParsedFile<TNL_File>(path, true);

                Section charaSection = file.GetSection(Sections.TNL_Character);
                Section teacherSection = file.GetSection(Sections.TNL_Teacher);
                Section objectSection = file.GetSection(Sections.TNL_Object);
                Section actionSection = file.GetSection(Sections.TNL_Action);

                if (charaSection != null)
                    UninstallEntries(binaryFile.Characters, (cpkBinFile != null) ? cpkBinFile.Characters : null, charaSection.IDs);
                if (teacherSection != null)
                    UninstallEntries(binaryFile.Teachers, (cpkBinFile != null) ? cpkBinFile.Teachers : null, teacherSection.IDs);
                if (objectSection != null)
                    UninstallEntries(binaryFile.Objects, (cpkBinFile != null) ? cpkBinFile.Objects : null, objectSection.IDs);
                if (actionSection != null)
                    UninstallEntries(binaryFile.Actions, (cpkBinFile != null) ? cpkBinFile.Actions : null, actionSection.IDs);

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.TNL, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_EMB(string path, _File file)
        {
            try
            {
                EMB_File binaryFile = (EMB_File)GetParsedFile<EMB_File>(path, false);
                EMB_File cpkBinFile = (EMB_File)GetParsedFile<EMB_File>(path, true);

                Section section = file.GetSection(Sections.EMB_Entry);

                if (section != null)
                {
                    for(int i = 0; i < section.IDs.Count; i++)
                    {
                        int idNum;

                        if (int.TryParse(section.IDs[i], out idNum))
                        {
                            //ID is number (index)
                            EmbEntry original = (cpkBinFile != null) ? cpkBinFile.GetEntry(idNum) : null;
                            binaryFile.RemoveEntry(section.IDs[i], original);
                        }
                        else
                        {
                            //ID is string (name)
                            EmbEntry original = (cpkBinFile != null) ? cpkBinFile.GetEntry(section.IDs[i]) : null;
                            var existingEntry = binaryFile.Entry.FirstOrDefault(x => x.Name == section.IDs[i]);

                            if(existingEntry != null)
                            {
                                binaryFile.RemoveEntry(binaryFile.Entry.IndexOf(existingEntry).ToString(), original);
                            }
                        }
                        
                    }

                    binaryFile.TrimNullEntries();
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.EMB, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_QXD(string path, _File file)
        {
            try
            {
                QXD_File binaryFile = (QXD_File)GetParsedFile<QXD_File>(path, false);
                QXD_File cpkBinFile = (QXD_File)GetParsedFile<QXD_File>(path, true);

                Section questSection = file.GetSection(Sections.QXD_Quest);
                Section chara1Section = file.GetSection(Sections.QXD_Character1);
                Section chara2Section = file.GetSection(Sections.QXD_Character2);
                Section collectionSection = file.GetSection(Sections.QXD_Collection);

                if (questSection != null)
                    UninstallEntries(binaryFile.Quests, (cpkBinFile != null) ? cpkBinFile.Quests : null, questSection.IDs);
                if (chara1Section != null)
                    UninstallEntries(binaryFile.Characters1, (cpkBinFile != null) ? cpkBinFile.Characters1 : null, chara1Section.IDs);
                if (chara2Section != null)
                    UninstallEntries(binaryFile.Characters2, (cpkBinFile != null) ? cpkBinFile.Characters2 : null, chara2Section.IDs);
                if (collectionSection != null)
                    UninstallEntries(binaryFile.Collections, (cpkBinFile != null) ? cpkBinFile.Collections : null, collectionSection.IDs);

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at {0} uninstall phase ({1}).", ErrorCode.QXD, path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_BGM_ACB(string path, _File file)
        {
            try
            {
                new Music.MusicUninstaller(this, file);
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at BGM ACB uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_PAL(string path, _File file)
        {
            try
            {
                PAL_File binaryFile = (PAL_File)GetParsedFile<PAL_File>(path, false);
                PAL_File cpkBinFile = (PAL_File)GetParsedFile<PAL_File>(path, true);

                Section section = file.GetSection(Sections.PAL_Entry);

                if (section != null)
                {
                    UninstallEntries(binaryFile.PalEntries, (cpkBinFile != null) ? cpkBinFile.PalEntries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at PAL uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_TTB(string path, _File file)
        {
            try
            {
                TTB_File binaryFile = (TTB_File)GetParsedFile<TTB_File>(path, false);
                TTB_File cpkBinFile = (TTB_File)GetParsedFile<TTB_File>(path, true);

                UninstallSubEntries<TTB_Event, TTB_Entry>(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, file, true);
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at TTB uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_TTC(string path, _File file)
        {
            try
            {
                TTC_File binaryFile = (TTC_File)GetParsedFile<TTC_File>(path, false);
                TTC_File cpkBinFile = (TTC_File)GetParsedFile<TTC_File>(path, true);

                Section section = file.GetSection(Sections.TTC_Entry);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, section.IDs);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at TTC uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_SEV(string path, _File file)
        {
            try
            {
                SEV_File binaryFile = (SEV_File)GetParsedFile<SEV_File>(path, false);
                SEV_File cpkBinFile = (SEV_File)GetParsedFile<SEV_File>(path, true);

                UninstallSubEntries<SEV_CharEvent, SEV_Entry>(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, file, true);
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at SEV uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }
        
        private void Uninstall_HCI(string path, _File file)
        {
            try
            {
                HCI_File binaryFile = (HCI_File)GetParsedFile<HCI_File>(path, false);
                HCI_File cpkBinFile = (HCI_File)GetParsedFile<HCI_File>(path, true);

                Section section = file.GetSection(Sections.HCI_Entry);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, section.IDs);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at HCI uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_CML(string path, _File file)
        {
            try
            {
                CML_File binaryFile = (CML_File)GetParsedFile<CML_File>(path, false);
                CML_File cpkBinFile = (CML_File)GetParsedFile<CML_File>(path, true);

                Section section = file.GetSection(Sections.CML_Entry);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, section.IDs);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at CML uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        //Generic uninstallers
        private void UninstallEntries<T>(IList<T> entries, IList<T> ogEntries, List<string> ids) where T : IInstallable
        {
            if (entries == null) return;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (ids.Contains(entries[i].Index))
                {
                    T newEntry = GetOriginalEntry<T>(ogEntries, entries[i].Index);

                    if (newEntry != null)
                    {
                        entries[i] = newEntry;
                    }
                    else
                    {
                        entries.RemoveAt(i);
                    }
                }
            }
        }

        private void UninstallSubEntries<T, M>(IList<M> entries, IList<M> ogEntries, _File file, bool removeRootIfNoChildren) where T : IInstallable where M : class, IInstallable_2<T>
        {
            if (entries == null) return;

            List<string> roots = new List<string>();
            
            foreach(var section in file.Sections)
            {
                var splitName = section.FileSection.Split('/');
                string rootId = splitName[splitName.Length - 1];

                M rootEntry = GetOriginalEntry2<T,M>(entries, rootId, true);
                M originalRootEntry = GetOriginalEntry2<T, M>(ogEntries, rootId);

                if(rootEntry != null)
                {
                    if (!roots.Contains(rootId))
                        roots.Add(rootId);

                    UninstallEntries(rootEntry.SubEntries, (originalRootEntry != null) ? originalRootEntry.SubEntries : null, section.IDs);
                }
            }

            //Remove root entries
            if (removeRootIfNoChildren)
            {
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    if (roots.Contains(entries[i].Index) && Utils.IsListNullOrEmpty(entries[i].SubEntries))
                        entries.Remove(entries[i]);
                }
            }

        }

        private T GetOriginalEntry<T>(IList<T> ogEntries, string id) where T :IInstallable
        {
            if (ogEntries == null) return default(T);

            if(ogEntries.Any(e => e.Index == id))
            {
                return ogEntries.FirstOrDefault(e => e.Index == id);
            }
            else
            {
                return default(T);
            }
        }

        private M GetOriginalEntry2<T, M>(IList<M> ogEntries, string id, bool returnNull = false) where T : IInstallable where M : class, IInstallable_2<T>
        {
            if (ogEntries == null) return (returnNull) ? null : default(M);

            if (ogEntries.Any(e => e.Index == id))
            {
                return ogEntries.FirstOrDefault(e => e.Index == id);
            }
            else
            {
                return (returnNull) ? null : default(M);
            }
        }

        //MsgComponent
        private void Uninstall_MsgComponent(string path, _File file)
        {
            foreach(var langSuffix in GeneralInfo.LanguageSuffix)
            {
                Uninstall_MSG(path + langSuffix, file);
            }
        }


        //File Handling. Methods for getting files from the game and parsing them.
        /// <summary>
        /// Gets a parsed file from the game and caches it. If it exists in the cache already then that is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public object GetParsedFile<T>(string path, bool fromCpk, bool raiseEx = true) where T : new()
        {
            if (fromCpk)
            {
                return Install_NEW.GetParsedFileFromGame(path, FileIO, fromCpk, raiseEx);
            }

            var cachedFile = fileManager.GetParsedFile<T>(path);

            if (cachedFile != null)
            {
                //File is already cached. Return that instance.
                return cachedFile;
            }
            else
            {
                //File is not cached. So parse it, add it and then return it.
                var file = Install_NEW.GetParsedFileFromGame(path, FileIO, fromCpk, raiseEx);
                if(file != null)
                    fileManager.AddParsedFile(path, file);
                return file;
            }
        }


        //UI
        private void SetProgressBarSteps()
        {
            parent.Dispatcher.Invoke((Action)(() =>
            {
                parent.ProgressBar_Main.Maximum = currentMod.TotalInstalledFiles;
                parent.ProgressBar_Main.Value = 0;
            }));
        }

        private void UpdateProgessBarText(string text, bool advanceProgress = true)
        {
            parent.Dispatcher.Invoke((Action)(() =>
            {
                if (advanceProgress)
                    parent.ProgressBar_Main.Value++;

                parent.ProgressBar_Label.Content = text;
            }));
        }

    }
}
