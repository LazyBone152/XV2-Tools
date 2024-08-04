using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Threading;
using System.Globalization;
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
using Xv2CoreLib.Eternity;
using Xv2CoreLib.CST;
using Xv2CoreLib.OCS;
using Xv2CoreLib.QML;
using Xv2CoreLib.OCO;
using Xv2CoreLib.DML;
using Xv2CoreLib.QSF;
using Xv2CoreLib.QED;
using Xv2CoreLib.QSL;
using Xv2CoreLib.QBT;
using Xv2CoreLib.TNN;
using Xv2CoreLib.ODF;
using Xv2CoreLib.BCM;
using Xv2CoreLib.VLC;

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
            foreach (var file in currentMod.Files)
            {
                UpdateProgessBarText(string.Format("_Uninstalling \"{0}\"...", file.filePath));
                ResolveFileType(file.filePath, file);
            }

            //MsgComponents
            foreach (var file in currentMod.MsgComponents)
            {
                UpdateProgessBarText(string.Format("_Uninstalling \"{0}\"...", file.filePath));
                Uninstall_MsgComponent(file.filePath, file);
            }

            //Clear trackers
            currentMod.Files.Clear();
            currentMod.MsgComponents.Clear();
            currentMod.Aliases.Clear();
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
                fileManager.NukeEmptyDirectories();
            }
#if !DEBUG
            catch (Exception ex)
            {
                //Handle install errors here
                MessageBox.Show(string.Format("{0}\n\n{1}\n\nFile: {2}", ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : "", fileManager.lastSaved), "Uninstall Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
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
            if (path.Equals(ACB.AcbInstaller.DIRECT_INSTALL_TYPE, StringComparison.OrdinalIgnoreCase) || path.Equals(ACB.AcbInstaller.OPTION_INSTALL_TYPE, StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path) == ".acb")
            {
                Uninstall_ACB(path, file);
                return;
            }
            if (path.Equals(CharaSlotsFile.FILE_NAME_BIN, StringComparison.OrdinalIgnoreCase))
            {
                Uninstall_CharaSlots(file);
                return;
            }
            if (path.Equals(StageSlotsFile.FILE_NAME_BIN, StringComparison.OrdinalIgnoreCase) || path.Equals(StageSlotsFile.FILE_NAME_LOCAL_BIN, StringComparison.OrdinalIgnoreCase))
            {
                Uninstall_StageSlots(file);
                return;
            }
            if (path.Equals(StageDefFile.PATH, StringComparison.OrdinalIgnoreCase))
            {
                Uninstall_StageDef(file);
                return;
            }
            if (path.Equals(PrebakedFile.PATH, StringComparison.OrdinalIgnoreCase))
            {
                Uninstall_Prebaked(file);
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
                case ".ocs":
                    Uninstall_OCS(path, file);
                    break;
                case ".qml":
                    Uninstall_QML(path, file);
                    break;
                case ".cst":
                    Uninstall_CST(path, file);
                    break;
                case ".oco":
                    Uninstall_OCO(path, file);
                    break;
                case ".dml":
                    Uninstall_DML(path, file);
                    break;
                case ".qsf":
                    Uninstall_QSF(path, file);
                    break;
                case ".qsl":
                    Uninstall_QSL(path, file);
                    break;
                case ".qbt":
                    Uninstall_QBT(path, file);
                    break;
                case ".qed":
                    Uninstall_QED(path, file);
                    break;
                case ".tnn":
                    Uninstall_TNN(path, file);
                    break;
                case ".odf":
                    Uninstall_ODF(path, file);
                    break;
                case ".bcm":
                    Uninstall_BCM(path, file);
                    break;
                case ".vlc":
                    Uninstall_VLC(path, file);
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

                for (int i = currentMod.JungleFiles.Count - 1; i >= 0; i--)
                {
                    if (!currentMod.JungleFiles[i].InstalledThisRun)
                    {
                        if (Directory.Exists(GeneralInfo.GetPathInGameDir(currentMod.JungleFiles[i].filePath)))
                        {
                            Directory.Delete(GeneralInfo.GetPathInGameDir(currentMod.JungleFiles[i].filePath), true);
                        }
                        else if (File.Exists(GeneralInfo.GetPathInGameDir(currentMod.JungleFiles[i].filePath)))
                        {
                            File.Delete(GeneralInfo.GetPathInGameDir(currentMod.JungleFiles[i].filePath));
                        }

                        currentMod.JungleFiles.RemoveAt(i);
                    }
                }

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
                string error = string.Format("Failed at IDB uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at CUS uninstall phase ({0}).", path);
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
                Section skeletonDataSection = file.GetSection(Sections.BCS_SkeletonData);

                if (partSetSection != null)
                    UninstallEntries(binaryFile.PartSets, (cpkBinFile != null) ? cpkBinFile.PartSets : null, partSetSection.IDs);

                if (binaryFile.PartColors != null)
                {
                    //Standard path
                    foreach (PartColor section in binaryFile.PartColors)
                    {
                        Section partColorSection = file.GetSection(Sections.GetBcsPartColor(section.Index));

                        if (partColorSection != null)
                        {
                            PartColor cpkSection = (cpkBinFile != null) ? cpkBinFile.GetPartColors(section.Index, section.Name, false) : null;
                            UninstallEntries(section.ColorsList, (cpkSection != null) ? cpkSection.ColorsList : null, partColorSection.IDs);
                        }
                    }

                    //Overwrite path
                    for (int i = binaryFile.PartColors.Count - 1; i >= 0; i--)
                    {
                        Section partColorOverwirteSection = file.GetSection(Sections.GetBcsPartColorOverwrite(binaryFile.PartColors[i].Index));

                        if (partColorOverwirteSection != null)
                        {
                            PartColor cpkSection = (cpkBinFile != null) ? cpkBinFile.GetPartColors(binaryFile.PartColors[i].Index, binaryFile.PartColors[i].Name, false) : null;

                            if (cpkSection != null)
                            {
                                binaryFile.PartColors[i] = cpkSection;
                            }
                            else
                            {
                                binaryFile.PartColors.RemoveAt(i);
                            }
                        }
                    }
                }

                if (bodiesSection != null)
                    UninstallEntries(binaryFile.Bodies, (cpkBinFile != null) ? cpkBinFile.Bodies : null, bodiesSection.IDs);

                if (skeletonDataSection != null)
                {
                    foreach (string _idStr in skeletonDataSection.IDs)
                    {
                        int id;

                        if (int.TryParse(_idStr, out id))
                        {
                            if (id == 0)
                            {
                                binaryFile.SkeletonData1 = (cpkBinFile != null) ? cpkBinFile.SkeletonData1 : null;
                            }
                            else if (id == 1)
                            {
                                binaryFile.SkeletonData2 = (cpkBinFile != null) ? cpkBinFile.SkeletonData2 : null;
                            }

                        }
                    }

                    skeletonDataSection.IDs.Clear();
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at BCS uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at CMS uninstall phase ({0}).", path);
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
                    UninstallEntries(binaryFile.BacEntries, (cpkBinFile != null) ? cpkBinFile.BacEntries : null, section.IDs);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at BAC uninstall phase ({0}).", path);
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

                if (cpkBinFile != null)
                    cpkBinFile.ConvertToXv2();

                Section section = file.GetSection(Sections.BDM_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.BDM_Entries, (cpkBinFile != null) ? cpkBinFile.BDM_Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at BDM uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at BEV uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at BPE uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at BSA uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at CNC uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at CNS uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at CSO uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_EAN(string path, _File file)
        {
            try
            {
                EAN_File binaryFile = (EAN_File)GetParsedFile<EAN_File>(path, false);
                EAN_File cpkBinFile = (EAN_File)GetParsedFile<EAN_File>(path, true, false);

                Section section = file.GetSection(Sections.EAN_Entries);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Animations, (cpkBinFile != null) ? cpkBinFile.Animations : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at EAN uninstall phase ({0}).", path);
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

                    if (!IDB_File.IsIdbMsgFile(file.filePath))
                    {
                        //Default ID-based uninstallation
                        UninstallEntries(binaryFile.MSG_Entries, cpkBinFile?.MSG_Entries, section.IDs);
                    }
                    else
                    {
                        //Index based uninstallation
                        for (int i = binaryFile.MSG_Entries.Count - 1; i >= 0; i--)
                        {
                            if (section.IDs.Contains(binaryFile.MSG_Entries[i].Index))
                            {
                                MSG_Entry newEntry = (cpkBinFile?.MSG_Entries != null) ? GetOriginalEntry(cpkBinFile.MSG_Entries, i.ToString()) : null;

                                if (newEntry != null)
                                {
                                    binaryFile.MSG_Entries[i] = newEntry;
                                }
                                else
                                {
                                    if (i == binaryFile.MSG_Entries.Count - 1)
                                    {
                                        //Last index, can remove freely
                                        binaryFile.MSG_Entries.RemoveAt(i);
                                    }
                                    else
                                    {
                                        //Cant remove entry or the index of every other entry after it will be wrong.
                                        //We can make it a dummy entry instead.
                                        binaryFile.MSG_Entries[i].Msg_Content.Clear();
                                        binaryFile.MSG_Entries[i].Msg_Content.Add(new Msg_Line() { Text = "" });
                                    }
                                }
                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at MSG uninstall phase ({0}).", path);
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
                EepkToolInterlop.FullDecompile = false;
                EepkToolInterlop.IsInstaller = true;

                EffectContainerFile binaryFile = (EffectContainerFile)GetParsedFile<EffectContainerFile>(path, false);
                EffectContainerFile cpkBinFile = (EffectContainerFile)GetParsedFile<EffectContainerFile>(path, true, false);

                Section section = file.GetSection(Sections.EEPK_Effect);

                if (section != null)
                {
                    binaryFile.UninstallEffects(section.IDs, cpkBinFile);
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at EEPK uninstall phase ({0}).",  path);
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
                        if (binaryFile.Configurations[i].PscEntries[a].PscSpecEntries.Count == 0)
                        {
                            binaryFile.Configurations[i].PscEntries.RemoveAt(a);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at PSC uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at PUP uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at AUR uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at TSD uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at TNL uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_EMB(string path, _File file)
        {
            try
            {
                EMB_File binaryFile = (EMB_File)GetParsedFile<EMB_File>(path, false);
                EMB_File cpkBinFile = (EMB_File)GetParsedFile<EMB_File>(path, true, false);

                Section section = file.GetSection(Sections.EMB_Entry);

                if (section != null)
                {
                    for (int i = 0; i < section.IDs.Count; i++)
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

                            if (existingEntry != null)
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
                string error = string.Format("Failed at EMB uninstall phase ({0}).", path);
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
                string error = string.Format("Failed at QXD uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_ACB(string path, _File file)
        {
            try
            {
                new ACB.AcbUninstaller(this, file);
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at ACB uninstall phase ({0}).", path);
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

        private void Uninstall_VLC(string path, _File file)
        {
            try
            {
                VLC_File binaryFile = (VLC_File)GetParsedFile<VLC_File>(path, false);
                VLC_File cpkBinFile = (VLC_File)GetParsedFile<VLC_File>(path, true);

                Section zoomInCameraSection = file.GetSection(Sections.VLC_ZoomInCamera);
                Section unkCameraSection = file.GetSection(Sections.VLC_UnkCamera);

                if (zoomInCameraSection != null)
                    UninstallEntries(binaryFile.ZoomInCamera, (cpkBinFile != null) ? cpkBinFile.ZoomInCamera : null, zoomInCameraSection.IDs);
                if (unkCameraSection != null)
                    UninstallEntries(binaryFile.UnkCamera, (cpkBinFile != null) ? cpkBinFile.UnkCamera : null, unkCameraSection.IDs);
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at VLC uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_CharaSlots(_File file)
        {
            try
            {
                CharaSlotsFile charaSlotsFile = (CharaSlotsFile)GetParsedFile<CharaSlotsFile>(CharaSlotsFile.FILE_NAME_BIN, false, true);
                CST_File cstFile = (CST_File)GetParsedFile<CST_File>(CST_File.CST_PATH, true, true);
                CST_File x2sFileConverted = charaSlotsFile.ConvertToCst();

                if (charaSlotsFile == null) return;

                Section section = file.GetSection(Sections.CharaSlotEntry);

                if (section != null)
                {
                    x2sFileConverted.UninstallEntries(section.IDs, cstFile);
                }

                //Convert back to X2S
                CharaSlotsFile tempCharaSlotsFile = x2sFileConverted.ConvertToPatcherSlotsFile();
                charaSlotsFile.CharaSlots = tempCharaSlotsFile.CharaSlots;

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at CharaSlots uninstall phase ({0}).", CharaSlotsFile.FILE_NAME_BIN);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_StageSlots(_File file)
        {
            try
            {
                StageSlotsFile stageSlotFile = (StageSlotsFile)GetParsedFile<StageSlotsFile>(file.filePath, false, true);

                Section section = file.GetSection(Sections.StageSlotEntry);

                if (section != null)
                {
                    StageSlotsFile defaultFile = file.filePath.Equals(StageSlotsFile.FILE_NAME_LOCAL_BIN) ? StageSlotsFile.DefaultLocalFile : StageSlotsFile.DefaultFile;
                    UninstallEntries(stageSlotFile.StageSlots, defaultFile.StageSlots, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at StageSlots uninstall phase ({0}).", file.filePath);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_StageDef(_File file)
        {
            try
            {
                StageDefFile stageDefs = (StageDefFile)GetParsedFile<StageDefFile>(StageDefFile.PATH, false, true);

                Section section = file.GetSection(Sections.StageDefEntry);

                if (section != null)
                {
                    stageDefs.UninstallStages(section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at StageDef uninstall phase ({0}).", StageDefFile.PATH);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_OCS(string path, _File file)
        {
            try
            {
                OCS_File binaryFile = (OCS_File)GetParsedFile<OCS_File>(path, false);
                OCS_File cpkBinFile = (OCS_File)GetParsedFile<OCS_File>(path, true);

                Section section = file.GetSection(Sections.OCS_Skill);

                if (section != null)
                {
                    binaryFile.UninstallEntries(section.IDs, cpkBinFile);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at OCS uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_Prebaked(_File file)
        {
            try
            {
                PrebakedFile prebakedFile = (PrebakedFile)GetParsedFile<PrebakedFile>(PrebakedFile.PATH, false, false);

                if (prebakedFile == null) return;

                //Uninstall entries
                //Cus Auras:
                Section cusAuraSection = file.GetSection(Sections.PrebakedCusAura);

                if (cusAuraSection != null)
                    prebakedFile.UninstallCusAuras(cusAuraSection.IDs);

                //Body Shapes:
                Section bodyShapeSection = file.GetSection(Sections.PrebakedBodyShape);

                if (bodyShapeSection != null)
                    prebakedFile.UninstallBodyShapes(bodyShapeSection.IDs);

                //Aliases:
                Section aliasSection = file.GetSection(Sections.PrebakedAlias);

                if (aliasSection != null)
                    prebakedFile.UninstallAliases(aliasSection.IDs);

                //Ozarus:
                Section ozaruSection = file.GetSection(Sections.PrebakedOzarus);

                if (ozaruSection != null)
                    prebakedFile.UninstallOzarus(ozaruSection.IDs);

                //AutoPortraits:
                Section autoPortraitSection = file.GetSection(Sections.PrebakedAutoBattlePortrait);

                if (autoPortraitSection != null)
                    prebakedFile.UninstallAutoBattlePortraits(autoPortraitSection.IDs);

                //AnyDualSkill:
                Section anyDualSkillSection = file.GetSection(Sections.PrebakedAnyDualSkillList);

                if (anyDualSkillSection != null)
                    prebakedFile.UninstallAnyDualSkill(anyDualSkillSection.IDs);

                //Aura Extra Data:
                Section auraExtraSection = file.GetSection(Sections.PrebakedAuraExtraData);

                if (auraExtraSection != null)
                    prebakedFile.UninstallAuraExtraData(auraExtraSection.IDs);

                //Cell Maxes:
                Section cellMaxSection = file.GetSection(Sections.PrebakedCellMax);

                if (cellMaxSection != null)
                    prebakedFile.UninstallCellMaxes(cellMaxSection.IDs);

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at Prebaked uninstall phase ({0}).", PrebakedFile.PATH);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_QML(string path, _File file)
        {
            try
            {
                QML_File binaryFile = (QML_File)GetParsedFile<QML_File>(path, false);
                QML_File cpkBinFile = (QML_File)GetParsedFile<QML_File>(path, true, false);

                Section section = file.GetSection(Sections.QML_Entry);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Entries, (cpkBinFile != null) ? cpkBinFile.Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at QML uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_CST(string path, _File file)
        {
            try
            {
                CST_File binaryCpkFile = (CST_File)GetParsedFile<CST_File>(path, false, false);
                CST_File cpkCstFile = (CST_File)GetParsedFile<CST_File>(path, true, false);

                Section section = file.GetSection(Sections.CST_Entry);

                if (section != null)
                {
                    binaryCpkFile.UninstallEntries(section.IDs, cpkCstFile);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at CST uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_OCO(string path, _File file)
        {
            try
            {
                OCO_File binaryFile = (OCO_File)GetParsedFile<OCO_File>(path, false);
                OCO_File cpkBinFile = (OCO_File)GetParsedFile<OCO_File>(path, true);

                UninstallSubEntries<OCO_Costume, OCO_Partner>(binaryFile.Partners, (cpkBinFile != null) ? cpkBinFile.Partners : null, file, true);
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at OCO uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_DML(string path, _File file)
        {
            try
            {
                DML_File binaryFile = (DML_File)GetParsedFile<DML_File>(path, false);
                DML_File cpkBinFile = (DML_File)GetParsedFile<DML_File>(path, true, false);

                Section section = file.GetSection(Sections.DML_Entry);

                if (section != null)
                {
                    UninstallEntries(binaryFile.DML_Entries, (cpkBinFile != null) ? cpkBinFile.DML_Entries : null, section.IDs);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at DML uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_QSF(string path, _File file)
        {
            try
            {
                QSF_File binaryCpkFile = (QSF_File)GetParsedFile<QSF_File>(path, false, false);
                QSF_File cpkQsfFile = (QSF_File)GetParsedFile<QSF_File>(path, true, false);

                Section section = file.GetSection(Sections.QSF_Entry);

                if (section != null)
                {
                    binaryCpkFile.UninstallEntries(section.IDs, cpkQsfFile);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at QSF uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_QBT(string path, _File file)
        {
            try
            {
                QBT_File binaryFile = (QBT_File)GetParsedFile<QBT_File>(path, false);
                QBT_File cpkBinFile = (QBT_File)GetParsedFile<QBT_File>(path, true, false);

                Section normalSection = file.GetSection(Sections.QBT_NormalDialogue);
                Section interactiveSection = file.GetSection(Sections.QBT_InteractiveDialogue);
                Section specialSection = file.GetSection(Sections.QBT_SpecialDialogue);

                if (normalSection != null)
                {
                    UninstallEntries(binaryFile.NormalDialogues, (cpkBinFile != null) ? cpkBinFile.NormalDialogues : null, normalSection.IDs);
                }

                if (interactiveSection != null)
                {
                    UninstallEntries(binaryFile.InteractiveDialogues, (cpkBinFile != null) ? cpkBinFile.InteractiveDialogues : null, interactiveSection.IDs);
                }

                if (specialSection != null)
                {
                    UninstallEntries(binaryFile.SpecialDialogues, (cpkBinFile != null) ? cpkBinFile.SpecialDialogues : null, specialSection.IDs);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at QBT uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_QSL(string path, _File file)
        {
            try
            {
                QSL_File binaryFile = (QSL_File)GetParsedFile<QSL_File>(path, false);
                QSL_File cpkBinFile = (QSL_File)GetParsedFile<QSL_File>(path, true, false);

                UninstallSubEntries<PositionEntry, StageEntry>(binaryFile.Stages, (cpkBinFile != null) ? cpkBinFile.Stages : null, file, true);
            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at QSL uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_QED(string path, _File file)
        {
            try
            {
                QED_File binaryFile = (QED_File)GetParsedFile<QED_File>(path, false);
                QED_File cpkBinFile = (QED_File)GetParsedFile<QED_File>(path, true, false);

                Section normalSection = file.GetSection(Sections.QED_Entry);

                if (normalSection != null)
                {
                    UninstallEntries(binaryFile.Events, (cpkBinFile != null) ? cpkBinFile.Events : null, normalSection.IDs);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at QED uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_TNN(string path, _File file)
        {
            try
            {
                TNN_File binaryFile = (TNN_File)GetParsedFile<TNN_File>(path, false);
                TNN_File cpkBinFile = (TNN_File)GetParsedFile<TNN_File>(path, true, true);

                Section section = file.GetSection(Sections.TNN_Tutorial);

                if (section != null)
                {
                    UninstallEntries(binaryFile.Tutorials, (cpkBinFile != null) ? cpkBinFile.Tutorials : null, section.IDs);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at TNN uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }

        private void Uninstall_ODF(string path, _File file)
        {
            try
            {
                ODF_File binaryFile = (ODF_File)GetParsedFile<ODF_File>(path, false);
                ODF_File cpkBinFile = (ODF_File)GetParsedFile<ODF_File>(path, true, true);

                Section section = file.GetSection(Sections.ODF_Entry);

                if (section != null)
                {
                    UninstallEntries(binaryFile.SubHeader[0].Entries, (cpkBinFile != null) ? cpkBinFile.SubHeader[0].Entries : null, section.IDs);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at ODF uninstall phase ({0}).", path);
                throw new Exception(error, ex);
            }
        }
        
        private void Uninstall_BCM(string path, _File file)
        {
            try
            {
                BCM_File binaryFile = (BCM_File)GetParsedFile<BCM_File>(path, false);
                BCM_File cpkBinFile = (BCM_File)GetParsedFile<BCM_File>(path, true, false);

                Section section = file.GetSection(Sections.BCM_Entry);

                if (section != null)
                {
                    binaryFile.UninstallEntries(section.IDs, cpkBinFile);
                }

            }
            catch (Exception ex)
            {
                string error = string.Format("Failed at BCM uninstall phase ({0}).", path);
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

        private void UninstallSubEntries<T, M>(IList<M> entries, IList<M> ogEntries, _File file, bool removeRootIfNoChildren) where T : IInstallable where M : class, IInstallable_2<T>, IInstallable
        {
            if (entries == null) return;

            List<string> roots = new List<string>();

            foreach (var section in file.Sections)
            {
                var splitName = section.FileSection.Split('/');
                string rootId = splitName[splitName.Length - 1];

                M rootEntry = GetOriginalEntry2<T, M>(entries, rootId, true);
                M originalRootEntry = GetOriginalEntry2<T, M>(ogEntries, rootId);

                if (rootEntry != null)
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

        private T GetOriginalEntry<T>(IList<T> ogEntries, string id) where T : IInstallable
        {
            if (ogEntries == null) return default(T);

            if (ogEntries.Any(e => e.Index == id))
            {
                return ogEntries.FirstOrDefault(e => e.Index == id);
            }
            else
            {
                return default(T);
            }
        }

        private M GetOriginalEntry2<T, M>(IList<M> ogEntries, string id, bool returnNull = false) where T : IInstallable where M : class, IInstallable_2<T>, IInstallable
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
            foreach (var langSuffix in GeneralInfo.LanguageSuffix)
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
                return Install.GetParsedFileFromGame(path, FileIO, fromCpk, raiseEx);
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
                var file = Install.GetParsedFileFromGame(path, FileIO, fromCpk, raiseEx);
                if (file != null)
                    fileManager.AddParsedFile(path, file);
                return file;
            }
        }


        //UI
        private void SetProgressBarSteps()
        {
            parent.Dispatcher.Invoke((System.Action)(() =>
            {
                parent.ProgressBar_Main.Maximum = currentMod.TotalInstalledFiles;
                parent.ProgressBar_Main.Value = 0;
            }));
        }

        private void UpdateProgessBarText(string text, bool advanceProgress = true)
        {
            parent.Dispatcher.Invoke((System.Action)(() =>
            {
                if (advanceProgress)
                    parent.ProgressBar_Main.Value++;

                parent.ProgressBar_Label.Content = text;
            }));
        }

    }
}
