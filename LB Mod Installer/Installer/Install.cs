using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using LB_Mod_Installer.Binding;
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
using Xv2CoreLib.AUR;
using Xv2CoreLib.PUP;
using Xv2CoreLib.TSD;
using Xv2CoreLib.TNL;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.QXD;
using Xv2CoreLib.OBL;
using Xv2CoreLib.ACB_NEW;
using Xv2CoreLib.PAL;
using LB_Mod_Installer.Installer.ACB;
using Xv2CoreLib.TTB;
using Xv2CoreLib.TTC;
using Xv2CoreLib.SEV;
using Xv2CoreLib.HCI;
using Xv2CoreLib.CML;
using Xv2CoreLib.Eternity;

namespace LB_Mod_Installer.Installer
{
    public class Install
    {
        private const string JUNGLE1 = "JUNGLE1";
        private const string JUNGLE2 = "JUNGLE2";
        private const string JUNGLE3 = "JUNGLE3";

        internal List<FilePath> Files;
        internal InstallerXml installerXml;
        internal ZipReader zipManager;
        internal MainWindow Parent;
        internal Xv2FileIO FileIO;
        internal FileCacheManager fileManager;
        internal MsgComponentInstall msgComponentInstall;
        internal IdBindingManager bindingManager;

        private bool useJungle1 = false;
        private bool useJungle2 = false;

        private bool startedSaving = false; //If false and a error happens we dont need to restore files


        public Install(InstallerXml _installerXml, ZipReader _zipManager, MainWindow parent, Xv2FileIO fileIO, FileCacheManager _fileManager)
        {
            Files = _installerXml.GetInstallFiles();
            installerXml = _installerXml;
            zipManager = _zipManager;
            Parent = parent;
            FileIO = fileIO;
            msgComponentInstall = new MsgComponentInstall(this);
            fileManager = _fileManager;

            bindingManager = new IdBindingManager(this);
        }

        public void Start()
        {
#if !DEBUG
            try
#endif
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                JungleCheck();
                SetProgressBarSteps(); 
                StartInstall();

                //Finalize
                SaveFiles();

                try
                {
                    GeneralInfo.SaveTracker();
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed at tracker xml save phase.", ex);
                }

                MessageBox.Show("The mod was successfully installed.", GeneralInfo.InstallerXmlInfo.InstallerName, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

#if !DEBUG
            catch (Exception ex)
            {
                //Handle install errors here
                SaveErrorLog(ex.ToString());
                MessageBox.Show(string.Format("{0}\n\n{1}", ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : ""), "Install Error", MessageBoxButton.OK, MessageBoxImage.Error);

                if (startedSaving)
                {
                    MessageBox.Show("Installation changes will now be undone.", "Install Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                    try
                    {
                        UpdateProgessBarText("_Restoring files...", false);
                        fileManager.RestoreBackups();
                    }
                    catch
                    {
                        MessageBox.Show("Warning: Installation changes could not be undone. The installer will now close.", "Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Parent.ShutdownApp();
                    }

                    MessageBox.Show("Installation changes were undone. The installer will now close.", "Changes Undone", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    Parent.ShutdownApp();
                }
                else
                {
                    MessageBox.Show("No changes were made to the installation. The installer will now close.", "Install Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                Parent.ShutdownApp();
            }
#endif
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

        private void StartInstall()
        {
            //Files. 
            foreach (var File in Files)
            {
                FileType type = File.GetFileType();

                //Process bindings in InstallPath
                File.InstallPath = bindingManager.ParseInstallPath(File.InstallPath, File.SourcePath);

                switch (type)
                {
                    case FileType.Binary:
                    case FileType.XML:
                        //Install XML or Binary
                        UpdateProgessBarText(String.Format("_Installing \"{0}\"...", Path.GetFileName(File.InstallPath)));
                        ResolveFileType(File.SourcePath, File.InstallPath, type == FileType.XML);
                        break;
                    case FileType.VfxPackage:
                        //Install effects
                        UpdateProgessBarText(String.Format("_Installing \"{0}\"...", Path.GetFileName(File.InstallPath)));
                        Install_EEPK(File.SourcePath, File.InstallPath);
                        break;
                    case FileType.MusicPackage:
                        //Install new BGM or CSS tracks
                        UpdateProgessBarText("_Installing Audio...");
                        Install_ACB(File.SourcePath);
                        break;
                    case FileType.CopyFile:
                        //Binary file. Copy to dir.
                        UpdateProgessBarText(String.Format("_Copying \"{0}\"...", Path.GetFileNameWithoutExtension(File.SourcePath)));

                        if (!IsJungleFileBlacklisted(File.InstallPath))
                        {
                            fileManager.AddStreamFile(File.InstallPath, zipManager.GetZipEntry(string.Format("data/{0}", File.SourcePath)), File.AllowOverwrite());
                        }
                        break;
                    case FileType.CopyDir:
                        UpdateProgessBarText($"_Copying {File.SourcePath}...");
                        ProcessJungle($"{JUNGLE3}/{File.SourcePath}", true, File.InstallPath, true);
                        break;
                    default:
                        MessageBox.Show($"Unknown File.Type: {type}");
                        break;
                }
            }

            //JUNGLES

            if (useJungle1)
            {
                UpdateProgessBarText("_Installing JUNGLE1...");
                ProcessJungle(JUNGLE1, true);
            }

            if (useJungle2)
            {
                UpdateProgessBarText("_Installing JUNGLE2...");
                ProcessJungle(JUNGLE2, false);
            }

        }
        
        private void ResolveFileType(string xmlPath, string installPath, bool isXml)
        {
            switch (Path.GetExtension(Path.GetFileNameWithoutExtension(xmlPath)))
            {
                case ".eepk":
                    MessageBox.Show(string.Format("The old eepk.xml installer is no longer supported.\n\nPlease use the export functionality of EEPK Organiser (v0.4 and greater)."), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                case ".idb":
                    Install_IDB(xmlPath, installPath, isXml);
                    break;
                case ".cus":
                    Install_CUS(xmlPath, installPath, isXml);
                    break;
                case ".bcs":
                    Install_BCS(xmlPath, installPath, isXml);
                    break;
                case ".ers":
                    Install_ERS(xmlPath, installPath, isXml);
                    break;
                case ".cms":
                    Install_CMS(xmlPath, installPath, isXml);
                    break;
                case ".bac":
                    Install_BAC(xmlPath, installPath, isXml);
                    break;
                case ".bdm":
                    Install_BDM(xmlPath, installPath, isXml);
                    break;
                case ".bev":
                    Install_BEV(xmlPath, installPath, isXml);
                    break;
                case ".bpe":
                    Install_BPE(xmlPath, installPath, isXml);
                    break;
                case ".bsa":
                    Install_BSA(xmlPath, installPath, isXml);
                    break;
                case ".cnc":
                    Install_CNC(xmlPath, installPath, isXml);
                    break;
                case ".cns":
                    Install_CNS(xmlPath, installPath, isXml);
                    break;
                case ".cso":
                    Install_CSO(xmlPath, installPath, isXml);
                    break;
                case ".ean":
                    Install_EAN(xmlPath, installPath, isXml);
                    break;
                case ".msg":
                    Install_MSG(xmlPath, installPath, isXml);
                    break;
                case ".psc":
                    Install_PSC(xmlPath, installPath, isXml);
                    break;
                case ".aur":
                    Install_AUR(xmlPath, installPath, isXml);
                    break;
                case ".pup":
                    Install_PUP(xmlPath, installPath, isXml);
                    break;
                case ".tsd":
                    Install_TSD(xmlPath, installPath, isXml);
                    break;
                case ".tnl":
                    Install_TNL(xmlPath, installPath, isXml);
                    break;
                case ".emb":
                    Install_EMB(xmlPath, installPath, isXml);
                    break;
                case ".qxd":
                    Install_QXD(xmlPath, installPath, isXml);
                    break;
                case ".pal":
                    Install_PAL(xmlPath, installPath, isXml);
                    break;
                case ".ttb":
                    Install_TTB(xmlPath, installPath, isXml);
                    break;
                case ".ttc":
                    Install_TTC(xmlPath, installPath, isXml);
                    break;
                case ".sev":
                    Install_SEV(xmlPath, installPath, isXml);
                    break;
                case ".hci":
                    Install_HCI(xmlPath, installPath, isXml);
                    break;
                case ".cml":
                    Install_CML(xmlPath, installPath, isXml);
                    break;
                case ".x2s":
                    Install_CharaSlots(xmlPath);
                    break;
                default:
                    throw new InvalidDataException(string.Format("The filetype of \"{0}\" is not supported.", xmlPath));
            }
        }
        
        private void JungleCheck()
        {
            if (zipManager.Exists(JUNGLE1 + "/"))
                useJungle1 = true;

            if (zipManager.Exists(JUNGLE2 + "/"))
                useJungle2 = true;
        }

        private void ProcessJungle(string jungleDir, bool allowOverwrite, string gameDirPath = null, bool isDirCopy = false)
        {
            foreach(var file in zipManager.archive.Entries)
            {
                if (file.FullName.StartsWith(jungleDir) && !string.IsNullOrEmpty(file.Name))
                {
                    if(file.FullName.Length > jungleDir.Length + 1)
                    {
                        //string filePath = file.FullName.Remove(0, jungleDir.Length + 1);
                        string filePath = Utils.SanitizePath(file.FullName);

                        if (isDirCopy)
                        {
                            string sanitizedDir = Utils.SanitizePath(jungleDir);
                            int rootCount = sanitizedDir.Split('/').Count();

                            for (int i = 0; i < rootCount; i++)
                                filePath = Utils.PathRemoveRoot(filePath);
                        }
                        else
                        {
                            //Just one root level for JUNGLE1/2
                            filePath = Utils.PathRemoveRoot(file.FullName);
                        }

                        if (!IsJungleFileBlacklisted(filePath))
                        {
                            fileManager.AddStreamFile((string.IsNullOrWhiteSpace(gameDirPath)) ? filePath : $"{gameDirPath}/{filePath}", file, allowOverwrite);
                        }
                    }
                }
            }
        }

        private void SaveFiles()
        {
#if !DEBUG
            try
#endif
            {
                UpdateProgessBarText("_Saving files...", false);
                startedSaving = true;

                fileManager.SaveParsedFiles();

                //Binary files. They are a "stream" from the zip file, not loaded into memory.
                //Save them last to decrease chance of errors, as these files cant be restored if anything happens
                fileManager.SaveStreamFiles();
            }
#if !DEBUG
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed at file save phase."), ex);
            }
#endif
        }
        
        /// <summary>
        /// Returns true if file is blacklisted.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsJungleFileBlacklisted(string path)
        {
            if(GeneralInfo.JungleBlacklist.Contains(Path.GetFileName(path)))
            {
                //is blacklisted
                MessageBox.Show(string.Format("\"{0}\" is blacklisted and cannot be used in JUNGLE1, JUNGLE2 or as a binary copy. \n\nFile skipped.", Path.GetFileName(path)), "Blacklisted File", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
            else
            {
                return false;
            }
        }

        //File Install Methods
        private void Install_IDB(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                IDB_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<IDB_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : IDB_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                IDB_File binaryFile = (IDB_File)GetParsedFile<IDB_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Entries, binaryFile.Entries, installPath);

                //MsgComponent Code
                foreach(var idbEntry in xmlFile.Entries)
                {
                    if (idbEntry.MsgComponents != null)
                        IdbMsgWriter(idbEntry, installPath);
                }

                //Install entries
                InstallEntries(xmlFile.Entries, binaryFile.Entries, installPath, Sections.IDB_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at IDB install phase ({0}).",  xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_CUS(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                CUS_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<CUS_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : CUS_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                CUS_File binaryFile = (CUS_File)GetParsedFile<CUS_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Skillsets, binaryFile.Skillsets, installPath);
                bindingManager.ParseProperties(xmlFile.SuperSkills, binaryFile.SuperSkills, installPath);
                bindingManager.ParseProperties(xmlFile.UltimateSkills, binaryFile.UltimateSkills, installPath);
                bindingManager.ParseProperties(xmlFile.EvasiveSkills, binaryFile.EvasiveSkills, installPath);
                bindingManager.ParseProperties(xmlFile.BlastSkills, binaryFile.BlastSkills, installPath);
                bindingManager.ParseProperties(xmlFile.AwokenSkills, binaryFile.AwokenSkills, installPath);

                //Install entries
                InstallEntries(xmlFile.Skillsets, binaryFile.Skillsets, installPath, Sections.CUS_Skillsets);
                InstallEntries(xmlFile.SuperSkills, binaryFile.SuperSkills, installPath, Sections.CUS_SuperSkills);
                InstallEntries(xmlFile.UltimateSkills, binaryFile.UltimateSkills, installPath, Sections.CUS_UltimateSkills);
                InstallEntries(xmlFile.EvasiveSkills, binaryFile.EvasiveSkills, installPath, Sections.CUS_EvasiveSkills);
                InstallEntries(xmlFile.BlastSkills, binaryFile.BlastSkills, installPath, Sections.CUS_BlastSkills);
                InstallEntries(xmlFile.AwokenSkills, binaryFile.AwokenSkills, installPath, Sections.CUS_AwokenSkills);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at CUS install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_BCS(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                BCS_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<BCS_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : BCS_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                BCS_File binaryFile = (BCS_File)GetParsedFile<BCS_File>(installPath);

                if (binaryFile == null)
                {
                    //No matching file exists in the game, so use xml as base
                    binaryFile = zipManager.DeserializeXmlFromArchive<BCS_File>(GeneralInfo.GetPathInZipDataDir(xmlPath));
                    fileManager.AddParsedFile(installPath, binaryFile);
                }

                //Init if needed
                if (binaryFile.PartSets == null && xmlFile.Part_Colors != null)
                    binaryFile.PartSets = new List<PartSet>();
                if (binaryFile.Part_Colors == null && xmlFile.Part_Colors != null)
                    binaryFile.Part_Colors = new List<PartColor>();
                if (binaryFile.Bodies == null && xmlFile.Bodies != null)
                    binaryFile.Bodies = new List<Body>();

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.PartSets, binaryFile.PartSets, installPath);
                bindingManager.ParseProperties(xmlFile.Bodies, binaryFile.Bodies, installPath);

                //Install entries
                InstallEntries(xmlFile.PartSets, binaryFile.PartSets, installPath, Sections.BCS_PartSets);
                InstallEntries(xmlFile.Bodies, binaryFile.Bodies, installPath, Sections.BCS_Bodies);

                //Install PartColors
                if(xmlFile.Part_Colors != null)
                {
                    for (int i = 0; i < xmlFile.Part_Colors.Count; i++)
                    {
                        PartColor binPartColor = binaryFile.GetPartColors(xmlFile.Part_Colors[i].Index, xmlFile.Part_Colors[i].Str_00);
                        bindingManager.ParseProperties(xmlFile.Part_Colors[i]._Colors, binPartColor._Colors, installPath);

                        if(xmlFile.Part_Colors[i]._Colors != null)
                        {
                            foreach (var color in xmlFile.Part_Colors[i]._Colors)
                            {
                                GeneralInfo.Tracker.AddID(installPath, Sections.GetBcsPartColor(xmlFile.Part_Colors[i].Index), color.Index);
                                binPartColor.AddColor(color);
                            }
                        }
                    }
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at BCS install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_CMS(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                CMS_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<CMS_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : CMS_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                CMS_File binaryFile = (CMS_File)GetParsedFile<CMS_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.CMS_Entries, binaryFile.CMS_Entries, installPath);

                //MsgComponent Code
                foreach (var msgEntry in xmlFile.CMS_Entries)
                {
                    if (msgEntry.MsgComponents != null)
                        CmsMsgWriter(msgEntry, installPath);
                }

                //Install entries
                InstallEntries(xmlFile.CMS_Entries, binaryFile.CMS_Entries, installPath, Sections.CMS_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at CMS install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_BAC(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                if (!isXml)
                    throw new Exception("Type.Binary not possible for bac files. You must use XML.");

                BAC_File xmlFile = zipManager.DeserializeXmlFromArchive<BAC_File>(GeneralInfo.GetPathInZipDataDir(xmlPath));
                BAC_File binaryFile = (BAC_File)GetParsedFile<BAC_File>(installPath);

                if(binaryFile == null)
                {
                    //No matching file exists in the game, so use xml as base
                    binaryFile = zipManager.DeserializeXmlFromArchive<BAC_File>(GeneralInfo.GetPathInZipDataDir(xmlPath));
                    fileManager.AddParsedFile(installPath, binaryFile);
                }

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.BacEntries, binaryFile.BacEntries, installPath);

                //Install entries
                InstallEntries(xmlFile.BacEntries, binaryFile.BacEntries, installPath, Sections.BAC_Entries);

            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at BAC install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_PSC(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                PSC_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<PSC_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : PSC_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                PSC_File binaryFile = (PSC_File)GetParsedFile<PSC_File>(installPath);

                foreach(var config in xmlFile.Configurations)
                {
                    var binaryConfig = binaryFile.GetConfiguration(config.Index);

                    foreach(var pscEntry in config.PscEntries)
                    {
                        var binaryPscConfig = binaryConfig.GetPscEntry(pscEntry.Index);
                        bindingManager.ParseProperties(pscEntry.PscSpecEntries, binaryPscConfig.PscSpecEntries, installPath);
                        InstallEntries(pscEntry.PscSpecEntries, binaryPscConfig.PscSpecEntries, installPath, Sections.GetPscEntry(pscEntry.Index));
                    }
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at PSC install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_EEPK(string xmlPath, string installPath)
        {
#if !DEBUG
            try
#endif
            {
                //Allows the eepk installer to reuse textures and assets by matching names.
                EepkToolInterlop.TextureImportMatchNames = true;
                EepkToolInterlop.AssetReuseMatchName = true;

                //Load files
                EffectContainerFile installFile;

                using (Stream stream = zipManager.GetZipEntry(GeneralInfo.GetPathInZipDataDir(xmlPath)).Open())
                {
                    installFile = EffectContainerFile.LoadVfx2(stream, xmlPath);
                }

                EffectContainerFile binaryFile = (EffectContainerFile)GetParsedFile<EffectContainerFile>(installPath);

                //Crash fix for when too many auras are installed.
                binaryFile.Pbind.I_08 = 0x9C40;
                binaryFile.Pbind.I_12 = 0x9C40;

                //Add effect IDs to tracker
                foreach (var effect in installFile.Effects)
                {
                    GeneralInfo.Tracker.AddID(installPath, Sections.EEPK_Effect, effect.Index);
                }

                //Install effects
                binaryFile.InstallEffects(installFile.Effects);


            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at EEPK install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_EMB(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                EMB_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<EMB_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : EMB_File.LoadEmb(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                EMB_File binaryFile = (EMB_File)GetParsedFile<EMB_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Entry, binaryFile.Entry, installPath);

                //Install entries
                if (xmlFile.Entry != null)
                {
                    if (xmlFile.installMode == InstallMode.MatchName && !binaryFile.UseFileNames)
                        throw new Exception("InstallMode.NameMatch not possible when UseFileNames is false.");

                    foreach (var entry in xmlFile.Entry)
                    {
                        int idx = binaryFile.AddEntry(entry, entry.Index, xmlFile.installMode);

                        if(xmlFile.installMode == InstallMode.MatchIndex)
                            GeneralInfo.Tracker.AddID(installPath, Sections.EMB_Entry, idx.ToString());
                        else if (xmlFile.installMode == InstallMode.MatchName)
                            GeneralInfo.Tracker.AddID(installPath, Sections.EMB_Entry, entry.Name);
                    }
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at EMB install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_ACB(string xmlPath)
        {
#if !DEBUG
            try
#endif
            {
                new AcbInstaller(this, xmlPath);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at ACB install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_CharaSlots(string xmlPath)
        {
#if !DEBUG
            try
#endif
            {
                CharaSlotsFile xmlFile = zipManager.DeserializeXmlFromArchive<CharaSlotsFile>(GeneralInfo.GetPathInZipDataDir(xmlPath));
                CharaSlotsFile slotsFile = (CharaSlotsFile)GetParsedFile<CharaSlotsFile>(CharaSlotsFile.FILE_NAME_BIN, false, false);

                if(slotsFile == null)
                {
                    throw new FileNotFoundException($"Could not find {CharaSlotsFile.FILE_NAME_BIN}. This file must exist before install - to create it simply run xv2ins.exe once (the X2M installer).");
                }

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.CharaSlots, null, xmlPath);

                List<string> installIDs = new List<string>();
                bool success = slotsFile.InstallEntries(xmlFile.CharaSlots, installIDs);

                if (!success)
                {
                    throw new Exception("Attempted to install a slot that already existed.");
                }

                foreach(var id in installIDs)
                {
                    GeneralInfo.Tracker.AddID(CharaSlotsFile.FILE_NAME_BIN, Sections.CharaSlotEntry, id);
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at CharaSlots install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        //Copy-paste generic install methods
        private void Install_BDM(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                BDM_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<BDM_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : BDM_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                BDM_File binaryFile = (BDM_File)GetParsedFile<BDM_File>(installPath);

                if (binaryFile == null)
                {
                    //No matching file exists in the game, so use xml as base
                    binaryFile = zipManager.DeserializeXmlFromArchive<BDM_File>(GeneralInfo.GetPathInZipDataDir(xmlPath));
                    fileManager.AddParsedFile(installPath, binaryFile);
                }

                //Convrt type (if its not type xv1_0, then convert)
                xmlFile.ConvertToXv2();
                binaryFile.ConvertToXv2();

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.BDM_Entries, binaryFile.BDM_Entries, installPath);

                //Install entries
                InstallEntries(xmlFile.BDM_Entries, binaryFile.BDM_Entries, installPath, Sections.BDM_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at BDM install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_BEV(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                BEV_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<BEV_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : BEV_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                BEV_File binaryFile = (BEV_File)GetParsedFile<BEV_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Entries, binaryFile.Entries, installPath);

                //Install entries
                InstallEntries(xmlFile.Entries, binaryFile.Entries, installPath, Sections.BEV_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at BEV install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_BPE(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                BPE_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<BPE_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : BPE_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                BPE_File binaryFile = (BPE_File)GetParsedFile<BPE_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Entries, binaryFile.Entries, installPath);

                //Install entries
                InstallEntries(xmlFile.Entries, binaryFile.Entries, installPath, Sections.BPE_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at BPE install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_BSA(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                BSA_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<BSA_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : BSA_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                BSA_File binaryFile = (BSA_File)GetParsedFile<BSA_File>(installPath);

                if (binaryFile == null)
                {
                    //No matching file exists in the game, so use xml as base
                    binaryFile = zipManager.DeserializeXmlFromArchive<BSA_File>(GeneralInfo.GetPathInZipDataDir(xmlPath));
                    fileManager.AddParsedFile(installPath, binaryFile);
                }

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.BSA_Entries, binaryFile.BSA_Entries, installPath);

                //Install entries
                InstallEntries(xmlFile.BSA_Entries, binaryFile.BSA_Entries, installPath, Sections.BSA_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at BSA install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_CNC(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                CNC_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<CNC_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : CNC_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                CNC_File binaryFile = (CNC_File)GetParsedFile<CNC_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.CncEntries, binaryFile.CncEntries, installPath);

                //Install entries
                InstallEntries(xmlFile.CncEntries, binaryFile.CncEntries, installPath, Sections.CNC_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at CNC install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_CNS(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                CNS_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<CNS_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : CNS_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                CNS_File binaryFile = (CNS_File)GetParsedFile<CNS_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.CnsEntries, binaryFile.CnsEntries, installPath);

                //Install entries
                InstallEntries(xmlFile.CnsEntries, binaryFile.CnsEntries, installPath, Sections.CNS_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at CNS install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_CSO(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                CSO_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<CSO_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : CSO_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                CSO_File binaryFile = (CSO_File)GetParsedFile<CSO_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.CsoEntries, binaryFile.CsoEntries, installPath);

                //Install entries
                InstallEntries(xmlFile.CsoEntries, binaryFile.CsoEntries, installPath, Sections.CSO_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at CSO install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_EAN(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                EAN_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<EAN_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : EAN_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                EAN_File binaryFile = (EAN_File)GetParsedFile<EAN_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Animations, binaryFile.Animations, installPath);

                //Install entries
                InstallEntries(xmlFile.Animations, binaryFile.Animations, installPath, Sections.EAN_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at EAN install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_MSG(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                MSG_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<MSG_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : MSG_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                MSG_File binaryFile = (MSG_File)GetParsedFile<MSG_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.MSG_Entries, binaryFile.MSG_Entries, installPath);

                //Install entries
                InstallEntries(xmlFile.MSG_Entries, binaryFile.MSG_Entries, installPath, Sections.MSG_Entries);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at MSG install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_AUR(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                AUR_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<AUR_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : AUR_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                AUR_File binaryFile = (AUR_File)GetParsedFile<AUR_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Auras, binaryFile.Auras, installPath);
                bindingManager.ParseProperties(xmlFile.CharacterAuras, binaryFile.CharacterAuras, installPath);

                //Install entries
                InstallEntries(xmlFile.Auras, binaryFile.Auras, installPath, Sections.AUR_Aura);
                InstallEntries(xmlFile.CharacterAuras, binaryFile.CharacterAuras, installPath, Sections.AUR_Chara);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at AUR install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_PUP(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                PUP_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<PUP_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : PUP_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                PUP_File binaryFile = (PUP_File)GetParsedFile<PUP_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.PupEntries, binaryFile.PupEntries, installPath);

                //Install entries
                InstallEntries(xmlFile.PupEntries, binaryFile.PupEntries, installPath, Sections.PUP_Entry);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at PUP install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_TSD(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                TSD_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<TSD_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : TSD_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                TSD_File binaryFile = (TSD_File)GetParsedFile<TSD_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Globals, binaryFile.Globals, installPath);
                bindingManager.ParseProperties(xmlFile.Constants, binaryFile.Constants, installPath);
                bindingManager.ParseProperties(xmlFile.Events, binaryFile.Events, installPath);
                bindingManager.ParseProperties(xmlFile.Zones, binaryFile.Zones, installPath);
                bindingManager.ParseProperties(xmlFile.Triggers, binaryFile.Triggers, installPath);

                //Install entries
                InstallEntries(xmlFile.Triggers, binaryFile.Triggers, installPath, Sections.TSD_Trigger);
                InstallEntries(xmlFile.Globals, binaryFile.Globals, installPath, Sections.TSD_Global);
                InstallEntries(xmlFile.Constants, binaryFile.Constants, installPath, Sections.TSD_Constant);
                InstallEntries(xmlFile.Events, binaryFile.Events, installPath, Sections.TSD_Event);
                InstallEntries(xmlFile.Zones, binaryFile.Zones, installPath, Sections.TSD_Zone);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at TSD install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_TNL(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                TNL_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<TNL_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : TNL_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                TNL_File binaryFile = (TNL_File)GetParsedFile<TNL_File>(installPath);

                //Character and Masters share the same IDs, so we must merge the Index list and pass it to ParseProperties, rather than have it check the lists directly.
                List<string> usedIds = xmlFile.GetAllUsedIDs();
                usedIds.AddRange(binaryFile.GetAllUsedIDs());

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Actions, binaryFile.Actions, installPath);
                bindingManager.ParseProperties(xmlFile.Characters, binaryFile.Characters, installPath, usedIds);
                bindingManager.ParseProperties(xmlFile.Teachers, binaryFile.Teachers, installPath, usedIds);
                bindingManager.ParseProperties(xmlFile.Objects, binaryFile.Objects, installPath, usedIds);

                //Install entries
                InstallEntries(xmlFile.Characters, binaryFile.Characters, installPath, Sections.TNL_Character);
                InstallEntries(xmlFile.Teachers, binaryFile.Teachers, installPath, Sections.TNL_Teacher);
                InstallEntries(xmlFile.Objects, binaryFile.Objects, installPath, Sections.TNL_Object);
                InstallEntries(xmlFile.Actions, binaryFile.Actions, installPath, Sections.TNL_Action);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at TNL install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_QXD(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                QXD_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<QXD_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : QXD_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                QXD_File binaryFile = (QXD_File)GetParsedFile<QXD_File>(installPath);

                //Init if needed
                if (binaryFile.Collections == null)
                    binaryFile.Collections = new List<QXD_CollectionEntry>();
                if (binaryFile.Characters1 == null)
                    binaryFile.Characters1 = new List<Quest_Characters>();
                if (binaryFile.Characters2 == null)
                    binaryFile.Characters2 = new List<Quest_Characters>();

                //Used id list for collections/characters
                List<string> usedColIds = xmlFile.GetAllUsedCollectionIds();
                usedColIds.AddRange(binaryFile.GetAllUsedCollectionIds());

                List<string> usedCharacterIds = xmlFile.GetAllUsedCharacterIds();
                usedCharacterIds.AddRange(binaryFile.GetAllUsedCharacterIds());

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.Collections, binaryFile.Collections, installPath, usedColIds);
                bindingManager.ParseProperties(xmlFile.Quests, binaryFile.Quests, installPath);
                bindingManager.ParseProperties(xmlFile.Characters1, binaryFile.Characters1, installPath, usedCharacterIds);
                bindingManager.ParseProperties(xmlFile.Characters2, binaryFile.Characters2, installPath, usedCharacterIds);

                //Install entries
                InstallEntries(xmlFile.Quests, binaryFile.Quests, installPath, Sections.QXD_Quest);
                InstallEntries(xmlFile.Characters1, binaryFile.Characters1, installPath, Sections.QXD_Character1);
                InstallEntries(xmlFile.Characters2, binaryFile.Characters2, installPath, Sections.QXD_Character2);
                InstallEntries(xmlFile.Collections, binaryFile.Collections, installPath, Sections.QXD_Collection);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at QXD install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_PAL(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                PAL_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<PAL_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : PAL_File.Parse(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                PAL_File binaryFile = (PAL_File)GetParsedFile<PAL_File>(installPath);

                //Parse bindings
                bindingManager.ParseProperties(xmlFile.PalEntries, binaryFile.PalEntries, installPath);

                //Install entries
                InstallEntries(xmlFile.PalEntries, binaryFile.PalEntries, installPath, Sections.PAL_Entry);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at PAL install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_TTB(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                TTB_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<TTB_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : TTB_File.Parse(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                TTB_File binaryFile = (TTB_File)GetParsedFile<TTB_File>(installPath);

                //Install entries
                InstallSubEntries<TTB_Event, TTB_Entry>(xmlFile.Entries, binaryFile.Entries, installPath, Sections.TTB_Entry);

            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at TTB install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_TTC(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                TTC_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<TTC_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : TTC_File.Parse(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                TTC_File binaryFile = (TTC_File)GetParsedFile<TTC_File>(installPath);

                //Install entries
                InstallEntries(xmlFile.Entries, binaryFile.Entries, installPath, Sections.TTC_Entry);
            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at TTC install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_SEV(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                SEV_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<SEV_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : SEV_File.Parse(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                SEV_File binaryFile = (SEV_File)GetParsedFile<SEV_File>(installPath);

                //Install entries
                InstallSubEntries<SEV_CharEvent, SEV_Entry>(xmlFile.Entries, binaryFile.Entries, installPath, Sections.SEV_Entry);

            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at SEV install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_HCI(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                HCI_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<HCI_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : HCI_File.Parse(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                HCI_File binaryFile = (HCI_File)GetParsedFile<HCI_File>(installPath);

                //Install entries
                InstallEntries<HCI_Entry>(xmlFile.Entries, binaryFile.Entries, installPath, Sections.HCI_Entry);

            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at HCI install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_CML(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                CML_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<CML_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : CML_File.Parse(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                CML_File binaryFile = (CML_File)GetParsedFile<CML_File>(installPath);

                //Install entries
                InstallEntries(xmlFile.Entries, binaryFile.Entries, installPath, Sections.CML_Entry);

            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at CML install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }

        private void Install_ERS(string xmlPath, string installPath, bool isXml)
        {
#if !DEBUG
            try
#endif
            {
                ERS_File xmlFile = (isXml) ? zipManager.DeserializeXmlFromArchive<ERS_File>(GeneralInfo.GetPathInZipDataDir(xmlPath)) : ERS_File.Load(zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(xmlPath)));
                ERS_File binaryFile = (ERS_File)GetParsedFile<ERS_File>(installPath);

                InstallSubEntries<ERS_MainTableEntry, ERS_MainTable>(xmlFile.Entries, binaryFile.Entries, installPath, Sections.ERS_Entries);

            }
#if !DEBUG
            catch (Exception ex)
            {
                string error = string.Format("Failed at ERS install phase ({0}).", xmlPath);
                throw new Exception(error, ex);
            }
#endif
        }


        //Generic Install Methods
        //We need generic methods for IInstallable via List and ObservableCollection. Most file types will be handled with this.
        //Specific methods for IDB and CMS, so we can handle the MsgComponents. (Or do we handle this first, then pass it to the IInstallable method?)
        //Specific methods for BAC and EMB, so we can handle the absolute indexes. (or make another interface?)

        /// <summary>
        /// Generic IInstallable method for IList<T>
        /// </summary>
        /// <param name="installEntries">The entries to install.</param>
        /// <param name="destEntries">The entries to install into.</param>
        private void InstallEntries<T>(IList<T> installEntries, IList<T> destEntries, string path, string section) where T : IInstallable
        {
            if (installEntries == null) return;
            if (destEntries == null) throw new InvalidOperationException(string.Format("InstallEntries: destEntries was null. Cannot install entries. ({0})", path));

            foreach (var entryToInstall in installEntries)
            {
                //Add entry to tracker
                GeneralInfo.Tracker.AddID(path, section, entryToInstall.Index);

                //Find index
                int index = destEntries.IndexOf(destEntries.FirstOrDefault(x => x.Index == entryToInstall.Index));

                //Install entry
                if (index != -1)
                {
                    //Entry with same index exists, so overwrite it.
                    destEntries[index] = entryToInstall;
                }
                else
                {
                    //Add entry
                    destEntries.Add(entryToInstall);
                }
            }
        }

        private void InstallSubEntries<T, M>(IList<M> installEntries, IList<M> destEntries, string path, string section) where T : IInstallable, new() where M : IInstallable_2<T>, IInstallable, new()
        {
            if (installEntries == null) return;
            if (destEntries == null) throw new InvalidOperationException(string.Format("InstallSubEntries: destEntries was null. Cannot install entries. ({0})", path));

            //Initial binding pass (This wont parse the AutoID bindings on SubEntries, as we need to resolve the root ID first)
            bindingManager.ParseProperties(installEntries, destEntries, path);

            foreach (var entryToInstall in installEntries) 
            {
                //Get index
                int index = destEntries.IndexOf(destEntries.FirstOrDefault(x => x.Index == entryToInstall.Index));

                //Create root entry if required
                if(index == -1)
                {
                    destEntries.Add(new M() { Index = entryToInstall.Index, SubEntries = new List<T>() });
                    index = destEntries.Count - 1;
                }

                //Second binding pass
                bindingManager.ParseProperties(entryToInstall.SubEntries, destEntries[index].SubEntries, path);

                //Install entries
                if (entryToInstall.SubEntries == null) continue;

                if (destEntries[index].SubEntries == null)
                    destEntries[index].SubEntries = new List<T>();

                InstallEntries<T>(entryToInstall.SubEntries, destEntries[index].SubEntries, path, $"{section}/{entryToInstall.Index}");
            }
        }

        //MSG Component Writers
        private IDB_Entry IdbMsgWriter(IDB_Entry IdbEntry, string filePath)
        {
            //Save limit burst msg index for use when writing the msg entry to btlhud
            int limitBurstMsgIndex = -1;

            if (IdbEntry.MsgComponents != null)
            {
                foreach (var msgComponent in IdbEntry.MsgComponents)
                {
                    if (msgComponent.MsgType == Msg_Component.MsgComponentType.Name)
                    {
                        string nameMsgPath = String.Format("msg/{0}", IDB_File.NameMsgFile(Path.GetFileName(filePath)));
                        IdbEntry.I_04 = (ushort)msgComponentInstall.WriteMsgEntries(msgComponent, nameMsgPath, MsgComponentInstall.Mode.IDB);
                    }
                    else if (msgComponent.MsgType == Msg_Component.MsgComponentType.Info)
                    {
                        string infoMsgPath = String.Format("msg/{0}", IDB_File.InfoMsgFile(Path.GetFileName(filePath)));
                        IdbEntry.I_06 = (ushort)msgComponentInstall.WriteMsgEntries(msgComponent, infoMsgPath, MsgComponentInstall.Mode.IDB);
                    }
                    else if (msgComponent.MsgType == Msg_Component.MsgComponentType.LimitBurst)
                    {
                        string lbMsgPath = String.Format("msg/{0}", IDB_File.LimitBurstMsgFile(Path.GetFileName(filePath)));
                        IdbEntry.I_40 = (ushort)msgComponentInstall.WriteMsgEntries(msgComponent, lbMsgPath, MsgComponentInstall.Mode.IDB);
                        limitBurstMsgIndex = IdbEntry.I_40;
                    }
                    else if (msgComponent.MsgType == Msg_Component.MsgComponentType.LimitBurstBattle)
                    {
                        //Do nothing right now. We will deal with this one last.
                    }
                    else
                    {
                        throw new Exception(String.Format("Unrecognized MsgComponent Type for IDB: {0}", msgComponent.MsgType));
                    }
                }

                //Now check for the LimitBurstBattle component, since that needs to be installed last
                foreach (var msgComponent in IdbEntry.MsgComponents)
                {
                    if (msgComponent.MsgType == Msg_Component.MsgComponentType.LimitBurstBattle)
                    {
                        if (limitBurstMsgIndex != -1)
                        {
                            string lbMsgPath = String.Format("msg/{0}", IDB_File.LimitBurstHudMsgFile(Path.GetFileName(filePath)));
                            msgComponentInstall.WriteMsgEntries(msgComponent, lbMsgPath, MsgComponentInstall.Mode.IDB_LB_HUD, limitBurstMsgIndex.ToString());
                        }
                        else
                        {
                            throw new Exception(String.Format("MsgComponent Type LimitBurstBattle cannot be used without a LimitBurst MsgComponent as well!"));
                        }
                    }
                }

            }

            return IdbEntry;
        }

        private CMS_Entry CmsMsgWriter(CMS_Entry CmsEntry, string filePath)
        {
            if (CmsEntry.MsgComponents != null)
            {
                foreach (var msgComponent in CmsEntry.MsgComponents)
                {
                    if (msgComponent.MsgType == Msg_Component.MsgComponentType.Name)
                    {
                        string nameMsgPath = "msg/proper_noun_character_name_";
                        msgComponentInstall.WriteMsgEntries(msgComponent, nameMsgPath, MsgComponentInstall.Mode.CMS, CmsEntry.Str_04);
                    }
                    else
                    {
                        throw new Exception(String.Format("Unrecognized MsgComponent Type for CMS: {0}", msgComponent.MsgType));
                    }
                }
            }

            return CmsEntry;
        }



        //File Handling. Methods for getting files from the game and parsing them.
        /// <summary>
        /// Gets a parsed file from the game and caches it. If it exists in the cache already then that is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public object GetParsedFile<T>(string path, bool fromCpk = false, bool raiseEx = true) where T : new()
        {
            if (fromCpk)
            {
                return GetParsedFileFromGame(path, FileIO, fromCpk, raiseEx);
            }

            var cachedFile = fileManager.GetParsedFile<T>(path);

            if(cachedFile != null)
            {
                //File is already cached. Return that instance.
                return cachedFile;
            }
            else
            {
                //File is not cached. So parse it, add it and then return it.
                var file = GetParsedFileFromGame(path, FileIO, false, raiseEx);
                if(file != null)
                    fileManager.AddParsedFile(path, file);
                return file;
            }
        }
        

        public static object GetParsedFileFromGame(string path, Xv2FileIO fileIO, bool onlyFromCpk, bool raiseEx = true)
        {
            if (onlyFromCpk)
            {
                if (!fileIO.FileExistsInCpk(path) && !raiseEx)
                {
                    return null;
                }
                //if (fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk) == null && !raiseEx)
                //    return null;
            }
            else if (!fileIO.FileExists(path) && !raiseEx)
            {
                return null;
            }

            //Special case: slots file
            if(path == CharaSlotsFile.FILE_NAME_BIN)
            {
                return CharaSlotsFile.Load(fileIO.GetFileFromGame(path, false, false));
            }

            switch (Path.GetExtension(path))
            {
                case ".bac":
                    return BAC_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".bcs":
                    return BCS_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".bdm":
                    return BDM_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk), true);
                case ".bev":
                    return BEV_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".bpe":
                    return BPE_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".bsa":
                    return BSA_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".cms":
                    return CMS_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".cnc":
                    return CNC_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".cns":
                    return CNS_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".cso":
                    return CSO_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".cus":
                    return CUS_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".ean":
                    return EAN_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".ers":
                    return ERS_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".idb":
                    return IDB_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".msg":
                    return MSG_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".psc":
                    return PSC_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".pup":
                    return PUP_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".aur":
                    return AUR_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".tsd":
                    return TSD_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".tnl":
                    return TNL_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".emb":
                    return EMB_File.LoadEmb(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".qxd":
                    return QXD_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".eepk":
                    return EffectContainerFile.Load(path, fileIO, onlyFromCpk);
                case ".obl":
                    return OBL_File.Parse(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".pal":
                    return PAL_File.Parse(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".acb":
                    return ACB_File.Load(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk), fileIO.GetFileFromGame(string.Format("{0}/{1}.awb", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)), false, onlyFromCpk));
                case ".ttb":
                    return TTB_File.Parse(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".ttc":
                    return TTC_File.Parse(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".sev":
                    return SEV_File.Parse(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".hci":
                    return HCI_File.Parse(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                case ".cml":
                    return CML_File.Parse(fileIO.GetFileFromGame(path, raiseEx, onlyFromCpk));
                default:
                    throw new InvalidDataException(String.Format("GetParsedFileFromGame: The filetype of \"{0}\" is not supported.", path));
            }
        }

        public static byte[] GetBytesFromParsedFile(string path, object data)
        {
            switch (Path.GetExtension(path))
            {
                case ".bac":
                    return ((BAC_File)data).SaveToBytes();
                case ".bcs":
                    return ((BCS_File)data).SaveToBytes();
                case ".bdm":
                    return ((BDM_File)data).SaveToBytes();
                case ".bev":
                    return ((BEV_File)data).SaveToBytes();
                case ".bpe":
                    return ((BPE_File)data).SaveToBytes();
                case ".bsa":
                    return ((BSA_File)data).SaveToBytes();
                case ".cms":
                    return ((CMS_File)data).SaveToBytes();
                case ".cnc":
                    return ((CNC_File)data).SaveToBytes();
                case ".cns":
                    return ((CNS_File)data).SaveToBytes();
                case ".cso":
                    return ((CSO_File)data).SaveToBytes();
                case ".cus":
                    return ((CUS_File)data).SaveToBytes();
                case ".ean":
                    return ((EAN_File)data).SaveToBytes();
                case ".ers":
                    return ((ERS_File)data).SaveToBytes();
                case ".idb":
                    return ((IDB_File)data).SaveToBytes();
                case ".msg":
                    return ((MSG_File)data).SaveToBytes();
                case ".psc":
                    return ((PSC_File)data).SaveToBytes();
                case ".aur":
                    return ((AUR_File)data).SaveToBytes();
                case ".pup":
                    return ((PUP_File)data).SaveToBytes();
                case ".tsd":
                    return ((TSD_File)data).SaveToBytes();
                case ".tnl":
                    return ((TNL_File)data).SaveToBytes();
                case ".emb":
                    return ((EMB_File)data).SaveToBytes();
                case ".qxd":
                    return ((QXD_File)data).SaveToBytes();
                case ".obl":
                    return ((OBL_File)data).SaveToBytes();
                case ".pal":
                    return ((PAL_File)data).SaveToBytes();
                case ".ttb":
                    return ((TTB_File)data).SaveToBytes();
                case ".ttc":
                    return ((TTC_File)data).SaveToBytes();
                case ".sev":
                    return ((SEV_File)data).SaveToBytes();
                case ".hci":
                    return ((HCI_File)data).SaveToBytes();
                case ".cml":
                    return ((CML_File)data).SaveToBytes();
                case ".x2s":
                    return ((CharaSlotsFile)data).SaveToBytes();
                default:
                    throw new InvalidDataException(String.Format("GetBytesFromParsedFile: The filetype of \"{0}\" is not supported.", path));
            }
        }


        //UI
        private void SetProgressBarSteps()
        {
            Parent.Dispatcher.Invoke((Action)(() =>
            {
                Parent.ProgressBar_Main.Maximum = Files.Count + ((useJungle1) ? 1 : 0) + ((useJungle2) ? 1 : 0);
                Parent.ProgressBar_Main.Value = 0;
            }));
        }
        
        private void UpdateProgessBarText(string text, bool advanceProgress = true)
        {
            Parent.Dispatcher.BeginInvoke((Action)(() =>
            {
                if(advanceProgress)
                    Parent.ProgressBar_Main.Value++;

                Parent.ProgressBar_Label.Content = text;
            }));
        }

    }

}
