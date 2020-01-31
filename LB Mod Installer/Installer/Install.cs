using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using Xv2CoreLib;
using Xv2CoreLib.MSG;
using Xv2CoreLib.IDB;
using Xv2CoreLib.CMS;
using Xv2CoreLib.CPK;
using Xv2CoreLib.BAC;


namespace LB_Mod_Installer.Installer
{
    public class Install : EepkExporter.EepkInstallReceive
    {
        public int StatusCode { get; private set; }
        public string Error_FileName { get; private set; }
        public string Error_Message { get; private set; }
        // 0 = Install started, but never finished and no errors occured (this state should never happen)
        // 1 = Install was successful (no errors)
        // 2 = InstallStep xml error: The InstallStep Type is == Options, but no Option List was found.
        // 3 = File Error: Cannot find the specified file (Additional Info)
        // 4 = InstallStep xml error: XmlPaths and DataFolderPaths not in sync.
        // 5 = CPK Initialization failed
        // 6 = Unspecified exception
        // 10 = CUS fail
        // 11 = CMS fail
        // 12 = BPE fail
        // 13 = BCS fail
        // 14 = IDB fail
        // 15 = EEPK fail
        // 16 = CSO fail
        // 17 = ERS fail
        // 18 = CNS fail
        // 19 = BSA fail
        // 20 = EAN fail
        // 21 = BEV fail
        // 22 = CNC fail
        // 23 = BDM fail
        // 24 = BAC fail
        // 25 = MSG fail
        // 50 = IDB MsgWrite failed
        // 200 = binding fail

        private InstallerXml Installer_Xml { get; set; }
        private List<int> SelectedOptions { get; set; }
        private string ExtractedPath { get; set; }
        private string BackupPath { get; set; }

        //Meta
        private TrackingXml Tracker { get; set; }
        private MsgComponentInstall msgComponentInstall { get; set; }

        //UI
        MainWindow parent { get; set; }

        //CPK
        CPK_Reader cpkReader { get; set; }

        //Binding Manager
        Binding.IdBindingManager bindingManager { get; set; }

        public Install(InstallerXml _installerXml, List<int> _SelectedOptions, string _ExtractedPath, MainWindow _parent, Xv2CoreLib.CPK.CPK_Reader _cpkReader)
        {
            //CultureInfo
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Installer_Xml = _installerXml;
            SelectedOptions = _SelectedOptions;
            ExtractedPath = _ExtractedPath;
            BackupPath = String.Format("{0}/backup", ExtractedPath);
            StatusCode = 0;
            parent = _parent;
            //Loading tracker xml
            Tracker = GetTrackerXml();
            GeneralInfo.Tracker = Tracker;
            //msgComponentInstall = new MsgComponentInstall(this);

            cpkReader = _cpkReader;

            //Binding Manager
            bindingManager = new Binding.IdBindingManager(new Xv2CoreLib.CUS.Parser(GetFilePath("system/custom_skill.cus"), false).GetCusFile(), new Xv2CoreLib.CMS.Parser(GetFilePath("system/char_model_spec.cms"), false).GetCmsFile());
        }

        public void Start()
        {
            Main();
        }

        private void SaveTrackerXml()
        {
            //Finalize the tracker first
            Tracker.CleanUsedIds();

            //Saving tracker xml
            YAXSerializer serializer = new YAXSerializer(typeof(TrackingXml));
            serializer.SerializeToFile(Tracker, String.Format("{0}/{1}.xml", GeneralInfo.GameDataFolder, GeneralInfo.TrackerName));
        }

        private TrackingXml GetTrackerXml()
        {
            if (File.Exists(String.Format("{0}/{1}.xml", GeneralInfo.GameDataFolder, GeneralInfo.TrackerName)))
            {
                try
                {
                    YAXSerializer serializer = new YAXSerializer(typeof(TrackingXml), YAXSerializationOptions.DontSerializeNullObjects);
                    return (TrackingXml)serializer.DeserializeFromFile(String.Format("{0}/{1}.xml", GeneralInfo.GameDataFolder, GeneralInfo.TrackerName));
                }
                catch
                {
                    return new TrackingXml()
                    {
                        Mods = new List<Mod>()
                    };
                }
            }
            else
            {
                return new TrackingXml()
                {
                    Mods = new List<Mod>()
                };
            }
        }

        private int GetTotalInstallSteps()
        {
            int total = 0;

            for (int i = 0; i < Installer_Xml.InstallOptionSteps.Count(); i++)
            {
                if (Installer_Xml.InstallOptionSteps[i].StepType ==  InstallStep.StepTypes.Options)
                {
                    if (Installer_Xml.InstallOptionSteps[i].OptionList != null)
                    {
                        if (Installer_Xml.InstallOptionSteps[i].OptionList.Count != 0)
                        {
                            for (int a = 0; a < Installer_Xml.InstallOptionSteps[i].OptionList[SelectedOptions[i]].PathCount; a++)
                            {
                                total++;
                            }
                        }
                    }


                }

            }

            if (Installer_Xml.InstallFiles != null)
            {
                total += Installer_Xml.InstallFiles.Count();
            }

            if (Installer_Xml.UseJungle1)
            {
                total++;
            }

            if (Installer_Xml.UseJungle2)
            {
                total++;
            }

            return total;
        }

        private void Main()
        {
            parent.Dispatcher.Invoke((Action)(() =>
            {
                parent.ProgressBar_Main.Maximum = GetTotalInstallSteps();
            }));
#if !DEBUG
            try
#endif
            {
                //InstallSteps
                for (int i = 0; i < Installer_Xml.InstallOptionSteps.Count(); i++)
                {
                    if (Installer_Xml.InstallOptionSteps[i].StepType == InstallStep.StepTypes.Options)
                    {
                        //Option Type InstallStep
                        if (Installer_Xml.InstallOptionSteps[i].OptionList == null)
                        {
                            StatusCode = 2;
                            return;
                        }

                        if (StatusCode != 0)
                        {
                            return;
                        }

                        for (int a = 0; a < Installer_Xml.InstallOptionSteps[i].OptionList[SelectedOptions[i]].PathCount; a++)
                        {
                            if (Path.GetExtension(Installer_Xml.InstallOptionSteps[i].OptionList[SelectedOptions[i]].Paths[a].XmlPath) == ".xml")
                            {
                                //Xml files
                                ResolveFileType(Installer_Xml.InstallOptionSteps[i].OptionList[SelectedOptions[i]].Paths[a].XmlPath, Installer_Xml.InstallOptionSteps[i].OptionList[SelectedOptions[i]].Paths[a].InstallPath);
                            }
                            else
                            {
                                //Binary files. Simply perform a copy of the file to the game directory.
                                CopyFileToGameDir(Installer_Xml.InstallOptionSteps[i].OptionList[SelectedOptions[i]].Paths[a].XmlPath, Installer_Xml.InstallOptionSteps[i].OptionList[SelectedOptions[i]].Paths[a].InstallPath, Installer_Xml.InstallOptionSteps[i].OptionList[SelectedOptions[i]].Paths[a].AllowOverwrite());
                            }

                            if (StatusCode != 0)
                            {
                                return;
                            }
                        }

                    }

                }

                //FileToInstall
                if (Installer_Xml.InstallFiles != null)
                {
                    for (int i = 0; i < Installer_Xml.InstallFiles.Count(); i++)
                    {
                        if (Path.GetExtension(Installer_Xml.InstallFiles[i].XmlPath) == ".xml")
                        {
                            ResolveFileType(Installer_Xml.InstallFiles[i].XmlPath, Installer_Xml.InstallFiles[i].InstallPath);
                        }
                        else
                        {
                            CopyFileToGameDir(Installer_Xml.InstallFiles[i].XmlPath, Installer_Xml.InstallFiles[i].InstallPath, Installer_Xml.InstallFiles[i].AllowOverwrite());

                        }

                        if (StatusCode != 0)
                        {
                            return;
                        }
                    }
                }

                //Jungle1
                if (Installer_Xml.UseJungle1 == true)
                {
                    if (Directory.Exists(String.Format("{0}/JUNGLE1", ExtractedPath)))
                    {
                        InstallJungle(String.Format("{0}/JUNGLE1", ExtractedPath), true);
                    }
                    else
                    {
                        MessageBox.Show("JUNGLE1 not found.", GeneralInfo.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                //Jungle2
                if (Installer_Xml.UseJungle2 == true)
                {
                    if (Directory.Exists(String.Format("{0}/JUNGLE2", ExtractedPath)))
                    {
                        InstallJungle(String.Format("{0}/JUNGLE2", ExtractedPath), false);
                    }
                    else
                    {
                        MessageBox.Show("JUNGLE2 not found.", GeneralInfo.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                StatusCode = 1;

                SaveTrackerXml();
            }
#if !DEBUG
            catch (Exception ex)
            {
                StatusCode = 6;
                Error_Message = ex.Message;
            }
#endif

        }

        private void ResolveFileType(string _path, string _dataFolderPath)
        {
            parent.Dispatcher.Invoke((Action)(() =>
            {
                parent.ProgressBar_Main.Value++;
                parent.ProgressBar_Label.Content = String.Format("_Installing \"{0}\"...", Path.GetFileNameWithoutExtension(_path));
            }));


            switch (Path.GetExtension(Path.GetFileNameWithoutExtension(_path)))
            {
                case ".idb":
                    InstallIdb(_path, _dataFolderPath);
                    break;
                case ".cus":
                    InstallCus(_path, _dataFolderPath);
                    break;
                case ".bpe":
                    InstallBpe(_path, _dataFolderPath);
                    break;
                case ".bcs":
                    InstallBcs(_path, _dataFolderPath);
                    break;
                case ".cms":
                    InstallCms(_path, _dataFolderPath);
                    break;
                case ".eepk":
                    InstallEepk(_path, _dataFolderPath);
                    break;
                case ".cso":
                    InstallCso(_path, _dataFolderPath);
                    break;
                case ".ers":
                    InstallErs(_path, _dataFolderPath);
                    break;
                case ".cns":
                    InstallCns(_path, _dataFolderPath);
                    break;
                case ".bsa":
                    InstallBsa(_path, _dataFolderPath);
                    break;
                case ".ean":
                    InstallEan(_path, _dataFolderPath);
                    break;
                case ".bev":
                    InstallBev(_path, _dataFolderPath);
                    break;
                case ".cnc":
                    InstallCnc(_path, _dataFolderPath);
                    break;
                case ".bdm":
                    InstallBdm(_path, _dataFolderPath);
                    break;
                case ".bac":
                    InstallBac(_path, _dataFolderPath);
                    break;
                case ".msg":
                    InstallMsg(_path, _dataFolderPath);
                    break;
                default:
                    MessageBox.Show(String.Format("Unable to resolve file type of \"{0}\"", _path), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void CopyFileToGameDir(string _path, string _dataFolderPath, bool overwite)
        {
            parent.Dispatcher.Invoke((Action)(() =>
            {
                parent.ProgressBar_Main.Value++;
                parent.ProgressBar_Label.Content = String.Format("_Copying \"{0}\"...", _path);
            }));

            if (!JungleBlacklistCheck(Path.GetFileName(_path)))
            {
                throw new Exception(String.Format("Blacklisted file: {0}", _path));
            }

            string copyPath = GetFilePathInDataDir(_dataFolderPath);
            string filePath = GetFilePathInZipDir(_path);

            if (!Directory.Exists(Path.GetDirectoryName(copyPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(copyPath));
            }

            if (overwite)
            {
                if (File.Exists(copyPath))
                {
                    CreateBackup(_dataFolderPath);
                }

                File.Copy(filePath, copyPath, true);
            }
            else if (!File.Exists(copyPath))
            {
                File.Copy(filePath, copyPath, false);
            }
        }


        //Install Jungle
        private void InstallJungle(string JungleDataPath, bool allowOverwrite)
        {
            if (allowOverwrite)
            {
                parent.Dispatcher.Invoke((Action)(() =>
                {
                    parent.ProgressBar_Main.Value++;
                    parent.ProgressBar_Label.Content = String.Format("Installing \"JUNGLE1\"...");
                }));
            }
            else if (allowOverwrite)
            {
                parent.Dispatcher.Invoke((Action)(() =>
                {
                    parent.ProgressBar_Main.Value++;
                    parent.ProgressBar_Label.Content = String.Format("Installing \"JUNGLE2\"...");
                }));
            }

            //AddToLog(String.Format("Start Jungle Copy (overwrite = {0}):", allowOverwrite.ToString()), true);
            string[] fileNames = Directory.GetFiles(JungleDataPath, "*.*", SearchOption.AllDirectories);

            for (int i = 0; i < fileNames.Count(); i++)
            {
                string copyPath = (string)fileNames[i].Clone();
                fileNames[i] = CommonOperations.CleanPath(fileNames[i].Remove(0, JungleDataPath.Count()));
                if (JungleBlacklistCheck(Path.GetFileName(fileNames[i])))
                {
                    if (!File.Exists(GetFilePathInDataDir(fileNames[i])) || allowOverwrite == true)
                    {
                        if (File.Exists(GetFilePathInDataDir(fileNames[i])))
                        {
                            CreateBackup(GetFilePathInDataDir(fileNames[i]));
                        }

                        File_Ex.CreateDirectory(Path.GetDirectoryName(GetFilePathInDataDir(fileNames[i])));
                        File.Copy(copyPath, GetFilePathInDataDir(fileNames[i]), true);
                    }
                    //AddToLog(fileNames[i], false);
                }
                else
                {
                    //AddToLog(String.Format("Blacklisted file: {0}", fileNames[i]), false);
                }

            }
            //AddToLog(String.Empty, false);
        }

        private bool JungleBlacklistCheck(string name)
        {
            if (GeneralInfo.JungleBlacklist.IndexOf(name) != -1)
            {
                MessageBox.Show(String.Format("File \"{0}\" is blacklisted and cannot be used in JUNGLE1 or JUNGLE2.", name), "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        //Type Installers
        private void InstallIdb(string XmlToInstall_Path, string DataFolderPath)
        {
            //try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.IDB.IDB_File binaryIdbFile = new Xv2CoreLib.IDB.Parser(dataPath, false).GetIdbFile();
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.IDB.IDB_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlIdbFile = (Xv2CoreLib.IDB.IDB_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));


                //Parsing bindings
                try
                {
                    xmlIdbFile = bindingManager.ParseIdb(xmlIdbFile, binaryIdbFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                //Installing entries
                for (int i = 0; i < xmlIdbFile.Entries.Count(); i++)
                {
                    if (StatusCode != 0)
                    {
                        return;
                    }

                    for (int a = 0; a < binaryIdbFile.Entries.Count(); a++)
                    {
                        if (xmlIdbFile.Entries[i].Index == binaryIdbFile.Entries[a].Index)
                        {
                            //If entry with same ID/Index already exists in file
                            binaryIdbFile.Entries[a] = xmlIdbFile.Entries[i];

                            //If MsgComponent exists
                            binaryIdbFile.Entries[a] = IdbMsgWriter(xmlIdbFile.Entries[i], DataFolderPath);
                            break;
                        }

                        //If entry with same ID/Index does not exist in file
                        if (a == (binaryIdbFile.Entries.Count() - 1))
                        {
                            int newIndex = binaryIdbFile.Entries.Count();
                            binaryIdbFile.Entries.Add(xmlIdbFile.Entries[i]);

                            //If MsgComponent exists
                            binaryIdbFile.Entries[newIndex] = IdbMsgWriter(xmlIdbFile.Entries[i], DataFolderPath);
                            break;
                        }

                    }
                }

                //Saving binary file
                binaryIdbFile.SaveBinary(GetFilePathInDataDir(relativePath));

            }
            //catch (Exception ex)
            {
                //    StatusCode = 14;
                //    Error_FileName = XmlToInstall_Path;
                //    Error_Message = ex.Message;
            }
        }

        private void InstallCus(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);
                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                //MessageBox.Show(ExtractedPath); //relativePath is null, maybe an issue with the path calc
                Xv2CoreLib.CUS.CUS_File binaryCusFile = new Xv2CoreLib.CUS.Parser(dataPath, false).GetCusFile();
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.CUS.CUS_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlCusFile = (Xv2CoreLib.CUS.CUS_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                //Parsing bindings
                try
                {
                    xmlCusFile = bindingManager.ParseCus(xmlCusFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                //Installing Skillsets
                if (xmlCusFile.Skillsets != null)
                {
                    for (int i = 0; i < xmlCusFile.Skillsets.Count(); i++)
                    {
                        //Installing the entry
                        int charaID = int.Parse(xmlCusFile.Skillsets[i].I_00);
                        int costumeID = xmlCusFile.Skillsets[i].I_04;
                        int preset = xmlCusFile.Skillsets[i].I_26;

                        if (binaryCusFile.GetIndexOf(charaID, costumeID, preset) != -1)
                        {
                            binaryCusFile.Skillsets[binaryCusFile.GetIndexOf(charaID, costumeID, preset)] = xmlCusFile.Skillsets[i];
                        }
                        else
                        {
                            binaryCusFile.Skillsets.Add(xmlCusFile.Skillsets[i]);
                        }
                    }
                }

                //Installing Skills
                binaryCusFile.SuperSkills = InstallCusSkill(xmlCusFile.SuperSkills, binaryCusFile.SuperSkills, binaryCusFile.GetNameMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Super), binaryCusFile.GetInfoMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Super));
                binaryCusFile.UltimateSkills = InstallCusSkill(xmlCusFile.UltimateSkills, binaryCusFile.UltimateSkills, binaryCusFile.GetNameMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Ultimate), binaryCusFile.GetInfoMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Ultimate));
                binaryCusFile.EvasiveSkills = InstallCusSkill(xmlCusFile.EvasiveSkills, binaryCusFile.EvasiveSkills, binaryCusFile.GetNameMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Evasive), binaryCusFile.GetInfoMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Evasive));
                binaryCusFile.BlastSkills = InstallCusSkill(xmlCusFile.BlastSkills, binaryCusFile.BlastSkills, binaryCusFile.GetNameMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Blast), binaryCusFile.GetInfoMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Blast));
                binaryCusFile.AwokenSkills = InstallCusSkill(xmlCusFile.AwokenSkills, binaryCusFile.AwokenSkills, binaryCusFile.GetNameMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Awoken), binaryCusFile.GetInfoMsgPath(Xv2CoreLib.CUS.CUS_File.SkillType.Awoken));


                //Saving binary file
                binaryCusFile.SaveBinary(GetFilePathInDataDir(relativePath));
            }
            catch (Exception ex)
            {
                StatusCode = 10;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }

        }

        private void InstallBpe(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.BPE.BPE_File binaryBpeFile = new Xv2CoreLib.BPE.Parser(dataPath, false).GetBpeFile();
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.BPE.BPE_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlBpeFile = (Xv2CoreLib.BPE.BPE_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                //Parsing bindings
                try
                {
                    xmlBpeFile = bindingManager.ParseBpe(xmlBpeFile, binaryBpeFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                //Installing entries
                for (int i = 0; i < xmlBpeFile.Entries.Count(); i++)
                {
                    for (int a = 0; a < binaryBpeFile.Entries.Count(); a++)
                    {
                        if (binaryBpeFile.Entries[a].Index == xmlBpeFile.Entries[i].Index)
                        {
                            binaryBpeFile.Entries[a] = xmlBpeFile.Entries[i];
                            break;
                        }

                        if (a == (binaryBpeFile.Entries.Count() - 1))
                        {
                            binaryBpeFile.Entries.Add(xmlBpeFile.Entries[i]);
                            break;
                        }
                    }
                }


                //Saving binary file
                binaryBpeFile.SaveBinary(GetFilePathInDataDir(relativePath));

            }
            catch (Exception ex)
            {
                StatusCode = 12;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallBcs(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.BCS.BCS_File binaryBcsFile = new Xv2CoreLib.BCS.Parser(dataPath, false).GetBcsFile();
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.BCS.BCS_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlBcsFile = (Xv2CoreLib.BCS.BCS_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                //Parsing bindings
                try
                {
                    xmlBcsFile = bindingManager.ParseBcs(xmlBcsFile, binaryBcsFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                //Installing PartSets
                if (xmlBcsFile.PartSets != null)
                {
                    for (int i = 0; i < xmlBcsFile.PartSets.Count(); i++)
                    {
                        if (binaryBcsFile.PartSets != null)
                        {
                            for (int a = 0; a < binaryBcsFile.PartSets.Count(); a++)
                            {
                                if (binaryBcsFile.PartSets[a].Index == xmlBcsFile.PartSets[i].Index)
                                {
                                    binaryBcsFile.PartSets[a] = xmlBcsFile.PartSets[i];
                                    break;
                                }

                                if (a == (binaryBcsFile.PartSets.Count() - 1))
                                {
                                    binaryBcsFile.PartSets.Add(xmlBcsFile.PartSets[i]);
                                    break;
                                }

                            }
                        }
                        else
                        {
                            binaryBcsFile.PartSets = new List<Xv2CoreLib.BCS.PartSet>()
                            {
                                xmlBcsFile.PartSets[i]
                            };
                        }

                    }
                }

                //Installing PartColors
                if (xmlBcsFile.Part_Colors != null)
                {
                    for (int i = 0; i < xmlBcsFile.Part_Colors.Count(); i++)
                    {
                        if (binaryBcsFile.Part_Colors != null)
                        {
                            for (int a = 0; a < binaryBcsFile.Part_Colors.Count(); a++)
                            {
                                if (binaryBcsFile.Part_Colors[a].Index == xmlBcsFile.Part_Colors[i].Index)
                                {
                                    binaryBcsFile.Part_Colors[a] = xmlBcsFile.Part_Colors[i];
                                    break;
                                }

                                if (a == (binaryBcsFile.Part_Colors.Count() - 1))
                                {
                                    binaryBcsFile.Part_Colors.Add(xmlBcsFile.Part_Colors[i]);
                                    break;
                                }

                            }
                        }
                        else
                        {
                            binaryBcsFile.Part_Colors = new List<Xv2CoreLib.BCS.PartColor>()
                        {
                            xmlBcsFile.Part_Colors[i]
                        };
                        }
                    }
                }

                //Installing Bodies
                if (xmlBcsFile.Bodies != null)
                {
                    for (int i = 0; i < xmlBcsFile.Bodies.Count(); i++)
                    {
                        if (binaryBcsFile.Bodies != null)
                        {
                            for (int a = 0; a < binaryBcsFile.Bodies.Count(); a++)
                            {
                                if (binaryBcsFile.Bodies[a].Index == xmlBcsFile.Bodies[i].Index)
                                {
                                    binaryBcsFile.Bodies[a] = xmlBcsFile.Bodies[i];
                                    break;
                                }

                                if (a == (binaryBcsFile.Bodies.Count() - 1))
                                {
                                    binaryBcsFile.Bodies.Add(xmlBcsFile.Bodies[i]);
                                    break;
                                }

                            }
                        }
                        else
                        {
                            binaryBcsFile.Bodies = new List<Xv2CoreLib.BCS.Body>()
                        {
                            xmlBcsFile.Bodies[i]
                        };
                        }
                    }
                }

                //Saving binary file
                binaryBcsFile.SaveBinary(GetFilePathInDataDir(relativePath));

            }
            catch (Exception ex)
            {
                StatusCode = 13;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallCms(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.CMS.CMS_File binaryCmsFile = new Xv2CoreLib.CMS.Parser(dataPath, false).GetCmsFile();
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.CMS.CMS_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlCmsFile = (Xv2CoreLib.CMS.CMS_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                //Parsing bindings
                try
                {
                    xmlCmsFile = bindingManager.ParseCms(xmlCmsFile, binaryCmsFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                //Installing Entries

                for (int i = 0; i < xmlCmsFile.CMS_Entries.Count(); i++)
                {
                    for (int a = 0; a < binaryCmsFile.CMS_Entries.Count(); a++)
                    {
                        if (xmlCmsFile.CMS_Entries[i].Index == binaryCmsFile.CMS_Entries[a].Index)
                        {
                            binaryCmsFile.CMS_Entries[a] = xmlCmsFile.CMS_Entries[i];
                            binaryCmsFile.CMS_Entries[a] = CmsMsgWriter(xmlCmsFile.CMS_Entries[i], DataFolderPath);
                            break;
                        }

                        if (a == (binaryCmsFile.CMS_Entries.Count() - 1))
                        {
                            int newIdx = binaryCmsFile.CMS_Entries.Count;
                            binaryCmsFile.CMS_Entries.Add(xmlCmsFile.CMS_Entries[i]);
                            binaryCmsFile.CMS_Entries[newIdx] = CmsMsgWriter(xmlCmsFile.CMS_Entries[i], DataFolderPath);
                            break;
                        }
                    }
                }


                //Saving binary file
                binaryCmsFile.SaveBinary(GetFilePathInDataDir(relativePath));

            }
            catch (Exception ex)
            {
                StatusCode = 11;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallEepk(string XmlToInstall_Path, string DataFolderPath)
        {
            //This method is different from the other ones, as it mostly relies on EepkInstall instead of doing the work itself.
            string relativePath = DataFolderPath;
            string dataPath = GetFilePath(relativePath);

            if (StatusCode != 0)
            {
                return;
            }

            try
            {

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                XV2_Serializer.EEPK.EEPK_File binaryEepkFile = new XV2_Serializer.EEPK.Parser(dataPath, false).GetEepkFile();
                YAXSerializer serializer = new YAXSerializer(typeof(XV2_Serializer.EEPK.EEPK_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlEepkFile = (XV2_Serializer.EEPK.EEPK_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                EepkExporter.EepkInstall eepkInstaller = new EepkExporter.EepkInstall(this, binaryEepkFile, xmlEepkFile, GetFilePathInDataDir(relativePath), GetFilePathInZipDir(XmlToInstall_Path), GeneralInfo.GameDataFolder);

                switch (eepkInstaller.ErrorCode)
                {
                    case 0:
                        throw new Exception();
                    case 2:
                        MessageBox.Show("EEPK: Texture Overflow error.", GeneralInfo.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        throw new Exception();
                }

            }
            catch (Exception ex)
            {
                StatusCode = 15;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
                return;
            }
        }

        private void InstallCso(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.CSO.CSO_File binaryCsoFile = new Xv2CoreLib.CSO.Parser(dataPath, false).GetCsoFile();
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.CSO.CSO_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlCsoFile = (Xv2CoreLib.CSO.CSO_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                if (xmlCsoFile.CsoEntries == null)
                {
                    return;
                }

                //Parsing bindings
                try
                {
                    xmlCsoFile = bindingManager.ParseCso(xmlCsoFile, binaryCsoFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                for (int i = 0; i < xmlCsoFile.CsoEntries.Count(); i++)
                {
                    if (binaryCsoFile.CsoEntries != null)
                    {
                        bool entryExists = false;
                        for (int a = 0; a < binaryCsoFile.CsoEntries.Count(); a++)
                        {
                            if (xmlCsoFile.CsoEntries[i].I_00 == binaryCsoFile.CsoEntries[a].I_00 && xmlCsoFile.CsoEntries[i].I_04 == binaryCsoFile.CsoEntries[a].I_04)
                            {
                                entryExists = true;
                                binaryCsoFile.CsoEntries[a] = xmlCsoFile.CsoEntries[i];
                                break;
                            }
                        }

                        if (entryExists == false)
                        {
                            binaryCsoFile.CsoEntries.Add(xmlCsoFile.CsoEntries[i]);
                        }
                    }
                    else
                    {
                        binaryCsoFile.CsoEntries = new List<Xv2CoreLib.CSO.CSO_Entry>();
                        binaryCsoFile.CsoEntries.Add(xmlCsoFile.CsoEntries[i]);
                    }
                }

                binaryCsoFile.SaveBinary(GetFilePathInDataDir(relativePath));
            }
            catch (Exception ex)
            {
                StatusCode = 16;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallErs(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.ERS.ERS_File binaryErsFile = new Xv2CoreLib.ERS.Parser(dataPath, false).ersFile;
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.ERS.ERS_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlErsFile = (Xv2CoreLib.ERS.ERS_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                if (xmlErsFile.Entries == null)
                {
                    return;
                }

                //Parsing bindings
                try
                {
                    xmlErsFile = bindingManager.ParseErs(xmlErsFile, binaryErsFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }


                for (int i = 0; i < xmlErsFile.Entries.Count(); i++)
                {
                    for (int a = 0; a < xmlErsFile.Entries[i].SubEntries.Count(); a++)
                    {
                        binaryErsFile.AddEntry(ushort.Parse(xmlErsFile.Entries[i].Index), xmlErsFile.Entries[i].SubEntries[a]);
                    }
                }

                binaryErsFile.SaveBinary(GetFilePathInDataDir(relativePath));
            }
            catch (Exception ex)
            {
                StatusCode = 17;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallCns(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.CNS.CNS_File binaryCnsFile = Xv2CoreLib.CNS.CNS_File.Read(dataPath, false);
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.CNS.CNS_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlCnsFile = (Xv2CoreLib.CNS.CNS_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                if (xmlCnsFile.CnsEntries == null)
                {
                    return;
                }

                //Parsing bindings
                try
                {
                    xmlCnsFile = bindingManager.ParseCns(xmlCnsFile, binaryCnsFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }


                for (int i = 0; i < xmlCnsFile.CnsEntries.Count(); i++)
                {
                    binaryCnsFile.AddEntry(xmlCnsFile.CnsEntries[i], xmlCnsFile.CnsEntries[i].Str_00, int.Parse(xmlCnsFile.CnsEntries[i].Index));
                }
                Xv2CoreLib.CNS.CNS_File.Write(binaryCnsFile, GetFilePathInDataDir(relativePath));
            }
            catch (Exception ex)
            {
                StatusCode = 18;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallBsa(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.BSA.BSA_File binaryBsaFile = new Xv2CoreLib.BSA.Parser(dataPath, false).GetBsaFile();
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.BSA.BSA_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlBsaFile = (Xv2CoreLib.BSA.BSA_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                if (xmlBsaFile.BSA_Entries == null)
                {
                    return;
                }

                //Parsing bindings
                try
                {
                    xmlBsaFile = bindingManager.ParseBsa(xmlBsaFile, binaryBsaFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                for (int i = 0; i < xmlBsaFile.BSA_Entries.Count(); i++)
                {
                    binaryBsaFile.AddEntry(int.Parse(xmlBsaFile.BSA_Entries[i].Index), xmlBsaFile.BSA_Entries[i]);
                }

                binaryBsaFile.SaveBinary(GetFilePathInDataDir(relativePath));
            }
            catch (Exception ex)
            {
                StatusCode = 19;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallEan(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.EAN.EAN_File binaryEanFile = new Xv2CoreLib.EAN.Parser(dataPath, false).eanFile;
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.EAN.EAN_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlEanFile = (Xv2CoreLib.EAN.EAN_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                if (xmlEanFile.Animations == null)
                {
                    return;
                }

                for (int i = 0; i < xmlEanFile.Animations.Count(); i++)
                {
                    binaryEanFile.AddEntry(xmlEanFile.Animations[i].IndexNumeric, xmlEanFile.Animations[i]);
                }

                binaryEanFile.SaveBinary(GetFilePathInDataDir(relativePath));
            }
            catch (Exception ex)
            {
                StatusCode = 20;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallBev(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.BEV.BEV_File binaryBevFile = new Xv2CoreLib.BEV.Parser(dataPath, false).bevFile;
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.BEV.BEV_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlBevFile = (Xv2CoreLib.BEV.BEV_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                if (xmlBevFile.Entries == null)
                {
                    return;
                }

                //Parsing bindings
                try
                {
                    xmlBevFile = bindingManager.ParseBev(xmlBevFile, binaryBevFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                for (int i = 0; i < xmlBevFile.Entries.Count(); i++)
                {
                    binaryBevFile.AddEntry(int.Parse(xmlBevFile.Entries[i].Index), xmlBevFile.Entries[i]);
                }

                binaryBevFile.SaveBinary(GetFilePathInDataDir(relativePath));
            }
            catch (Exception ex)
            {
                StatusCode = 21;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallCnc(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.CNC.CNC_File binaryCncFile = Xv2CoreLib.CNC.CNC_File.Read(dataPath, false);
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.CNC.CNC_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlCncFile = (Xv2CoreLib.CNC.CNC_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                if (xmlCncFile.CncEntries == null)
                {
                    return;
                }

                //Parsing bindings
                try
                {
                    xmlCncFile = bindingManager.ParseCnc(xmlCncFile, binaryCncFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }


                for (int i = 0; i < xmlCncFile.CncEntries.Count(); i++)
                {
                    binaryCncFile.AddEntry(xmlCncFile.CncEntries[i]);
                }
                Xv2CoreLib.CNC.CNC_File.Write(binaryCncFile, GetFilePathInDataDir(relativePath));
            }
            catch (Exception ex)
            {
                StatusCode = 22;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallBdm(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                Xv2CoreLib.BDM.BDM_File binaryBdmFile = new Xv2CoreLib.BDM.Parser(dataPath, false).bdmFile;
                YAXSerializer serializer = new YAXSerializer(typeof(Xv2CoreLib.BDM.BDM_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlBdmFile = (Xv2CoreLib.BDM.BDM_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                if (xmlBdmFile.BDM_Entries == null)
                {
                    return;
                }

                //Bdm Type validation
                if (binaryBdmFile.BDM_Type == Xv2CoreLib.BDM.BDM_Type.XV2_1)
                {
                    binaryBdmFile.ConvertToXv2_0();
                }
                if (xmlBdmFile.BDM_Type == Xv2CoreLib.BDM.BDM_Type.XV2_1)
                {
                    xmlBdmFile.ConvertToXv2_0();
                }

                //No bindings for bdm

                for (int i = 0; i < xmlBdmFile.BDM_Entries.Count(); i++)
                {
                    binaryBdmFile.AddEntry(xmlBdmFile.BDM_Entries[i].I_00, xmlBdmFile.BDM_Entries[i]);
                }

                binaryBdmFile.SaveBinary(GetFilePathInDataDir(relativePath));
            }
            catch (Exception ex)
            {
                StatusCode = 23;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }

        private void InstallBac(string XmlToInstall_Path, string DataFolderPath)
        {
#if !DEBUG
            try
#endif
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                BAC_File binaryBacFile = new Xv2CoreLib.BAC.Parser(dataPath, false).bacFile;
                YAXSerializer serializer = new YAXSerializer(typeof(BAC_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlBacFile = (BAC_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                //Parsing bindings
                try
                {
                    xmlBacFile = bindingManager.ParseBac(xmlBacFile, binaryBacFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                //Installing entries
                for (int i = 0; i < xmlBacFile.BacEntries.Count(); i++)
                {
                    for (int a = 0; a < binaryBacFile.BacEntries.Count(); a++)
                    {
                        if (binaryBacFile.BacEntries[a].Index == xmlBacFile.BacEntries[i].Index)
                        {
                            binaryBacFile.BacEntries[a] = xmlBacFile.BacEntries[i];
                            break;
                        }

                        if (a == (binaryBacFile.BacEntries.Count() - 1))
                        {
                            binaryBacFile.AddEntry(xmlBacFile.BacEntries[i], int.Parse(xmlBacFile.BacEntries[i].Index));
                            break;
                        }
                    }
                }


                //Saving binary file
                binaryBacFile.SaveBinary(GetFilePathInDataDir(relativePath));

            }
#if !DEBUG
            catch (Exception ex)
            {
                StatusCode = 24;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
#endif
        }

        private void InstallMsg(string XmlToInstall_Path, string DataFolderPath)
        {
            try
            {
                string relativePath = DataFolderPath;
                string dataPath = GetFilePath(relativePath);

                if (StatusCode != 0)
                {
                    return;
                }

                //Parsing the binary file and xml
                CreateBackup(relativePath);
                MSG_File binaryMsgFile = new Xv2CoreLib.MSG.Parser(dataPath, false).msg_File;
                YAXSerializer serializer = new YAXSerializer(typeof(MSG_File), YAXSerializationOptions.DontSerializeNullObjects);
                var xmlMsgFile = (MSG_File)serializer.DeserializeFromFile(GetFilePathInZipDir(XmlToInstall_Path));

                //Parsing bindings
                try
                {
                    xmlMsgFile = bindingManager.ParseMsg(xmlMsgFile, binaryMsgFile, relativePath);
                }
                catch (Exception ex)
                {
                    Error_Message = String.Format("Could not parse the ID bindings for \"{0}\"\n\n{1}", XmlToInstall_Path, ex.Message);
                    Error_FileName = XmlToInstall_Path;
                    StatusCode = 200;
                    return;
                }

                //Installing entries
                for (int i = 0; i < xmlMsgFile.MSG_Entries.Count(); i++)
                {
                    for (int a = 0; a < binaryMsgFile.MSG_Entries.Count(); a++)
                    {
                        if (binaryMsgFile.MSG_Entries[a].Index == xmlMsgFile.MSG_Entries[i].Index)
                        {
                            binaryMsgFile.MSG_Entries[a] = xmlMsgFile.MSG_Entries[i];
                            break;
                        }

                        if (a == (binaryMsgFile.MSG_Entries.Count() - 1))
                        {
                            binaryMsgFile.MSG_Entries.Add(xmlMsgFile.MSG_Entries[i]);
                            break;
                        }
                    }
                }


                //Saving binary file
                binaryMsgFile.SaveBinary(GetFilePathInDataDir(relativePath));

            }
            catch (Exception ex)
            {
                StatusCode = 25;
                Error_FileName = XmlToInstall_Path;
                Error_Message = ex.Message;
            }
        }


        //Msg Writer
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


        //Additional methods for "Type Installers"
        private List<Xv2CoreLib.CUS.Skill> InstallCusSkill(List<Xv2CoreLib.CUS.Skill> SkillsToInstall, List<Xv2CoreLib.CUS.Skill> BinaryList, string MsgNamePath, string MsgInfoPath)
        {
            if (SkillsToInstall != null)
            {
                for (int i = 0; i < SkillsToInstall.Count(); i++)
                {
                    for (int a = 0; a < BinaryList.Count(); a++)
                    {
                        if (SkillsToInstall[i].Index == BinaryList[a].Index)
                        {
                            BinaryList[a] = SkillsToInstall[i];
                            break;
                        }

                        if (a == (BinaryList.Count() - 1))
                        {
                            BinaryList.Add(SkillsToInstall[i]);
                            break;
                        }
                    }
                }
            }

            return BinaryList;
        }


        //Interface for EepkInstall
        public string ReceiveFile(string absolutePath, string dataFolderPath)
        {
            //In this method I need to check if the file exists in the game data folder. If it doesn't, then I need to grab it from the "DEFAULT" folder.
            //If it doesn't exist in the "DEFAULT" folder, then installation should be aborted and file restore triggered. Throwing an exception can be used for this.
            string relativePath = absolutePath.Remove(0, dataFolderPath.Count());
            relativePath = CommonOperations.CleanPath(relativePath);

            GetFilePath(relativePath);

            //Checking game data folder
            if (File.Exists(absolutePath))
            {
                return absolutePath;
            }

            //Checking default folder
            if (File.Exists(GetFilePath(relativePath)))
            {
                File.Copy(GetFilePath(relativePath), absolutePath);
                return absolutePath;
            }
            else
            {
                throw new FileNotFoundException("Could not find eepk resource file.");
            }
        }

        public void SendBackupRequest(string path, string dataFolderPath)
        {
            string relativePath = path.Remove(0, dataFolderPath.Count());
            //CreateBackup(relativePath);
        }
        
        //Helper
        public string GetFilePath(string _path)
        {
            //Checks game data folder > game cpk > installer DEFAULT

            if (File.Exists(GetFilePathInDataDir(_path)))
            {
                return GetFilePathInDataDir(_path);
            }
            else if (cpkReader.GetFile(String.Format("data/{0}", _path), GetFilePathInDataDir(_path))) //Checking CPK
            {
                return GetFilePathInDataDir(_path);
            }
            else if (File.Exists(String.Format("{0}/DEFAULT/{1}", ExtractedPath, _path)))
            {
                return String.Format("{0}/DEFAULT/{1}", ExtractedPath, _path);
            }
            else
            {
                StatusCode = 3;
                Error_FileName = _path;
                return null;

            }
        }

        public string GetFilePathInDataDir(string _path)
        {
            return String.Format("{0}/{1}", GeneralInfo.GameDataFolder, _path);
        }

        public string GetFilePathInZipDir(string _path)
        {
            return String.Format("{0}/data/{1}", ExtractedPath, _path);
        }

        public void CreateBackup(string file)
        {
            if (File.Exists(GetFilePath(file)))
            {
                string directory = String.Format("{0}/{1}", BackupPath, Path.GetDirectoryName(file));

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(String.Format("{0}/{1}", directory, Path.GetFileName(file))))
                {
                    File.Copy(GetFilePath(file), String.Format("{0}/{1}", directory, Path.GetFileName(file)));
                }
            }
        }

    }
}
