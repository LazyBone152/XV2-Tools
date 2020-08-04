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
    public class MusicUninstaller
    {
        private Uninstall uninstall;

        private static ACB_File cpkAcbFile = null; //Load this only once
        private ACB_File bgmAcb;
        private OBL_File oblFile;
        private List<MSG_File> msgFiles = new List<MSG_File>();
        _File file;

        public MusicUninstaller(Uninstall _uninstall, _File _file)
        {
            file = _file;
            uninstall = _uninstall;
            bgmAcb = (ACB_File)uninstall.GetParsedFile<ACB_File>(MusicInstaller.BGM_PATH, false);

            if(_file.filePath == MusicInstaller.OPTION_INSTALL_TYPE)
            {
                oblFile = (OBL_File)uninstall.GetParsedFile<OBL_File>(MusicInstaller.OBL_PATH, false);

                for (int i = 0; i < GeneralInfo.LanguageSuffix.Length; i++)
                {
                    msgFiles.Add((MSG_File)uninstall.GetParsedFile<MSG_File>($"{MusicInstaller.OPTION_MSG_PATH}{GeneralInfo.LanguageSuffix[i]}", false));
                }
            }
            else if (_file.filePath == MusicInstaller.DIRECT_INSTALL_TYPE)
            {
                if(cpkAcbFile == null)
                    cpkAcbFile = (ACB_File)uninstall.GetParsedFile<ACB_File>(MusicInstaller.BGM_PATH, true);
            }

            Uninstall();
        }

        private void Uninstall()
        {
            var cueSection = file.GetSection(Sections.ACB_Cue);

            if(cueSection != null)
            {
                foreach(var id in cueSection.IDs)
                {
                    int cueId;
                    if(int.TryParse(id, out cueId))
                    {
                        bgmAcb.Cues.RemoveAll(x => x.ID == (uint)cueId);
                        
                        if(file.filePath == MusicInstaller.OPTION_INSTALL_TYPE)
                        {
                            oblFile.RemoveEntry(cueId);

                            foreach (var msgFile in msgFiles)
                                msgFile.MSG_Entries.RemoveAll(x => x.Name == $"OPT_BGM_{cueId.ToString("D3")}");
                        }
                        else if(file.filePath == MusicInstaller.DIRECT_INSTALL_TYPE)
                        {
                            if (cpkAcbFile.Cues.Exists(x => x.ID == (uint)cueId))
                                bgmAcb.CopyCue(cueId, cpkAcbFile);
                        }
                    }
                }
            }
        }
    }
}
