using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.MSG;
using Xv2CoreLib.OBL;

namespace LB_Mod_Installer.Installer.ACB
{
    public class AcbUninstaller
    {
        private Uninstall uninstall;

        private static ACB_File cpkAcbFile = null; //Load this only once
        private ACB_File acbFile;
        private OBL_File oblFile;
        private List<MSG_File> msgFiles = new List<MSG_File>();
        _File file;

        public AcbUninstaller(Uninstall _uninstall, _File _file)
        {
            file = _file;
            uninstall = _uninstall;

            if(file.filePath == AcbInstaller.OPTION_INSTALL_TYPE || file.filePath == AcbInstaller.DIRECT_INSTALL_TYPE)
            {
                //BGM
                LoadBgm();
                UninstallBgm();
            }
            else if(Utils.SanitizePath(file.filePath) == AcbInstaller.CSS_VOICE_EN_PATH || Utils.SanitizePath(file.filePath) == AcbInstaller.CSS_VOICE_JPN_PATH || file.GetSection(Sections.ACB_Voice_Cue) != null)
            {
                //Voice
                VoiceUninstall();
            }
            else
            {
                throw new Exception($"Cannot uninstall file \"{file.filePath}\". It is not recognized.\n\nOnly BGM and Voice ACB files are supported.");
            }
        }

        #region CSS
        private void VoiceUninstall()
        {
            acbFile = (ACB_File)uninstall.GetParsedFile<ACB_File>(file.filePath, false);
            ACB_File cpkAcbFile = (ACB_File)uninstall.GetParsedFile<ACB_File>(file.filePath, true, false);
            acbFile.CleanTablesOnSave = false; //Too slow on CS_ALL

            Section section = file.GetSection(Sections.ACB_Voice_Cue);

            //Backwards compat with older "CSS_Voice" types
            if (section == null)
                section = file.GetSection(Sections.ACB_Cue);

            foreach (string id in section.IDs)
            {
                uint intId;

                if (uint.TryParse(id, out intId))
                {
                    ACB_Cue cue = acbFile.GetCue((int)intId);
                    ACB_Cue originalCue = cpkAcbFile != null ? cpkAcbFile.GetCue((int)intId) : null;

                    if(cue != null)
                    {
                        if(originalCue != null)
                        {
                            //Restore OG cue
                            var waveforms = cpkAcbFile.GetWaveformsFromCue(originalCue);
                            
                            if(waveforms.Count > 0)
                            {
                                byte[] trackBytes = cpkAcbFile.GetAfs2Entry(waveforms[0].AwbId)?.bytes;
                                acbFile.ReplaceTrackOnCue(cue, trackBytes, acbFile.IsStreamingAcb(), EncodeType.HCA);
                            }
                        }
                        else
                        {
                            //Mark cue as free, in same manner as xv2ins
                            cue.Name = "X2_FREE";

                            var waveforms = acbFile.GetWaveformsFromCue(cue);

                            if (waveforms.Count == 1)
                            {
                                var awbEntry = acbFile.GetAfs2Entry(waveforms[0].AwbId);

                                if (awbEntry != null)
                                {
                                    awbEntry.bytes = Properties.Resources.silence;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region BGM
        private void LoadBgm()
        {
            acbFile = (ACB_File)uninstall.GetParsedFile<ACB_File>(AcbInstaller.BGM_PATH, false);

            if (file.filePath == AcbInstaller.OPTION_INSTALL_TYPE)
            {
                oblFile = (OBL_File)uninstall.GetParsedFile<OBL_File>(AcbInstaller.OBL_PATH, false);

                for (int i = 0; i < GeneralInfo.LanguageSuffix.Length; i++)
                {
                    msgFiles.Add((MSG_File)uninstall.GetParsedFile<MSG_File>($"{AcbInstaller.OPTION_MSG_PATH}{GeneralInfo.LanguageSuffix[i]}", false));
                }
            }
            else if (file.filePath == AcbInstaller.DIRECT_INSTALL_TYPE)
            {
                if (cpkAcbFile == null)
                    cpkAcbFile = (ACB_File)uninstall.GetParsedFile<ACB_File>(AcbInstaller.BGM_PATH, true);
            }
        }

        private void UninstallBgm()
        {
            var cueSection = file.GetSection(Sections.ACB_Cue);

            if(cueSection != null)
            {
                foreach(var id in cueSection.IDs)
                {
                    int cueId;
                    if(int.TryParse(id, out cueId))
                    {
                        acbFile.Cues.RemoveAll(x => x.ID == (uint)cueId);
                        
                        if(file.filePath == AcbInstaller.OPTION_INSTALL_TYPE)
                        {
                            oblFile.RemoveEntry(cueId);

                            foreach (var msgFile in msgFiles)
                                msgFile.MSG_Entries.RemoveAll(x => x.Name == $"OPT_BGM_{cueId.ToString("D3")}");
                        }
                        else if(file.filePath == AcbInstaller.DIRECT_INSTALL_TYPE)
                        {
                            if (cpkAcbFile.Cues.Exists(x => x.ID == (uint)cueId))
                                acbFile.CopyCue(cueId, cpkAcbFile, false);
                        }
                    }
                }
            }
        }
        #endregion

    }
}
