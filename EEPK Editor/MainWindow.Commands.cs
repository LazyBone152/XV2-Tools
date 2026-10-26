using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.Input;
using LB_Common.Forms;
using LB_Common.Mvvm;
using LB_Common.Utils;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.App;
using EEPK_Organiser.Forms;
using EEPK_Organiser.View;

namespace EEPK_Organiser
{
    public partial class MainWindow : AutoObservableWindow
    {
        #region Menu
        [RelayCommand]
        private void MenuNew()
        {
            if (EepkFile != null)
            {
                var ret = MessagePrompt.Show(string.Format("Do you want to save the currently opened file first?", EepkFile.Name), "Open", MessagePromptButtons.YesNoCancel, MessagePromptIcon.Question);

                if (ret == MessagePromptResult.Yes)
                {
                    if (EepkFile.CanSave)
                    {
                        MenuSave();
                    }
                    else
                    {
                        MenuSaveAs();
                    }
                }
                else if (ret == MessagePromptResult.Cancel)
                {
                    return;
                }
            }

            EepkFile = EffectContainerFile.New();
            UpdateSelectedVersion();
            View.EepkEditor.CloseAllEditorForms();
        }

        [RelayCommand]
        private async Task MenuOpen()
        {
            await Load();
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void MenuSave()
        {
            if (EepkFile == null) return;

            if (EepkFile.saveFormat == SaveFormat.EEPK)
            {
                EepkFile.Save();
                FileCleanUp();
            }
            else if (EepkFile.saveFormat == SaveFormat.VfxPackage)
            {
                EepkFile.SaveVfxPackage();
                FileCleanUp();
            }

            MessagePrompt.Show("Save successful!", "Save", MessagePromptButtons.OK, MessagePromptIcon.Information);
        }

        [RelayCommand(CanExecute = nameof(CanSaveAs))]
        private void MenuSaveAs()
        {
            if (EepkFile == null) return;

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Save As..",
                Filter = string.Format("EEPK File | *.eepk; |{1} File |*{0};", EffectContainerFile.ZipExtension, EffectContainerFile.ZipExtension.ToUpper().Remove(0, 1)),
                AddExtension = true
            };
            saveDialog.ShowDialog(this);


            if (!String.IsNullOrWhiteSpace(saveDialog.FileName))
            {
                if (System.IO.Path.GetExtension(saveDialog.FileName) == EffectContainerFile.ZipExtension)
                {
                    EepkFile.Directory = string.Format("{0}/{1}", System.IO.Path.GetDirectoryName(saveDialog.FileName), System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName));
                    EepkFile.saveFormat = SaveFormat.VfxPackage;
                    EepkFile.SaveVfxPackage();
                }
                else if (System.IO.Path.GetExtension(saveDialog.FileName) == ".eepk")
                {
                    EepkFile.Directory = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                    EepkFile.Name = System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName);
                    EepkFile.saveFormat = SaveFormat.EEPK;
                    EepkFile.Save();
                }
                else
                {
                    throw new InvalidOperationException(string.Format("The extension of \"{0}\" is invalid.", saveDialog.FileName));
                }

                //No call to FileCleanUp because we are moving the EEPK location, thus the original EEPK wont be affected.
                MessagePrompt.Show("Save successful!", "Save", MessagePromptButtons.OK, MessagePromptIcon.Information);
            }

            NotifyPropertyChanged(nameof(CanSave));
        }

        [RelayCommand]
        private async Task MenuSettings()
        {
            string originalGameDir = SettingsManager.Instance.Settings.GameDirectory;

            Forms.Settings settingsForm = new Forms.Settings(this);
            settingsForm.ShowDialog();
            SettingsManager.Instance.SaveSettings();
            InitTheme();

            //Reload game cpk stuff if directory was changed
            if (SettingsManager.Instance.Settings.GameDirectory != originalGameDir && SettingsManager.Instance.Settings.ValidGameDir)
            {
                await AsyncInit();
            }
        }

        [RelayCommand]
        private void MenuExit()
        {
            Environment.Exit(0);
        }

        [RelayCommand]
        private async Task MenuLoadFromGame(EntitySelector.EntityType type)
        {
            if (!EepkEditor.GameDirectoryCheck()) return;

            EffectContainerFile effectFile = await eepkEditor.LoadEepkFromGame(type, false);

            if (effectFile != null)
            {
                //Clear undo stack
                UndoManager.Instance.Clear();
                EepkEditor.CloseAllEditorForms();

                EepkFile = effectFile;
                NotifyPropertyChanged(nameof(CanSave));
                UpdateSelectedVersion();
            }
        }

        private bool CanSaveAs()
        {
            return EepkFile != null;
        }
        #endregion

        #region HelpMenu

        [RelayCommand]
        private async Task HelpMenu_CheckForUpdates()
        {
            CheckForUpdate(true);
        }

        [RelayCommand]
        private void HelpMenu_ShortcutKeys()
        {
            MessagePrompt.Show("Ctrl + C = Copy\n" +
                "Ctrl + V = Paste\n" +
                "Ctrl + X = Paste Values\n" +
                "Del = Delete\n" +
                "Ctrl + N = New\n" +
                "Ctrl + D = Duplicate\n" +
                "Ctrl + Q = Used By?\n" +
                "Ctrl + Alt + V = Paste As Child (EMP Editor)\n" +
                "Ctrl + A = Add File (EMO Tab)\n" +
                "Ctrl + H = Hue Adjustment\n" +
                "Alt + H = Hue Set\n",
                "S = Toggle Selection (Effect Selector)\n" +
                "Hotkeys", MessagePromptButtons.OK, MessagePromptIcon.Information);
        }

        [RelayCommand]
        private void HelpMenu_About()
        {
            MessagePrompt.Show(string.Format("{0} is a tool for editing Dragon Ball Xenoverse 2 EEPKs and its " +
                "associated effect files (emp, ecf, etr, emo, ema, emb, emm...).\n\n" +
                "Frameworks/Libraries used:\n" +
                "WPF (UI)\n" +
                "MahApps (UI)\n" +
                "Pfim (primary texture loading)\n" +
                "CSharpImageLibrary (texture saving and alternative texture loading)\n" +
                "YAXLib (xml)", "EEPK Organiser", SettingsManager.Instance.CurrentVersionString), "About", MessagePromptButtons.OK, MessagePromptIcon.Information);
        }

        [RelayCommand]
        private void HelpMenu_OpenGitHub()
        {
            Process.Start("https://github.com/LazyBone152/EEPKOrganiser");
        }

        [RelayCommand]
        private void ToolMenu_AssociateEepkExt()
        {
            if (MessagePrompt.Show(string.Format("This will associate the .eepk extension with EEPK Organiser and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location), "Associate Extension?", MessagePromptButtons.YesNo, MessagePromptIcon.Question) == MessagePromptResult.Yes)
            {
                FileAssociations.EepkOrganiser_EnsureAssociationsSetForEepk();
                MessagePrompt.Show(".eepk extension successfully associated!", "Associate Extension", MessagePromptButtons.OK, MessagePromptIcon.Information);
            }
        }

        [RelayCommand]
        private void ToolMenu_AssociateVfxExt()
        {
            if (MessagePrompt.Show(string.Format("This will associate the .vfxpackage extension with EEPK Organiser and make that the default application for those files.\n\nPlease note that the association will be with \"{0}\" and if the executable is moved anywhere else you will have to re-associate it.", System.Reflection.Assembly.GetEntryAssembly().Location), "Associate Extension?", MessagePromptButtons.YesNo, MessagePromptIcon.Question) == MessagePromptResult.Yes)
            {
                FileAssociations.EepkOrganiser_EnsureAssociationsSetForVfxPackage();
                MessagePrompt.Show(".vfxpackage extension successfully associated!", "Associate Extension", MessagePromptButtons.OK, MessagePromptIcon.Information);
            }
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private void ToolMenu_ExportEffects()
        {
            if (EepkFile == null) return;

            if (EepkFile.Effects.Count > 0)
            {
                eepkEditor.ExportVfxPackage(EepkFile.Effects);
            }
        }

        #endregion

        #region Import
        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private async Task ImportEffectsFromExternalEepk()
        {
            await eepkEditor.ImportEffectsFromFile();
        }

        [RelayCommand(CanExecute = nameof(IsFileLoaded))]
        private async Task ImportEffectsFromGame(EntitySelector.EntityType type)
        {
            await eepkEditor.ImportEffectsFromGame(type);
        }

        #endregion

    }
}
