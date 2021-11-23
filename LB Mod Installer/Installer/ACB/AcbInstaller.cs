using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xv2CoreLib.ACB_NEW;
using Xv2CoreLib.MSG;
using Xv2CoreLib.OBL;

namespace LB_Mod_Installer.Installer.ACB
{
    public class AcbInstaller
    {
        //CSS Constants
        internal const string CSS_VOICE_EN_PATH = "sound/VOX/Sys/en/CRT_CS_vox.acb";
        internal const string CSS_VOICE_JPN_PATH = "sound/VOX/Sys/CRT_CS_vox.acb";

        //BGM Constants
        internal const string BGM_PATH = "sound/BGM/CAR_BGM.acb";
        internal const string OBL_PATH = "system/OptionBGMList.obl";
        internal const string OPTION_MSG_PATH = "msg/option_text_";
        internal const string OPTION_INSTALL_TYPE = "BGM_OPTION";
        internal const string DIRECT_INSTALL_TYPE = "BGM_DIRECT";

        private Install install;

        private ACB_File musicPackage;

        //CSS:
        private ACB_File enCssFile;
        private ACB_File jpnCssFile;

        //BGM:
        private ACB_File bgmFile;
        private OBL_File oblFile;
        private List<MSG_File> msgFiles;
        private int currentCueId = 500;

        public AcbInstaller(Install _install, string musicPackagePath)
        {
            install = _install;
            musicPackage = ACB_File.Load(install.zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(musicPackagePath)), null, false, true);

            if(musicPackage.MusicPackageType == MusicPackageType.BGM_Direct || musicPackage.MusicPackageType == MusicPackageType.BGM_NewOption)
            {
                LoadMusicFiles();
                InstallMusic();
            }
            else if(musicPackage.MusicPackageType == MusicPackageType.CSS_Voice)
            {
                LoadCssFiles();
                InstallCss();
            }
            else
            {
                throw new InvalidOperationException($"Unknown MusicPackage Type: {musicPackage.MusicPackageType}");
            }
        }

        #region CSS
        private void LoadCssFiles()
        {
            enCssFile = (ACB_File)install.GetParsedFile<ACB_File>(CSS_VOICE_EN_PATH, false);
            jpnCssFile = (ACB_File)install.GetParsedFile<ACB_File>(CSS_VOICE_JPN_PATH, false);

            enCssFile.CleanTablesOnSave = false;
            jpnCssFile.CleanTablesOnSave = false;
        }

        private void InstallCss()
        {
            foreach(var enCue in musicPackage.Cues.Where(x => x.Name.Contains("_en")))
            {
                string[] nameSplit = enCue.Name.Split('_');
                if (nameSplit.Length != 2) throw new InvalidDataException($"Cannot parse the name for cue \"{enCue.Name}\", ID: {enCue.ID}. For installing new CSS voices, the name must include the alias and language, formated like this:\ntheAlias_en (for English)\ntheAlias_jpn (for Japanese).");

                string alias = nameSplit[0];
                ACB_Cue jpnCue = musicPackage.Cues.FirstOrDefault(x => x.Name == $"{alias}_jpn");

                //Use English cue if no Japanese one was supplied
                if (jpnCue == null)
                    jpnCue = enCue;

                //Get waveform to install, rather than just installing the cue directly (which could break compat with xv2ins if that cue does something it doesnt like)
                var enWaveforms = musicPackage.GetWaveformsFromCue(enCue);
                var jpnWaveforms = musicPackage.GetWaveformsFromCue(jpnCue);

                if (enWaveforms.Count != 1 || jpnWaveforms.Count != 1)
                    throw new InvalidDataException("Invalid number of tracks on cue (there must be 1). CSS voice install failed.");

                byte[] enHca = musicPackage.GetAfs2Entry(enWaveforms[0].AwbId)?.bytes;
                byte[] jpnHca = musicPackage.GetAfs2Entry(jpnWaveforms[0].AwbId)?.bytes;

                //Install new cue
                bool reusing;
                int cueId = AssignCommonCssCueId(out reusing);
                ACB_Cue newEnCue;
                ACB_Cue newJpnCue;

                //ACB settings
                enCssFile.ReuseTrackCommand = true;
                enCssFile.ReuseSequenceCommand = true;
                enCssFile.AllowSharedAwbEntries = false;
                jpnCssFile.ReuseTrackCommand = true;
                jpnCssFile.ReuseSequenceCommand = true;
                jpnCssFile.AllowSharedAwbEntries = false;

                if (reusing)
                {
                    newEnCue = enCssFile.GetCue(cueId);
                    newJpnCue = jpnCssFile.GetCue(cueId);

                    newEnCue.Name = $"LB_NEW_VOICE_{cueId}";
                    newJpnCue.Name = $"LB_NEW_VOICE_{cueId}";

                    enCssFile.ReplaceTrackOnCue(enCssFile.GetCue(cueId), enHca, true, EncodeType.HCA);
                    jpnCssFile.ReplaceTrackOnCue(jpnCssFile.GetCue(cueId), jpnHca, true, EncodeType.HCA);
                }
                else
                {
                    //Add cues
                    enCssFile.AddCue($"LB_NEW_VOICE_{cueId}", cueId, ReferenceType.Sequence, enHca, true, false, EncodeType.HCA, out newEnCue);
                    jpnCssFile.AddCue($"LB_NEW_VOICE_{cueId}", cueId, ReferenceType.Sequence, jpnHca, true, false, EncodeType.HCA, out newJpnCue);
                }
                
                //Set binding
                Install.bindingManager.AddAlias(cueId, alias);

                //Add tracker entries
                GeneralInfo.Tracker.AddID(CSS_VOICE_EN_PATH, Sections.ACB_Cue, cueId.ToString());
                GeneralInfo.Tracker.AddID(CSS_VOICE_JPN_PATH, Sections.ACB_Cue, cueId.ToString());
            }
        }

        private int AssignCommonCssCueId(out bool reusing)
        {
             //For whatever reason XV2INS wants to overwrite tracks that are replaced, so best to always add new cues.
            ACB_Cue enFreeCue = enCssFile.GetCue("X2_FREE");
            ACB_Cue jpnFreeCue = jpnCssFile.GetCue("X2_FREE");

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
                    enCueId = enCssFile.GetFreeCueId();
                    jpnCueId = jpnCssFile.GetFreeCueId();
                    reusing = false;
                }
            }
            else
            {
                enCueId = enCssFile.GetFreeCueId();
                jpnCueId = jpnCssFile.GetFreeCueId();
                reusing = false;
            }

            //int enCueId = enCssFile.GetFreeCueId();
            //int jpnCueId = jpnCssFile.GetFreeCueId();

            //Repair CSS ACB if out of sync
            if (enCueId != jpnCueId)
            {
                RepairCss();
                enCueId = enCssFile.GetFreeCueId();
            }

            //reusing = false;

            return enCueId;
        }

        private void RepairCss()
        {
            if (enCssFile.Cues.Count == jpnCssFile.Cues.Count) return;

            ACB_File main = (enCssFile.Cues.Count > jpnCssFile.Cues.Count) ? enCssFile : jpnCssFile;
            ACB_File second = (enCssFile.Cues.Count > jpnCssFile.Cues.Count) ? jpnCssFile : enCssFile;

            while(main.Cues.Count > second.Cues.Count)
            {
                if (main.Cues.Count <= second.Cues.Count) break;
                int newCueId = second.GetFreeCueId();
                ACB_Cue cueToCopy = main.GetCue(newCueId);

                if(cueToCopy != null)
                {
                    second.CopyCue(newCueId, main);
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
            bgmFile = (ACB_File)install.GetParsedFile<ACB_File>(BGM_PATH, false);

            if (musicPackage.MusicPackageType == MusicPackageType.BGM_NewOption)
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
            foreach (var waveform in musicPackage.Waveforms.Where(x => !x.Streaming))
                waveform.Streaming = true;

            if(musicPackage.MusicPackageType == MusicPackageType.BGM_NewOption)
            {
                foreach (var cue in musicPackage.Cues)
                {
                    cue.ID = (uint)GetNextCueId();
                    bgmFile.CopyCue((int)cue.ID, musicPackage);
                    oblFile.AddEntry((int)cue.ID);

                    foreach (var msgFile in msgFiles)
                    {
                        msgFile.AddEntry($"OPT_BGM_{cue.ID.ToString("D3")}", cue.Name);
                    }

                    GeneralInfo.Tracker.AddID(OPTION_INSTALL_TYPE, Sections.ACB_Cue, cue.ID.ToString());
                }
            }
            else if (musicPackage.MusicPackageType == MusicPackageType.BGM_Direct)
            {
                //Direct mode. Just copy the cues into the BGM.
                foreach (var cue in musicPackage.Cues)
                {
                    //Remove cue with matching ID (if exists)
                    bgmFile.Cues.RemoveAll(x => x.ID == cue.ID);
                    
                    bgmFile.CopyCue((int)cue.ID, musicPackage);

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

    }
}
