using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace LB_Common.Utils
{
    public class FileAssociation
    {
        public string Extension { get; set; }
        public string ProgId { get; set; }
        public string FileTypeDescription { get; set; }
        public string ExecutableFilePath { get; set; }
    }

    public class FileAssociations
    {
        // needed so that Explorer windows get refreshed after the registry is updated
        [System.Runtime.InteropServices.DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH = 0x1000;

        #region EepkOrg
        public static void EepkOrganiser_EnsureAssociationsSetForEepk()
        {
            var filePath = Process.GetCurrentProcess().MainModule.FileName;
            EnsureAssociationsSet(
                new FileAssociation
                {
                    Extension = ".eepk",
                    ProgId = "EEPK_File",
                    FileTypeDescription = "EEPK File",
                    ExecutableFilePath = filePath
                });
        }

        public static void EepkOrganiser_EnsureAssociationsSetForVfxPackage()
        {
            var filePath = Process.GetCurrentProcess().MainModule.FileName;
            EnsureAssociationsSet(
                new FileAssociation
                {
                    Extension = ".vfxpackage",
                    ProgId = "VFXPACKAGE",
                    FileTypeDescription = "VfxPackage",
                    ExecutableFilePath = filePath
                });
        }
        #endregion

        #region ACE
        private const string AUDIO_PACKAGE_EXTENSION = ".audiopackage";

        public static void ACE_EnsureAssociationsSetForAcb()
        {
            var filePath = Process.GetCurrentProcess().MainModule.FileName;
            EnsureAssociationsSet(
                new FileAssociation
                {
                    Extension = ".acb",
                    ProgId = "ACB_File",
                    FileTypeDescription = "CRIWARE ACB File",
                    ExecutableFilePath = filePath
                });
        }

        public static void ACE_EnsureAssociationsSetForAwb()
        {
            var filePath = Process.GetCurrentProcess().MainModule.FileName;
            EnsureAssociationsSet(
                new FileAssociation
                {
                    Extension = ".awb",
                    ProgId = "AWB_File",
                    FileTypeDescription = "CRIWARE AWB File",
                    ExecutableFilePath = filePath
                });
        }

        public static void ACE_EnsureAssociationsSetForAudioPackage()
        {
            var filePath = Process.GetCurrentProcess().MainModule.FileName;
            EnsureAssociationsSet(
                new FileAssociation
                {
                    Extension = AUDIO_PACKAGE_EXTENSION,
                    ProgId = "AudioPackage",
                    FileTypeDescription = "Installable XV2 AudioPackage",
                    ExecutableFilePath = filePath
                });
        }

        #endregion

        #region TheMainStuff
        public static void EnsureAssociationsSet(params FileAssociation[] associations)
        {
            bool madeChanges = false;
            foreach (var association in associations)
            {
                madeChanges |= SetAssociation(
                    association.Extension,
                    association.ProgId,
                    association.FileTypeDescription,
                    association.ExecutableFilePath);
            }

            if (madeChanges)
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static bool SetAssociation(string extension, string progId, string fileTypeDescription, string applicationFilePath)
        {
            bool madeChanges = false;
            madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, progId);
            madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + progId, fileTypeDescription);
            madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}\shell\open\command", "\"" + applicationFilePath + "\" \"%1\"");
            return madeChanges;
        }

        private static bool SetKeyDefaultValue(string keyPath, string value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
            {
                if (key.GetValue(null) as string != value)
                {
                    key.SetValue(null, value);
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
