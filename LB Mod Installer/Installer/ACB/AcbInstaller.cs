using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.ACB;
using Xv2CoreLib.MSG;
using Xv2CoreLib.OBL;

namespace LB_Mod_Installer.Installer.ACB
{
    public class AcbInstaller
    {
        //Voice ACB Constants
        internal const string CSS_VOICE_EN_PATH = "sound/VOX/Sys/en/CRT_CS_vox.acb";
        internal const string CSS_VOICE_JPN_PATH = "sound/VOX/Sys/CRT_CS_vox.acb";
        internal const string TTB_VOICE_EN_PATH = "sound/VOX/Quest/Dialogue/en/CAQD_ADD_VOX.acb";
        internal const string TTB_VOICE_JPN_PATH = "sound/VOX/Quest/Dialogue/CAQD_ADD_VOX.acb";
        internal const string SEV_VOICE_EN_PATH = "sound/VOX/Quest/Dialogue/en/CAQD_ALL_VOX.acb";
        internal const string SEV_VOICE_JPN_PATH = "sound/VOX/Quest/Dialogue/CAQD_ALL_VOX.acb";
        internal const string TMQ_VOICE_EN_PATH = "sound/VOX/Quest/TMQ/en/CAQR_ALL_VOX";
        internal const string TMQ_VOICE_JPN_PATH = "sound/VOX/Quest/TMQ/CAQR_ALL_VOX";

        //BGM Constants
        internal const string BGM_PATH = "sound/BGM/CAR_BGM.acb";
        internal const string BGM_TU10_PATH = "sound/BGM/CAR_TU10_Add_BGM.acb";
        internal const string OBL_PATH = "system/OptionBGMList.obl";
        internal const string OPTION_MSG_PATH = "msg/option_text_";
        internal const string OPTION_INSTALL_TYPE = "BGM_OPTION";
        internal const string DIRECT_INSTALL_TYPE = "BGM_DIRECT";

        private Install install;

        private ACB_File audioPackage;
        private string installPath;
        private string audioPackagePath;

        //CSS:
        private ACB_File enVoiceFile;
        private ACB_File jpnVoiceFile;
        private string enPath;
        private string jpnPath;

        //BGM:
        private ACB_File bgmFile;
        private OBL_File oblFile;
        private List<MSG_File> msgFiles;
        private int currentCueId = 500;

        public AcbInstaller(Install _install, string audioPackagePath, string _installPath)
        {
            install = _install;
            audioPackage = ACB_File.Load(install.zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(audioPackagePath)), null, false, true);
            installPath = !string.IsNullOrWhiteSpace(_installPath) ? Xv2CoreLib.Utils.SanitizePath(_installPath) : string.Empty;
            this.audioPackagePath = audioPackagePath;

            if(audioPackage.AudioPackageType == AudioPackageType.BGM_Direct || audioPackage.AudioPackageType == AudioPackageType.BGM_NewOption)
            {
                //Validate the install path
                if (!string.IsNullOrWhiteSpace(installPath))
                {
                    if (!installPath.Equals(BGM_TU10_PATH, StringComparison.OrdinalIgnoreCase) && !installPath.Equals(BGM_PATH, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException($"InstallPath is not valid. It must be either for a supported BGM ACB (CAR_BGM and CAR_TU10_Add_BGM) or empty.");
                    
                    if(installPath.Equals(BGM_TU10_PATH, StringComparison.OrdinalIgnoreCase) && audioPackage.AudioPackageType == AudioPackageType.BGM_NewOption)
                        throw new ArgumentException($"NewOption cannot be used when installing into CAR_TU10_Add_BGM.acb.");

                }
                else
                {
                    installPath = BGM_PATH;
                }

                LoadMusicFiles();
                InstallMusic();
            }
            else if(audioPackage.AudioPackageType == AudioPackageType.AutoVoice)
            {
                LoadVoiceFiles();
                InstallVoices();
            }
            else
            {
                throw new ArgumentException($"Unknown AudioPackage Type: {audioPackage.AudioPackageType}");
            }
        }

        #region Voice
        private void LoadVoiceFiles()
        {
            if (string.IsNullOrWhiteSpace(installPath))
            {
                enPath = CSS_VOICE_EN_PATH;
                jpnPath = CSS_VOICE_JPN_PATH;
            }
            else if (Path.GetFileName(Path.GetDirectoryName(installPath)).Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                enPath = installPath;
                jpnPath = string.Format("{0}/{1}", Path.GetDirectoryName(Path.GetDirectoryName(installPath)), Path.GetFileName(installPath));
            }
            else
            {
                enPath = string.Format("{0}/en/{1}", Path.GetDirectoryName(installPath), Path.GetFileName(installPath));
                jpnPath = installPath;
            }

            enVoiceFile = (ACB_File)install.GetParsedFile<ACB_File>(enPath, false);
            jpnVoiceFile = (ACB_File)install.GetParsedFile<ACB_File>(jpnPath, false);

            enVoiceFile.CleanTablesOnSave = false;
            jpnVoiceFile.CleanTablesOnSave = false;
        }

        private void InstallVoices()
        {
            foreach(var enCue in audioPackage.Cues.Where(x => x.VoiceLanguage == VoiceLanguageEnum.English))
            {
                ACB_Cue jpnCue = audioPackage.Cues.FirstOrDefault(x => x.InstallID == enCue.InstallID && x.VoiceLanguage == VoiceLanguageEnum.Japanese);

                //Use English cue if no Japanese one was supplied
                if (jpnCue == null)
                    jpnCue = enCue;

                //Get waveform to install, rather than just installing the cue directly (which could break compat with xv2ins if that cue does something it doesnt like)
                var enWaveforms = audioPackage.GetWaveformsFromCue(enCue);
                var jpnWaveforms = audioPackage.GetWaveformsFromCue(jpnCue);

                if (enWaveforms.Count != 1 || jpnWaveforms.Count != 1)
                    throw new InvalidDataException("Invalid number of tracks on cue (there must be 1). ACB voice install failed.");

                byte[] enHca = audioPackage.GetAfs2Entry(enWaveforms[0].AwbId)?.bytes;
                byte[] jpnHca = audioPackage.GetAfs2Entry(jpnWaveforms[0].AwbId)?.bytes;

                //Install new cue
                bool reusing;
                int cueId = AssignCommonVoiceCueId(out reusing);
                ACB_Cue newEnCue;
                ACB_Cue newJpnCue;

                //ACB settings
                enVoiceFile.ReuseTrackCommand = true;
                enVoiceFile.ReuseSequenceCommand = true;
                enVoiceFile.AllowSharedAwbEntries = false;
                jpnVoiceFile.ReuseTrackCommand = true;
                jpnVoiceFile.ReuseSequenceCommand = true;
                jpnVoiceFile.AllowSharedAwbEntries = false;

                //Assign a cue name (check to prevent duplicates)
                string cueName = enVoiceFile.IsCueNameUsed(enCue.Name) ? $"{enCue.Name}_{enVoiceFile.GetFreeCueId()}" : enCue.Name;

                //Search for an existing cue with the same name, and if one exists, it can be overwritten
                bool replacing = false;
                var existingCue = enVoiceFile.GetCue(enCue.Name);

                if(existingCue != null)
                {
                    replacing = true;
                    cueId = (int)existingCue.ID;
                }

                if (reusing || replacing)
                {
                    newEnCue = enVoiceFile.GetCue(cueId);
                    newJpnCue = jpnVoiceFile.GetCue(cueId);

                    if (reusing)
                    {
                        newEnCue.Name = cueName;
                        newJpnCue.Name = cueName;
                    }

                    enVoiceFile.ReplaceTrackOnCue(enVoiceFile.GetCue(cueId), enHca, enVoiceFile.IsStreamingAcb(), EncodeType.HCA);
                    jpnVoiceFile.ReplaceTrackOnCue(jpnVoiceFile.GetCue(cueId), jpnHca, jpnVoiceFile.IsStreamingAcb(), EncodeType.HCA);

                }
                else
                {
                    //Add cues
                    enVoiceFile.AddCue(cueName, cueId, ReferenceType.Sequence, enHca, enVoiceFile.IsStreamingAcb(), false, EncodeType.HCA, out newEnCue);
                    jpnVoiceFile.AddCue(cueName, cueId, ReferenceType.Sequence, jpnHca, jpnVoiceFile.IsStreamingAcb(), false, EncodeType.HCA, out newJpnCue);
                }

                //Handle 3D_Def (3d position enabled sounds)
                bool isNewCue3d = audioPackage.DoesCueHave3dDef(enCue);

                //The new cue may be 3d if its reused, or replaced. New ones wont be.
                bool isCurrentCue3d = enVoiceFile.DoesCueHave3dDef(newEnCue);

                if(isCurrentCue3d && !isNewCue3d)
                {
                    enVoiceFile.Remove3dDefFromCue(newEnCue);
                    jpnVoiceFile.Remove3dDefFromCue(newJpnCue);
                }
                else if (isNewCue3d && !isCurrentCue3d)
                {
                    enVoiceFile.Add3dDefToCue(newEnCue);
                    jpnVoiceFile.Add3dDefToCue(newJpnCue);
                }

                //Set alias, if one was defined on the cue
                Install.bindingManager.AddAlias(cueId.ToString(), enCue.AliasBinding);

                //Add tracker entries
                GeneralInfo.Tracker.AddID(enPath, Sections.ACB_Voice_Cue, cueId.ToString());
                GeneralInfo.Tracker.AddID(jpnPath, Sections.ACB_Voice_Cue, cueId.ToString());

            }
        }

        private int AssignCommonVoiceCueId(out bool reusing)
        {
             //For whatever reason XV2INS wants to overwrite tracks that are replaced, so best to always add new cues.
            ACB_Cue enFreeCue = enVoiceFile.GetCue("X2_FREE");
            ACB_Cue jpnFreeCue = jpnVoiceFile.GetCue("X2_FREE");

            int enCueId;
            int jpnCueId;

            if(enFreeCue != null && jpnFreeCue != null)
            {
                if(enFreeCue.ID == jpnFreeCue.ID)
                {
                    enCueId = (int)enFreeCue.ID;
                    jpnCueId = (int)enFreeCue.ID;
                    reusing = true;
                }
                else
                {
                    enCueId = enVoiceFile.GetFreeCueId();
                    jpnCueId = jpnVoiceFile.GetFreeCueId();
                    reusing = false;
                }
            }
            else
            {
                enCueId = enVoiceFile.GetFreeCueId();
                jpnCueId = jpnVoiceFile.GetFreeCueId();
                reusing = false;
            }

            //int enCueId = enCssFile.GetFreeCueId();
            //int jpnCueId = jpnCssFile.GetFreeCueId();

            //Repair CSS ACB if out of sync
            if (enCueId != jpnCueId)
            {
                RepairCss();
                enCueId = enVoiceFile.GetFreeCueId();
            }

            //reusing = false;

            return enCueId;
        }

        private void RepairCss()
        {
            if (enVoiceFile.Cues.Count == jpnVoiceFile.Cues.Count) return;

            ACB_File main = (enVoiceFile.Cues.Count > jpnVoiceFile.Cues.Count) ? enVoiceFile : jpnVoiceFile;
            ACB_File second = (enVoiceFile.Cues.Count > jpnVoiceFile.Cues.Count) ? jpnVoiceFile : enVoiceFile;

            while(main.Cues.Count > second.Cues.Count)
            {
                if (main.Cues.Count <= second.Cues.Count) break;
                int newCueId = second.GetFreeCueId();
                ACB_Cue cueToCopy = main.GetCue(newCueId);

                if(cueToCopy != null)
                {
                    second.CopyCue(newCueId, main, false);
                }
                else
                {
                    break;
                }
            }
        }
        #endregion

        #region BGM
        private void LoadMusicFiles()
        {
            bgmFile = (ACB_File)install.GetParsedFile<ACB_File>(installPath, false);

            if (audioPackage.AudioPackageType == AudioPackageType.BGM_NewOption)
            {
                oblFile = (OBL_File)install.GetParsedFile<OBL_File>(OBL_PATH, false);
                msgFiles = new List<MSG_File>();

                for (int i = 0; i < GeneralInfo.LanguageSuffix.Length; i++)
                {
                    msgFiles.Add((MSG_File)install.GetParsedFile<MSG_File>($"{OPTION_MSG_PATH}{GeneralInfo.LanguageSuffix[i]}", false));
                }
            }
        }
        
        private void InstallMusic()
        {
            //Force all tracks to be streamed
            foreach (var waveform in audioPackage.Waveforms.Where(x => !x.IsStreaming))
                waveform.IsStreaming = true;

            if(audioPackage.AudioPackageType == AudioPackageType.BGM_NewOption)
            {
                foreach (var cue in audioPackage.Cues)
                {
                    cue.ID = (uint)GetNextCueId();
                    bgmFile.CopyCue((int)cue.ID, audioPackage, false);
                    oblFile.AddEntry((int)cue.ID);

                    foreach (var msgFile in msgFiles)
                    {
                        msgFile.AddEntry($"OPT_BGM_{cue.ID.ToString("D3")}", cue.Name);
                    }

                    //Set alias, if one was defined on the cue
                    Install.bindingManager.AddAlias(cue.ID.ToString(), cue.AliasBinding);

                    GeneralInfo.Tracker.AddID(OPTION_INSTALL_TYPE, Sections.ACB_Cue, cue.ID.ToString());
                }
            }
            else if (audioPackage.AudioPackageType == AudioPackageType.BGM_Direct)
            {
                //Direct mode. Just copy the cues into the BGM.
                foreach (var cue in audioPackage.Cues)
                {
                    //Remove cue with matching ID (if exists)
                    bgmFile.Cues.RemoveAll(x => x.ID == cue.ID);
                    
                    bgmFile.CopyCue((int)cue.ID, audioPackage, false);

                    GeneralInfo.Tracker.AddID(DIRECT_INSTALL_TYPE, Sections.ACB_Cue, cue.ID.ToString());
                }
            }
        }

        /// <summary>
        /// Return the next available Cue ID (unusued in ACB, OBL and MSG).
        /// </summary>
        private int GetNextCueId()
        {
            while (IsCueIdUsed(currentCueId))
                currentCueId++;

            currentCueId++;
            return currentCueId - 1;
        }

        private bool IsCueIdUsed(int cueId)
        {
            if (bgmFile.Cues.Any(x => x.ID == (uint)cueId)) return true;
            if (oblFile.IsCueAndMessageIdUsed(cueId)) return true;

            foreach (var msgFile in msgFiles)
                if (msgFile.MSG_Entries.Any(x => x.Name == $"OPT_BGM_{cueId.ToString("D3")}")) return true;

            return false;
        }
        #endregion

        private bool IsCssFile()
        {
            return installPath.Equals(CSS_VOICE_EN_PATH, StringComparison.OrdinalIgnoreCase) || installPath.Equals(CSS_VOICE_JPN_PATH, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsTtbFile()
        {
            return installPath.Equals(TTB_VOICE_EN_PATH, StringComparison.OrdinalIgnoreCase) || installPath.Equals(TTB_VOICE_JPN_PATH, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSevFile()
        {
            return installPath.Equals(SEV_VOICE_EN_PATH, StringComparison.OrdinalIgnoreCase) || installPath.Equals(SEV_VOICE_JPN_PATH, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsProtectedAcb()
        {
            //A "protected" acb is one that xv2ins installs into. Extra care must be taken with these ACBs, otherwise xv2ins could have issues loading/adding to them.
            if (installPath.Contains("sound/VOX/Quest")) return true;
            return IsCssFile() || IsTtbFile() || IsSevFile();
        }
    }
}
