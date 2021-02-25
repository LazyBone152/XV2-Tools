using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.ACB_NEW;
using Xv2CoreLib.MSG;
using Xv2CoreLib.OBL;

namespace LB_Mod_Installer.Installer.Music
{
    public class MusicInstaller
    {
        internal const string BGM_PATH = "sound/BGM/CAR_BGM.acb";
        internal const string OBL_PATH = "system/OptionBGMList.obl";
        internal const string OPTION_MSG_PATH = "msg/option_text_";
        internal const string OPTION_INSTALL_TYPE = "BGM_OPTION";
        internal const string DIRECT_INSTALL_TYPE = "BGM_DIRECT";

        private Install install;

        private ACB_File musicPackage;
        private ACB_File bgmAcb;
        private OBL_File oblFile;
        private List<MSG_File> msgFiles = new List<MSG_File>();

        private int currentCueId = 500;

        public MusicInstaller(Install _install, string musicPackagePath)
        {
            install = _install;
            musicPackage = ACB_File.Load(install.zipManager.GetFileFromArchive(GeneralInfo.GetPathInZipDataDir(musicPackagePath)), null, false, true);
            bgmAcb = (ACB_File)install.GetParsedFile<ACB_File>(BGM_PATH, false);

            if(musicPackage.MusicPackageType == MusicPackageType.NewOption)
            {
                oblFile = (OBL_File)install.GetParsedFile<OBL_File>(OBL_PATH, false);

                for (int i = 0; i < GeneralInfo.LanguageSuffix.Length; i++)
                {
                    msgFiles.Add((MSG_File)install.GetParsedFile<MSG_File>($"{OPTION_MSG_PATH}{GeneralInfo.LanguageSuffix[i]}", false));
                }
            }

            Install();
        }
        
        private void Install()
        {
            //Force all tracks to be streamed
            foreach (var waveform in musicPackage.Waveforms.Where(x => !x.Streaming))
                waveform.Streaming = true;

            if(musicPackage.MusicPackageType == MusicPackageType.NewOption)
            {
                foreach (var cue in musicPackage.Cues)
                {
                    cue.ID = (uint)GetNextCueId();
                    bgmAcb.CopyCue((int)cue.ID, musicPackage);
                    oblFile.AddEntry((int)cue.ID);

                    foreach (var msgFile in msgFiles)
                    {
                        msgFile.AddEntry($"OPT_BGM_{cue.ID.ToString("D3")}", cue.Name);
                    }

                    GeneralInfo.Tracker.AddID(OPTION_INSTALL_TYPE, Sections.ACB_Cue, cue.ID.ToString());
                }
            }
            else if (musicPackage.MusicPackageType == MusicPackageType.Direct)
            {
                //Direct mode. Just copy the cues into the BGM.
                foreach (var cue in musicPackage.Cues)
                {
                    //Remove cue with matching ID (if exists)
                    bgmAcb.Cues.RemoveAll(x => x.ID == cue.ID);
                    
                    bgmAcb.CopyCue((int)cue.ID, musicPackage);

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
            if (bgmAcb.Cues.Any(x => x.ID == (uint)cueId)) return true;
            if (oblFile.IsCueAndMessageIdUsed(cueId)) return true;

            foreach (var msgFile in msgFiles)
                if (msgFile.MSG_Entries.Any(x => x.Name == $"OPT_BGM_{cueId.ToString("D3")}")) return true;

            return false;
        }
    }
}
